using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace LP
{
    public static class Service_SphereIntersection
    {
        /// <summary>
        /// Обчислює точку вставки (перетин трьох сфер) для трьох центрів.
        /// Радіус береться однаковий для всіх сфер (LP_Radius).
        /// </summary>
        /// <param name="c1">Центр 1</param>
        /// <param name="c2">Центр 2</param>
        /// <param name="c3">Центр 3</param>
        /// <param name="radius">Радіус сфер у футах (Revit internal units)</param>
        /// <returns>XYZ точки (0 або 1)</returns>
        public static List<XYZ> IntersectThreeSpheres(XYZ c1, XYZ c2, XYZ c3, double radius)
        {
            List<XYZ> results = new List<XYZ>();

            // Орти
            XYZ ex = (c2 - c1).Normalize();
            double i = ex.DotProduct(c3 - c1);
            XYZ ey = ((c3 - c1) - i * ex).Normalize();
            XYZ ez = ex.CrossProduct(ey);

            double d = c1.DistanceTo(c2);
            double j = ey.DotProduct(c3 - c1);

            // Координати у базисі (ex, ey, ez)
            double x = (Math.Pow(radius, 2) - Math.Pow(radius, 2) + d * d) / (2 * d);
            double y = (Math.Pow(radius, 2) - Math.Pow(radius, 2) + i * i + j * j - 2 * i * x) / (2 * j);
            double z2 = Math.Pow(radius, 2) - x * x - y * y;

            if (z2 < 0)
            {
                // Немає розв’язку
                return results;
            }

            double z = Math.Sqrt(z2);

            XYZ result1 = c1 + x * ex + y * ey + z * ez;
            XYZ result2 = c1 + x * ex + y * ey - z * ez;

            if (Math.Abs(z) < 1e-6)
            {
                // Одна точка
                results.Add(result1);
            }
            else
            {
                // Дві точки → залишаємо лише ту, що вище
                if (Math.Abs(result1.Z - result2.Z) < 1e-6)
                {
                    // Висоти однакові → помилка
                    return new List<XYZ>();
                }

                XYZ higher = result1.Z > result2.Z ? result1 : result2;
                results.Add(higher);
            }

            return results;
        }
    }
}
