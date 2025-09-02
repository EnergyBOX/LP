using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace LP
{
    public static class SphereMath
    {
        /// <summary>
        /// Повертає 0/1/2 центри сфери, що одночасно на відстані R від трьох точок p1,p2,p3.
        /// Це еквівалент точки(ок) перетину трьох сфер з радіусом R.
        /// </summary>
        public static List<XYZ> IntersectThreeEqualSpheres(XYZ p1, XYZ p2, XYZ p3, double R)
        {
            // Базуємося на геометрії: шукаємо центр C, що |C - pi| = R.
            // Розв’язуємо в локальній системі координат (p1 як початок, e1 вздовж p2-p1, e2 у площині p1,p2,p3, e3 перпендикуляр).
            var e1v = (p2 - p1);
            double d = e1v.GetLength();
            if (d < 1e-9) return new List<XYZ>(); // p1 ~ p2

            var e1 = e1v.Normalize();
            var v2 = p3 - p1;
            double i = e1.DotProduct(v2);
            var tmp = v2 - i * e1;
            double tmpLen = tmp.GetLength();
            if (tmpLen < 1e-9) return new List<XYZ>(); // точки колінеарні

            var e2 = tmp.Normalize();
            var e3 = e1.CrossProduct(e2); // правобічна

            // В координатах (x,y,z) центрів p2 і p3:
            // p1 -> (0,0,0), p2 -> (d,0,0), p3 -> (i, j, 0) де j = |tmp|
            double j = tmpLen;

            // Система рівнянь: x^2 + y^2 + z^2 = R^2
            // (x-d)^2 + y^2 + z^2 = R^2  => x = d/2
            // (x-i)^2 + (y-j)^2 + z^2 = R^2
            // Підставляємо x = d/2 у третє:
            // (d/2 - i)^2 + (y - j)^2 + z^2 = R^2
            // А з першого: y^2 + z^2 = R^2 - x^2 = R^2 - (d^2/4)
            // Різниця дає: (y - j)^2 - y^2 = R^2 - (d/2 - i)^2 - (R^2 - d^2/4) => -2jy + j^2 = - (d/2 - i)^2 + d^2/4
            // => y = (j^2 + (d/2 - i)^2 - d^2/4) / (2j)
            double x = d * 0.5;
            double y = (j * j + (x - i) * (x - i) - (d * d) / 4.0) / (2.0 * j);

            // z^2 = R^2 - x^2 - y^2
            double z2 = R * R - x * x - y * y;
            if (z2 < -1e-9) return new List<XYZ>(); // нема розв’язку
            double z = z2 < 0 ? 0 : Math.Sqrt(z2);

            // Перетворюємо назад у глобальні координати
            XYZ c1 = p1 + x * e1 + y * e2 + z * e3;
            if (z == 0) return new List<XYZ> { c1 };

            XYZ c2 = p1 + x * e1 + y * e2 - z * e3;
            return new List<XYZ> { c1, c2 };
        }
    }
}
