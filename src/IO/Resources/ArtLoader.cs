using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.IO.Resources
{
    internal class ArtLoader : ResourceLoader<ArtTexture>
    {
        private UOFile _file;
        private readonly Dictionary<uint, SpriteTexture> _landDictionary = new Dictionary<uint, SpriteTexture>();

        private static readonly ushort[] _empty = { };

        public override void Load()
        {
            string filepath = Path.Combine(FileManager.UoFolderPath, "artLegacyMUL.uop");

            if (File.Exists(filepath))
                _file = new UOFileUop(filepath, ".tga", Constants.MAX_STATIC_DATA_INDEX_COUNT);
            else
            {
                filepath = Path.Combine(FileManager.UoFolderPath, "art.mul");
                string idxpath = Path.Combine(FileManager.UoFolderPath, "artidx.mul");

                if (File.Exists(filepath) && File.Exists(idxpath))
                    _file = new UOFileMul(filepath, idxpath, Constants.MAX_STATIC_DATA_INDEX_COUNT);
            }
        }

        public override ArtTexture GetTexture(uint g)
        {
            if (!ResourceDictionary.TryGetValue(g, out ArtTexture texture) || texture.IsDisposed)
            {
                ushort[] pixels = ReadStaticArt((ushort) g, out short w, out short h, out Rectangle imageRectangle);
                texture = new ArtTexture(imageRectangle, w, h);
                texture.SetDataHitMap16(pixels);
                ResourceDictionary.Add(g, texture);
            }
            //else
            //    texture.Ticks = Engine.Ticks + 3000;
            return texture;
        }

        public SpriteTexture GetLandTexture(uint g)
        {
            if (!_landDictionary.TryGetValue(g, out SpriteTexture texture) || texture.IsDisposed)
            {
                const int SIZE = 44;
                ushort[] pixels = ReadLandArt((ushort) g);
                texture = new SpriteTexture(SIZE, SIZE, false);
                texture.SetDataHitMap16(pixels);
                _landDictionary.Add(g, texture);
            }
            //else
            //    texture.Ticks = Engine.Ticks + 3000;
            return texture;
        }

        public override bool TryGetEntryInfo(int entry, out long address, out long size, out long compressedsize)
        {
            entry += 0x4000;

            if (entry < _file.Length && entry >= 0)
            {
                UOFileIndex3D e = _file.Entries[entry];

                address = _file.StartAddress.ToInt64() + e.Offset;
                size = e.DecompressedLength == 0 ? e.Length : e.DecompressedLength;
                compressedsize = e.Length;
                return true;
            }

            return base.TryGetEntryInfo(entry, out address, out size, out compressedsize);
        }

        public override void CleanResources()
        {
            ResourceDictionary.ToList().ForEach( s =>
            {
                s.Value.Dispose();
                ResourceDictionary.Remove(s.Key);
            });

            _landDictionary.ToList().ForEach(s =>
            {
                s.Value.Dispose();
                _landDictionary.Remove(s.Key);
            });
        }

        public override void CleaUnusedResources()
        {
            base.CleaUnusedResources();

            long ticks = Engine.Ticks - Constants.CLEAR_TEXTURES_DELAY;

            _landDictionary
               .Where(s => s.Value.Ticks < ticks)
               .Take(Constants.MAX_ART_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR)
               .ToList()
               .ForEach(s =>
                {
                    s.Value.Dispose();
                    _landDictionary.Remove(s.Key);
                });
        }

        public unsafe ushort[] ReadStaticArt(ushort graphic, out short width, out short height, out Rectangle imageRectangle)
        {
            //if (Engine.Profile != null && Engine.Profile.Current != null && Engine.Profile.Current.TreeToStumps && StaticFilters.IsTree(graphic))
            //{
            //    width = 47;
            //    height = 64;
            //    imageRectangle = Rectangle.Empty;

            //    ushort[] pixels = new ushort[width * height];

            //    fixed (ushort* srcPtr = StaticFilters.StumpsData)
            //        fixed (ushort* pixelsPtr = pixels)
            //            Buffer.MemoryCopy(srcPtr, pixelsPtr, pixels.Length * 2, pixels.Length);

            //    return pixels;
            //}
            //else

            {

                imageRectangle = Rectangle.Empty;

                (int length, int extra, bool patcher) = _file.SeekByEntryIndex(graphic + 0x4000);

                if (length == 0)
                {
                    width = height = 0;
                    return _empty;
                }

                _file.Skip(4);
                width = _file.ReadShort();
                height = _file.ReadShort();

                if (width == 0 || height == 0)
                    return _empty;

                ushort[] pixels = new ushort[width * height];
                ushort* ptr = (ushort*) _file.PositionAddress;
                ushort* lineoffsets = ptr;
                byte* datastart = (byte*) ptr + height * 2;
                int x = 0;
                int y = 0;
                ptr = (ushort*) (datastart + lineoffsets[0] * 2);
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

                            if (val != 0)
                                val = (ushort) (0x8000 | val);
                            pixels[pos++] = val;
                        }

                        x += run;
                    }
                    else
                    {
                        x = 0;
                        y++;
                        ptr = (ushort*) (datastart + lineoffsets[y] * 2);
                    }
                }

                if ((graphic >= 0x2053 && graphic <= 0x2062) || (graphic >= 0x206A && graphic <= 0x2079))
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
                else if (StaticFilters.IsCave(graphic) && Engine.Profile.Current != null && Engine.Profile.Current.EnableCaveBorder)
                {
                    for (int yy = 0; yy < height; yy++)
                    {
                        int startY = (yy != 0 ? -1 : 0);
                        int endY = (yy + 1 < height ? 2 : 1);

                        for (int xx = 0; xx < width; xx++)
                        {
                            ref var pixel = ref pixels[yy * width + xx];

                            if (pixel == 0)
                                continue;

                            int startX = (xx != 0 ? -1 : 0);
                            int endX = (xx + 1 < width ? 2 : 1);

                            for (int i = startY; i < endY; i++)
                            {
                                int currentY = yy + i;

                                for (int j = startX; j < endX; j++)
                                {
                                    int currentX = (int)xx + (int)j;

                                    ref var currentPixel = ref pixels[currentY * width + currentX];

                                    if (currentPixel == 0u)
                                    {
                                        pixel = 0x8000;
                                    }
                                }
                            }
                        }
                    }

                }

                int pos1 = 0;

                for (y = 0; y < height; y++)
                {
                    for (x = 0; x < width; x++)
                    {
                        if (pixels[pos1++] != 0)
                        {
                            minX = Math.Min(minX, x);
                            maxX = Math.Max(maxX, x);
                            minY = Math.Min(minY, y);
                            maxY = Math.Max(maxY, y);
                        }
                    }
                }

                imageRectangle.X = minX;
                imageRectangle.Y = minY;
                imageRectangle.Width = maxX - minX;
                imageRectangle.Height = maxY - minY;

                return pixels;
            }
        }

        private ushort[] ReadLandArt(ushort graphic)
        {
            graphic &= FileManager.GraphicMask;
            (int length, int extra, bool patcher) = _file.SeekByEntryIndex(graphic);

            if (length == 0)
                return new ushort[44 * 44];

            ushort[] pixels = new ushort[44 * 44];

            for (int i = 0; i < 22; i++)
            {
                int start = 22 - (i + 1);
                int pos = i * 44 + start;
                int end = start + (i + 1) * 2;

                for (int j = start; j < end; j++)
                {
                    ushort val = _file.ReadUShort();

                    if (val != 0)
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

                    if (val != 0)
                        val = (ushort)(0x8000 | val);
                    pixels[pos++] = val;
                }
            }

            return pixels;
        }
    }

}
