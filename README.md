# Aqua Command System

[![Unity](https://img.shields.io/badge/Unity-6000.2+-black.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.md)

A robust, production-ready **Command Pattern** implementation for Unity game development, featuring asynchronous execution, entity locking, validation, and comprehensive event system.

## 📋 Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Architecture](#architecture)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [Advanced Usage](#advanced-usage)
- [API Reference](#api-reference)
- [Best Practices](#best-practices)
- [Performance Considerations](#performance-considerations)
- [Troubleshooting](#troubleshooting)

---

## Overview

The Aqua Command System is an enterprise-grade command bus implementation for Unity that decouples command execution from command creation. It follows the **Command Pattern** and **Mediator Pattern**, providing a clean, testable, and maintainable architecture for game logic.

### Use Cases

- **Turn-based games**: Player actions, AI decisions, ability execution
- **Networking**: Client commands, server validation, rollback systems
- **Save/Load systems**: Command history for undo/redo functionality
- **AI systems**: Action planning and execution
- **UI interactions**: Decoupled UI logic from game state
- **Testing**: Easily mock and test game logic without coupling

---

## Key Features

### 🚀 Core Features

- **Async/Await Support**: Built on UniTask for performant async operations
- **Entity Locking**: Prevent concurrent operations on the same entity
- **Command Validation**: Validate commands before execution
- **Cancellation Support**: Cancel individual commands or entire queue
- **Event System**: React to command lifecycle events
- **Parallel Execution**: Optional concurrent command processing
- **SOLID Principles**: Clean, maintainable, and extensible architecture
- **Zero Allocations**: Optimized for minimal GC pressure

### 🎯 Design Patterns

- **Command Pattern**: Encapsulate requests as objects
- **Mediator Pattern**: Centralized command coordination
- **Result Pattern**: Type-safe error handling without exceptions
- **Observer Pattern**: Event-driven command lifecycle notifications

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Game Layer                           │
│  (MonoBehaviours, UI, Input Handlers, Game Systems)         │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Enqueue(ICommand)
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                    ICommandBus                              │
│                  (AsyncCommandBus)                          │
│                                                             │
│  ┌──────────────┐  ┌───────────────┐  ┌──────────────┐      │
│  │    Queue     │  │ Lock Manager  │  │   Events     │      │
│  └──────────────┘  └───────────────┘  └──────────────┘      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Dispatch(command, token)
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              ICommandHandler<TCommand>                      │
│                                                             │
│  ┌──────────────────────┐  ┌─────────────────────┐          │
│  │  Validate(command)   │  │  ExecuteAsync()     │          │
│  └──────────────────────┘  └─────────────────────┘          │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Modify State
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                   Domain Layer                              │
│        (Game State, Entities, Services, Data)               │
└─────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Thread-Safe |
|-----------|---------------|-------------|
| `ICommandBus` | Queue management, coordination | ✅ Yes |
| `ICommandHandler<T>` | Business logic, validation | ⚠️ Context-dependent |
| `IEntityLockManager` | Resource locking | ✅ Yes |
| `ICommandBusEvents` | Lifecycle notifications | ⚠️ Event handlers must be safe |
| `ICommand` | Data container (immutable) | ✅ Yes (if immutable) |

---

## Installation

### Via Unity Package Manager (Git URL)

1. Open Package Manager (`Window > Package Manager`)
2. Click `+` → `Add package from git URL`
3. Enter: `[your-git-url]/com.aqua.system.git`

### Via Local Package

1. Copy `aqua-command` folder to `Assets/Packages/` or `Packages/`
2. Unity will automatically detect and import it

### Dependencies

This package requires:
- **UniTask**: `com.cysharp.unitask` (automatically resolved)
- **Unity 6000.2+** (or Unity 2021.3+ with modifications)

---

## Quick Start

### 1. Define a Command

Commands are immutable data objects representing user intent.

```csharp
using com.aqua.system;

// Simple command
public class MovePlayerCommand : ICommand
{
    public readonly int PlayerId;
    public readonly Vector3 Position;

    public MovePlayerCommand(int playerId, Vector3 position)
    {
        PlayerId = playerId;
        Position = position;
    }

    // Allow parallel execution with other commands
    public bool AllowParallelExecution => true;
}
```

### 2. Create a Command Handler

Handlers contain the business logic for executing commands.

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using com.aqua.system;

public class MovePlayerHandler : CommandHandlerBase<MovePlayerCommand>
{
    private readonly IPlayerService _playerService;

    public MovePlayerHandler(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    public override ValidationResult Validate(MovePlayerCommand command)
    {
        var player = _playerService.GetPlayer(command.PlayerId);
        if (player == null)
            return ValidationResult.Invalid("Player not found");

        if (!_playerService.CanMoveTo(command.Position))
            return ValidationResult.Invalid("Invalid position");

        return ValidationResult.Valid();
    }

    public override async UniTask<CommandResult> ExecuteAsync(
        MovePlayerCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            await _playerService.MovePlayerAsync(
                command.PlayerId,
                command.Position,
                cancellationToken
            );

            return CommandResult.Success();
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex);
        }
    }
}
```

### 3. Setup Command Bus

```csharp
using UnityEngine;
using com.aqua.system;

public class GameBootstrap : MonoBehaviour
{
    private AsyncCommandBus _commandBus;
    private IPlayerService _playerService;

    private void Awake()
    {
        // Initialize dependencies
        _playerService = new PlayerService();

        // Create command bus
        _commandBus = new AsyncCommandBus();

        // Register handlers
        _commandBus.RegisterHandler(new MovePlayerHandler(_playerService));

        // Subscribe to events (optional)
        _commandBus.Events.OnCommandCompleted += OnCommandCompleted;
        _commandBus.Events.OnCommandFailed += OnCommandFailed;
    }

    private void OnCommandCompleted(CommandEventArgs args)
    {
        Debug.Log($"✓ Command completed: {args.Command.GetType().Name}");
    }

    private void OnCommandFailed(CommandEventArgs args)
    {
        Debug.LogError($"✗ Command failed: {args.Result.ErrorMessage}");
    }

    private void OnDestroy()
    {
        _commandBus?.Dispose();
    }
}
```

### 4. Execute Commands

```csharp
public class PlayerController : MonoBehaviour
{
    [SerializeField] private AsyncCommandBus _commandBus;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var worldPos = GetMouseWorldPosition();
            var command = new MovePlayerCommand(playerId: 1, worldPos);
            _commandBus.Enqueue(command);
        }
    }
}
```

---

## Core Concepts

### Commands

Commands are **immutable data objects** representing an action to be performed.

#### Simple Command
```csharp
public class AttackCommand : ICommand
{
    public readonly int AttackerId;
    public readonly int TargetId;

    public AttackCommand(int attackerId, int targetId)
    {
        AttackerId = attackerId;
        TargetId = targetId;
    }
}
```

#### Cancellable Command
```csharp
public class LoadLevelCommand : ICancellableCommand
{
    public readonly string LevelName;
    private Action _onCancelled;

    public LoadLevelCommand(string levelName, Action onCancelled = null)
    {
        LevelName = levelName;
        _onCancelled = onCancelled;
    }

    public void OnCancelled()
    {
        Debug.Log($"Level load cancelled: {LevelName}");
        _onCancelled?.Invoke();
    }

    public bool AllowParallelExecution => false; // Sequential loading
}
```

#### Locking Command
```csharp
public class TransferItemCommand : ILockingCommand
{
    public readonly int FromInventoryId;
    public readonly int ToInventoryId;
    public readonly int ItemId;

    public TransferItemCommand(int from, int to, int item)
    {
        FromInventoryId = from;
        ToInventoryId = to;
        ItemId = item;
    }

    public object[] GetLockKeys()
    {
        // Lock both inventories to prevent concurrent modifications
        return new object[] { FromInventoryId, ToInventoryId };
    }
}
```

### Command Handlers

Handlers implement the **business logic** for commands.

#### Basic Handler
```csharp
public class AttackHandler : CommandHandlerBase<AttackCommand>
{
    public override async UniTask<CommandResult> ExecuteAsync(
        AttackCommand command,
        CancellationToken cancellationToken)
    {
        // Implement attack logic
        await ApplyDamage(command.AttackerId, command.TargetId);
        return CommandResult.Success();
    }
}
```

#### Handler with Validation
```csharp
public class PurchaseItemHandler : CommandHandlerBase<PurchaseItemCommand>
{
    private readonly IInventoryService _inventory;
    private readonly IEconomyService _economy;

    public override ValidationResult Validate(PurchaseItemCommand command)
    {
        var player = _inventory.GetPlayer(command.PlayerId);
        if (player == null)
            return ValidationResult.Invalid("Player not found");

        var item = _inventory.GetItem(command.ItemId);
        if (item == null)
            return ValidationResult.Invalid("Item not found");

        if (_economy.GetBalance(command.PlayerId) < item.Price)
            return ValidationResult.Invalid("Insufficient funds");

        if (_inventory.IsFull(command.PlayerId))
            return ValidationResult.Invalid("Inventory full");

        return ValidationResult.Valid();
    }

    public override async UniTask<CommandResult> ExecuteAsync(
        PurchaseItemCommand command,
        CancellationToken cancellationToken)
    {
        await _economy.DeductCurrency(command.PlayerId, item.Price);
        await _inventory.AddItem(command.PlayerId, command.ItemId);
        return CommandResult.Success();
    }
}
```

### Command Results

Type-safe error handling using the Result pattern.

```csharp
// Success
CommandResult.Success();

// Failure with message
CommandResult.Failure("Not enough mana", CommandFailureReason.ValidationFailed);

// Failure with exception
CommandResult.Failure(exception);

// Entity locked
CommandResult.EntityLocked("Player is busy");

// Validation failed
CommandResult.ValidationFailed("Invalid target");

// Cancelled
CommandResult.Cancelled();
```

### Events

React to command lifecycle events.

```csharp
_commandBus.Events.OnCommandEnqueued += (args) => {
    Debug.Log($"Queued: {args.Command.GetType().Name}");
};

_commandBus.Events.OnCommandExecuting += (args) => {
    ShowLoadingUI();
};

_commandBus.Events.OnCommandCompleted += (args) => {
    HideLoadingUI();
    ShowSuccessNotification();
};

_commandBus.Events.OnCommandFailed += (args) => {
    HideLoadingUI();
    ShowErrorDialog(args.Result.ErrorMessage);
};

_commandBus.Events.OnCommandCancelled += (args) => {
    ShowCancelledNotification();
};
```

---

## Advanced Usage

### Cancellation

#### Cancel Specific Command
```csharp
var command = new LongRunningCommand();
_commandBus.Enqueue(command);

// Later...
if (_commandBus.TryCancel(command))
{
    Debug.Log("Command cancelled successfully");
}
```

#### Cancel All Commands
```csharp
// Clear queue and cancel all executing commands
_commandBus.CancelAll();
```

### Parallel vs Sequential Execution

```csharp
// Parallel execution (default)
public class FastCommand : ICommand
{
    public bool AllowParallelExecution => true;
}

// Sequential execution
public class CriticalCommand : ICommand
{
    public bool AllowParallelExecution => false; // Wait for completion
}
```

### Entity Locking

Prevent concurrent operations on the same resource:

```csharp
public class TradeCommand : ILockingCommand
{
    public readonly int Player1Id;
    public readonly int Player2Id;

    public object[] GetLockKeys()
    {
        // Lock both players during trade
        return new object[] { Player1Id, Player2Id };
    }
}
```

### Monitoring Queue Status

```csharp
void Update()
{
    int queued = _commandBus.QueuedCommandCount;
    int executing = _commandBus.ExecutingCommandCount;
    
    // Show loading indicator if commands are processing
    _loadingIndicator.SetActive(queued > 0 || executing > 0);
}
```

### Dependency Injection

```csharp
// Using VContainer, Zenject, or manual DI
public void ConfigureContainer(IContainerBuilder builder)
{
    // Register command bus
    builder.Register<ICommandBus, AsyncCommandBus>(Lifetime.Singleton);

    // Register handlers
    builder.Register<ICommandHandler<MovePlayerCommand>, MovePlayerHandler>(Lifetime.Singleton);
    builder.Register<ICommandHandler<AttackCommand>, AttackHandler>(Lifetime.Singleton);
}
```

### Undo/Redo System

```csharp
public interface IUndoableCommand : ICommand
{
    ICommand GetUndoCommand();
}

public class MoveCommand : IUndoableCommand
{
    public Vector3 NewPosition { get; }
    public Vector3 OldPosition { get; }

    public ICommand GetUndoCommand()
    {
        return new MoveCommand(OldPosition, NewPosition);
    }
}

// Command history stack
private Stack<IUndoableCommand> _history = new();

public void ExecuteAndTrack(IUndoableCommand command)
{
    _commandBus.Enqueue(command);
    _history.Push(command);
}

public void Undo()
{
    if (_history.Count > 0)
    {
        var command = _history.Pop();
        _commandBus.Enqueue(command.GetUndoCommand());
    }
}
```

---

## API Reference

### ICommandBus

| Method | Description |
|--------|-------------|
| `Enqueue(ICommand)` | Add command to execution queue |
| `TryCancel(ICommand)` | Cancel specific command |
| `CancelAll()` | Clear queue and cancel all |
| `QueuedCommandCount` | Number of queued commands |
| `ExecutingCommandCount` | Number of executing commands |
| `Events` | Access event system |

### ICommand

| Property | Description | Default |
|----------|-------------|---------|
| `AllowParallelExecution` | Allow concurrent execution | `true` |

### ICommandHandler<TCommand>

| Method | Description |
|--------|-------------|
| `Validate(command)` | Validate command against rules |
| `ExecuteAsync(command, ct)` | Execute command logic |

### CommandResult

| Method | Description |
|--------|-------------|
| `Success()` | Create success result |
| `Failure(message, reason)` | Create failure result |
| `ValidationFailed(message)` | Validation failure |
| `EntityLocked(message)` | Entity locked failure |
| `Cancelled()` | Cancellation result |

---

## Best Practices

### ✅ Do

1. **Keep Commands Immutable**: Use `readonly` fields
2. **Keep Commands Simple**: Pure data, no logic
3. **Validate Before Execute**: Use `Validate()` method
4. **Use Specific Errors**: Provide helpful error messages
5. **Handle Cancellation**: Check `cancellationToken.IsCancellationRequested`
6. **Use Entity Locking**: For commands modifying shared state
7. **Subscribe to Events**: For UI feedback and logging
8. **Dispose Command Bus**: Call `Dispose()` on cleanup

### ❌ Don't

1. **Don't Put Logic in Commands**: Commands are data, handlers are logic
2. **Don't Modify Commands**: They should be immutable
3. **Don't Ignore Validation**: Always validate before execution
4. **Don't Block Main Thread**: Use `async/await` properly
5. **Don't Forget Cancellation**: Long operations should support cancellation
6. **Don't Leak Event Handlers**: Unsubscribe in `OnDestroy()`

### Command Design

```csharp
// ❌ Bad: Mutable, has logic
public class BadCommand : ICommand
{
    public int Value; // Mutable
    
    public void DoSomething() // Logic in command
    {
        Value *= 2;
    }
}

// ✅ Good: Immutable, data only
public class GoodCommand : ICommand
{
    public readonly int Value;
    
    public GoodCommand(int value)
    {
        Value = value;
    }
}
```

### Handler Design

```csharp
// ❌ Bad: No validation, no error handling
public class BadHandler : CommandHandlerBase<MyCommand>
{
    public override async UniTask<CommandResult> ExecuteAsync(
        MyCommand command, CancellationToken ct)
    {
        DoSomething(); // Can throw, no try-catch
        return CommandResult.Success();
    }
}

// ✅ Good: Validation, error handling, cancellation
public class GoodHandler : CommandHandlerBase<MyCommand>
{
    public override ValidationResult Validate(MyCommand command)
    {
        if (command.Value < 0)
            return ValidationResult.Invalid("Value must be positive");
        return ValidationResult.Valid();
    }

    public override async UniTask<CommandResult> ExecuteAsync(
        MyCommand command, CancellationToken ct)
    {
        try
        {
            await DoSomethingAsync(ct);
            return CommandResult.Success();
        }
        catch (OperationCanceledException)
        {
            return CommandResult.Cancelled();
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex);
        }
    }
}
```

---

## Performance Considerations

### Memory

- **Zero Allocation Events**: Events use structs to minimize GC
- **Object Pooling**: Consider pooling frequently created commands
- **Lock Management**: Uses `HashSet<object>` for O(1) lookups

### CPU

- **Parallel Execution**: Enable for independent commands
- **Validation Caching**: Cache expensive validation checks
- **Async Yield**: Built-in frame yielding prevents hitches

### Profiling

```csharp
// Add custom profiler markers
public override async UniTask<CommandResult> ExecuteAsync(...)
{
    using (new ProfilerMarker("MyCommand.Execute").Auto())
    {
        // Your logic
    }
}
```

---

## Troubleshooting

### Command Not Executing

1. Check if handler is registered: `_commandBus.RegisterHandler(...)`
2. Check validation: Override `Validate()` and return `Valid()`
3. Check events: Subscribe to `OnCommandFailed` to see errors

### Entity Locked Errors

- Commands using same lock keys can't run concurrently
- Use `ILockingCommand` carefully
- Check `_entityLockManager.GetLockedKeys()` for debugging

### Commands Running Too Long

- Break into smaller commands
- Use `AllowParallelExecution = false` sparingly
- Profile handlers with Unity Profiler

### Memory Leaks

- Unsubscribe from events in `OnDestroy()`
- Call `_commandBus.Dispose()` on cleanup
- Don't hold references to completed commands

---

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Follow existing code style and conventions
2. Add XML documentation to public APIs
3. Write unit tests for new features
4. Update README for new functionality

---

## License

This project is licensed under the MIT License - see [LICENSE.md](LICENSE.md) for details.

---

## Credits

**Author**: Aqua  
**Email**: aqua@aqua.com  
**Website**: https://www.aqua.com

Built with ❤️ for the Unity community.

