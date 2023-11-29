using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                preload.Remove(name);
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

    public class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Color color = new Color();
            string value = reader.GetString();
            if (!string.IsNullOrEmpty(value)) {
                string[] parts = value.Split(':');

                if (int.TryParse(parts[0], out int r))
                {
                    color.R = (byte)r;
                }
                if (int.TryParse(parts[1], out int g))
                {
                    color.G = (byte)g;
                }
                if (int.TryParse(parts[2], out int b))
                {
                    color.B = (byte)b;
                }
                if (int.TryParse(parts[3], out int a))
                {
                    color.A = (byte)a;
                }
            }

            return color;
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) => writer.WriteStringValue($"{value.R}:{value.G}:{value.B}:{value.A}");
    }
}
