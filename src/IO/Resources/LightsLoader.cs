﻿#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System.IO;

using ClassicUO.Game;
using ClassicUO.Renderer;

namespace ClassicUO.IO.Resources
{
    internal class LightsLoader : ResourceLoader<SpriteTexture>
    {
        private UOFileMul _file;

        public override void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "light.mul");
            string pathidx = Path.Combine(FileManager.UoFolderPath, "lightidx.mul");

            if (!File.Exists(path) || !File.Exists(pathidx))
                throw new FileNotFoundException();

            _file = new UOFileMul(path, pathidx, Constants.MAX_LIGHTS_DATA_INDEX_COUNT);
        }


        public override void CleanResources()
        {
        }

        public override SpriteTexture GetTexture(uint id)
        {
            if (!ResourceDictionary.TryGetValue(id, out var texture))
            {
                ushort[] pixels = GetLight(id, out int w, out int h);

                texture = new SpriteTexture(w, h, false);
                texture.SetData(pixels);
                ResourceDictionary.Add(id, texture);
            }

            return texture;
        }


        private ushort[] GetLight(uint idx, out int width, out int height)
        {
            (int length, int extra, bool patched) = _file.SeekByEntryIndex((int) idx);
            width = extra & 0xFFFF;
            height = (extra >> 16) & 0xFFFF;
            ushort[] pixels = new ushort[width * height];

            for (int i = 0; i < height; i++)
            {
                int pos = i * width;

                for (int j = 0; j < width; j++)
                {
                    ushort val = _file.ReadByte();
                    val = (ushort) ((val << 10) | (val << 5) | val);
                    pixels[pos + j] = (ushort) ((val != 0 ? 0x8000 : 0) | val);
                }
            }

            return pixels;
        }
    }
}