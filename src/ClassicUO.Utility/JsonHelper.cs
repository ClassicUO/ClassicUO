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
    }
}
