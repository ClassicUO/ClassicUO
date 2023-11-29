using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ClassicUO.Configuration
{
    internal abstract class UISettings
    {
        private static string savePath { get { return Path.Combine(CUOEnviroment.ExecutablePath, "Data", "UI"); } }
        private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions() { WriteIndented = true };
        private static Dictionary<string, string> preload = new Dictionary<string, string>();

        public static string ReadJsonFile(string name)
        {
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
            string jsonData;

            if (preload.TryGetValue(name, out var value))
            {
                jsonData = value;
            }
            else
            {
                jsonData = ReadJsonFile(name);
            }

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

        public static void Preload()
        {
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                if (Directory.Exists(savePath))
                {
                    string[] allFiles = Directory.GetFiles(savePath, "*.json");
                    foreach (string file in allFiles)
                    {
                        try
                        {
                            preload.Add(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file));
                        }
                        catch { }
                    }
                }
                else
                {
                    Directory.CreateDirectory(savePath);
                }
            });
        }
    }
}
