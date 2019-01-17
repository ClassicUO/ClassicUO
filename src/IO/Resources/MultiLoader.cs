using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    class MultiLoader : ResourceLoader
    {
        private UOFileMul _file;
        private UOFileUopNoFormat _fileUop;
        private int _itemOffset;
        private DataReader _reader;

        public int Count { get; private set; }

        private GCHandle _handle;

        public override void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "multi.mul");
            string pathidx = Path.Combine(FileManager.UoFolderPath, "multi.idx");

            if (File.Exists(path) && File.Exists(pathidx))
                _file = new UOFileMul(path, pathidx, Constants.MAX_MULTI_DATA_INDEX_COUNT, 14);
            else
                throw new FileNotFoundException();

            Count = _itemOffset = FileManager.ClientVersion >= ClientVersions.CV_7090 ? UnsafeMemoryManager.SizeOf<MultiBlockNew>() : UnsafeMemoryManager.SizeOf<MultiBlock>();


            string uopPath = Path.Combine(FileManager.UoFolderPath, "MultiCollection.uop");

            if (File.Exists(uopPath))
            {
                Count = Constants.MAX_MULTI_DATA_INDEX_COUNT;
                _fileUop = new UOFileUopNoFormat(uopPath);
                _reader = new DataReader();

                for (int i = 0; i < _fileUop.Entries.Length; i++)
                {
                    long offset = _fileUop.Entries[i].Offset;
                    int csize = _fileUop.Entries[i].Length;
                    int dsize = _fileUop.Entries[i].DecompressedLength;

                    _fileUop.Seek(offset);
                    byte[] cdata = _fileUop.ReadArray<byte>(csize);
                    byte[] ddata = new byte[dsize];

                    ZLib.Decompress(cdata, 0, ddata, dsize);
                    _reader.SetData(ddata, dsize);

                    uint id = _reader.ReadUInt();

                    if (id < Constants.MAX_MULTI_DATA_INDEX_COUNT && id < _file.Entries.Length)
                    {
                        ref UOFileIndex3D index = ref _file.Entries[id];
                        int count = _reader.ReadInt();

                        index = new UOFileIndex3D(offset, csize, dsize, (int)MathHelper.Combine(count, index.Extra));
                    }
                }

                _reader.ReleaseData();
            }
        }

        protected override void CleanResources()
        {
            // do nothing
        }

        public unsafe void GetMultiData(int index, ushort g, bool uopValid, out ushort graphic, out short x, out short y, out short z, out uint flags)
        {
            if (_fileUop != null && uopValid)
            {             
                graphic = _reader.ReadUShort();
                x = _reader.ReadShort();
                y = _reader.ReadShort();
                z = _reader.ReadShort();
                flags = _reader.ReadUShort();

                if (flags == 0)
                    flags = 1;

                uint clilocsCount = _reader.ReadUInt();

                if (clilocsCount != 0)
                    _reader.Skip( (int) (clilocsCount * 4));

                _reader.ReleaseData();
            }
            else
            {
                MultiBlock* block = (MultiBlock*)(_file.PositionAddress + index * _itemOffset);

                graphic = block->ID;
                x = block->X;
                y = block->Y;
                z = block->Z;
                flags = block->Flags;
            }
        }

        public int GetCount(int graphic, out bool uopValid)
        {
            int count;

            if (graphic < _file.Entries.Length)
            {
                ref UOFileIndex3D index = ref _file.Entries[graphic];

                MathHelper.GetNumbersFromCombine((ulong) index.Extra, out count, out _);

                if (_fileUop != null && count > 0)
                {
                    uopValid = true;

                    long offset = index.Offset;
                    int csize = index.Length;
                    int dsize = index.DecompressedLength;

                    _fileUop.Seek(offset);
                    byte[] cdata = _fileUop.ReadArray<byte>(csize);
                    byte[] ddata = new byte[dsize];
                    ZLib.Decompress(cdata, 0, ddata, dsize);

                    _reader.SetData(ddata, dsize);
                    _reader.Skip(8);

                    return count;
                }
            }

            uopValid = false;

            (int length, int extra, bool patcher) = _file.SeekByEntryIndex(graphic);
            count = length / _itemOffset;

            return count;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct MultiBlock
    {
        public readonly ushort ID;
        public readonly short X;
        public readonly short Y;
        public readonly short Z;
        public readonly uint Flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct MultiBlockNew
    {
        public readonly ushort ID;
        public readonly short X;
        public readonly short Y;
        public readonly short Z;
        public readonly uint Flags;
        public readonly int Unknown;
    }
}
