using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.aqua.grid
{
    /// <summary>
    /// Base class providing stateless/shared grid helpers and core data containers.
    /// </summary>
    public abstract class GridBase<T>
        where T : Tile, new()
    {
        // Core data containers shared by all grids
        public Vector3 Origin { get; protected set; }
        protected GridPlane GridPlane { get; private set; }
        public Vector2Int GridSize { get; protected set; }
        public float CellSize { get; protected set; }
        public float CellSpacing { get; protected set; }
        public Vector3 BottomLeftAnchor { get; protected set; }

        public Cell<T>[,] Cells { get; protected set; }

        protected GridBase(Vector3 origin, GridPlane gridPlane, Vector2Int gridSize, float cellSize, float cellSpacing)
        {
            Origin = origin;
            GridPlane = gridPlane;
            GridSize = gridSize;
            CellSize = cellSize;
            CellSpacing = cellSpacing;
            BottomLeftAnchor = GetBottomLeftAnchor(gridSize, cellSize, cellSpacing);
            Cells = new Cell<T>[gridSize.x, gridSize.y];
        }

        /**
        * Get the cell at the specified position.
        */
        public Cell<T> GetCellAt(Vector2Int gridPos)
        {
            if (IsValidPosition(gridPos))
            {
                return Cells[gridPos.x, gridPos.y];
            }
            throw new ArgumentException("Querying invalid grid position");
        }

        /// <summary>
        /// Get the world position of a cell
        /// </summary>
        public Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            float stepX = (CellSize + CellSpacing);
            float stepY = (CellSize + CellSpacing);
            if (GridPlane == GridPlane.XY)
            {
                return new Vector3(
                        gridPosition.x * stepX,
                        gridPosition.y * stepY,
                        0f
                    ) + BottomLeftAnchor;
            }
            else
            {
                return new Vector3(
                        gridPosition.x * stepX,
                        0f,
                        gridPosition.y * stepY
                    ) + BottomLeftAnchor;
            }
        }

        /// <summary>
        /// Get the center world position of a cell
        /// </summary>
        public Vector3 GetCenterWorldPosition(Vector2Int gridPosition)
        {
            float cellWidth = CellSize + CellSpacing;
            float cellHeight = CellSize + CellSpacing;
            if (GridPlane == GridPlane.XY)
            {
                return new Vector3(
                        gridPosition.x * cellWidth + CellSize / 2,
                        gridPosition.y * cellHeight + CellSize / 2,
                        0f
                    ) + BottomLeftAnchor;
            }
            else
            {
                return new Vector3(
                        gridPosition.x * cellWidth + CellSize / 2,
                        0f,
                        gridPosition.y * cellHeight + CellSize / 2
                    ) + BottomLeftAnchor;
            }
        }

        /// <summary>
        /// Convert world position to grid position
        /// </summary>
        public Vector2Int GetGridPosition(Vector3 cellWorldPosition)
        {
            Vector3 local = cellWorldPosition - BottomLeftAnchor;
            int x = Mathf.FloorToInt(local.x / (CellSize + CellSpacing));
            int y;
            if (GridPlane == GridPlane.XY)
            {
                y = Mathf.FloorToInt(local.y / (CellSize + CellSpacing));
            }
            else
            {
                y = Mathf.FloorToInt(local.z / (CellSize + CellSpacing));
            }
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Check if a position is within grid bounds
        /// </summary>
        public bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0
                && position.x < GridSize.x
                && position.y >= 0
                && position.y < GridSize.y;
        }

        /// <summary>
        /// Assert that a position is within grid bounds; throws if invalid
        /// </summary>
        protected void AssertValidPosition(Vector2Int position, string parameterName)
        {
            if (!IsValidPosition(position))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    $"Position {position} is outside grid bounds"
                );
            }
        }

        /// <summary>
        /// Check if two positions are adjacent
        /// </summary>
        public bool AreAdjacent(Vector2Int pos1, Vector2Int pos2)
        {
            int dx = Mathf.Abs(pos1.x - pos2.x);
            int dy = Mathf.Abs(pos1.y - pos2.y);

            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        /// <summary>
        /// Check if two positions are diagonal
        /// </summary>
        public bool AreDiagonal(Vector2Int pos1, Vector2Int pos2)
        {
            int dx = Mathf.Abs(pos1.x - pos2.x);
            int dy = Mathf.Abs(pos1.y - pos2.y);
            return dx == 1 && dy == 1;
        }

        protected Vector3 GetBottomLeftAnchor(
            Vector2Int gridSize,
            float cellSize,
            float cellSpacing
        )
        {
            float totalWidth = gridSize.x * (cellSize + cellSpacing) - cellSpacing;
            float totalHeight = gridSize.y * (cellSize + cellSpacing) - cellSpacing;
            if (GridPlane == GridPlane.XY)
            {
                Vector3 bottomLeftOffset = new Vector3(-totalWidth / 2f, -totalHeight / 2f, 0f);
                return Origin + bottomLeftOffset;
            }
            else
            {
                Vector3 bottomLeftOffset = new Vector3(-totalWidth / 2f, 0f, -totalHeight / 2f);
                return Origin + bottomLeftOffset;
            }
        }

        public override string ToString()
        {
            int width = Cells?.GetLength(0) ?? 0;
            int height = Cells?.GetLength(1) ?? 0;
            return $"Grid(GridSize: {GridSize}, CellSize: {CellSize}, CellSpacing: {CellSpacing}, BottomLeftAnchor: {BottomLeftAnchor}, Cells: {width}x{height})";
        }
    }
}
