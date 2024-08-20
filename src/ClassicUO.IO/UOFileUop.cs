﻿#region license

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
    public enum CompressionType : ushort
    {
        None,
        Zlib,
        ZlibBwt = 3
    }

    public class UOFileUop : UOFile
    {
        private const uint UOP_MAGIC_NUMBER = 0x50594D;
        private readonly bool _hasExtra;
        private readonly string _pattern;
        private readonly Dictionary<ulong, UOFileIndex> _hashes = new Dictionary<ulong, UOFileIndex>();

        public UOFileUop(string path, string pattern, bool hasextra = false) : base(path)
        {
            _pattern = pattern;
            _hasExtra = hasextra;
            Load();
        }

        public string Pattern => _pattern;


        protected override void Load()
        {
            base.Load();

            var reader = GetReader();
            reader.Seek(0);

            if (reader.ReadUInt32LE() != UOP_MAGIC_NUMBER)
            {
                throw new ArgumentException("Bad uop file");
            }

            var version = reader.ReadUInt32LE();
            var format_timestamp = reader.ReadUInt32LE();
            var nextBlock = reader.ReadInt64LE();
            var block_size = reader.ReadUInt32LE();
            var count = reader.ReadInt32LE();


            reader.Seek(nextBlock);
            int total = 0;
            int real_total = 0;

            do
            {
                var filesCount = reader.ReadInt32LE();
                nextBlock = reader.ReadInt64LE();
                total += filesCount;

                for (int i = 0; i < filesCount; i++)
                {
                    long offset = reader.ReadInt64LE();
                    int headerLength = reader.ReadInt32LE();
                    int compressedLength = reader.ReadInt32LE();
                    int decompressedLength = reader.ReadInt32LE();
                    var hash = reader.ReadUInt64LE();
                    uint data_hash = reader.ReadUInt32LE();
                    short flag = reader.ReadInt16LE();
                    int length = flag == 1 ? compressedLength : decompressedLength;

                    if (offset == 0)
                    {
                        continue;
                    }

                    real_total++;

                    offset += headerLength;

                    if (_hasExtra && flag != 3)
                    {
                        long curpos = reader.Position;
                        reader.Seek(offset);

                        var extra1 = reader.ReadInt32LE();
                        var extra2 = reader.ReadInt32LE();

                        _hashes.Add
                        (
                            hash,
                            new UOFileIndex
                            (
                                reader.StartAddress,
                                (uint)Length,
                                offset + 8,
                                compressedLength - 8,
                                decompressedLength,
                                (CompressionType)flag,
                                extra1,
                                extra2
                            )
                        );

                        reader.Seek(curpos);
                    }
                    else
                    {
                        _hashes.Add
                        (
                            hash,
                            new UOFileIndex
                            (
                                reader.StartAddress,
                                (uint)Length,
                                offset,
                                compressedLength,
                                decompressedLength,
                                (CompressionType)flag,
                                0,
                                0
                            )
                        );
                    }
                }

                reader.Seek(nextBlock);
            } while (nextBlock != 0);

            Entries = new UOFileIndex[ushort.MaxValue];
            FillEntries();
        }

        public void ClearHashes()
        {
            // _hashes.Clear();
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

        public override void FillEntries()
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                string file = string.Format(_pattern, i);
                ulong hash = CreateHash(file);

                if (_hashes.TryGetValue(hash, out var e))
                {
                    Entries[i] = e;
                }
            }
        }

        public static ulong CreateHash(string s)
        {
            uint eax, ecx, edx, ebx, esi, edi;
            eax = ecx = edx = ebx = esi = edi = 0;
            ebx = edi = esi = (uint)s.Length + 0xDEADBEEF;
            int i = 0;

            for (i = 0; i + 12 < s.Length; i += 12)
            {
                edi = (uint)((s[i + 7] << 24) | (s[i + 6] << 16) | (s[i + 5] << 8) | s[i + 4]) + edi;
                esi = (uint)((s[i + 11] << 24) | (s[i + 10] << 16) | (s[i + 9] << 8) | s[i + 8]) + esi;
                edx = (uint)((s[i + 3] << 24) | (s[i + 2] << 16) | (s[i + 1] << 8) | s[i]) - esi;
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
                        esi += (uint)s[i + 11] << 24;
                        goto case 11;

                    case 11:
                        esi += (uint)s[i + 10] << 16;
                        goto case 10;

                    case 10:
                        esi += (uint)s[i + 9] << 8;
                        goto case 9;

                    case 9:
                        esi += s[i + 8];
                        goto case 8;

                    case 8:
                        edi += (uint)s[i + 7] << 24;
                        goto case 7;

                    case 7:
                        edi += (uint)s[i + 6] << 16;
                        goto case 6;

                    case 6:
                        edi += (uint)s[i + 5] << 8;
                        goto case 5;

                    case 5:
                        edi += s[i + 4];
                        goto case 4;

                    case 4:
                        ebx += (uint)s[i + 3] << 24;
                        goto case 3;

                    case 3:
                        ebx += (uint)s[i + 2] << 16;
                        goto case 2;

                    case 2:
                        ebx += (uint)s[i + 1] << 8;
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

                return ((ulong)edi << 32) | eax;
            }

            return ((ulong)esi << 32) | eax;
        }
    }
}