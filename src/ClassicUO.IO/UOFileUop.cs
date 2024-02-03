#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;

namespace ClassicUO.IO
{
    public class UOFileUop : UOFile
    {
        private const uint UOP_MAGIC_NUMBER = 0x50594D;
        private readonly bool _hasExtra;
        private readonly Dictionary<ulong, UOFileIndex> _hashes = new Dictionary<ulong, UOFileIndex>();
        private readonly string _pattern;

        public UOFileUop(string path, string pattern, bool hasextra = false) : base(path)
        {
            _pattern = pattern;
            _hasExtra = hasextra;
            Load();
        }

        public int TotalEntriesCount { get; private set; }
        public string Pattern => _pattern;
        public Dictionary<ulong, UOFileIndex> Hashes => _hashes;

        
        protected override void Load()
        {
            base.Load();

            Seek(0);

            if (ReadUInt() != UOP_MAGIC_NUMBER)
            {
                throw new ArgumentException("Bad uop file");
            }

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
                    {
                        continue;
                    }

                    real_total++;
                    offset += headerLength;

                    if (_hasExtra)
                    {
                        long curpos = Position;
                        Seek(offset);
                        short extra1 = (short) ReadInt();
                        short extra2 = (short) ReadInt();

                        _hashes.Add
                        (
                            hash,
                            new UOFileIndex
                            (
                                StartAddress,
                                (uint) Length,
                                offset + 8,
                                compressedLength - 8,
                                decompressedLength,
                                extra1,
                                extra2
                            )
                        );

                        Seek(curpos);
                    }
                    else
                    {
                        _hashes.Add
                        (
                            hash,
                            new UOFileIndex
                            (
                                StartAddress,
                                (uint) Length,
                                offset,
                                compressedLength,
                                decompressedLength
                            )
                        );
                    }
                }

                Seek(nextBlock);
            } while (nextBlock != 0);

            TotalEntriesCount = real_total;
        }

        public void ClearHashes()
        {
            _hashes.Clear();
        }

        public override void Dispose()
        {
            ClearHashes();
            base.Dispose();
        }


        public bool TryGetUOPData(ulong hash, out UOFileIndex data)
        {
            return _hashes.TryGetValue(hash, out data);
        }

        public override void FillEntries(ref UOFileIndex[] entries)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                string file = string.Format(_pattern, i);
                ulong hash = CreateHash(file);

                if (_hashes.TryGetValue(hash, out UOFileIndex data))
                {
                    entries[i] = data;
                }
            }
        }

        public static ulong CreateHash(string s)
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