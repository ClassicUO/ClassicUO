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
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ClassicUO.IO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO
{
    internal unsafe class UOFile : DataReader
    {
        private MemoryMappedViewAccessor _accessor;
        private MemoryMappedFile _file;

        public UOFile(string filepath)
        {
            FilePath = filepath;
        }

        public string FilePath { get; private set; }

        internal uint UltimaLiveReloader()
        {
            string oldfile = FilePath;
            FilePath = Path.Combine(UltimaLive.ShardName, Path.GetFileName(FilePath));
            if (!Directory.Exists(UltimaLive.ShardName))
                return 0;
            if (!File.Exists(FilePath) || new FileInfo(FilePath).Length == 0)
            {
                Log.Message(LogTypes.Trace, $"UltimaLive -> copying file:\t{FilePath} from {oldfile}");
                File.Copy(oldfile, FilePath, true);
            }
            FileInfo fileInfo = new FileInfo(FilePath);
            if (!fileInfo.Exists)
                return 0;
            uint size = (uint)fileInfo.Length;
            Log.Message(LogTypes.Trace, $"UltimaLive -> ReLoading file:\t{FilePath}");
            if (size > 0)
            {
                Resize(size);
            }
            else
                return 0;
            return size;
        }

        internal void Resize(uint newsize)
        {
            var newmmf = MemoryMappedFile.CreateFromFile(File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite), null, newsize, MemoryMappedFileAccess.ReadWrite, null, HandleInheritability.None, false);
            var newam = newmmf.CreateViewAccessor(0, newsize, MemoryMappedFileAccess.ReadWrite);
            byte* ptr = null;

            try
            {
                newam.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                SetData(ptr, (long)newam.SafeMemoryMappedViewHandle.ByteLength);
            }
            catch
            {
                newmmf.Dispose();
                newam.SafeMemoryMappedViewHandle.ReleasePointer();
                newam.Dispose();
                UltimaLive.IsUltimaLiveActive = false;
                return;
            }
            _file?.Dispose();
            _file = newmmf;
            _accessor?.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor?.Dispose();
            _accessor = newam;
        }

        public UOFileIndex3D[] Entries { get; protected set; }

        protected virtual void Load(bool loadentries = true)
        {
            Log.Message(LogTypes.Trace, $"Loading file:\t\t{FilePath}");
            FileInfo fileInfo = new FileInfo(FilePath);

            if (!fileInfo.Exists)
                throw new FileNotFoundException(fileInfo.FullName);
            long size = fileInfo.Length;

            if (size > 0)
            {
                _file = MemoryMappedFile.CreateFromFile(File.OpenRead(fileInfo.FullName), null, 0, MemoryMappedFileAccess.Read, null, HandleInheritability.None, false);
                //_file = MemoryMappedFile.CreateFromFile(fileInfo.FullName, FileMode.Open);
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
                throw new Exception($"{FilePath} size must be > 0");
        }

        public virtual void Dispose()
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor.Dispose();
            _file.Dispose();
            UnloadEntries();
            Log.Message(LogTypes.Trace, $"Unloaded:\t\t{FilePath}");
        }

        public void UnloadEntries()
        {
            if (Entries != null)
            {
                Entries = null;
            }
        }

        internal void Fill(ref byte[] buffer, int count)
        {
            fixed (byte* ptr = buffer)
            {
                Buffer.MemoryCopy((byte*)PositionAddress, ptr, count, count);
            }

            Position += count;
        }

        internal T[] ReadArray<T>(int count) where T : struct
        {
            T[] t = ReadArray<T>(Position, count);
            Position += UnsafeMemoryManager.SizeOf<T>() * count;

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
            if (entryidx < 0 || entryidx >= Entries.Length)
                return (0, 0, false);
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

        internal void WriteArray(long position, byte[] array)
        {
            if (!_accessor.CanWrite)
                return;
            _accessor.WriteArray(position, array, 0, array.Length);
            _accessor.Flush();
        }
    }
}