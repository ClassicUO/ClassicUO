#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using System.Runtime.CompilerServices;

using ClassicUO.IO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO
{
    internal unsafe class UOFile : DataReader
    {
        private protected MemoryMappedViewAccessor _accessor;
        private protected MemoryMappedFile _file;

        public UOFile(string filepath, bool loadfile = false)
        {
            FilePath = filepath;

            if (loadfile)
                Load();
        }

        public string FilePath { get; private protected set; }


        protected virtual void Load()
        {
            Log.Message(LogTypes.Trace, $"Loading file:\t\t{FilePath}");

            FileInfo fileInfo = new FileInfo(FilePath);

            if (!fileInfo.Exists)
            {
                Log.Message(LogTypes.Error, $"{FilePath}  not exists.");

                return;
            }

            long size = fileInfo.Length;

            if (size > 0)
            {
                _file = MemoryMappedFile.CreateFromFile(File.OpenRead(fileInfo.FullName), null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
                _accessor = _file.CreateViewAccessor(0, size, MemoryMappedFileAccess.Read);

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
                Log.Message(LogTypes.Error, $"{FilePath}  size must be > 0");
        }

        public virtual void FillEntries(ref UOFileIndex[] entries)
        {

        }

        public virtual void Dispose()
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor.Dispose();
            _file.Dispose();
            Log.Message(LogTypes.Trace, $"Unloaded:\t\t{FilePath}");
        }


        [MethodImpl(256)]
        internal void Fill(ref byte[] buffer, int count)
        {
            byte* ptr = (byte*) PositionAddress;
            for (int i = 0; i < count; i++)
            {
                buffer[i] = ptr[i];
            }
            //fixed (byte* ptr = buffer) Buffer.MemoryCopy((byte*) PositionAddress, ptr, count, count);

            Position += count;
        }

        [MethodImpl(256)]
        internal T[] ReadArray<T>(int count) where T : struct
        {
            T[] t = ReadArray<T>(Position, count);
            Position += UnsafeMemoryManager.SizeOf<T>() * count;

            return t;
        }

        [MethodImpl(256)]
        internal T[] ReadArray<T>(long position, int count) where T : struct
        {
            T[] array = new T[count];
            _accessor.ReadArray(position, array, 0, count);

            return array;
        }

        [MethodImpl(256)]
        internal T ReadStruct<T>(long position) where T : struct
        {
            _accessor.Read(position, out T s);

            return s;
        }

        //[MethodImpl(256)]
        //internal (int, int, bool) SeekByEntryIndex(int entryidx)
        //{
        //    if (entryidx < 0 || Entries == null || entryidx >= Entries.Length)
        //        return (0, 0, false);

        //    ref readonly UOFileIndex3D e = ref Entries[entryidx];

        //    if (e.Offset < 0) return (0, 0, false);

        //    int length = e.Length & 0x7FFFFFFF;
        //    int extra = e.Extra;

        //    if ((e.Length & (1 << 31)) != 0)
        //    {
        //        Verdata.File.Seek(e.Offset);

        //        return (length, extra, true);
        //    }

        //    if (e.Length < 0) return (0, 0, false);

        //    Seek(e.Offset);

        //    return (length, extra, false);
        //}
    }
}