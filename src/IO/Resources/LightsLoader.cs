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
                if (GetLight(ref texture, id))
                {
                    SaveId(id);
                }
            }
            else
            {
                texture.Ticks = Time.Ticks;
            }

            return texture;
        }


        private bool GetLight(ref UOTexture texture, uint idx)
        {
            ref UOFileIndex entry = ref GetValidRefEntry((int) idx);

            if (entry.Width == 0 && entry.Height == 0)
            {
                return false;
            }

            uint[] pixels = System.Buffers.ArrayPool<uint>.Shared.Rent(entry.Width * entry.Height);

            try
            {
                _file.SetData(entry.Address, entry.FileSize);
                _file.Seek(entry.Offset);

                for (int i = 0; i < entry.Height; i++)
                {
                    int pos = i * entry.Width;

                    for (int j = 0; j < entry.Width; j++)
                    {
                        ushort val = _file.ReadByte();
                        uint rgb24 = (uint) ((val << 19) | (val << 11) | (val << 3));

                        if (val != 0)
                        {
                            pixels[pos + j] = rgb24 | 0xFF_00_00_00;
                        }
                    }
                }

                texture = new UOTexture(entry.Width, entry.Height);
                texture.SetData(pixels, 0, entry.Width * entry.Height);
            }
            finally
            {
                System.Buffers.ArrayPool<uint>.Shared.Return(pixels, true);
            }

            return true;
        }
    }
}