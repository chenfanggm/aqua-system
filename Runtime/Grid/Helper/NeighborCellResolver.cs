using System.Collections.Generic;
using UnityEngine;

namespace com.aqua.grid
{
    /// <summary>
    /// Handles all neighbor-related calculations and caching for grid cells
    /// Extracted from Grid.cs for better separation of concerns
    /// </summary>
    public class NeighborCellResolver<T>
        where T : Tile, new()
    {
        /// <summary>
        /// Direction vectors for adjacent neighbors (orthogonal)
        /// </summary>
        public static readonly Vector2Int[] ADJACENT_DIRECTIONS = new Vector2Int[]
        {
            Vector2Int.up, // (0, 1)
            Vector2Int.down, // (0, -1)
            Vector2Int.left, // (-1, 0)
            Vector2Int.right // (1, 0)
        };

        /// <summary>
        /// Direction vectors for diagonal neighbors
        /// </summary>
        public static readonly Vector2Int[] DIAGONAL_DIRECTIONS = new Vector2Int[]
        {
            new Vector2Int(-1, -1), // Bottom-left
            new Vector2Int(1, -1), // Bottom-right
            new Vector2Int(-1, 1), // Top-left
            new Vector2Int(1, 1) // Top-right
        };

        /// <summary>
        /// All 8 direction vectors (adjacent + diagonal)
        /// </summary>
        public static readonly Vector2Int[] ALL_DIRECTIONS = new Vector2Int[]
        {
            Vector2Int.up, // (0, 1)
            Vector2Int.down, // (0, -1)
            Vector2Int.left, // (-1, 0)
            Vector2Int.right, // (1, 0)
            new Vector2Int(-1, -1), // Bottom-left
            new Vector2Int(1, -1), // Bottom-right
            new Vector2Int(-1, 1), // Top-left
            new Vector2Int(1, 1) // Top-right
        };

        /// <summary>
        /// Initialize neighbor relationships for all cells in the grid
        /// </summary>
        public void InitializeNeighborRelationships(Grid<T> grid)
        {
            if (grid == null)
                throw new System.ArgumentNullException(nameof(grid));

            for (int x = 0; x < grid.GridSize.x; x++)
            {
                for (int y = 0; y < grid.GridSize.y; y++)
                {
                    var cell = grid.GetCellAt(new Vector2Int(x, y));
                    if (cell != null)
                    {
                        // Clear existing neighbors
                        cell.AdjacentNeighbors.Clear();
                        cell.DiagonalNeighbors.Clear();

                        // Add adjacent neighbors (orthogonal)
                        foreach (var direction in ADJACENT_DIRECTIONS)
                        {
                            TryAddNeighbor(
                                grid,
                                x + direction.x,
                                y + direction.y,
                                cell.AdjacentNeighbors
                            );
                        }

                        // Add diagonal neighbors
                        foreach (var direction in DIAGONAL_DIRECTIONS)
                        {
                            TryAddNeighbor(
                                grid,
                                x + direction.x,
                                y + direction.y,
                                cell.DiagonalNeighbors
                            );
                        }
                    }
                }
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Helper method to add a neighbor if it exists within grid bounds
        /// </summary>
        private void TryAddNeighbor(Grid<T> grid, int x, int y, List<Cell<T>> neighborList)
        {
            if (grid.IsValidPosition(new Vector2Int(x, y)))
            {
                var neighbor = grid.GetCellAt(new Vector2Int(x, y));
                if (neighbor != null)
                    neighborList.Add(neighbor);
            }
        }

        #endregion
    }
}
