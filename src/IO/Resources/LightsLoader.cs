#region license

// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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