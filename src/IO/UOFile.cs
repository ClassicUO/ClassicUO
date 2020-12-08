using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO
{
    internal unsafe class UOFile : DataReader
    {
        public UOFile(string filepath, bool loadFile = false)
        {
            FilePath = filepath;

            if (loadFile)
            {
                Load();
            }
        }

        public string FilePath { get; }
        private protected MemoryMappedViewAccessor _accessor;
        private protected MemoryMappedFile _file;

        protected virtual void Load()
        {
            Log.Trace($"Loading file:\t\t{FilePath}");

            FileInfo fileInfo = new FileInfo(FilePath);

            if (!fileInfo.Exists)
            {
                Log.Error($"{FilePath}  not exists.");

                return;
            }

            long size = fileInfo.Length;

            if (size > 0)
            {
                _file = MemoryMappedFile.CreateFromFile
                (
                    File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), null, 0,
                    MemoryMappedFileAccess.Read, HandleInheritability.None, false
                );

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
            {
                Log.Error($"{FilePath}  size must be > 0");
            }
        }

        public virtual void FillEntries(ref UOFileIndex[] entries)
        {
        }

        public virtual void Dispose()
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor.Dispose();
            _file.Dispose();
            Log.Trace($"Unloaded:\t\t{FilePath}");
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Fill(ref byte[] buffer, int count)
        {
            byte* ptr = (byte*) PositionAddress;

            for (int i = 0; i < count; i++)
            {
                buffer[i] = ptr[i];
            }

            Position += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T[] ReadArray<T>(int count) where T : struct
        {
            T[] t = ReadArray<T>(Position, count);
            Position += UnsafeMemoryManager.SizeOf<T>() * count;

            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T[] ReadArray<T>(long position, int count) where T : struct
        {
            T[] array = new T[count];
            _accessor.ReadArray(position, array, 0, count);

            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T ReadStruct<T>(long position) where T : struct
        {
            _accessor.Read(position, out T s);

            return s;
        }
    }
}