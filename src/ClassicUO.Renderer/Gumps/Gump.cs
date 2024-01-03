using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Gumps
{
    public sealed class Gump
    {
        private readonly TextureAtlas _atlas;
        private readonly SpriteInfo[] _spriteInfos;
        private readonly PixelPicker _picker = new PixelPicker();

        public Gump(GraphicsDevice device)
        {
            _atlas = new TextureAtlas(device, 4096, 4096, SurfaceFormat.Color);
            _spriteInfos = new SpriteInfo[GumpsLoader.Instance.Entries.Length];
        }

        public ref readonly SpriteInfo GetGump(uint idx)
        {
            if (idx >= _spriteInfos.Length)
                return ref SpriteInfo.Empty;

            ref var spriteInfo = ref _spriteInfos[idx];

            if (spriteInfo.Texture == null)
            {
                var gumpInfo = PNGLoader.Instance.LoadGumpTexture(idx);

                if (gumpInfo.Pixels == null || gumpInfo.Pixels.IsEmpty)
                {
                    gumpInfo = GumpsLoader.Instance.GetGump(idx);
                }
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

        public bool PixelCheck(uint idx, int x, int y, double scale = 1f) => _picker.Get(idx, x, y, scale: scale);
    }
}
