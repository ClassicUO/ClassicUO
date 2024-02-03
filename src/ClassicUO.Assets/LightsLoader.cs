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

using ClassicUO.IO;
using ClassicUO.Utility;
using System;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class LightsLoader : UOFileLoader
    {
        private static LightsLoader _instance;
        private UOFileMul _file;

        public const int MAX_LIGHTS_DATA_INDEX_COUNT = 100;

        private LightsLoader(int count) { }

        public static LightsLoader Instance =>
            _instance ?? (_instance = new LightsLoader(MAX_LIGHTS_DATA_INDEX_COUNT));

        public UOFileMul File => _file;

        public override Task Load()
        {
            return Task.Run(() =>
            {
                string path = UOFileManager.GetUOFilePath("light.mul");
                string pathidx = UOFileManager.GetUOFilePath("lightidx.mul");

                FileSystemHelper.EnsureFileExists(path);
                FileSystemHelper.EnsureFileExists(pathidx);

                _file = new UOFileMul(path, pathidx, MAX_LIGHTS_DATA_INDEX_COUNT);
                _file.FillEntries(ref Entries);
            });
        }

        public LightInfo GetLight(uint idx)
        {
            ref var entry = ref GetValidRefEntry((int)idx);

            if (entry.Width == 0 && entry.Height == 0)
            {
                return default;
            }

            var buffer = new uint[entry.Width * entry.Height];
            _file.SetData(entry.Address, entry.FileSize);
            _file.Seek(entry.Offset);

            for (int i = 0; i < entry.Height; i++)
            {
                int pos = i * entry.Width;

                for (int j = 0; j < entry.Width; j++)
                {
                    ushort val = _file.ReadByte();
                    // Light can be from -31 to 31. When they are below 0 they are bit inverted
                    if (val > 0x1F)
                    {
                        val = (ushort)(~val & 0x1F);
                    }
                    uint rgb24 = (uint)((val << 19) | (val << 11) | (val << 3));

                    if (val != 0)
                    {
                        buffer[pos + j] = rgb24 | 0xFF_00_00_00;
                    }
                }
            }

            return new LightInfo()
            {
                Pixels = buffer,
                Width = entry.Width,
                Height = entry.Height
            };
        }
    }

    public ref struct LightInfo
    {
        public Span<uint> Pixels;
        public int Width;
        public int Height;
    }
}
