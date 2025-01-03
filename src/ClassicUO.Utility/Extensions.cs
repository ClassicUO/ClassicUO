// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ClassicUO.Utility
{
    public static class Exstentions
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
