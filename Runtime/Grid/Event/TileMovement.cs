using UnityEngine;

namespace com.aqua.grid
{
    /// <summary>
    /// Data structure to represent a tile movement during gravity or other operations
    /// Shared across Grid, Puzzle, and GravityEngine systems
    /// </summary>
    [System.Serializable]
    public struct TileMovement<T>
        where T : Tile, new()
    {
        public T Tile { get; private set; }
        public Vector2Int FromPosition { get; private set; }
        public Vector2Int ToPosition { get; private set; }

        public TileMovement(T tile, Vector2Int from, Vector2Int to)
        {
            Tile = tile;
            FromPosition = from;
            ToPosition = to;
        }
    }
}
