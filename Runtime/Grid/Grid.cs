using System;
using System.Collections.Generic;
using System.Linq;
using com.aqua.system;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.aqua.grid
{
    //<summary>
    /// The Grid is the core of anything, so it's designed to be flexible.
    ///</summary>
    public class Grid<T> : GridBase<T>, IDisposable
        where T : Tile, new()
    {
        public GridState State => _stateFlower.CurrentState;

        // Events
        public event EventHandler<GridInitializedEventArgs<T>> OnInitialized;
        public event EventHandler<GridUpdatedEventArgs<T>> OnUpdated;
        public event EventHandler<IReadOnlyList<T>> OnCleared;
        public event EventHandler<GridStateChangedEventArgs> OnStateChanged;

        // Flag to control event emission (only the main grid should emit events)
        private bool _allowEventEmission = true;

        private readonly StateFlower<GridState> _stateFlower = new(
            initialState: GridState.Initializing,
            stateFlowRuler: new GridStateFlowRule()
        );

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
            // Clear all tiles
            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    Tiles[x, y] = null;
                }
            }

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
            Tiles = new T[gridSize.x, gridSize.y];
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
                    if (Tiles[x, y] != null)
                    {
                        clearedTiles.Add(Tiles[x, y]);
                        Tiles[x, y] = null;
                    }
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

                    tile.SetGridPosition(gridPos);
                    Tiles[gridPos.x, gridPos.y] = tile;
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

            T currTile = Tiles[tile.GridPosition.x, tile.GridPosition.y];
            _ = currTile ?? throw new InvalidOperationException("Tile in cell cannot be null");
            if (currTile != tile)
                throw new InvalidOperationException(
                    "Tile in cell does not match the removing tile"
                );
            Tiles[tile.GridPosition.x, tile.GridPosition.y] = null;
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
                var pos1 = tile1.GridPosition;
                var pos2 = tile2.GridPosition;

                // Verify the tiles are actually at those positions
                if (Tiles[pos1.x, pos1.y] != tile1)
                    throw new InvalidOperationException("Tile1 is not in the expected position");
                if (Tiles[pos2.x, pos2.y] != tile2)
                    throw new InvalidOperationException("Tile2 is not in the expected position");

                affectedTiles.Add(tile1);
                affectedTiles.Add(tile2);

                // Swap the tiles
                tile1.SetGridPosition(pos2);
                tile2.SetGridPosition(pos1);
                Tiles[pos1.x, pos1.y] = tile2;
                Tiles[pos2.x, pos2.y] = tile1;

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
                    if (Tiles[x, y] != null)
                    {
                        allTiles.Add(Tiles[x, y]);
                    }
                }
            }
            return allTiles;
        }

        /**
        * Check if a position has a tile.
        */
        public bool HasTile(Vector2Int gridPos)
        {
            AssertIdleState("HasTile");
            return HasTileAt(gridPos);
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
            Grid<T> clone = new(Origin, GridPlane, GridSize, CellSize, CellSpacing)
            {
                _allowEventEmission = false,
            };

            for (int x = 0; x < Tiles.GetLength(0); x++)
            {
                for (int y = 0; y < Tiles.GetLength(1); y++)
                {
                    if (Tiles[x, y] != null)
                    {
                        var tile = Tiles[x, y];
                        tile.SetGridPosition(new Vector2Int(x, y));
                        clone.Tiles[x, y] = tile;
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

            var previousState = State;
            _stateFlower.Transition(state, () =>
            {
                RaiseStateChangedEvent(previousState, state);
                return UniTask.CompletedTask;
            });
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
