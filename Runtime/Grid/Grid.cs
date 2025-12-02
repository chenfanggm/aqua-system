using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace com.aqua.grid
{
    //<summary>
    /// The Grid is the core of anything, so it's designed to be flexible.
    ///</summary>
    public class Grid<T> : GridBase<T>, IDisposable
        where T : Tile, new()
    {
        // Define allowed state transitions for runtime safety
        private static readonly Dictionary<GridState, GridState[]> ALLOWED_STATE_TRANSITIONS =
            new()
            {
                { GridState.Initializing, new[] { GridState.Idle } },
                { GridState.Idle, new[] { GridState.Updating, GridState.Initializing } },
                { GridState.Updating, new[] { GridState.Idle } }
            };

        // Instance-based components for better separation of concerns and memory management
        private static readonly NeighborCellResolver<T> NEIGHBOR_CELL_RESOLVER = new();

        public GridState State { get; private set; } = GridState.Initializing;

        // Events
        public event EventHandler<GridInitializedEventArgs<T>> OnInitialized;
        public event EventHandler<GridUpdatedEventArgs<T>> OnUpdated;
        public event EventHandler<IReadOnlyList<T>> OnCleared;
        public event EventHandler<GridStateChangedEventArgs> OnStateChanged;

        // Flag to control event emission (only the main grid should emit events)
        private bool _allowEventEmission = true;

        public Grid(
            Vector3 origin,
            GridPlane gridPlane,
            Vector2Int gridSize,
            float cellSize,
            float cellSpacing
        )
            : base(origin, gridPlane, gridSize, cellSize, cellSpacing)
        {
            Initialize();
        }

        #region Initialization

        public void Initialize()
        {
            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    if (Cells[x, y] == null)
                    {
                        Cells[x, y] = new Cell<T>(new Vector2Int(x, y));
                    }
                    else
                    {
                        Cells[x, y].Clear();
                    }
                }
            }

            // Initialize neighbor relationships after all cells are created
            NEIGHBOR_CELL_RESOLVER.InitializeNeighborRelationships(this);

            // Transition from Initializing to Idle after initialization is complete
            TransitionToState(GridState.Idle);

            // Raise the initialized event
            RaiseInitializedEvent();
        }

        /// <summary>
        /// Resize the same grid so we don't need update all other events subscribers to this Grid
        /// </summary>
        public void Reconfigure(Vector2Int gridSize, float cellSize, float cellSpacing)
        {
            TransitionToState(GridState.Initializing);
            GridSize = gridSize;
            CellSize = cellSize;
            CellSpacing = cellSpacing;
            BottomLeftAnchor = GetBottomLeftAnchor(gridSize, cellSize, cellSpacing);
            Cells = new Cell<T>[gridSize.x, gridSize.y];
            Initialize();
        }

        /// <summary>
        /// Dispose the grid
        /// </summary>
        public void Dispose()
        {
            OnInitialized = null;
            OnUpdated = null;
            Clear();
        }

        /**
        * Clear the grid.
        */
        public List<T> Clear()
        {
            AssertIdleState("Clear");
            TransitionToState(GridState.Updating);
            var clearedTiles = new List<T>();
            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    clearedTiles.Add(Cells[x, y].RemoveTile());
                }
            }
            TransitionToState(GridState.Idle);
            RaiseClearedEvent(clearedTiles);
            return clearedTiles;
        }

        /// <summary>
        /// Emit the initialization event for the grid
        /// </summary>
        private void RaiseInitializedEvent()
        {
            if (_allowEventEmission)
            {
                // Raise the specific initialization event
                OnInitialized?.Invoke(this, new GridInitializedEventArgs<T>(this));
            }
        }

        /// <summary>
        /// Emit the OnUpdated event for the grid
        /// </summary>
        private void RaiseUpdatedEvent(
            GridOperation operation,
            IReadOnlyList<T> affectedTiles = null
        )
        {
            if (_allowEventEmission)
            {
                OnUpdated?.Invoke(this, new GridUpdatedEventArgs<T>(operation, affectedTiles));
            }
        }

        private void RaiseClearedEvent(IReadOnlyList<T> affectedTiles)
        {
            if (_allowEventEmission)
            {
                OnCleared?.Invoke(this, affectedTiles);
            }
        }

        #endregion

        #region Write Operations

        /**
        * Set multiple tiles at their respective grid positions in a single batch operation.
        * This method emits only one update event at the end instead of one per tile.
        */
        public void SetTiles(IEnumerable<(T tile, Vector2Int gridPos)> tilesWithPositions)
        {
            AssertIdleState("SetTiles");

            _ = tilesWithPositions ?? throw new ArgumentNullException(nameof(tilesWithPositions));

            var assignments = tilesWithPositions.ToList();

            if (assignments.Count == 0)
                return;

            var affectedTiles = new List<T>();
            using (BeginUpdateTransaction(GridOperation.Insert, affectedTiles))
            {
                foreach (var (tile, gridPos) in assignments)
                {
                    AssertValidPosition(gridPos, nameof(gridPos));
                    _ =
                        tile
                        ?? throw new ArgumentNullException(nameof(tile), "Tile cannot be null");

                    Cells[gridPos.x, gridPos.y].SetTile(tile);
                    affectedTiles.Add(tile);
                }
            }
        }

        /**
        * Remove tiles from the grid.
        */
        public void RemoveTiles(IEnumerable<T> tiles)
        {
            AssertIdleState("RemoveTiles");

            _ = tiles ?? throw new ArgumentNullException(nameof(tiles));

            var tilesList = tiles.ToList();

            if (tilesList.Count == 0)
                return;

            using (BeginUpdateTransaction(GridOperation.Remove, tilesList))
            {
                foreach (var tile in tilesList)
                {
                    RemoveTileInternal(tile);
                }
            }
        }

        private void RemoveTileInternal(T tile)
        {
            _ = tile ?? throw new ArgumentNullException(nameof(tile), "Tile cannot be null");
            AssertValidPosition(tile.GridPosition, nameof(tile.GridPosition));

            T currTile = Cells[tile.GridPosition.x, tile.GridPosition.y].Tile;
            _ = currTile ?? throw new InvalidOperationException("Tile in cell cannot be null");
            if (currTile != tile)
                throw new InvalidOperationException(
                    "Tile in cell does not match the removing tile"
                );
            Cells[tile.GridPosition.x, tile.GridPosition.y].RemoveTile();
        }

        /**
        * Swap two tiles in the grid.
        */
        public List<TileMovement<T>> SwapTiles(T tile1, T tile2)
        {
            AssertIdleState("SwapTiles");

            _ = tile1 ?? throw new ArgumentNullException(nameof(tile1), "Tile1 cannot be null");
            _ = tile2 ?? throw new ArgumentNullException(nameof(tile2), "Tile2 cannot be null");

            var affectedTiles = new List<T>();
            var movements = new List<TileMovement<T>>(2);
            using (BeginUpdateTransaction(GridOperation.Swap, affectedTiles))
            {
                // Get the cells containing the tiles
                Cell<T> cell1 = GetCellAt(tile1.GridPosition);
                Cell<T> cell2 = GetCellAt(tile2.GridPosition);

                // Verify the tiles are actually in those cells
                if (cell1.Tile != tile1)
                    throw new InvalidOperationException("Tile1 is not in the expected cell");
                if (cell2.Tile != tile2)
                    throw new InvalidOperationException("Tile2 is not in the expected cell");

                affectedTiles.Add(tile1);
                affectedTiles.Add(tile2);

                // Record original positions
                var pos1 = tile1.GridPosition;
                var pos2 = tile2.GridPosition;

                // Swap the tiles
                cell1.SetTile(tile2);
                cell2.SetTile(tile1);

                // Create movements based on new positions
                movements.Add(new TileMovement<T>(tile1, pos1, tile1.GridPosition));
                movements.Add(new TileMovement<T>(tile2, pos2, tile2.GridPosition));
            }

            return movements;
        }
        #endregion

        #region Read Operations

        /**
        * Get all tiles in the grid (excluding empty cells).
        */
        public List<T> GetAllTiles()
        {
            AssertIdleState("GetAllTiles");
            List<T> allTiles = new();
            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    if (Cells[x, y].HasTile)
                    {
                        allTiles.Add(Cells[x, y].Tile);
                    }
                }
            }
            return allTiles;
        }

        /**
        * Get the tile at the specified position.
        */
        public T GetTileAt(Vector2Int gridPos)
        {
            AssertIdleState("GetTileAt");
            Cell<T> cell = GetCellAt(gridPos);
            return cell?.Tile;
        }

        /**
        * Check if a position has a tile.
        */
        public bool HasTile(Vector2Int gridPos)
        {
            AssertIdleState("HasTile");
            Cell<T> cell = GetCellAt(gridPos);
            return cell != null && cell.HasTile;
        }

        /**
        * Get all tiles in a specific column from bottom to top.
        */
        public List<T> GetColumnTiles(int columnIndex)
        {
            AssertIdleState("GetColumn");
            var column = new List<T>();
            for (int y = 0; y < GridSize.y; y++)
            {
                var tile = GetTileAt(new Vector2Int(columnIndex, y));
                column.Add(tile);
            }
            return column;
        }

        /**
        * Get all tiles in a specific row from left to right.
        */
        public List<T> GetRowTiles(int rowIndex)
        {
            AssertIdleState("GetRow");
            var row = new List<T>();
            for (int x = 0; x < GridSize.x; x++)
            {
                var tile = GetTileAt(new Vector2Int(x, rowIndex));
                row.Add(tile);
            }
            return row;
        }

        #endregion

        #region Helper
        /// <summary>
        /// Create a deep clone of this grid for simulation or read-only inspection purposes.
        /// The returned grid should be treated as immutable snapshot data.
        /// </summary>
        public Grid<T> Clone()
        {
            AssertIdleState("Clone");
            // Create simulation grid with events disabled
            Grid<T> clone =
                new(Origin, GridPlane, GridSize, CellSize, CellSpacing)
                {
                    _allowEventEmission = false
                };

            for (int x = 0; x < Cells.GetLength(0); x++)
            {
                for (int y = 0; y < Cells.GetLength(1); y++)
                {
                    if (Cells[x, y].HasTile)
                    {
                        clone.Cells[x, y].SetTile(Cells[x, y].Tile);
                    }
                }
            }

            return clone;
        }

        private IDisposable BeginUpdateTransaction(
            GridOperation operation,
            IReadOnlyList<T> affectedTiles = null
        )
        {
            return new GridUpdateTransaction(this, operation, affectedTiles);
        }

        /// <summary>
        /// Check if the grid is in a stable state for read operations
        /// </summary>
        private void AssertIdleState(string operationName)
        {
            // Fast path: most of the time the grid is idle
            if (State == GridState.Idle)
                return;

            // Only throw exception if actually in updating state
            if (State == GridState.Updating)
            {
                throw new InvalidOperationException(
                    $"Cannot perform {operationName} while grid is updating. "
                        + "This indicates a logic error in the main workflow. "
                        + "Ensure all update operations are properly completed before performing read operations."
                );
            }

            // Handle other states if needed
            if (State == GridState.Initializing)
            {
                throw new InvalidOperationException(
                    $"Cannot perform {operationName} while grid is initializing."
                );
            }
        }

        private void TransitionToState(GridState state)
        {
            if (State == state)
                return;

            // Check if the transition is allowed
            if (ALLOWED_STATE_TRANSITIONS.TryGetValue(State, out var allowedStates))
            {
                if (!allowedStates.Contains(state))
                {
                    throw new InvalidOperationException(
                        $"Cannot transition from {State} to {state}. "
                            + $"Allowed transitions from {State} are: {string.Join(", ", allowedStates)}."
                    );
                }
            }
            else
            {
                throw new InvalidOperationException($"No transitions defined from state {State}.");
            }

            var previousState = State;
            State = state;
            RaiseStateChangedEvent(previousState, state);
        }

        private void DisableEventEmission()
        {
            _allowEventEmission = false;
        }

        private void EnableEventEmission()
        {
            _allowEventEmission = true;
        }

        private void RaiseStateChangedEvent(GridState previousState, GridState newState)
        {
            if (!_allowEventEmission)
                return;

            if (previousState == newState)
                return;

            OnStateChanged?.Invoke(this, new GridStateChangedEventArgs(previousState, newState));
        }

        private sealed class GridUpdateTransaction : IDisposable
        {
            private readonly Grid<T> _grid;
            private bool _disposed;
            private readonly GridOperation _operation;
            private readonly IReadOnlyList<T> _affectedTiles;

            public GridUpdateTransaction(
                Grid<T> grid,
                GridOperation operation,
                IReadOnlyList<T> affectedTiles = null
            )
            {
                _grid = grid ?? throw new ArgumentNullException(nameof(grid));
                _operation = operation;
                _affectedTiles = affectedTiles;
                _grid.DisableEventEmission();
                _grid.TransitionToState(GridState.Updating);
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                _grid.EnableEventEmission();
                _grid.TransitionToState(GridState.Idle);
                IReadOnlyList<T> affectedTilesSnapshot =
                    _affectedTiles == null ? null : new List<T>(_affectedTiles);
                _grid.RaiseUpdatedEvent(_operation, affectedTilesSnapshot);
            }
        }

        #endregion
    }
}
