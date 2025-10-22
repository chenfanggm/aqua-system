namespace com.aqua.command
{
    public enum CommandFailureReason
    {
        None,
        EntityLocked,
        ValidationFailed,
        Cancelled,
        ExecutionError,
        Exception
    }
}

