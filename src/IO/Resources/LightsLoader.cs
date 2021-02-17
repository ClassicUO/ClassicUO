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

using System.Threading.Tasks;
using ClassicUO.Game;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class LightsLoader : UOFileLoader<UOTexture>
    {
        private static LightsLoader _instance;
        private UOFileMul _file;

        private LightsLoader(int count) : base(count)
        {
        }

        public static LightsLoader Instance =>
            _instance ?? (_instance = new LightsLoader(Constants.MAX_LIGHTS_DATA_INDEX_COUNT));

        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string path = UOFileManager.GetUOFilePath("light.mul");
                    string pathidx = UOFileManager.GetUOFilePath("lightidx.mul");

                    FileSystemHelper.EnsureFileExists(path);
                    FileSystemHelper.EnsureFileExists(pathidx);

                    _file = new UOFileMul(path, pathidx, Constants.MAX_LIGHTS_DATA_INDEX_COUNT);
                    _file.FillEntries(ref Entries);
                }
            );
        }

        public override UOTexture GetTexture(uint id)
        {
            if (id >= Resources.Length)
            {
                return null;
            }

            ref UOTexture texture = ref Resources[id];

            if (texture == null || texture.IsDisposed)
            {
                uint[] pixels = GetLight(id, out int w, out int h);

                if (w == 0 && h == 0)
                {
                    return null;
                }

                texture = new UOTexture(w, h);
                texture.PushData(pixels);

                SaveId(id);
            }
            else
            {
                texture.Ticks = Time.Ticks;
            }

            return texture;
        }


        private uint[] GetLight(uint idx, out int width, out int height)
        {
            ref UOFileIndex entry = ref GetValidRefEntry((int) idx);

            width = entry.Width;
            height = entry.Height;

            if (width == 0 && height == 0)
            {
                return null;
            }

            uint[] pixels = new uint[width * height];

            _file.SetData(entry.Address, entry.FileSize);
            _file.Seek(entry.Offset);

            for (int i = 0; i < height; i++)
            {
                int pos = i * width;

                for (int j = 0; j < width; j++)
                {
                    ushort val = _file.ReadByte();
                    val = (ushort) ((val << 10) | (val << 5) | val);

                    if (val != 0)
                    {
                        pixels[pos + j] = HuesHelper.Color16To32(val) | 0xFF_00_00_00;
                        ;
                    }
                }
            }

            return pixels;
        }
    }
}