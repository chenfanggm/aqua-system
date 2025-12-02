using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace com.aqua.grid.tests
{
    public class GridTests
    {
        [Test]
        public void Reconfigure_ResizesGridAndReturnsToIdle()
        {
            Grid<TestTile> grid = CreateGrid(new Vector2Int(2, 2), 1f, 0.1f);
            Vector2Int newSize = new Vector2Int(4, 3);
            float newCellSize = 2f;
            float newCellSpacing = 0.25f;

            Assert.DoesNotThrow(() => grid.Reconfigure(newSize, newCellSize, newCellSpacing));

            Assert.AreEqual(newSize, grid.GridSize);
            Assert.AreEqual(newCellSize, grid.CellSize);
            Assert.AreEqual(newCellSpacing, grid.CellSpacing);
            Assert.AreEqual(GridState.Idle, grid.State);

            List<TestTile> column = grid.GetColumnTiles(0);
            Assert.AreEqual(newSize.y, column.Count);
        }

        [Test]
        public void RemoveTiles_NullTileThrowsArgumentNullException()
        {
            Grid<TestTile> grid = CreateGrid(new Vector2Int(2, 2), 1f, 0.1f);

            Assert.Throws<ArgumentNullException>(
                () => grid.RemoveTiles(new List<TestTile> { null })
            );
        }

        private static Grid<TestTile> CreateGrid(
            Vector2Int gridSize,
            float cellSize,
            float cellSpacing
        )
        {
            return new Grid<TestTile>(Vector3.zero, GridPlane.XY, gridSize, cellSize, cellSpacing);
        }

        private class TestTile : Tile { }
    }
}
