using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace com.aqua.grid
{
    /// <summary>
    /// Generic cell container that holds a tile of type T
    /// </summary>
    public class Cell<T>
        where T : Tile, new()
    {
        public Vector2Int GridPosition { get; private set; }

        // Throws error if no tile is assigned when accessed
        public T Tile { get; private set; }

        // Neighbor caching for performance
        public List<Cell<T>> AdjacentNeighbors { get; private set; }
        public List<Cell<T>> DiagonalNeighbors { get; private set; }

        /// <summary>
        /// Check if the cell has a tile
        /// </summary>
        public bool HasTile => Tile != null;

        /// <summary>
        /// Create an empty cell
        /// </summary>
        public Cell(Vector2Int gridPosition)
        {
            GridPosition = gridPosition;
            Tile = null;
            AdjacentNeighbors = new List<Cell<T>>();
            DiagonalNeighbors = new List<Cell<T>>();
        }

        /// <summary>
        /// Set a tile in this cell
        /// </summary>
        public void SetTile(T tile)
        {
            _ = tile ?? throw new ArgumentNullException(nameof(tile), "Tile cannot be null");
            tile.SetGridPosition(GridPosition);
            Tile = tile;
        }

        /// <summary>
        /// Remove the tile from this cell and return it
        /// </summary>
        public T RemoveTile()
        {
            T removedTile = Tile;
            Tile = null;
            return removedTile;
        }

        /// <summary>
        /// Clear the cell (remove tile)
        /// </summary>
        public void Clear()
        {
            Tile = null;
            AdjacentNeighbors.Clear();
            DiagonalNeighbors.Clear();
        }

        public override string ToString()
        {
            return HasTile ? $"Cell({Tile})" : "Cell(Empty)";
        }
    }
}
