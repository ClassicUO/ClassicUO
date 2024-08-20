using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Gumps
{
    public sealed class Gump
    {
        private readonly TextureAtlas _atlas;
        private readonly SpriteInfo[] _spriteInfos;
        private readonly PixelPicker _picker = new PixelPicker();
        private readonly GumpsLoader _gumpsLoader;

        public Gump(GumpsLoader gumpsLoader, GraphicsDevice device)
        {
            _gumpsLoader = gumpsLoader;
            _atlas = new TextureAtlas(device, 4096, 4096, SurfaceFormat.Color);
            _spriteInfos = new SpriteInfo[gumpsLoader.File.Entries.Length];
        }

        public ref readonly SpriteInfo GetGump(uint idx)
        {
            if (idx >= _spriteInfos.Length)
                return ref SpriteInfo.Empty;

            ref var spriteInfo = ref _spriteInfos[idx];

            if (spriteInfo.Texture == null)
            {
                var gumpInfo = _gumpsLoader.GetGump(idx);
                if (!gumpInfo.Pixels.IsEmpty)
                {
                    spriteInfo.Texture = _atlas.AddSprite(
                        gumpInfo.Pixels,
                        gumpInfo.Width,
                        gumpInfo.Height,
                        out spriteInfo.UV
                    );

                    _picker.Set(idx, gumpInfo.Width, gumpInfo.Height, gumpInfo.Pixels);
                }
            }

            return ref spriteInfo;
        }

        public bool PixelCheck(uint idx, int x, int y) => _picker.Get(idx, x, y);
    }
}
