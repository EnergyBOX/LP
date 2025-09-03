using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace LP
{
    public class SpatialHash2D
    {
        private readonly double cellSize;
        private readonly Dictionary<(int, int), List<(int index, XYZ point)>> grid = new();

        public SpatialHash2D(double cell)
        {
            cellSize = cell;
        }

        private (int, int) Hash(XYZ point)
        {
            return (
                (int)Math.Floor(point.X / cellSize),
                (int)Math.Floor(point.Y / cellSize)
            );
        }

        public void Insert(int index, XYZ point)
        {
            var key = Hash(point);
            if (!grid.ContainsKey(key))
                grid[key] = new List<(int, XYZ)>();
            grid[key].Add((index, point));
        }

        /// <summary>
        /// Повертає індекси точок, що знаходяться в межах радіуса (2D-відстань).
        /// </summary>
        public List<int> Query(XYZ point, double radius)
        {
            var result = new List<int>();
            int range = (int)Math.Ceiling(radius / cellSize);

            var centerKey = Hash(point);
            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = -range; dy <= range; dy++)
                {
                    var key = (centerKey.Item1 + dx, centerKey.Item2 + dy);
                    if (grid.TryGetValue(key, out var bucket))
                    {
                        foreach (var (index, pt) in bucket)
                        {
                            double dist2D = Math.Sqrt(
                                Math.Pow(pt.X - point.X, 2) +
                                Math.Pow(pt.Y - point.Y, 2)
                            );
                            if (dist2D <= radius)
                                result.Add(index);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Повертає координати точок, що знаходяться в межах радіуса (2D-відстань).
        /// </summary>
        public List<XYZ> QueryPoints(XYZ point, double radius)
        {
            var result = new List<XYZ>();
            int range = (int)Math.Ceiling(radius / cellSize);

            var centerKey = Hash(point);
            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = -range; dy <= range; dy++)
                {
                    var key = (centerKey.Item1 + dx, centerKey.Item2 + dy);
                    if (grid.TryGetValue(key, out var bucket))
                    {
                        foreach (var (_, pt) in bucket)
                        {
                            double dist2D = Math.Sqrt(
                                Math.Pow(pt.X - point.X, 2) +
                                Math.Pow(pt.Y - point.Y, 2)
                            );
                            if (dist2D <= radius)
                                result.Add(pt);
                        }
                    }
                }
            }

            return result;
        }
    }
}