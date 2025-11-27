namespace com.aqua.system
{
    /// <summary>
    /// Ruler for the state flow.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    public interface IStateFlowRuler<TState>
    {
        /// <summary>
        /// Determines the next state in the flow.
        /// </summary>
        /// <param name="fromState">The current state.</param>
        /// <param name="toState">The target state.</param>
        /// <returns>True if the transition is allowed; otherwise, false.</returns>
        bool IsTransitionAllowed(TState fromState, TState toState);
    }
}

