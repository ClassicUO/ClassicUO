using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Configuration
{
    internal static class ProfileManager
    {
        private static readonly string _path = Path.Combine(Bootstrap.ExeDirectory, "Data");


        public static Profile Current { get; set; }


        public static void Load(string name)
        {
            string ext = Path.GetExtension(name);

            if (string.IsNullOrEmpty(ext))
                name = name + ".json";

            if (File.Exists(name))
            {
                Current = ConfigurationResolver.Load<Profile>(name);
            }

        }

        public static void Save()
        {
            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);

            ConfigurationResolver.Save(Current, Current.Path);
        }
    }
}
