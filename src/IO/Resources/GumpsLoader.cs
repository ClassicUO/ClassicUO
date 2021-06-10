#region license

// Copyright (c) 2021, andreakarasho
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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClassicUO.Game;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class GumpsLoader : UOFileLoader<UOTexture>
    {
        private static GumpsLoader _instance;
        private UOFile _file;
        private PixelPicker _picker = new PixelPicker();

        private GumpsLoader(int count) : base(count)
        {
        }

        public static GumpsLoader Instance =>
            _instance ?? (_instance = new GumpsLoader(Constants.MAX_GUMP_DATA_INDEX_COUNT));

        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string path = UOFileManager.GetUOFilePath("gumpartLegacyMUL.uop");

                    if (Client.IsUOPInstallation && File.Exists(path))
                    {
                        _file = new UOFileUop(path, "build/gumpartlegacymul/{0:D8}.tga", true);
                        Entries = new UOFileIndex[Math.Max(((UOFileUop) _file).TotalEntriesCount, Constants.MAX_GUMP_DATA_INDEX_COUNT)];
                        Client.UseUOPGumps = true;
                    }
                    else
                    {
                        path = UOFileManager.GetUOFilePath("gumpart.mul");
                        string pathidx = UOFileManager.GetUOFilePath("gumpidx.mul");

                        if (!File.Exists(path))
                        {
                            path = UOFileManager.GetUOFilePath("Gumpart.mul");
                        }

                        if (!File.Exists(pathidx))
                        {
                            pathidx = UOFileManager.GetUOFilePath("Gumpidx.mul");
                        }

                        _file = new UOFileMul(path, pathidx, Constants.MAX_GUMP_DATA_INDEX_COUNT, 12);

                        Client.UseUOPGumps = false;
                    }

                    _file.FillEntries(ref Entries);

                    string pathdef = UOFileManager.GetUOFilePath("gump.def");

                    if (!File.Exists(pathdef))
                    {
                        return;
                    }

                    using (DefReader defReader = new DefReader(pathdef, 3))
                    {
                        while (defReader.Next())
                        {
                            int ingump = defReader.ReadInt();

                            if (ingump < 0 || ingump >= Constants.MAX_GUMP_DATA_INDEX_COUNT || ingump >= Entries.Length || Entries[ingump].Length > 0)
                            {
                                continue;
                            }

                            int[] group = defReader.ReadGroup();

                            if (group == null)
                            {
                                continue;
                            }

                            for (int i = 0; i < group.Length; i++)
                            {
                                int checkIndex = group[i];

                                if (checkIndex < 0 || checkIndex >= Constants.MAX_GUMP_DATA_INDEX_COUNT || checkIndex >= Entries.Length || Entries[checkIndex].Length <= 0)
                                {
                                    continue;
                                }

                                Entries[ingump] = Entries[checkIndex];

                                Entries[ingump].Hue = (ushort) defReader.ReadInt();

                                break;
                            }
                        }
                    }
                }
            );
        }

        public override UOTexture GetTexture(uint g)
        {
            if (g >= Resources.Length)
            {
                return null;
            }

            ref UOTexture texture = ref Resources[g];

            if (texture == null || texture.IsDisposed)
            {
                if (GetGumpPixels(ref texture, g))
                {
                    SaveId(g);
                }
            }
            else
            {
                texture.Ticks = Time.Ticks;
            }

            return texture;
        }

        public bool PixelCheck(int index, int x, int y)
        {
            return _picker.Get((ulong) index, x, y);
        }

        private unsafe bool GetGumpPixels(ref UOTexture texture, uint index)
        {
            ref UOFileIndex entry = ref GetValidRefEntry((int) index);

            if (entry.Width <= 0 && entry.Height <= 0)
            {
                return false;
            }

            ushort color = entry.Hue;

            if (entry.Width == 0 || entry.Height == 0)
            {
                return false;
            }

            _file.SetData(entry.Address, entry.FileSize);
            _file.Seek(entry.Offset);

            IntPtr dataStart = _file.PositionAddress;

            uint[] pixels = System.Buffers.ArrayPool<uint>.Shared.Rent(entry.Width * entry.Height);

            try
            {
                int* lookuplist = (int*)dataStart;

                int gsize;

                for (int y = 0, half_len = entry.Length >> 2; y < entry.Height; y++)
                {
                    if (y < entry.Height - 1)
                    {
                        gsize = lookuplist[y + 1] - lookuplist[y];
                    }
                    else
                    {
                        gsize = half_len - lookuplist[y];
                    }

                    GumpBlock* gmul = (GumpBlock*)(dataStart + (lookuplist[y] << 2));

                    int pos = y * entry.Width;

                    for (int i = 0; i < gsize; i++)
                    {
                        uint val = gmul[i].Value;

                        if (color != 0 && val != 0)
                        {
                            val = HuesLoader.Instance.GetColor16(gmul[i].Value, color);
                        }

                        if (val != 0)
                        {
                            //val = 0x8000 | val;
                            val = HuesHelper.Color16To32(gmul[i].Value) | 0xFF_00_00_00;
                        }

                        int count = gmul[i].Run;

                        for (int j = 0; j < count; j++)
                        {
                            pixels[pos++] = val;
                        }
                    }
                }

                texture = new UOTexture(entry.Width, entry.Height);
                texture.SetData(pixels, 0, entry.Width * entry.Height);

                _picker.Set(index, entry.Width, entry.Height, pixels);
            }
            finally
            {
                System.Buffers.ArrayPool<uint>.Shared.Return(pixels, true);
            }

            return true;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private ref struct GumpBlock
        {
            public readonly ushort Value;
            public readonly ushort Run;
        }
    }
}