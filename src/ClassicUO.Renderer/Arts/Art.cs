using System;
using ClassicUO.Assets;
using ClassicUO.Utility;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;

namespace ClassicUO.Renderer.Arts
{
    public sealed class Art
    {
        private readonly SpriteInfo[] _spriteInfos;
        private readonly TextureAtlas _atlas;
        private readonly PixelPicker _picker = new PixelPicker();
        private readonly Rectangle[] _realArtBounds;

        public Art(GraphicsDevice device)
        {
            _atlas = new TextureAtlas(device, 4096, 4096, SurfaceFormat.Color);
            _spriteInfos = new SpriteInfo[ArtLoader.Instance.Entries.Length];
            _realArtBounds = new Rectangle[_spriteInfos.Length];
        }

        public ref readonly SpriteInfo GetLand(uint idx)
            => ref Get((uint)(idx & ~0x4000));

        public ref readonly SpriteInfo GetArt(uint idx)
            => ref Get(idx + 0x4000);

        private ref readonly SpriteInfo Get(uint idx)
        {
            if (idx >= _spriteInfos.Length)
                return ref SpriteInfo.Empty;

            ref var spriteInfo = ref _spriteInfos[idx];

            if (spriteInfo.Texture == null)
            {
                ArtInfo artInfo = PNGLoader.Instance.LoadArtTexture(idx);

                if (artInfo.Pixels == null || artInfo.Pixels.IsEmpty)
                {
                    artInfo = ArtLoader.Instance.GetArt(idx);
                }
                if (!artInfo.Pixels.IsEmpty)
                {
                    spriteInfo.Texture = _atlas.AddSprite(
                        artInfo.Pixels,
                        artInfo.Width,
                        artInfo.Height,
                        out spriteInfo.UV
                    );

                    if (idx > 0x4000)
                    {
                        idx -= 0x4000;
                        _picker.Set(idx, artInfo.Width, artInfo.Height, artInfo.Pixels);

                        var pos1 = 0;
                        int minX = artInfo.Width,
                            minY = artInfo.Height,
                            maxX = 0,
                            maxY = 0;

                        for (int y = 0; y < artInfo.Height; ++y)
                        {
                            for (int x = 0; x < artInfo.Width; ++x)
                            {
                                if (artInfo.Pixels[pos1++] != 0)
                                {
                                    minX = Math.Min(minX, x);
                                    maxX = Math.Max(maxX, x);
                                    minY = Math.Min(minY, y);
                                    maxY = Math.Max(maxY, y);
                                }
                            }
                        }

                        _realArtBounds[idx] = new Rectangle(minX, minY, maxX - minX, maxY - minY);
                    }
                }
            }

            return ref spriteInfo;
        }

        public unsafe IntPtr CreateCursorSurfacePtr(
            int index,
            ushort customHue,
            out int hotX,
            out int hotY
        )
        {
            hotX = hotY = 0;

            var artInfo = ArtLoader.Instance.GetArt((uint)(index + 0x4000));

            if (artInfo.Pixels.IsEmpty)
            {
                return IntPtr.Zero;
            }

            fixed (uint* ptr = artInfo.Pixels)
            {
                SDL.SDL_Surface* surface = (SDL.SDL_Surface*)
                    SDL.SDL_CreateRGBSurfaceWithFormatFrom(
                        (IntPtr)ptr,
                        artInfo.Width,
                        artInfo.Height,
                        32,
                        4 * artInfo.Width,
                        SDL.SDL_PIXELFORMAT_ABGR8888
                    );

                int stride = surface->pitch >> 2;
                uint* pixels_ptr = (uint*)surface->pixels;
                uint* p_line_end = pixels_ptr + artInfo.Width;
                uint* p_img_end = pixels_ptr + stride * artInfo.Height;
                int delta = stride - artInfo.Width;
                short curX = 0;
                short curY = 0;
                Color c = default;

                while (pixels_ptr < p_img_end)
                {
                    curX = 0;

                    while (pixels_ptr < p_line_end)
                    {
                        if (*pixels_ptr != 0 && *pixels_ptr != 0xFF_00_00_00)
                        {
                            if (curX >= artInfo.Width - 1 || curY >= artInfo.Height - 1)
                            {
                                *pixels_ptr = 0;
                            }
                            else if (curX == 0 || curY == 0)
                            {
                                if (*pixels_ptr == 0xFF_00_FF_00)
                                {
                                    if (curX == 0)
                                    {
                                        hotY = curY;
                                    }

                                    if (curY == 0)
                                    {
                                        hotX = curX;
                                    }
                                }

                                *pixels_ptr = 0;
                            }
                            else if (customHue > 0)
                            {
                                c.PackedValue = *pixels_ptr;
                                *pixels_ptr =
                                    HuesLoader.Instance.ApplyHueRgba8888(HuesHelper.Color32To16(*pixels_ptr), customHue);
                            }
                        }

                        ++pixels_ptr;

                        ++curX;
                    }

                    pixels_ptr += delta;
                    p_line_end += stride;

                    ++curY;
                }

                return (IntPtr)surface;
            }
        }

        public Rectangle GetRealArtBounds(uint idx) =>
            idx < 0 || idx >= _realArtBounds.Length
                ? Rectangle.Empty
                : _realArtBounds[idx];

        public bool PixelCheck(uint idx, int x, int y) => _picker.Get(idx, x, y);
    }
}
