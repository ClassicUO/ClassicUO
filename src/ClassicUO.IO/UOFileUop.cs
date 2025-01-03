// SPDX-License-Identifier: BSD-2-Clause

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

    public sealed class UOFileUop : UOFile
    {
        private const uint UOP_MAGIC_NUMBER = 0x50594D;
        private readonly bool _hasExtra;
        private readonly string _pattern;
        private readonly Dictionary<ulong, UOFileIndex> _hashes = new Dictionary<ulong, UOFileIndex>();

        public UOFileUop(string path, string pattern, bool hasextra = false) : base(path)
        {
            _pattern = pattern;
            _hasExtra = hasextra;
        }

        public string Pattern => _pattern;


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
            Seek(0, System.IO.SeekOrigin.Begin);

            if (ReadUInt32() != UOP_MAGIC_NUMBER)
            {
                throw new ArgumentException("Bad uop file");
            }

            var version = ReadUInt32();
            var format_timestamp = ReadUInt32();
            var nextBlock = ReadInt64();
            var block_size = ReadUInt32();
            var count = ReadInt32();


            Seek(nextBlock, System.IO.SeekOrigin.Begin);
            int total = 0;
            int real_total = 0;

            do
            {
                var filesCount = ReadInt32();
                nextBlock = ReadInt64();
                total += filesCount;

                for (int i = 0; i < filesCount; i++)
                {
                    long offset = ReadInt64();
                    int headerLength = ReadInt32();
                    int compressedLength = ReadInt32();
                    int decompressedLength = ReadInt32();
                    var hash = ReadUInt64();
                    uint data_hash = ReadUInt32();
                    short flag = ReadInt16();
                    int length = flag == 1 ? compressedLength : decompressedLength;

                    if (offset == 0)
                    {
                        continue;
                    }

                    real_total++;

                    offset += headerLength;

                    if (_hasExtra && flag != 3)
                    {
                        var pos = Position;
                        Seek(offset, System.IO.SeekOrigin.Begin);

                        var extra1 = ReadInt32();
                        var extra2 = ReadInt32();

                        _hashes.Add
                        (
                            hash,
                            new UOFileIndex
                            (
                                null,
                                offset + 8,
                                compressedLength - 8,
                                decompressedLength,
                                (CompressionType)flag,
                                extra1,
                                extra2
                            )
                        );

                        Seek(pos, System.IO.SeekOrigin.Begin);
                    }
                    else
                    {
                        _hashes.Add
                        (
                            hash,
                            new UOFileIndex
                            (
                                null,
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

                Seek(nextBlock, System.IO.SeekOrigin.Begin);
            } while (nextBlock != 0);

            Entries = new UOFileIndex[Math.Max(total, ushort.MaxValue) + 0x4000];

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