using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;
using System.Collections.Generic;
using System.IO;

namespace LP
{
    public static class SharedParameters
    {
        // Список усіх параметрів
        public static readonly List<(string Name, ForgeTypeId Type, ForgeTypeId Group, string Description)> Parameters =
            new List<(string, ForgeTypeId, ForgeTypeId, string)>
            {
                ("LP_Is_LightningRod", SpecTypeId.Boolean.YesNo, GroupTypeId.IdentityData, "Lightning protection marker"),
                ("LP_Is_ProtectedZone", SpecTypeId.Boolean.YesNo, GroupTypeId.IdentityData, "Zone protected by lightning rod")
            };

        /// <summary>
        /// Створює або повертає ExternalDefinition для всіх параметрів
        /// </summary>
        public static List<ExternalDefinition> GetOrCreate(Application app)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), "LP_SharedParams.txt");
            if (!File.Exists(tempFile))
                File.WriteAllText(tempFile, "");

            app.SharedParametersFilename = tempFile;
            DefinitionFile defFile = app.OpenSharedParameterFile();

            DefinitionGroup group = defFile.Groups.get_Item("LP") ?? defFile.Groups.Create("LP");

            var defs = new List<ExternalDefinition>();

            foreach (var p in Parameters)
            {
                Definition def = group.Definitions.get_Item(p.Name);
                if (def == null)
                {
                    var options = new ExternalDefinitionCreationOptions(p.Name, p.Type)
                    {
                        Visible = true,
                        Description = p.Description
                    };
                    def = group.Definitions.Create(options);
                }
                defs.Add(def as ExternalDefinition);
            }

            return defs;
        }

        /// <summary>
        /// Прив’язуємо параметри до категорій (оновлює binding, якщо вже є)
        /// </summary>
        public static void BindToCategories(Document doc, IEnumerable<ExternalDefinition> defs, CategorySet categories)
        {
            BindingMap map = doc.ParameterBindings;

            foreach (var def in defs)
            {
                var group = Parameters.Find(p => p.Name == def.Name).Group;

                if (map.Contains(def))
                {
                    // Якщо параметр вже прив’язаний – об’єднуємо категорії
                    Binding existingBinding = map.get_Item(def);
                    CategorySet existingCats = (existingBinding as InstanceBinding)?.Categories;

                    CategorySet mergedCats = doc.Application.Create.NewCategorySet();
                    foreach (Category c in existingCats) mergedCats.Insert(c);
                    foreach (Category c in categories) mergedCats.Insert(c);

                    InstanceBinding mergedBinding = doc.Application.Create.NewInstanceBinding(mergedCats);
                    map.ReInsert(def, mergedBinding, group);
                }
                else
                {
                    InstanceBinding newBinding = doc.Application.Create.NewInstanceBinding(categories);
                    map.Insert(def, newBinding, group);
                }
            }
        }
    }
}
