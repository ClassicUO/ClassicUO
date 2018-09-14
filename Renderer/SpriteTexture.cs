using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    public class SpriteTexture : Texture2D
    {
        private readonly byte[] _hitMap;

        public SpriteTexture(int width, int height, bool is32bit = true) : base(Service.Get<SpriteBatch3D>().GraphicsDevice, width, height, false, is32bit ? SurfaceFormat.Color : SurfaceFormat.Bgra5551)
        {
            _hitMap = new byte[width * height];
        }

        public long Ticks { get; set; }

        public virtual bool Contains(int x, int y, bool checkpixel = true)
        {
            if (x >= 0 && y >= 0 && x < Width && y < Height)
            {
                if (!checkpixel)
                    return true;
                int pos = (y * Width) + x;

                if (pos < _hitMap.Length)
                    return _hitMap[pos] != 0;
            }
            return false;
        }

        public void SetDataForHitBox(uint[] data)
        {
            SetData(data);
            int pos = 0;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _hitMap[pos] = (byte)(data[pos] != 0 ? 1 : 0);
                    pos++;
                }
            }
        }

        public void SetDataForHitBox(ushort[] data)
        {
            SetData(data);
            int pos = 0;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _hitMap[pos] = (byte)(data[pos] != 0 ? 1 : 0);
                    pos++;
                }
            }
        }

    }

}
