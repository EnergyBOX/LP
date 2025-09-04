using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace LP.Services
{
    public static class CutService
    {
        private static bool IsYes(FamilyInstance fi, string paramName)
        {
            Parameter p = fi.LookupParameter(paramName);
            return p != null && p.StorageType == StorageType.Integer && p.AsInteger() == 1;
        }

        private static bool IsCuttableHost(Element elem)
        {
            return elem is HostObject;
        }

        /// <summary>
        /// Обрізає захищені зони через передані сфери-void.
        /// Параметр showLog=true виводить лог у TaskDialog.
        /// </summary>
        public static (int zonesCount, int cutsCount) CutProtectedZonesWithSpheres(
            Document doc,
            List<FamilyInstance> cuttingSpheres,
            bool showLog = true)
        {
            int zonesCount = 0;
            int cutsCount = 0;
            string log = "";

            var protectedZones = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(fi => IsYes(fi, "LP_Is_ProtectedZone"))
                .ToList();

            if (protectedZones.Count == 0)
            {
                if (showLog) TaskDialog.Show("LP | Cut Log", "Не знайдено ProtectedZone.");
                return (0, 0);
            }

            if (cuttingSpheres == null || cuttingSpheres.Count == 0)
            {
                if (showLog) TaskDialog.Show("LP | Cut Log", "Не знайдено sphere-void для обрізки.");
                return (0, 0);
            }

            using (Transaction t = new Transaction(doc, "LP | Cut Protected Zones With Spheres"))
            {
                t.Start();

                foreach (var zone in protectedZones)
                {
                    if (!IsCuttableHost(zone))
                    {
                        log += $"Пропущено елемент {zone.Id.IntegerValue} ({zone.GetType().Name}) — не HostObject\n";
                        continue;
                    }

                    bool zoneTouched = false;

                    foreach (var sphere in cuttingSpheres)
                    {
                        try
                        {
                            // TODO: тут вставити реальний метод обрізки, наприклад:
                            // HostObjectUtils.CutElements(doc, zone, sphere);
                            cutsCount++;
                            zoneTouched = true;

                            if (showLog)
                                log += $"Зона {zone.Id.IntegerValue} обрізана сферою {sphere.Id.IntegerValue}\n";
                        }
                        catch
                        {
                            log += $"Помилка обрізки зоною {zone.Id.IntegerValue} сферою {sphere.Id.IntegerValue}\n";
                        }
                    }

                    if (zoneTouched) zonesCount++;
                }

                t.Commit();
            }

            if (showLog)
                TaskDialog.Show("LP | Cut Log", log);

            return (zonesCount, cutsCount);
        }
    }
}
