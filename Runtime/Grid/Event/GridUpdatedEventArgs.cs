using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.aqua.grid
{
    public class GridUpdatedEventArgs<T> : EventArgs
        where T : Tile, new()
    {
        public GridOperation Operation { get; }
        public IReadOnlyList<T> AffectedTiles { get; }

        public GridUpdatedEventArgs(GridOperation operation, IReadOnlyList<T> affectedTiles = null)
        {
            Operation = operation;
            AffectedTiles = affectedTiles ?? Array.Empty<T>();
        }
    }
}
