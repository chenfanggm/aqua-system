using UnityEngine;

namespace com.aqua.system
{
    public class CommandBusLogger : MonoBehaviour
    {
        private ICommandBus _commandBus;

        private void OnEnable()
        {
            _commandBus.Events.OnCommandEnqueued += OnCommandEnqueued;
            _commandBus.Events.OnCommandExecuting += OnCommandExecuting;
            _commandBus.Events.OnCommandCompleted += OnCommandCompleted;
            _commandBus.Events.OnCommandFailed += OnCommandFailed;
            _commandBus.Events.OnCommandCancelled += OnCommandCancelled;
        }

        private void OnDisable()
        {
            _commandBus.Events.OnCommandEnqueued -= OnCommandEnqueued;
            _commandBus.Events.OnCommandExecuting -= OnCommandExecuting;
            _commandBus.Events.OnCommandCompleted -= OnCommandCompleted;
            _commandBus.Events.OnCommandFailed -= OnCommandFailed;
            _commandBus.Events.OnCommandCancelled -= OnCommandCancelled;
        }

        private void OnCommandEnqueued(CommandEventArgs args)
        {
            Debug.Log($"Enqueued: {args.Command.GetType().Name}");
        }

        private void OnCommandExecuting(CommandEventArgs args)
        {
            Debug.Log($"Executing: {args.Command.GetType().Name}");
        }

        private void OnCommandCompleted(CommandEventArgs args)
        {
            Debug.Log($"Completed: {args.Command.GetType().Name}");
        }

        private void OnCommandFailed(CommandEventArgs args)
        {
            Debug.Log($"Failed: {args.Command.GetType().Name}: {args.Result}");
        }

        private void OnCommandCancelled(CommandEventArgs args)
        {
            Debug.Log($"Cancelled: {args.Command.GetType().Name}");
        }
    }
}
