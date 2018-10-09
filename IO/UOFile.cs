#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;

namespace ClassicUO.IO
{
    public abstract unsafe class UOFile : DataReader
    {
        private MemoryMappedViewAccessor _accessor;

        protected UOFile(string filepath) => Path = filepath;

        public string Path { get; }
        public UOFileIndex3D[] Entries { get; protected set; }

        protected virtual void Load()
        {
            Log.Message(LogTypes.Trace, $"Loading file:\t\t{Path}");
            FileInfo fileInfo = new FileInfo(Path);
            if (!fileInfo.Exists)
                throw new FileNotFoundException(fileInfo.FullName);
            long size = fileInfo.Length;
            if (size > 0)
            {
                MemoryMappedFile file = MemoryMappedFile.CreateFromFile(fileInfo.FullName, FileMode.Open);
                _accessor = file.CreateViewAccessor(0, size, MemoryMappedFileAccess.Read);

                byte* ptr = null;
                try
                {
                    _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                    SetData(ptr, (long) _accessor.SafeMemoryMappedViewHandle.ByteLength);
                }
                catch
                {
                    _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                    throw new Exception("Something goes wrong...");
                }
            }
            else
                throw new Exception($"{Path} size must has > 0");
        }

        public virtual void Unload()
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            Entries = null;
            Log.Message(LogTypes.Trace, $"Unloaded:\t\t{Path}");
        }


        internal void Fill(byte[] buffer, int count)
        {
            for (int i = 0; i < count; i++) buffer[i] = ReadByte();
        }

        internal T[] ReadArray<T>(int count) where T : struct
        {
            T[] t = ReadArray<T>(Position, count);
            Position += Marshal.SizeOf<T>() * count;
            return t;
        }

        internal T[] ReadArray<T>(long position, int count) where T : struct
        {
            T[] array = new T[count];
            _accessor.ReadArray(position, array, 0, count);
            return array;
        }

        internal T ReadStruct<T>(long position) where T : struct
        {
            _accessor.Read(position, out T s);
            return s;
        }

        internal (int, int, bool) SeekByEntryIndex(int entryidx)
        {
            if (entryidx < 0 || entryidx >= Entries.Length) return (0, 0, false);

            UOFileIndex3D e = Entries[entryidx];
            if (e.Offset < 0) return (0, 0, false);

            int length = e.Length & 0x7FFFFFFF;
            int extra = e.Extra;

            if ((e.Length & (1 << 31)) != 0)
            {
                Verdata.File.Seek(e.Offset);
                return (length, extra, true);
            }

            if (e.Length < 0) return (0, 0, false);

            Seek(e.Offset);
            return (length, extra, false);
        }
    }
}