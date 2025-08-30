using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;
using System.Collections.Generic;
using System.IO;

namespace LP
{
    public static class SharedParameters
    {
        // Список параметрів
        public static readonly List<(string Name, ForgeTypeId Type, ForgeTypeId Group, string Description)> Parameters =
            new List<(string, ForgeTypeId, ForgeTypeId, string)>
            {
                ("LP_Is_LightningRod", SpecTypeId.Boolean.YesNo, GroupTypeId.IdentityData, "Lightning protection marker"),
                ("LP_Is_ProtectedZone", SpecTypeId.Boolean.YesNo, GroupTypeId.IdentityData, "Zone protected by lightning rod")
            };

        /// <summary>
        /// Створює або повертає ExternalDefinition для кожного параметра
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
        /// Прив'язуємо всі параметри до потрібних категорій
        /// </summary>
        public static void BindToCategories(Document doc, IEnumerable<ExternalDefinition> defs, CategorySet categories)
        {
            BindingMap map = doc.ParameterBindings;
            InstanceBinding binding = doc.Application.Create.NewInstanceBinding(categories);

            foreach (var def in defs)
            {
                if (!map.Contains(def))
                {
                    var group = Parameters.Find(p => p.Name == def.Name).Group;
                    map.Insert(def, binding, group);
                }
            }
        }
    }
}
