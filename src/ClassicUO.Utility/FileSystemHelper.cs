// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Utility.Logging;
using System;
using System.IO;
using System.Text;

namespace ClassicUO.Utility
{
    public static class FileSystemHelper
    {
        public static string CreateFolderIfNotExists(string path, params string[] parts)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            char[] invalid = Path.GetInvalidFileNameChars();

            for (int i = 0; i < parts.Length; i++)
            {
                for (int j = 0; j < invalid.Length; j++)
                {
                    parts[i] = parts[i].Replace(invalid[j].ToString(), "");
                }
            }

            StringBuilder sb = new StringBuilder();

            foreach (string part in parts)
            {
                sb.Append(Path.Combine(path, part));

                string r = sb.ToString();

                if (!Directory.Exists(r))
                {
                    Directory.CreateDirectory(r);
                }

                path = r;
                sb.Clear();
            }

            return path;
        }

        /// <summary>
        ///     Returns the path of <paramref name="relativePath"/> inside <paramref name="directory"/>.
        ///     On case-sensitive file systems every part of the path is matched ignoring case,
        ///     so "Music/Digital/Config.txt" finds "music/digital/Config.txt".
        ///     If nothing matches, the path is returned unchanged.
        /// </summary>
        public static string GetPathIgnoreCase(string directory, string relativePath)
        {
            string path = Path.Combine(directory, relativePath);

            if (File.Exists(path) || Directory.Exists(path) || !Directory.Exists(directory))
            {
                return path;
            }

            string current = directory;

            foreach (string part in relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string next = Path.Combine(current, part);

                if (!File.Exists(next) && !Directory.Exists(next))
                {
                    if (!Directory.Exists(current))
                    {
                        return path;
                    }

                    string match = null;
                    int matches = 0;

                    foreach (string entry in Directory.EnumerateFileSystemEntries(current))
                    {
                        if (string.Equals(Path.GetFileName(entry), part, StringComparison.OrdinalIgnoreCase))
                        {
                            match ??= entry;
                            matches++;
                        }
                    }

                    if (match == null)
                    {
                        return path;
                    }

                    if (matches > 1)
                    {
                        Log.Warn($"Multiple files with ambiguous case found for {part}, using {Path.GetFileName(match)}. Check your data directory for duplicate files.");
                    }

                    next = match;
                }

                current = next;
            }

            return current;
        }

        public static void EnsureFileExists(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }
        }


        public static void CopyAllTo(this DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);

                diSourceSubDir.CopyAllTo(nextTargetSubDir);
            }
        }
    }
}