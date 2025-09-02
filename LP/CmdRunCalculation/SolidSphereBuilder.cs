using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace LP
{
    public static class SolidSphereBuilder
    {
        /// <summary>
        /// Створює приблизний Solid-сферу шляхом обертання напівкола (арки) навколо осі Z (Revolve).
        /// Працює у проектному середовищі через GeometryCreationUtilities.
        /// </summary>
        public static Solid CreateSphereSolidApprox(XYZ center, double radius, int tessellation)
        {
            tessellation = Math.Max(8, tessellation);
            double dTheta = Math.PI / tessellation;

            // Профіль: полілайн верхньої півокружності в XZ
            List<XYZ> profile = new List<XYZ>();
            for (int i = 0; i <= tessellation; i++)
            {
                double theta = i * dTheta; // 0..pi
                double x = radius * Math.Sin(theta);
                double z = radius * Math.Cos(theta);
                profile.Add(new XYZ(center.X + x, center.Y, center.Z + z));
            }

            // Створюємо CurveLoop з ліній між точками профілю
            CurveLoop loop = new CurveLoop();
            for (int i = 0; i < profile.Count - 1; i++)
            {
                loop.Append(Line.CreateBound(profile[i], profile[i + 1]));
            }

            // Frame для обертання: центр + осі координат (Z — вісь обертання)
            Frame frame = new Frame(center, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ);

            // Параметри Solid
            SolidOptions options = new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId);

            try
            {
                return GeometryCreationUtilities.CreateRevolvedGeometry(
                    frame,
                    new List<CurveLoop> { loop },
                    0,                   // початковий кут (радіани)
                    2 * Math.PI,         // кінцевий кут (радіани)
                    options);
            }
            catch
            {
                return null;
            }
        }
    }
}
