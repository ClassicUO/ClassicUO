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
using System.Threading.Tasks;
using ClassicUO.Game;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.IO.Resources
{
    internal class TexmapsLoader : UOFileLoader
    {
        private static TexmapsLoader _instance;
        private UOFile _file;

        private TexmapsLoader(int count)
        {
        }

        public static TexmapsLoader Instance =>
            _instance ?? (_instance = new TexmapsLoader(Constants.MAX_LAND_TEXTURES_DATA_INDEX_COUNT));

        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string path = UOFileManager.GetUOFilePath("texmaps.mul");
                    string pathidx = UOFileManager.GetUOFilePath("texidx.mul");

                    FileSystemHelper.EnsureFileExists(path);
                    FileSystemHelper.EnsureFileExists(pathidx);

                    _file = new UOFileMul(path, pathidx, Constants.MAX_LAND_TEXTURES_DATA_INDEX_COUNT, 10);
                    _file.FillEntries(ref Entries);
                    string pathdef = UOFileManager.GetUOFilePath("TexTerr.def");

                    if (!File.Exists(pathdef))
                    {
                        return;
                    }

                    using (DefReader defReader = new DefReader(pathdef))
                    {
                        while (defReader.Next())
                        {
                            int index = defReader.ReadInt();

                            if (index < 0 || index >= Entries.Length)
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
                                int checkindex = group[i];

                                if (checkindex < 0 || checkindex >= Entries.Length)
                                {
                                    continue;
                                }

                                Entries[index] = Entries[checkindex];
                            }
                        }
                    }

                    _spriteInfos = new SpriteInfo[Entries.Length];
                }
            );
        }
      

        struct SpriteInfo
        {
            public Texture2D Texture;
            public Rectangle UV;
        }

        private SpriteInfo[] _spriteInfos;

        public Texture2D GetLandTexture(uint g, out Rectangle bounds)
        {
            // avoid to mix land with statics
            //g += ushort.MaxValue;

            var atlas = TextureAtlas.Shared;

            ref var spriteInfo = ref _spriteInfos[g];

            if (spriteInfo.Texture == null)
            {
                AddSpriteToAtlas(atlas, g);
            }

            bounds = spriteInfo.UV;

            return spriteInfo.Texture;  //atlas.GetTexture(g, out bounds);
        }

        private unsafe void AddSpriteToAtlas(TextureAtlas atlas, uint index)
        {
            ref UOFileIndex entry = ref GetValidRefEntry((int) (index));

            if (entry.Length <= 0)
            {
                return;
            }

            _file.SetData(entry.Address, entry.FileSize);
            _file.Seek(entry.Offset);

            int size = entry.Length == 0x2000 ? 64 : 128;
            Span<uint> data = stackalloc uint[size * size];

            for (int i = 0; i < size; ++i)
            {
                int pos = i * size;

                for (int j = 0; j < size; ++j)
                {
                    data[pos + j] = HuesHelper.Color16To32(_file.ReadUShort()) | 0xFF_00_00_00;
                }
            }

            ref var spriteInfo = ref _spriteInfos[index];

            spriteInfo.Texture = atlas.AddSprite(data, size, size, out spriteInfo.UV);
        }
    }
}