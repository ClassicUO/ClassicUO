#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Utility
{
    internal static class Exstentions
    {
        public static void Raise(this EventHandler handler, object sender = null)
        {
            handler?.Invoke(sender, EventArgs.Empty);
        }

        public static void Raise<T>(this EventHandler<T> handler, T e, object sender = null)
        {
            handler?.Invoke(sender, e);
        }

        public static void RaiseAsync(this EventHandler handler, object sender = null)
        {
            if (handler != null)
                Task.Run(() => handler(sender, EventArgs.Empty)).Catch();
        }

        public static void RaiseAsync<T>(this EventHandler<T> handler, T e, object sender = null)
        {
            if (handler != null)
                Task.Run(() => handler(sender, e)).Catch();
        }

        public static Task Catch(this Task task)
        {
            return task.ContinueWith(t =>
            {
                t.Exception?.Handle(e =>
                {
                    Log.Panic(e.ToString());
                    //try
                    //{
                    //    using (StreamWriter txt = new StreamWriter("crash.log", true))
                    //    {
                    //        txt.AutoFlush = true;
                    //        txt.WriteLine("Exception @ {0}", Engine.CurrDateTime.ToString("MM-dd-yy HH:mm:ss.ffff"));
                    //        txt.WriteLine(e.ToString());
                    //        txt.WriteLine("");
                    //        txt.WriteLine("");
                    //    }
                    //}
                    //catch
                    //{
                    //}

                    return true;
                });
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public static void Resize<T>(this List<T> list, int size, T element = default)
        {
            int count = list.Count;

            if (size < count)
                list.RemoveRange(size, count - size);
            else if (size > count)
            {
                if (size > list.Capacity) // Optimization
                    list.Capacity = size;
                list.AddRange(Enumerable.Repeat(element, size - count));
            }
        }

        public static void ForEach<T>(this T[] array, Action<T> func)
        {
            foreach (T c in array) func(c);
        }

        [MethodImpl(256)]
        public static bool InRect(ref Rectangle rect, ref Rectangle r)
        {
            bool inrect = false;

            if (rect.X < r.X)
            {
                if (r.X < rect.Right)
                    inrect = true;
            }
            else
            {
                if (rect.X < r.Right)
                    inrect = true;
            }

            if (inrect)
            {
                if (rect.Y < r.Y)
                    inrect = r.Y < rect.Bottom;
                else
                    inrect = rect.Y < r.Bottom;
            }

            return inrect;
        }

        public static string MakeSafe(this string s)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < s.Length; i++)
            {
                if (StringHelper.IsSafeChar(s[i]))
                    sb.Append(s[i]);
            }

            return sb.ToString();
        }

        public static string ReadUTF8String(this BinaryReader reader, int length)
        {
            byte[] data = new byte[length];
            reader.Read(data, 0, length);

            return Encoding.UTF8.GetString(data);
        }

        public static void WriteUTF8String(this BinaryWriter writer, string str)
        {
            writer.Write(Encoding.UTF8.GetBytes(str));
        }

        public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
        {
            if (!overwrite)
            {
                archive.ExtractToDirectory(destinationDirectoryName);
                return;
            }

            DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
            string destinationDirectoryFullPath = di.FullName;

            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, file.FullName));

                if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
                }

                // Assuming Empty for Directory
                if (file.Name == "")
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                    continue;
                }
                file.ExtractToFile(completeFileName, true);
            }
        }

        [MethodImpl(256)]
        public static string ToHex(this uint serial)
        {
            return $"0x{serial:X8}";
        }

        [MethodImpl(256)]
        public static string ToHex(this ushort s)
        {
            return $"0x{s:X4}";
        }

        [MethodImpl(256)]
        public static string ToHex(this byte b)
        {
            return $"0x{b:X2}";
        }
    }
}