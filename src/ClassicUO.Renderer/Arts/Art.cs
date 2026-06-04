using ClassicUO.Assets;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;
using System;
using System.Buffers;

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
        private readonly GraphicsDevice _device;

        public Art(ArtLoader artLoader, HuesLoader huesLoader, GraphicsDevice device)
        {
            _artLoader = artLoader;
            _huesLoader = huesLoader;
            _device = device;
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

            int srcWidth = artInfo.Width;
            int srcHeight = artInfo.Height;

            // Make a copy of pixels to avoid modifying the original
            var rentedBuffer = ArrayPool<uint>.Shared.Rent(artInfo.Pixels.Length);
            try
            {
                var pixelsCopy = rentedBuffer.AsSpan(0, artInfo.Pixels.Length);
                artInfo.Pixels.CopyTo(pixelsCopy);

                // Process the copy: find hotX/Y and clear marker pixels
                for (int y = 0; y < srcHeight; y++)
                {
                    for (int x = 0; x < srcWidth; x++)
                    {
                        int idx = y * srcWidth + x;
                        uint pixel = pixelsCopy[idx];

                        if (pixel == 0)
                            continue;

                        // Clear black marker pixels
                        if (pixel == 0xFF_00_00_00)
                        {
                            pixelsCopy[idx] = 0;
                            continue;
                        }

                        // Check for green hotspot marker in first row/column
                        if (pixel == 0xFF_00_FF_00)
                        {
                            if (x == 0)
                                hotY = y;
                            if (y == 0)
                                hotX = x;
                            pixelsCopy[idx] = 0;
                            continue;
                        }

                        // Clear edge pixels (first/last row and column)
                        if (x == 0 || y == 0 || x == srcWidth - 1 || y == srcHeight - 1)
                        {
                            pixelsCopy[idx] = 0;
                            continue;
                        }

                        // Apply custom hue if needed
                        if (customHue > 0)
                        {
                            Color c = default;
                            c.PackedValue = pixel;
                            pixelsCopy[idx] = HuesHelper.Color16To32(
                                _huesLoader.GetColor16(
                                    HuesHelper.ColorToHue(c),
                                    customHue
                                )
                            ) | 0xFF_00_00_00;
                        }
                    }
                }

                // Scale hotX/Y by dpiScale
                hotX = (int)(hotX * dpiScale);
                hotY = (int)(hotY * dpiScale);

                // Now create the surface from cleaned pixels
                fixed (uint* ptr = pixelsCopy)
                {
                    SDL.SDL_Surface* surface = (SDL.SDL_Surface*)
                        SDL.SDL_CreateSurfaceFrom(
                            srcWidth,
                            srcHeight,
                            SDL.SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888,
                            (IntPtr)ptr,
                            4 * srcWidth);

                    if (dpiScale != 1f)
                    {
                        int width = (int)(srcWidth * dpiScale);
                        int height = (int)(srcHeight * dpiScale);

                        SDL.SDL_Surface* newSurface = (SDL.SDL_Surface*)SDL.SDL_ScaleSurface(
                            (nint)surface,
                            width,
                            height,
                            SDL.SDL_ScaleMode.SDL_SCALEMODE_NEAREST);

                        SDL.SDL_DestroySurface((nint)surface);
                        surface = newSurface;
                    }

                    return (IntPtr)surface;
                }
            }
            finally
            {
                ArrayPool<uint>.Shared.Return(rentedBuffer);
            } 
        }

        // Software-cursor twin of CreateCursorSurfacePtr (the SDL hardware-cursor
        // path): returns a standalone Texture2D with the cursor art's marker
        // pixels stripped, plus the hotspot. Drawing the atlas sub-rect directly
        // can't strip these — the markers are baked into the shared atlas — so the
        // ECS software cursor (GameCursorPlugin) uses this cleaned texture instead
        // of a 1px UV inset, which only catches markers on the literal outer ring
        // and leaves a stray blue/green edge line when the art has transparent
        // padding around the marker ring. Cleared exactly like the SDL path:
        // black markers (0xFF000000) and green hotspot markers (0xFF00FF00)
        // anywhere, plus the whole first/last row and column. customHue is baked
        // in when non-zero (felucca tint); 0 leaves the art untinted for the
        // shader to hue. Caller owns the returned texture (cache + dispose).
        public Texture2D CreateCursorTexture(int index, ushort customHue, out int hotX, out int hotY)
        {
            hotX = hotY = 0;

            var artInfo = _artLoader.GetArt((uint)(index + 0x4000));
            if (artInfo.Pixels.IsEmpty)
                return null;

            int w = artInfo.Width;
            int h = artInfo.Height;
            var buf = new uint[w * h];
            artInfo.Pixels.CopyTo(buf);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    uint pixel = buf[idx];

                    if (pixel == 0)
                        continue;

                    if (pixel == 0xFF_00_00_00)
                    {
                        buf[idx] = 0;
                        continue;
                    }

                    if (pixel == 0xFF_00_FF_00)
                    {
                        if (x == 0) hotY = y;
                        if (y == 0) hotX = x;
                        buf[idx] = 0;
                        continue;
                    }

                    if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
                    {
                        buf[idx] = 0;
                        continue;
                    }

                    if (customHue > 0)
                    {
                        Color c = default;
                        c.PackedValue = pixel;
                        buf[idx] = HuesHelper.Color16To32(
                            _huesLoader.GetColor16(HuesHelper.ColorToHue(c), customHue)
                        ) | 0xFF_00_00_00;
                    }
                }
            }

            var tex = new Texture2D(_device, w, h, false, SurfaceFormat.Color);
            tex.SetData(buf);
            return tex;
        }

        // Cursor hotspot: UO cursor art encodes its click point as a green
        // marker pixel (0xFF00FF00 ABGR) in the first row (gives hotX) and first
        // column (gives hotY). Mirrors the scan in CreateCursorSurfacePtr but
        // returns only the offset — the software cursor (ECS GameCursorPlugin)
        // draws the sprite at mouse-pos minus this hotspot so the tip aligns.
        public void GetCursorHotspot(uint idx, out int hotX, out int hotY)
        {
            hotX = hotY = 0;

            var artInfo = _artLoader.GetArt(idx + 0x4000);
            if (artInfo.Pixels.IsEmpty)
                return;

            int w = artInfo.Width;
            int h = artInfo.Height;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (artInfo.Pixels[y * w + x] != 0xFF_00_FF_00)
                        continue;

                    if (x == 0) hotY = y;
                    if (y == 0) hotX = x;
                }
            }
        }

        public Rectangle GetRealArtBounds(uint idx) =>
            idx < 0 || idx >= _realArtBounds.Length
                ? Rectangle.Empty
                : _realArtBounds[idx];

        public bool PixelCheck(uint idx, int x, int y) => _picker.Get(idx, x, y);
    }
}
