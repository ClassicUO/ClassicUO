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

using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class MultiLoader : ResourceLoader
    {
        private UOFileMul _file;
        private UOFileUop _fileUop;
        private int _itemOffset;
        private DataReader _reader;

        public int Count { get; private set; }

        public override Task Load()
        {
            return Task.Run(() =>
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
                    _fileUop = new UOFileUop(uopPath, ".bin", Count);
                    _reader = new DataReader();

                    for (int i = 0; i < _fileUop.Entries.Length; i++)
                    {
                        long offset = _fileUop.Entries[i].Offset;
                        int csize = _fileUop.Entries[i].Length;
                        int dsize = _fileUop.Entries[i].DecompressedLength;

                        if (offset == 0 && csize == 0 && dsize == 0)
                            continue;

                        _fileUop.Seek(offset);
                        byte[] cdata = _fileUop.ReadArray<byte>(csize);
                        byte[] ddata = new byte[dsize];

                        ZLib.Decompress(cdata, 0, ddata, dsize);
                        _reader.SetData(ddata, dsize);

                        uint id = _reader.ReadUInt();

                        if (id < Constants.MAX_MULTI_DATA_INDEX_COUNT && id < _file.Entries.Length)
                        {
                            ref UOFileIndex3D index = ref _fileUop.Entries[id];
                            int count = _reader.ReadInt();

                            index = new UOFileIndex3D(offset, csize, dsize, (int)MathHelper.Combine(count, index.Extra));
                        }
                    }

                    _reader.ReleaseData();
                }
            });
        }

        protected override void CleanResources()
        {
            // do nothing
        }

        public unsafe void GetMultiData(int index, ushort g, bool uopValid, out ushort graphic, out short x, out short y, out short z, out bool add)
        {
            if (_fileUop != null && uopValid)
            {
                graphic = _reader.ReadUShort();

                x = _reader.ReadShort();
                y = _reader.ReadShort();
                z = _reader.ReadShort();
                ushort flags = _reader.ReadUShort();

                uint clilocsCount = _reader.ReadUInt();

                if (clilocsCount != 0)
                    _reader.Skip((int) (clilocsCount * 4));

                add = flags == 0;
            }
            else
            {
                MultiBlock* block = (MultiBlock*) (_file.PositionAddress + index * _itemOffset);

                graphic = block->ID;
                x = block->X;
                y = block->Y;
                z = block->Z;
                uint flags = block->Flags;

                add = flags != 0;
            }
        }

        public void ReleaseLastMultiDataRead()
        {
            _reader?.ReleaseData();
        }


        public int GetCount(int graphic, out bool uopValid)
        {
            int count;

            //if (graphic < _file.Entries.Length)
            {
                //ref readonly UOFileIndex3D index = ref _file.Entries[graphic];

                //MathHelper.GetNumbersFromCombine((ulong) index.Extra, out count, out _);

                if (_fileUop != null && graphic < _fileUop.Entries.Length)
                {
                    ref readonly UOFileIndex3D index = ref _fileUop.Entries[graphic];


                    long offset = index.Offset;
                    int csize = index.Length;
                    int dsize = index.DecompressedLength;

                    if (csize > 0 && dsize > 0)
                    {
                        uopValid = true;

                        _fileUop.Seek(offset);
                        byte[] cdata = _fileUop.ReadArray<byte>(csize);
                        byte[] ddata = new byte[dsize];
                        ZLib.Decompress(cdata, 0, ddata, dsize);

                        _reader.SetData(ddata, dsize);
                        _reader.Skip(4);
                        count = (int)_reader.ReadUInt();

                        return count;
                    }
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