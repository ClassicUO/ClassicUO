﻿#region license

// Copyright (c) 2021, andreakarasho
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
            {
                Task.Run(() => handler(sender, EventArgs.Empty)).Catch();
            }
        }

        public static void RaiseAsync<T>(this EventHandler<T> handler, T e, object sender = null)
        {
            if (handler != null)
            {
                Task.Run(() => handler(sender, e)).Catch();
            }
        }

        public static Task Catch(this Task task)
        {
            return task.ContinueWith
            (
                t =>
                {
                    t.Exception?.Handle
                    (
                        e =>
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
                        }
                    );
                },
                TaskContinuationOptions.OnlyOnFaulted
            );
        }

        public static void Resize<T>(this List<T> list, int size, T element = default)
        {
            int count = list.Count;

            if (size < count)
            {
                list.RemoveRange(size, count - size);
            }
            else if (size > count)
            {
                if (size > list.Capacity) // Optimization
                {
                    list.Capacity = size;
                }

                list.AddRange(Enumerable.Repeat(element, size - count));
            }
        }

        public static void ForEach<T>(this T[] array, Action<T> func)
        {
            foreach (T c in array)
            {
                func(c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InRect(ref Rectangle rect, ref Rectangle r)
        {
            bool inrect = false;

            if (rect.X < r.X)
            {
                if (r.X < rect.Right)
                {
                    inrect = true;
                }
            }
            else
            {
                if (rect.X < r.Right)
                {
                    inrect = true;
                }
            }

            if (inrect)
            {
                if (rect.Y < r.Y)
                {
                    inrect = r.Y < rect.Bottom;
                }
                else
                {
                    inrect = rect.Y < r.Bottom;
                }
            }

            return inrect;
        }

        public static string MakeSafe(this string s)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < s.Length; i++)
            {
                if (StringHelper.IsSafeChar(s[i]))
                {
                    sb.Append(s[i]);
                }
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

#if NETFRAMEWORK
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
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToHex(this uint serial)
        {
            return $"0x{serial:X8}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToHex(this ushort s)
        {
            return $"0x{s:X4}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToHex(this byte b)
        {
            return $"0x{b:X2}";
        }
    }
}