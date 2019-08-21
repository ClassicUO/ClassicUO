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
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO.Resources
{
    internal class MultiLoader : ResourceLoader
    {
        private UOFile _file;
        private int _itemOffset;
        private DataReader _reader;

        public int Count { get; private set; }

        public override Task Load()
        {
            return Task.Run(() =>
            {
                string uopPath = Path.Combine(FileManager.UoFolderPath, "MultiCollection.uop");

                if (File.Exists(uopPath))
                {
                    Count = Constants.MAX_MULTI_DATA_INDEX_COUNT;
                    _file = new UOFileUop(uopPath, "build/multicollection/{0:D6}.bin");
                    Entries = new UOFileIndex[Count];
                    _reader = new DataReader();
                }
                else
                {
                    string path = Path.Combine(FileManager.UoFolderPath, "multi.mul");
                    string pathidx = Path.Combine(FileManager.UoFolderPath, "multi.idx");

                    if (File.Exists(path) && File.Exists(pathidx))
                    {
                        _file = new UOFileMul(path, pathidx, Constants.MAX_MULTI_DATA_INDEX_COUNT, 14);
                        Count = _itemOffset = FileManager.ClientVersion >= ClientVersions.CV_7090 ? UnsafeMemoryManager.SizeOf<MultiBlockNew>() : UnsafeMemoryManager.SizeOf<MultiBlock>();
                    }
                }

                _file.FillEntries(ref Entries);

            });
        }

        public override void CleanResources()
        {
            // do nothing
        }

        public unsafe void GetMultiData(int index, ushort g, bool uopValid, out ushort graphic, out short x, out short y, out short z, out bool add)
        {
            if (_file is UOFileUop)
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
            ref readonly var entry = ref GetValidRefEntry(graphic);

            if (_file is UOFileUop uop)
            {
                long offset = entry.Offset;
                int csize = entry.Length;
                int dsize = entry.DecompressedLength;

                if (csize > 0 && dsize > 0)
                {
                    uopValid = true;

                    _file.Seek(offset);

                    byte[] ddata = uop.GetData(csize, dsize);

                    _reader.SetData(ddata, dsize);
                    _reader.Skip(4);
                    count = (int) _reader.ReadUInt();

                }
                else
                {
                    uopValid = false;
                    count = 0;

                    Log.Message(LogTypes.Warning, $"[MultiCollection.uop] invalid entry (0x{graphic:X4})");
                }

                return count;
            }

            uopValid = false;

            count = entry.Length / _itemOffset;
            _file.Seek(entry.Offset);

            return count;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        readonly struct MultiBlock
        {
            public readonly ushort ID;
            public readonly short X;
            public readonly short Y;
            public readonly short Z;
            public readonly uint Flags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        readonly struct MultiBlockNew
        {
            public readonly ushort ID;
            public readonly short X;
            public readonly short Y;
            public readonly short Z;
            public readonly uint Flags;
            public readonly int Unknown;
        }
    }
}