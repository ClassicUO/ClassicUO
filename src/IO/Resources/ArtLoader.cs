using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.IO.Resources
{
    class ArtLoader : ResourceLoader<ArtTexture>
    {
        public const int ART_COUNT = 0x10000;

        private UOFile _file;
        private readonly List<uint> _usedIndex = new List<uint>();
        private readonly List<uint> _usedIndexLand = new List<uint>();
        private readonly Dictionary<uint, SpriteTexture> _landDictionary = new Dictionary<uint, SpriteTexture>();

        public override void Load()
        {
            string filepath = Path.Combine(FileManager.UoFolderPath, "artLegacyMUL.uop");

            if (File.Exists(filepath))
                _file = new UOFileUop(filepath, ".tga", ART_COUNT);
            else
            {
                filepath = Path.Combine(FileManager.UoFolderPath, "art.mul");
                string idxpath = Path.Combine(FileManager.UoFolderPath, "artidx.mul");

                if (File.Exists(filepath) && File.Exists(idxpath))
                    _file = new UOFileMul(filepath, idxpath, ART_COUNT);
            }
        }

        public override ArtTexture GetTexture(uint g)
        {
            if (!ResourceDictionary.TryGetValue(g, out ArtTexture texture) || texture.IsDisposed)
            {
                ushort[] pixels = ReadStaticArt((ushort)g, out short w, out short h, out Rectangle imageRectangle);
                texture = new ArtTexture(imageRectangle, w, h);
                texture.SetDataHitMap16(pixels);
                _usedIndex.Add(g);
                ResourceDictionary.Add(g, texture);
            }
            return texture;
        }

        public SpriteTexture GetLandTexture(uint g)
        {
            if (!_landDictionary.TryGetValue(g, out SpriteTexture texture) || texture.IsDisposed)
            {
                const int SIZE = 44;
                ushort[] pixels = ReadLandArt((ushort)g);
                texture = new SpriteTexture(SIZE, SIZE, false);
                texture.SetDataHitMap16(pixels);
                _usedIndexLand.Add(g);
                _landDictionary.Add(g, texture);
            }
            return texture;
        }

        protected override void CleanResources()
        {
            throw new NotImplementedException();
        }


        public void Clear()
        {
            for (int i = 0; i < _usedIndex.Count; i++)
            {
                uint g = _usedIndex[i];
                SpriteTexture texture = ResourceDictionary[g];
                texture.Dispose();
                _usedIndex.RemoveAt(i--);
                ResourceDictionary.Remove(g);
            }

            for (int i = 0; i < _usedIndexLand.Count; i++)
            {
                uint g = _usedIndexLand[i];
                SpriteTexture texture = _landDictionary[g];
                texture.Dispose();
                _usedIndexLand.RemoveAt(i--);
                _landDictionary.Remove(g);
            }
        }

        public void ClearUnusedTextures()
        {
            int count = 0;
            long ticks = Engine.Ticks - Constants.CLEAR_TEXTURES_DELAY;

            for (int i = 0; i < _usedIndex.Count; i++)
            {
                uint g = _usedIndex[i];
                SpriteTexture texture = ResourceDictionary[g];

                if (texture.Ticks < ticks)
                {
                    texture.Dispose();
                    _usedIndex.RemoveAt(i--);
                    ResourceDictionary.Remove(g);

                    if (++count >= Constants.MAX_ART_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR)
                        break;
                }
            }

            count = 0;

            for (int i = 0; i < _usedIndexLand.Count; i++)
            {
                uint g = _usedIndexLand[i];
                SpriteTexture texture = _landDictionary[g];

                if (texture.Ticks < ticks)
                {
                    texture.Dispose();
                    _usedIndexLand.RemoveAt(i--);
                    _landDictionary.Remove(g);

                    if (++count >= Constants.MAX_ART_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR)
                        break;
                }
            }
        }

        private unsafe ushort[] ReadStaticArt(ushort graphic, out short width, out short height, out Rectangle imageRectangle)
        {
            (int length, int extra, bool patcher) = _file.SeekByEntryIndex(graphic + 0x4000);
            _file.Skip(4);
            width = _file.ReadShort();
            height = _file.ReadShort();
            imageRectangle = Rectangle.Empty;

            if (width == 0 || height == 0)
                return new ushort[0];
            ushort[] pixels = new ushort[width * height];
            ushort* ptr = (ushort*)_file.PositionAddress;
            ushort* lineoffsets = ptr;
            byte* datastart = (byte*)ptr + height * 2;
            int x = 0;
            int y = 0;
            ptr = (ushort*)(datastart + lineoffsets[0] * 2);
            int minX = width, minY = height, maxX = 0, maxY = 0;

            while (y < height)
            {
                ushort xoffs = *ptr++;
                ushort run = *ptr++;

                if (xoffs + run >= 2048)
                {
                    pixels = new ushort[width * height];

                    return pixels;
                }

                if (xoffs + run != 0)
                {
                    x += xoffs;
                    int pos = y * width + x;

                    for (int j = 0; j < run; j++)
                    {
                        ushort val = *ptr++;


                        if (val > 0)
                            val = (ushort)(0x8000 | val);
                        pixels[pos++] = val;

                        if (val != 0)
                        {
                            minX = Math.Min(minX, x);
                            maxX = Math.Max(maxX, x);
                            minY = Math.Min(minY, y);
                            maxY = Math.Max(maxY, y);
                        }
                    }

                    x += run;
                }
                else
                {
                    x = 0;
                    y++;
                    ptr = (ushort*)(datastart + lineoffsets[y] * 2);
                }
            }

            if (graphic >= 0x2053 && graphic <= 0x2062 || graphic >= 0x206A && graphic <= 0x2079)
            {
                for (int i = 0; i < width; i++)
                {
                    pixels[i] = 0;
                    pixels[(height - 1) * width + i] = 0;
                }

                for (int i = 0; i < height; i++)
                {
                    pixels[i * width] = 0;
                    pixels[i * width + width - 1] = 0;
                }
            }

            //int pos1 = 0;

            //for (y = 0; y < height; y++)
            //    for (x = 0; x < width; x++)
            //    {
            //        if (pixels[pos1++] != 0)
            //        {
            //            minX = Math.Min(minX, x);
            //            maxX = Math.Max(maxX, x);
            //            minY = Math.Min(minY, y);
            //            maxY = Math.Max(maxY, y);
            //        }
            //    }

            //width = (short) (maxX - minX);
            //height = (short)(maxY - minY);
            imageRectangle.X = minX;
            imageRectangle.Y = minY;
            imageRectangle.Width = maxX - minX;
            imageRectangle.Height = maxY - minY;

            return pixels;
        }

        private ushort[] ReadLandArt(ushort graphic)
        {
            graphic &= FileManager.GraphicMask;
            (int length, int extra, bool patcher) = _file.SeekByEntryIndex(graphic);
            ushort[] pixels = new ushort[44 * 44];

            for (int i = 0; i < 22; i++)
            {
                int start = 22 - (i + 1);
                int pos = i * 44 + start;
                int end = start + (i + 1) * 2;

                for (int j = start; j < end; j++)
                {
                    ushort val = _file.ReadUShort();

                    if (val > 0)
                        val = (ushort)(0x8000 | val);
                    pixels[pos++] = val;
                }
            }

            for (int i = 0; i < 22; i++)
            {
                int pos = (i + 22) * 44 + i;
                int end = i + (22 - i) * 2;

                for (int j = i; j < end; j++)
                {
                    ushort val = _file.ReadUShort();

                    if (val > 0)
                        val = (ushort)(0x8000 | val);
                    pixels[pos++] = val;
                }
            }

            return pixels;
        }
    }

}
