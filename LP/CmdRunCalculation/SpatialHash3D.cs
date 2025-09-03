using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace LP
{
    public class SpatialHash3D
    {
        private readonly double _cellSize;
        private readonly Dictionary<(int, int, int), List<int>> _grid;

        public SpatialHash3D(double cell)
        {
            _cellSize = cell;
            _grid = new Dictionary<(int, int, int), List<int>>();
        }

        private (int, int, int) GetCell(XYZ point)
        {
            int ix = (int)Math.Floor(point.X / _cellSize);
            int iy = (int)Math.Floor(point.Y / _cellSize);
            int iz = (int)Math.Floor(point.Z / _cellSize);
            return (ix, iy, iz);
        }

        public void Insert(int index, XYZ point)
        {
            var cell = GetCell(point);
            if (!_grid.TryGetValue(cell, out var list))
            {
                list = new List<int>();
                _grid[cell] = list;
            }
            list.Add(index);
        }

        public List<int> Query(XYZ point, double searchRadius)
        {
            var result = new List<int>();
            int minX = (int)Math.Floor((point.X - searchRadius) / _cellSize);
            int maxX = (int)Math.Floor((point.X + searchRadius) / _cellSize);
            int minY = (int)Math.Floor((point.Y - searchRadius) / _cellSize);
            int maxY = (int)Math.Floor((point.Y + searchRadius) / _cellSize);
            int minZ = (int)Math.Floor((point.Z - searchRadius) / _cellSize);
            int maxZ = (int)Math.Floor((point.Z + searchRadius) / _cellSize);

            for (int ix = minX; ix <= maxX; ix++)
                for (int iy = minY; iy <= maxY; iy++)
                    for (int iz = minZ; iz <= maxZ; iz++)
                    {
                        var key = (ix, iy, iz);
                        if (_grid.TryGetValue(key, out var list))
                        {
                            result.AddRange(list);
                        }
                    }

            return result;
        }
    }
}
