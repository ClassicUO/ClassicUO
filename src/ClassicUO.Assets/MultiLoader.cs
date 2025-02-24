// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class MultiLoader : UOFileLoader
    {
        public const int MAX_MULTI_DATA_INDEX_COUNT = 0x2200;

        public MultiLoader(UOFileManager fileManager) : base(fileManager)
        {
        }

        internal UOFile File { get; private set; }

        public override unsafe void Load()
        {
            var uopPath = FileManager.GetUOFilePath("MultiCollection.uop");

            if (FileManager.IsUOPInstallation && System.IO.File.Exists(uopPath))
            {
                File = new UOFileUop(uopPath, "build/multicollection/{0:D6}.bin");
            }
            else
            {
                var path = FileManager.GetUOFilePath("multi.mul");
                var pathidx = FileManager.GetUOFilePath("multi.idx");

                if (System.IO.File.Exists(path) && System.IO.File.Exists(pathidx))
                {
                    File = new UOFileMul(path, pathidx);
                }
            }

            File.FillEntries();
        }

        public List<MultiInfo> GetMultis(uint idx)
        {
            var list = new List<MultiInfo>();

            var file = File;
            ref var entry = ref file.GetValidRefEntry((int)idx);

            if (entry.File != null)
                file = entry.File;

            file.Seek(entry.Offset, System.IO.SeekOrigin.Begin);

            var buf = new byte[entry.Length];
            file.Read(buf);

            var reader = new StackDataReader(buf);
            if (entry.CompressionFlag >= CompressionType.Zlib)
            {
                var dbuf = new byte[entry.DecompressedLength];
                var result = ZLib.Decompress(buf, dbuf);
                reader = new StackDataReader(dbuf);

                reader.Skip(sizeof(uint));

                var count = reader.ReadInt32LE();

                for (var i = 0; i < count; ++i)
                {
                    var block = reader.Read<MultiBlockNew>();

                    if (block.Unknown != 0)
                    {
                        reader.Skip((int)(block.Unknown * sizeof(uint)));
                    }

                    list.Add(new ()
                    {
                        ID = block.ID,
                        X = block.X,
                        Y = block.Y,
                        Z = block.Z,
                        IsVisible = block.Flags == 0 || block.Flags == 0x100
                    });
                }
            }
            else
            {
                var size = FileManager.Version >= ClientVersion.CV_7090 ? Unsafe.SizeOf<MultiBlockNew>() + 2 : Unsafe.SizeOf<MultiBlock>();
                var count = entry.Length / size;

                for (var i = 0; i < count; ++i)
                {
                    var block = reader.Read<MultiBlock>();
                    reader.Skip(size - Unsafe.SizeOf<MultiBlock>());

                    list.Add(new ()
                    {
                        ID = block.ID,
                        X = block.X,
                        Y = block.Y,
                        Z = block.Z,
                        IsVisible = block.Flags != 0
                    });
                }
            }

            return list;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct MultiBlock
        {
            public ushort ID;
            public short X;
            public short Y;
            public short Z;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct MultiBlockNew
        {
            public ushort ID;
            public short X;
            public short Y;
            public short Z;
            public ushort Flags;
            public uint Unknown;
        }
    }

    public struct MultiInfo
    {
        public ushort ID;
        public short X;
        public short Y;
        public short Z;
        public bool IsVisible;
    }
}