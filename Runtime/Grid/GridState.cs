using com.aqua.system;

namespace com.aqua.grid
{
    public enum GridState
    {
        Initializing,
        Idle,
        Updating,
    }

    public sealed class GridStateFlowRule : IStateFlowRule<GridState>
    {
        public bool IsTransitionAllowed(GridState fromState, GridState toState)
        {
            return fromState switch
            {
                GridState.Initializing => toState == GridState.Idle,
                GridState.Idle => toState == GridState.Updating
                    || toState == GridState.Initializing,
                GridState.Updating => toState == GridState.Idle,
                _ => false,
            };
        }
    }
}
