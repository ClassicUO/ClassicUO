﻿#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.IO;
using ClassicUO.Utility;
using System;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class LightsLoader : UOFileLoader
    {
        private UOFileMul _file;

        public const int MAX_LIGHTS_DATA_INDEX_COUNT = 100;

        public LightsLoader(UOFileManager fileManager) : base(fileManager) { }

        public UOFileMul File => _file;

        public override void Load()
        {
            string path = FileManager.GetUOFilePath("light.mul");
            string pathidx = FileManager.GetUOFilePath("lightidx.mul");

            FileSystemHelper.EnsureFileExists(path);
            FileSystemHelper.EnsureFileExists(pathidx);

            _file = new UOFileMul(path, pathidx);
            _file.FillEntries();
        }

        public LightInfo GetLight(uint idx)
        {
            ref var entry = ref _file.GetValidRefEntry((int)idx);

            if (entry.Width == 0 && entry.Height == 0)
            {
                return default;
            }

            _file.Seek(entry.Offset, System.IO.SeekOrigin.Begin);
            var buffer = new uint[entry.Width * entry.Height];

            for (int i = 0; i < entry.Height; i++)
            {
                int pos = i * entry.Width;

                for (int j = 0; j < entry.Width; j++)
                {
                    ushort val = _file.ReadUInt8();
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
