using ClassicUO.Configuration.Json;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using static ClassicUO.Game.UI.Gumps.ModernOptionsGump;

namespace ClassicUO.Configuration
{
    [JsonSourceGenerationOptions(WriteIndented = true, GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ThemeSettings), GenerationMode = JsonSourceGenerationMode.Metadata)]
    sealed partial class UISettingsJsonContext : JsonSerializerContext
    {
        // Fix para desserialização de JSON: https://github.com/ClassicUO/ClassicUO/issues/1663
        public static UISettingsJsonContext RealUITheme { get; } = new UISettingsJsonContext(
            new JsonSerializerOptions()
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new ColorJsonConverter() } // Adiciona o conversor de cor aqui
            });
    }

    internal class UISettings
    {
        private static string SavePath => Path.Combine(CUOEnviroment.ExecutablePath, "Data", "UI");
        private static Dictionary<string, string> preload = new Dictionary<string, string>();

        public static string ReadJsonFile(string name)
        {
            string filePath = Path.Combine(SavePath, name + ".json");

            if (File.Exists(filePath))
            {
                try
                {
                    return File.ReadAllText(filePath);
                }
                catch
                {
                    // Tratar possíveis exceções
                }
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

            // Usando o contexto gerado pelo JsonSourceGenerator para desserializar
            var obj = JsonSerializer.Deserialize(jsonData, UISettingsJsonContext.RealUITheme.ThemeSettings);

            if (obj is UISettings settings)
            {
                return settings;
            }

            return null;
        }

        public static void Save<T>(string name, ThemeSettings settings)
        {
            // Serializa o objeto ThemeSettings para JSON
            var json = JsonSerializer.Serialize(settings, UISettingsJsonContext.RealUITheme.ThemeSettings);

            // Define o caminho para salvar
            var filePath = Path.Combine(SavePath, $"{name}.json");

            // Salva os dados no arquivo JSON
            File.WriteAllText(filePath, json);
        }

        public static void Preload()
        {
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                if (Directory.Exists(SavePath))
                {
                    string[] allFiles = Directory.GetFiles(SavePath, "*.json");
                    foreach (string file in allFiles)
                    {
                        try
                        {
                            preload.Add(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file));
                        }
                        catch
                        {
                            // Tratamento de exceções aqui
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory(SavePath);
                }
            });
        }
    }

    // Converter de JSON para Microsoft.Xna.Framework.Color
    public class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Color color = new Color();
            string value = reader.GetString();
            if (!string.IsNullOrEmpty(value))
            {
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

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.R}:{value.G}:{value.B}:{value.A}");
        }
    }
}