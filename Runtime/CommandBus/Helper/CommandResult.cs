using System;

namespace com.aqua.system
{
    /// <summary>
    /// Result of command execution.
    /// Follows the Result pattern for better error handling than exceptions.
    /// </summary>
    public readonly struct CommandResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
        public CommandFailureReason FailureReason { get; }
        public Exception Exception { get; }

        private CommandResult(bool isSuccess, string errorMessage, CommandFailureReason failureReason, Exception exception)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            FailureReason = failureReason;
            Exception = exception;
        }

        public override string ToString()
        {
            return $"CommandResult: IsSuccess={IsSuccess}, ErrorMessage={ErrorMessage}, FailureReason={FailureReason}, Exception={Exception}";
        }

        public static CommandResult Success() => new CommandResult(true, null, CommandFailureReason.None, null);

        public static CommandResult Failure(string message, CommandFailureReason reason = CommandFailureReason.ExecutionError)
            => new CommandResult(false, message, reason, null);

        public static CommandResult Failure(Exception exception)
            => new CommandResult(false, exception.Message, CommandFailureReason.Exception, exception);

        public static CommandResult EntityLocked(string message = "Entity is locked")
            => new CommandResult(false, message, CommandFailureReason.EntityLocked, null);

        public static CommandResult ValidationFailed(string message)
            => new CommandResult(false, message, CommandFailureReason.ValidationFailed, null);

        public static CommandResult Cancelled()
            => new CommandResult(false, "Command was cancelled", CommandFailureReason.Cancelled, null);
    }
}

