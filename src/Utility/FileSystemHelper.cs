using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Utility
{
    static class FileSystemHelper
    {
        public static string CreateFolderIfNotExists(string path, params string[] parts)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            StringBuilder sb = new StringBuilder();

            foreach (string part in parts)
            {
                sb.Append(Path.Combine(path, part));

                string r = sb.ToString();

                if (!Directory.Exists(r))
                    Directory.CreateDirectory(r);

                path = r;
                sb.Clear();
            }

            return path;
        }

    }
}
