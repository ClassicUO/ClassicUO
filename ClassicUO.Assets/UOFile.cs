using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading.Tasks;
using ClassicUO.Assets;
using System.Runtime.InteropServices;

namespace ClassicUO.Assets
{
    public unsafe abstract class UOFile
    {
        private MemoryMappedViewAccessor _accessor;

        protected byte* _ptr;
        protected long _position;
        protected long _length;

        public UOFile(string filepath)
        {
            FileName = filepath;
            Path = System.IO.Path.GetDirectoryName(FileName);
        }

        public string FileName { get; }
        public string Path { get; }
        public long Length => _length;
        public UOFileIndex3D[] Entries { get; protected set; }
        public long Position { get => _position; set => _position = value; }
        public IntPtr StartAddress => (IntPtr)_ptr;
        public IntPtr PositionAddress => (IntPtr)(_ptr + _position);

        protected virtual void Load()
        {
            FileInfo fileInfo = new FileInfo(FileName);
            if (!fileInfo.Exists)
                throw new UOFileException(FileName + " not exists.");
            long size = fileInfo.Length;
            if (size > 0)
            {
                var file = MemoryMappedFile.CreateFromFile(fileInfo.FullName, FileMode.Open);
                if (file == null)
                    throw new UOFileException("Something goes wrong with file mapping creation '" + FileName + "'");
                //var stream = file.CreateViewStream(0, size, MemoryMappedFileAccess.Read);
                //_reader = new BinaryReader(stream);
                _accessor = file.CreateViewAccessor(0, size, MemoryMappedFileAccess.Read);
               // stream.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
                _position = 0;
                _length = _accessor.Capacity;
                try
                {
                    _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);

                }
                catch
                {
                    _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                    throw new UOFileException("Something goes wrong...");
                }

            }
            else
                throw new UOFileException($"{FileName} size must has > 0");
        }

        /*internal byte ReadByte() => _reader.ReadByte();
        internal sbyte ReadSByte() => _reader.ReadSByte();
        internal short ReadShort() => _reader.ReadInt16();
        internal ushort ReadUShort() => _reader.ReadUInt16();
        internal int ReadInt() => _reader.ReadInt32();
        internal uint ReadUInt() => _reader.ReadUInt32();
        internal long ReadLong() => _reader.ReadInt64();
        internal ulong ReadULong() => _reader.ReadUInt64();
        internal byte[] ReadArray(int count)
        {
            byte[] buffer = new byte[count];
            _reader.Read(buffer, 0, count);
            return buffer;
        }

        internal void Skip(int count) => _reader.BaseStream.Seek(count, SeekOrigin.Current);
        internal long Seek(int count) => _reader.BaseStream.Seek(count, SeekOrigin.Begin);
        internal long Seek(long count) => _reader.BaseStream.Seek(count, SeekOrigin.Begin);*/

         internal byte ReadByte() => _ptr[_position++];
         internal sbyte ReadSByte() => (sbyte)ReadByte();
         internal bool ReadBool() => ReadByte() != 0;
         internal short ReadShort() => (short)(ReadByte() | (ReadByte() << 8));
         internal ushort ReadUShort() => (ushort)ReadShort();
         internal int ReadInt() => (ReadByte() | (ReadByte() << 8) | (ReadByte() << 16) | (ReadByte() << 24));
         internal uint ReadUInt() => (uint)ReadInt();
         internal long ReadLong() => (ReadByte() | ((long)ReadByte() << 8) | ((long)ReadByte() << 16) | ((long)ReadByte() << 24) | ((long)ReadByte() << 32) | ((long)ReadByte() << 40) | ((long)ReadByte() << 48) | ((long)ReadByte() << 56));
         internal ulong ReadULong() => (ulong)ReadLong();
       /*  internal byte[] ReadArray(int count)
         {
             byte[] buffer = new byte[count];

             for (int i = 0; i < count; i++)
                 buffer[i] = ReadByte();
             return buffer;
         }*/

      /*  internal byte ReadByte() => _accessor.ReadByte(_position++);
        internal sbyte ReadSByte() => _accessor.ReadSByte(_position++);
        internal bool ReadBool() => _accessor.ReadBoolean(_position++);
        internal short ReadShort() { var r = _accessor.ReadInt16(_position); _position += 2; return r; }
        internal ushort ReadUShort() { var r = _accessor.ReadUInt16(_position); _position += 2; return r; }
        internal int ReadInt() { var r = _accessor.ReadInt32(_position); _position += 4; return r; }
        internal uint ReadUInt() { var r = _accessor.ReadUInt32(_position); _position += 4; return r; }
        internal long ReadLong() { var r = _accessor.ReadInt64(_position); _position += 8; return r; }
        internal ulong ReadULong() { var r = _accessor.ReadUInt64(_position); _position += 8; return r; }
        */
        internal T[] ReadArray<T>(int count) where T : struct
        {
            T[] t = ReadArray<T>(_position, count);
            _position += count;
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

        internal void Skip(int count) => _position += count;
        internal void Seek(int count) => _position = count;
        internal void Seek(long count) => _position = (int)count;
        internal (int, int, bool) SeekByEntryIndex(int entryidx)
        {
            if (entryidx < 0 || entryidx >= Entries.Length)
            {
                return (0, 0, false);
            }

            UOFileIndex3D e = Entries[entryidx];
            if (e.Offset < 0)
            {
                return (0, 0, false);
            }

            int length = e.Length & 0x7FFFFFFF;
            int extra = e.Extra;

            if ((e.Length & (1 << 31)) != 0)
            {
                Verdata.File.Seek(e.Offset);
                return (length, extra, true);
            }

            if (e.Length < 0)
            {
                return (0, 0, false);
            }

            Seek(e.Offset);
            return (length, extra, false);
        }
    }

    public class UOFileException : Exception
    {
        public UOFileException(string text) : base(text) { }
    }
}
