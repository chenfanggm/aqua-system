using System.Collections.Generic;
using UnityEngine;

namespace com.aqua.grid
{
    /// <summary>
    /// Static utility class for calculating grid neighbors on the fly
    /// </summary>
    public static class NeighborUtils
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
        /// Get all valid adjacent (orthogonal) neighbor positions
        /// </summary>
        public static List<Vector2Int> GetAdjacentPositions(Vector2Int position, Vector2Int gridSize)
        {
            var neighbors = new List<Vector2Int>(4);
            foreach (var direction in ADJACENT_DIRECTIONS)
            {
                var neighborPos = position + direction;
                if (IsValidPosition(neighborPos, gridSize))
                {
                    neighbors.Add(neighborPos);
                }
            }
            return neighbors;
        }

        /// <summary>
        /// Get all valid diagonal neighbor positions
        /// </summary>
        public static List<Vector2Int> GetDiagonalPositions(Vector2Int position, Vector2Int gridSize)
        {
            var neighbors = new List<Vector2Int>(4);
            foreach (var direction in DIAGONAL_DIRECTIONS)
            {
                var neighborPos = position + direction;
                if (IsValidPosition(neighborPos, gridSize))
                {
                    neighbors.Add(neighborPos);
                }
            }
            return neighbors;
        }

        /// <summary>
        /// Get all valid neighbor positions (adjacent + diagonal)
        /// </summary>
        public static List<Vector2Int> GetAllNeighborPositions(Vector2Int position, Vector2Int gridSize)
        {
            var neighbors = new List<Vector2Int>(8);
            foreach (var direction in ALL_DIRECTIONS)
            {
                var neighborPos = position + direction;
                if (IsValidPosition(neighborPos, gridSize))
                {
                    neighbors.Add(neighborPos);
                }
            }
            return neighbors;
        }

        /// <summary>
        /// Get all adjacent (orthogonal) neighbor tiles
        /// </summary>
        public static List<T> GetAdjacentTiles<T>(Vector2Int position, T[,] tiles, Vector2Int gridSize)
            where T : Tile
        {
            var neighbors = new List<T>(4);
            foreach (var direction in ADJACENT_DIRECTIONS)
            {
                var neighborPos = position + direction;
                if (IsValidPosition(neighborPos, gridSize))
                {
                    var tile = tiles[neighborPos.x, neighborPos.y];
                    if (tile != null)
                    {
                        neighbors.Add(tile);
                    }
                }
            }
            return neighbors;
        }

        /// <summary>
        /// Get all diagonal neighbor tiles
        /// </summary>
        public static List<T> GetDiagonalTiles<T>(Vector2Int position, T[,] tiles, Vector2Int gridSize)
            where T : Tile
        {
            var neighbors = new List<T>(4);
            foreach (var direction in DIAGONAL_DIRECTIONS)
            {
                var neighborPos = position + direction;
                if (IsValidPosition(neighborPos, gridSize))
                {
                    var tile = tiles[neighborPos.x, neighborPos.y];
                    if (tile != null)
                    {
                        neighbors.Add(tile);
                    }
                }
            }
            return neighbors;
        }

        /// <summary>
        /// Get all neighbor tiles (adjacent + diagonal)
        /// </summary>
        public static List<T> GetAllNeighborTiles<T>(Vector2Int position, T[,] tiles, Vector2Int gridSize)
            where T : Tile
        {
            var neighbors = new List<T>(8);
            foreach (var direction in ALL_DIRECTIONS)
            {
                var neighborPos = position + direction;
                if (IsValidPosition(neighborPos, gridSize))
                {
                    var tile = tiles[neighborPos.x, neighborPos.y];
                    if (tile != null)
                    {
                        neighbors.Add(tile);
                    }
                }
            }
            return neighbors;
        }

        /// <summary>
        /// Check if a position is within grid bounds
        /// </summary>
        public static bool IsValidPosition(Vector2Int position, Vector2Int gridSize)
        {
            return position.x >= 0
                && position.x < gridSize.x
                && position.y >= 0
                && position.y < gridSize.y;
        }
    }
}

