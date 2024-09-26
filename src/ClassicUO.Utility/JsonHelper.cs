using System;
using System.IO;
using System.Text.Json;

namespace ClassicUO.Utility
{
    public static class JsonHelper
    {
        /// <summary>
        /// Deserialize a json file into an object.
        /// Returns false on errors or file not found. Otherwise returns true if deserialize was successfull.
        /// </summary>
        /// <typeparam name="T">Class type to deserialize into</typeparam>
        /// <param name="path">Path to the file</param>
        /// <param name="obj">Output object</param>
        /// <returns></returns>
        public static bool LoadJsonFile<T>(string path, out T obj)
        {
            if (File.Exists(path))
            {
                try
                {
                    obj = JsonSerializer.Deserialize<T>(File.ReadAllText(path));
                    return true;
                }
                catch (Exception e) { Console.WriteLine(e.ToString()); }
            }

            obj = default(T);
            return false;
        }

        /// <summary>
        /// Save an object to a json file at the specified path.
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="obj">The object to be serialized into json</param>
        /// <param name="path">The path to the save file including file name and extension</param>
        /// <param name="prettified">Should the output file be indented for readability</param>
        /// <returns></returns>
        public static bool SaveJsonFile<T>(T obj, string path, bool prettified = true)
        {
            try
            {
                string output = JsonSerializer.Serialize(obj, new JsonSerializerOptions() { WriteIndented = prettified });
                File.WriteAllText(path, output);
                return true;
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }

            return false;
        }
    }
}
