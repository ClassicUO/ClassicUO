using Newtonsoft.Json;
using System.IO;

namespace ClassicUO.Configuration
{
    public static class ConfigurationResolver
    {
        public static T Load<T>(string file) where T : class
        {
            if (!File.Exists(file))
            {
                Utility.Log.Message(Utility.LogTypes.Warning, file + " not found.");
                return null;
            }
            T settings = JsonConvert.DeserializeObject<T>(File.ReadAllText(file));
            return settings;
        }

        public static void Save<T>(T obj,  string file) where T : class
        {
            string t = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(file, t);
        }
    }
}