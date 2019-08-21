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
using System.Collections.Generic;
using System.IO;
using System.Text;

using ClassicUO.IO.Resources;
using ClassicUO.Utility;

namespace ClassicUO.IO
{
    internal class UOFileUop : UOFile
    {
        private const uint UOP_MAGIC_NUMBER = 0x50594D;
        private readonly string _pattern;
        private readonly bool _hasExtra;
        private readonly Dictionary<ulong, UOFileIndex> _hashes = new Dictionary<ulong, UOFileIndex>();

        public UOFileUop(string path, string pattern, bool hasextra = false) : base(path)
        {
            _pattern = pattern;
            _hasExtra = hasextra;
            Load();
        }

        public bool TryGetUOPData(ulong hash, out UOFileIndex data)
            => _hashes.TryGetValue(hash, out data);

        public int TotalEntriesCount { get; private set; }

        protected override void Load()
        {
            base.Load();

            Seek(0);

            if (ReadUInt() != UOP_MAGIC_NUMBER)
                throw new ArgumentException("Bad uop file");

            uint version = ReadUInt();
            uint format_timestamp = ReadUInt();
            long nextBlock = ReadLong();
            uint block_size = ReadUInt();
            int count = ReadInt();


            Seek(nextBlock);
            int total = 0;
            int real_total = 0;
            do
            {
                int filesCount = ReadInt();
                nextBlock = ReadLong();
                total += filesCount;

                for (int i = 0; i < filesCount; i++)
                {
                    long offset = ReadLong();
                    int headerLength = ReadInt();
                    int compressedLength = ReadInt();
                    int decompressedLength = ReadInt();
                    ulong hash = ReadULong();
                    uint data_hash = ReadUInt();
                    short flag = ReadShort();
                    int length = flag == 1 ? compressedLength : decompressedLength;

                    if (offset == 0)
                        continue;

                    real_total++;
                    offset += headerLength;
                    if (_hasExtra)
                    {
                        long curpos = Position;
                        Seek(offset);
                        int extra1 = ReadInt();
                        int extra2 = ReadInt();

                        _hashes.Add(hash, new UOFileIndex(offset + 8, compressedLength - 8, decompressedLength, (extra1 << 16) | extra2));

                        Seek(curpos);
                    }
                    else
                        _hashes.Add(hash, new UOFileIndex(offset, compressedLength, decompressedLength, 0));
                }

                Seek(nextBlock);
            } while (nextBlock != 0);

            TotalEntriesCount = real_total;
        }

        public void ClearHashes()
            => _hashes.Clear();

        public override void Dispose()
        {
            ClearHashes();
            base.Dispose();
        }

        public override void FillEntries(ref UOFileIndex[] entries)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                string file = string.Format(_pattern, i);
                ulong hash = CreateHash(file);

                if (_hashes.TryGetValue(hash, out var data))
                {
                    entries[i] = data;
                }
            }
        }

        public void FillEntries(ref UOFileIndex[] entries, bool clearHashes)
        {
            FillEntries(ref entries);

            if (clearHashes)
                ClearHashes();
        }

        //public unsafe T[] GetData<T>(int compressedSize, int uncompressedSize) where T : struct
        //{
        //    T[] data = new T[uncompressedSize];
        //    IntPtr destPtr = (IntPtr) UnsafeMemoryManager.AsPointer(ref data);
        //    ZLib.Decompress(PositionAddress, compressedSize, 0, destPtr, uncompressedSize);

        //    return data;
        //}

        public unsafe byte[] GetData(int compressedSize, int uncompressedSize) 
        {
            byte[] data = new byte[uncompressedSize];

            fixed (byte* destPtr = data)
                ZLib.Decompress(PositionAddress, compressedSize, 0, (IntPtr) destPtr, uncompressedSize);

            return data;
        }

        internal static ulong CreateHash(string s)
        {
            uint eax, ecx, edx, ebx, esi, edi;
            eax = ecx = edx = ebx = esi = edi = 0;
            ebx = edi = esi = (uint) s.Length + 0xDEADBEEF;
            int i = 0;

            for (i = 0; i + 12 < s.Length; i += 12)
            {
                edi = (uint) ((s[i + 7] << 24) | (s[i + 6] << 16) | (s[i + 5] << 8) | s[i + 4]) + edi;
                esi = (uint) ((s[i + 11] << 24) | (s[i + 10] << 16) | (s[i + 9] << 8) | s[i + 8]) + esi;
                edx = (uint) ((s[i + 3] << 24) | (s[i + 2] << 16) | (s[i + 1] << 8) | s[i]) - esi;
                edx = (edx + ebx) ^ (esi >> 28) ^ (esi << 4);
                esi += edi;
                edi = (edi - edx) ^ (edx >> 26) ^ (edx << 6);
                edx += esi;
                esi = (esi - edi) ^ (edi >> 24) ^ (edi << 8);
                edi += edx;
                ebx = (edx - esi) ^ (esi >> 16) ^ (esi << 16);
                esi += edi;
                edi = (edi - ebx) ^ (ebx >> 13) ^ (ebx << 19);
                ebx += esi;
                esi = (esi - edi) ^ (edi >> 28) ^ (edi << 4);
                edi += ebx;
            }

            if (s.Length - i > 0)
            {
                switch (s.Length - i)
                {
                    case 12:
                        esi += (uint) s[i + 11] << 24;
                        goto case 11;

                    case 11:
                        esi += (uint) s[i + 10] << 16;
                        goto case 10;

                    case 10:
                        esi += (uint) s[i + 9] << 8;
                        goto case 9;

                    case 9:
                        esi += s[i + 8];
                        goto case 8;

                    case 8:
                        edi += (uint) s[i + 7] << 24;
                        goto case 7;

                    case 7:
                        edi += (uint) s[i + 6] << 16;
                        goto case 6;

                    case 6:
                        edi += (uint) s[i + 5] << 8;
                        goto case 5;

                    case 5:
                        edi += s[i + 4];
                        goto case 4;

                    case 4:
                        ebx += (uint) s[i + 3] << 24;
                        goto case 3;

                    case 3:
                        ebx += (uint) s[i + 2] << 16;
                        goto case 2;

                    case 2:
                        ebx += (uint) s[i + 1] << 8;
                        goto case 1;

                    case 1:
                        ebx += s[i];

                        break;
                }

                esi = (esi ^ edi) - ((edi >> 18) ^ (edi << 14));
                ecx = (esi ^ ebx) - ((esi >> 21) ^ (esi << 11));
                edi = (edi ^ ecx) - ((ecx >> 7) ^ (ecx << 25));
                esi = (esi ^ edi) - ((edi >> 16) ^ (edi << 16));
                edx = (esi ^ ecx) - ((esi >> 28) ^ (esi << 4));
                edi = (edi ^ edx) - ((edx >> 18) ^ (edx << 14));
                eax = (esi ^ edi) - ((edi >> 8) ^ (edi << 24));

                return ((ulong) edi << 32) | eax;
            }

            return ((ulong) esi << 32) | eax;
        }
    }
}