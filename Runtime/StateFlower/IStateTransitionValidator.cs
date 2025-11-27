namespace com.aqua.system
{
    /// <summary>
    /// Validates whether a state transition is allowed.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    public interface IStateTransitionValidator<TState>
    {
        /// <summary>
        /// Determines whether a transition from one state to another is allowed.
        /// </summary>
        /// <param name="fromState">The current state.</param>
        /// <param name="toState">The target state.</param>
        /// <returns>True if the transition is allowed; otherwise, false.</returns>
        bool IsTransitionAllowed(TState fromState, TState toState);
    }
}

