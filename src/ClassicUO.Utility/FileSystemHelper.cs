#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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

        public static string RemoveInvalidChars(string text)
        {
            char[] invalid = Path.GetInvalidFileNameChars();

            for (int j = 0; j < invalid.Length; j++)
            {
                text = text.Replace(invalid[j].ToString(), "");
            }

            return text;
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