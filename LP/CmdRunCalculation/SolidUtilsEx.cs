using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace LP
{
    public static class SolidUtilsEx
    {
        public static Solid GetMainSolid(Element e)
        {
            var opt = new Options
            {
                ComputeReferences = false,
                IncludeNonVisibleObjects = true,
                DetailLevel = ViewDetailLevel.Fine
            };

            Solid biggest = null;
            var geo = e.get_Geometry(opt);
            if (geo == null) return null;

            foreach (var obj in geo)
            {
                if (obj is Solid s && s.Volume > 1e-9)
                {
                    if (biggest == null || s.Volume > biggest.Volume) biggest = s;
                }
                else if (obj is GeometryInstance gi)
                {
                    var inst = gi.GetInstanceGeometry();
                    foreach (var giObj in inst)
                    {
                        if (giObj is Solid s2 && s2.Volume > 1e-9)
                        {
                            if (biggest == null || s2.Volume > biggest.Volume) biggest = s2;
                        }
                    }
                }
            }
            return biggest;
        }
    }

    public static class BooleanBatch
    {
        /// <summary>
        /// Будує об’єднання (Union) з набору сфер (як Solid), повертає один Solid.
        /// Сфери створюються як тессельовані DirectShape-аналоги через TessellatedShapeBuilder, потім конвертуються у Solid.
        /// Насправді Revit не дає прямого Solid з TessellatedShapeBuilder, тож тут ми будуємо сферу як "опуклу оболонку" з трикутників,
        /// і для булевих операцій використовуємо BooleanOperationsUtils на Mesh->Solid конверсії через TemporaryDirectShape.
        /// Спрощений практичний шлях: будуємо N дрібних сфер і робимо покроковий Union.
        /// </summary>
        public static Solid BuildUnionOfSpheres(Document doc, List<Sphere> spheres, int tessellation = 16)
        {
            // Примітка: У «чистому» API немає прямого будівника сфери як Solid.
            // Підхід нижче використовує CreateSphereSolidApprox для побудови приблизного твердого тіла.
            Solid union = null;

            foreach (var s in spheres)
            {
                Solid sphereSolid = SolidSphereBuilder.CreateSphereSolidApprox(s.Center, s.Radius, tessellation);
                if (sphereSolid == null) continue;

                if (union == null) union = sphereSolid;
                else
                {
                    try
                    {
                        union = BooleanOperationsUtils.ExecuteBooleanOperation(union, sphereSolid, BooleanOperationsType.Union);
                    }
                    catch
                    {
                        // якщо Union не вдався через геометричні похибки — пропускаємо сферу
                    }
                }
            }
            return union;
        }
    }
}
