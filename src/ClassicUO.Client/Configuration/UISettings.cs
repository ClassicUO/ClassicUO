using System.IO;
using System.Text.Json;

namespace ClassicUO.Configuration
{
    internal abstract class UISettings
    {
        private static string savePath;
        private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions() { WriteIndented = true };

        public static string ReadJsonFile(string name)
        {
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "UI");
            }

            string fp = Path.Combine(savePath, name + ".json");

            if (File.Exists(fp))
            {
                try
                {
                    return File.ReadAllText(fp);
                }
                catch { }
            }

            return string.Empty;
        }

        public static UISettings Load<T>(string name)
        {
            string jsonData = ReadJsonFile(name);

            if (string.IsNullOrEmpty(jsonData))
            {
                return null;
            }

            var obj = JsonSerializer.Deserialize<T>(jsonData);

            if (obj is UISettings settings)
            {
                return settings;
            }

            return null;
        }

        public static void Save<T>(string name, object settings)
        {
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "UI");
            }

            string fileSaveData = JsonSerializer.Serialize((T)settings, serializerOptions);

            try
            {
                if (!File.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                File.WriteAllText(Path.Combine(savePath, name + ".json"), fileSaveData);
            }
            catch { }
        }
    }
}
