using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.aqua.command
{
    /// <summary>
    /// Async command bus implementation with priority queue and entity locking.
    ///
    /// Key features:
    /// - Parallel execution support
    /// - Entity locking to prevent concurrent operations
    /// - Command validation
    /// - Progress reporting
    /// - Cancellation support
    /// - Event-driven feedback system
    ///
    /// SOLID Principles:
    /// - Single Responsibility: Coordinates command execution, delegates actual logic to handlers
    /// - Open/Closed: New commands/handlers can be added without modifying bus
    /// - Liskov Substitution: All ICommand implementations work seamlessly
    /// - Interface Segregation: Separate interfaces for locking, progress, validation
    /// - Dependency Inversion: Depends on abstractions (interfaces), not concrete classes
    /// </summary>
    public class AsyncCommandBus : ICommandBus, IDisposable
    {
        private readonly Queue<ICommand> _queue = new();
        private readonly CommandBusEvents _events = new();
        private readonly IEntityLockManager _locker = new EntityLockManager();
        private readonly Dictionary<
            Type,
            Func<ICommand, CancellationToken, UniTask<CommandResult>>
        > _handlers = new();
        private readonly HashSet<ICommand> _executingCommands = new();
        private readonly Dictionary<ICommand, CancellationTokenSource> _commandCancellationTokens =
            new();

        private bool _isProcessing;
        private bool _isDisposed;
        private CancellationTokenSource _processingCts;

        public int QueuedCommandCount => _queue.Count;
        public int ExecutingCommandCount
        {
            get
            {
                lock (_executingCommands)
                {
                    return _executingCommands.Count;
                }
            }
        }

        public ICommandBusEvents Events => _events;

        /// <summary>
        /// Register a command handler.
        /// Should be called during initialization/DI setup.
        /// </summary>
        public void RegisterHandler<TCommand>(ICommandHandler<TCommand> handler)
            where TCommand : ICommand
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var commandType = typeof(TCommand);

            // Wrapper to handle type conversion
            _handlers[commandType] = (command, ct) =>
                HandlerConversionAndValidationWrapper(handler, command, ct);
        }

        /// <summary>
        /// Unregister a command handler.
        /// Useful for testing or dynamic handler management.
        /// </summary>
        public void UnregisterHandler<TCommand>()
            where TCommand : ICommand
        {
            var commandType = typeof(TCommand);
            _handlers.Remove(commandType);
        }

        private UniTask<CommandResult> HandlerConversionAndValidationWrapper<TCommand>(
            ICommandHandler<TCommand> handler,
            ICommand command,
            CancellationToken ct
        )
            where TCommand : ICommand
        {
            var validationResult = handler.Validate((TCommand)command);
            if (!validationResult.IsValid)
            {
                return UniTask.FromResult(
                    CommandResult.ValidationFailed(validationResult.ErrorMessage)
                );
            }
            return handler.ExecuteAsync((TCommand)command, ct);
        }

        public void Enqueue(ICommand command)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncCommandBus));

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            _queue.Enqueue(command);
            _events.RaiseEnqueued(command);

            // Start processing if not already running
            if (!_isProcessing)
            {
                StartProcessing().Forget();
            }
        }

        public bool TryCancel(ICommand command)
        {
            if (command == null)
                return false;

            lock (_commandCancellationTokens)
            {
                if (_commandCancellationTokens.TryGetValue(command, out var cts))
                {
                    cts.Cancel();
                    _commandCancellationTokens.Remove(command);

                    if (command is ICancellableCommand cancellable)
                    {
                        cancellable.OnCancelled();
                    }

                    _events.RaiseCancelled(command);
                    return true;
                }
            }

            return false;
        }

        public void CancelAll()
        {
            _processingCts?.Cancel();
            _queue.Clear();

            lock (_commandCancellationTokens)
            {
                foreach (var kvp in _commandCancellationTokens.ToArray())
                {
                    kvp.Value.Cancel();

                    if (kvp.Key is ICancellableCommand cancellable)
                    {
                        cancellable.OnCancelled();
                    }

                    _events.RaiseCancelled(kvp.Key);
                }

                _commandCancellationTokens.Clear();
            }
        }

        private async UniTaskVoid StartProcessing()
        {
            if (_isProcessing)
                return;

            _isProcessing = true;
            _processingCts = new CancellationTokenSource();

            try
            {
                await ProcessQueueAsync(_processingCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when CancelAll is called
            }
            finally
            {
                _isProcessing = false;
                _processingCts?.Dispose();
                _processingCts = null;
            }
        }

        private async UniTask ProcessQueueAsync(CancellationToken globalCt)
        {
            var runningTasks = new List<UniTask>();

            while (_queue.Count > 0 || runningTasks.Count > 0)
            {
                // Dequeue and execute commands
                while (_queue.TryDequeue(out var command))
                {
                    if (globalCt.IsCancellationRequested)
                        break;

                    // Try to acquire locks
                    var lockKeys = GetLockKeys(command);
                    if (!_locker.TryAcquireLocks(lockKeys))
                    {
                        var result = CommandResult.EntityLocked(
                            $"Entity is locked for command {command.GetType().Name}"
                        );
                        _events.RaiseFailed(command, result);
                        continue;
                    }

                    // Execute command
                    if (command.AllowParallelExecution)
                    {
                        runningTasks.Add(ExecuteCommandAsync(command, lockKeys, globalCt));
                    }
                    else
                    {
                        // Sequential execution - wait for completion
                        await ExecuteCommandAsync(command, lockKeys, globalCt);
                    }

                    // Yield to prevent frame drops
                    await UniTask.Yield();
                }

                // Wait for any parallel tasks to complete
                if (runningTasks.Count > 0)
                {
                    await UniTask.WhenAll(runningTasks);
                    runningTasks.Clear();
                }

                // Small delay before checking queue again
                if (_queue.Count == 0)
                {
                    await UniTask.Delay(10, cancellationToken: globalCt);
                }
            }
        }

        private async UniTask ExecuteCommandAsync(
            ICommand command,
            object[] lockKeys,
            CancellationToken globalCt
        )
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(globalCt);

            lock (_commandCancellationTokens)
            {
                _commandCancellationTokens[command] = cts;
            }

            lock (_executingCommands)
            {
                _executingCommands.Add(command);
            }

            try
            {
                _events.RaiseExecuting(command);

                var result = await DispatchToHandler(command, cts.Token);

                if (result.IsSuccess)
                {
                    _events.RaiseCompleted(command, result);
                }
                else
                {
                    _events.RaiseFailed(command, result);
                }
            }
            catch (OperationCanceledException)
            {
                if (command is ICancellableCommand cancellable)
                {
                    cancellable.OnCancelled();
                }
                _events.RaiseCancelled(command);
            }
            finally
            {
                // Cleanup
                _locker.ReleaseLocks(lockKeys);

                lock (_executingCommands)
                {
                    _executingCommands.Remove(command);
                }

                lock (_commandCancellationTokens)
                {
                    _commandCancellationTokens.Remove(command);
                }

                cts?.Dispose();
            }
        }

        private async UniTask<CommandResult> DispatchToHandler(
            ICommand command,
            CancellationToken ct
        )
        {
            var commandType = command.GetType();

            if (!_handlers.TryGetValue(commandType, out var handler))
            {
                var errorMsg = $"No handler registered for command type: {commandType.Name}";
                Debug.LogError(errorMsg);
                return CommandResult.Failure(errorMsg, CommandFailureReason.ExecutionError);
            }

            return await handler(command, ct);
        }

        private object[] GetLockKeys(ICommand command)
        {
            if (command is ILockingCommand lockingCommand)
            {
                return lockingCommand.GetLockKeys();
            }

            return Array.Empty<object>();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            CancelAll();
            _events.ClearAll();
            _handlers.Clear();

            if (_locker is EntityLockManager lockMgr)
            {
                lockMgr.ClearAll();
            }
        }
    }
}
