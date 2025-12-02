using System;
using UnityEngine;

namespace com.aqua.grid
{
    /// <summary>
    /// Event arguments for grid initialization events
    /// </summary>
    public class GridInitializedEventArgs<T> : EventArgs
        where T : Tile, new()
    {
        /// <summary>
        /// The grid that was initialized
        /// </summary>
        public Grid<T> Grid { get; }

        public GridInitializedEventArgs(Grid<T> grid)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }
    }
}
