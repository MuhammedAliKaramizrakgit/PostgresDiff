using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PostgresDiff
{
    public static class ProjectDataHelper
    {
        private static readonly string SaveFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "projects.json");

        public static void SaveAllProjectDatas(List<ProjectData> allProjects)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true // JSON çıktısı okunabilir olsun
            };

            string json = JsonSerializer.Serialize(allProjects, options);
            File.WriteAllText(SaveFilePath, json);
        }

        public static List<ProjectData> LoadAllProjectDatas()
        {
            if (!File.Exists(SaveFilePath))
                return new List<ProjectData>();

            string json = File.ReadAllText(SaveFilePath);
            return JsonSerializer.Deserialize<List<ProjectData>>(json) ?? new List<ProjectData>();
        }
    }
}
