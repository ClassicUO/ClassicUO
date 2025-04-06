// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ClassicUO.Sdk.IO
{
    public abstract class FileReader : BinaryReader
    {
        protected FileReader(FileStream stream) : base(stream)
        {
            FilePath = stream.Name;
        }

        public string FilePath { get; }

        public long Position => BaseStream.Position;
        public long Length => BaseStream.Length;


        public void Seek(long offset, SeekOrigin origin = SeekOrigin.Begin) => BaseStream.Seek(offset, origin);

        public byte ReadUInt8() => ReadByte();
        public sbyte ReadInt8() => (sbyte)ReadByte();

        public unsafe T Read<T>() where T : unmanaged
        {
            Unsafe.SkipInit<T>(out var v);
            var p = new Span<byte>(&v, sizeof(T));
            Read(p);
            return v;
        }

        public new virtual void Dispose() => base.Dispose();
    }
}
