using ClassicUO.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.IO.Resources
{
    internal class LanguageLoader
    {
        public List<string> Language;
        public Dictionary<string,string> Dict;
        static string langPath;
        public LanguageLoader()
        {
            Language = new List<string>();
            Dict = new Dictionary<string, string>();
            langPath = Path.Combine(Engine.ExePath, "Data\\Language\\");
            string[] fileList = System.IO.Directory.GetFileSystemEntries(langPath);
            foreach (var item in fileList)
            {
                Language.Add(Path.GetExtension(item).Replace(".", "").ToUpper());
            }
        }
        public Task Load(string lang)
        {
            return Task.Run(() =>
            {
                string file = langPath + "Language." + lang;
                if (!File.Exists(file))
                    return;
                using (StreamReader reader = new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        string lower = line.ToLower();
                        if (line == "" || line[0] == '#' || line[0] == ';' || (line.Length >= 2 && line[0] == '/' && line[1] == '/'))
                            continue;
                        int idx = line.IndexOf('=');
                        if (idx < 0)
                            continue;
                        string key = line.Substring(0, idx).Trim();
                        string value = line.Substring(idx + 1).Trim().Replace("\\n", "\n");
                        Dict[key] = value;
                    }
                }
                FileManager.Cliloc.Load("Cliloc." + lang);
            });
        }
    }
}
