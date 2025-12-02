using System;
using UnityEngine;

namespace com.aqua.grid
{
    public class Tile
    {
        public Guid UUID { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public bool IsPlayerOperated { get; private set; }

        public Tile()
        {
            UUID = Guid.NewGuid();
            IsPlayerOperated = false;
        }

        public void SetPlayerOperated()
        {
            IsPlayerOperated = true;
        }

        public void ResetPlayerOperated()
        {
            IsPlayerOperated = false;
        }

        /// <summary>
        /// Set the grid position of this tile
        /// </summary>
        public void SetGridPosition(Vector2Int position)
        {
            GridPosition = position;
        }
    }
}
