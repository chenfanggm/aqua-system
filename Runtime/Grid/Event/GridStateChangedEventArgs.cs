using System;

namespace com.aqua.grid
{
    public sealed class GridStateChangedEventArgs : EventArgs
    {
        public GridState PreviousState { get; }
        public GridState CurrentState { get; }

        public GridStateChangedEventArgs(GridState previousState, GridState currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }
    }
}
