using ClassicUO.Assets;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;
using System;

namespace ClassicUO.Renderer.Arts
{
    public sealed class Art
    {
        private readonly SpriteInfo[] _spriteInfos;
        private readonly TextureAtlas _atlas;
        private readonly PixelPicker _picker = new PixelPicker();
        private readonly Rectangle[] _realArtBounds;
        private readonly ArtLoader _artLoader;
        private readonly HuesLoader _huesLoader;

        public Art(ArtLoader artLoader, HuesLoader huesLoader, GraphicsDevice device)
        {
            _artLoader = artLoader;
            _huesLoader = huesLoader;
            _atlas = new TextureAtlas(device, 4096, 4096, SurfaceFormat.Color);
            _spriteInfos = new SpriteInfo[_artLoader.File.Entries.Length];
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
                var artInfo = _artLoader.GetArt(idx);

                if (artInfo.Pixels.IsEmpty && idx > 0)
                {
                    // Trying to load a texture that does not exist in the client MULs
                    // Degrading gracefully and only crash if not even the fallback ItemID exists
                    Log.Error(
                        $"Texture not found for sprite: idx: {idx}; itemid: {(idx > 0x4000 ? idx - 0x4000 : '-')}"
                    );
                    return ref Get(0); // ItemID of "UNUSED" placeholder
                }

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

            return ref spriteInfo;
        }

        public unsafe IntPtr CreateCursorSurfacePtr(
            int index,
            ushort customHue,
            out int hotX,
            out int hotY,
            float dpiScale
        )
        {
            hotX = hotY = 0;

            var artInfo = _artLoader.GetArt((uint)(index + 0x4000));

            if (artInfo.Pixels.IsEmpty)
            {
                return IntPtr.Zero;
            }

            fixed (uint* ptr = artInfo.Pixels)
            {
                SDL.SDL_Surface* surface = (SDL.SDL_Surface*)
                    SDL.SDL_CreateSurfaceFrom(
                        artInfo.Width,
                        artInfo.Height,
                        SDL.SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888,
                        (IntPtr)ptr,
                        4 * artInfo.Width);

                int width = artInfo.Width;
                int height = artInfo.Height;

                if (dpiScale != 1f)
                {
                    width = (int)(artInfo.Width * dpiScale);
                    height = (int)(artInfo.Height * dpiScale);

                    SDL.SDL_Surface* newSurface = (SDL.SDL_Surface*)SDL.SDL_ScaleSurface(
                        (nint)surface,
                        width,
                        height,
                        SDL.SDL_ScaleMode.SDL_SCALEMODE_NEAREST);

                    SDL.SDL_DestroySurface((nint)surface);
                    surface = newSurface;
                }

                int stride = surface->pitch >> 2;
                uint* pixels_ptr = (uint*)surface->pixels;
                uint* p_line_end = pixels_ptr + width;
                uint* p_img_end = pixels_ptr + stride * height;
                int delta = stride - width;
                short curX = 0;
                short curY = 0;
                Color c = default;

                int dpiScaleInt = (int)Math.Ceiling(dpiScale);

                while (pixels_ptr < p_img_end)
                {
                    curX = 0;

                    while (pixels_ptr < p_line_end)
                    {
                        if (*pixels_ptr != 0 && *pixels_ptr != 0xFF_00_00_00)
                        {
                            if (curX >= width - dpiScaleInt || curY >= height - dpiScaleInt)
                            {
                                *pixels_ptr = 0;
                            }
                            else if (curX < dpiScaleInt || curY < dpiScaleInt)
                            {
                                if (*pixels_ptr == 0xFF_00_FF_00)
                                {
                                    if (curX < dpiScaleInt)
                                    {
                                        hotY = curY;
                                    }

                                    if (curY < dpiScaleInt)
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
                                    HuesHelper.Color16To32(
                                        _huesLoader.GetColor16(
                                            HuesHelper.ColorToHue(c),
                                            customHue
                                        )
                                    ) | 0xFF_00_00_00;
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
