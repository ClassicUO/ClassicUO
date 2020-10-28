using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal class UOTexture : Texture2D
    {
        public UOTexture(int width, int height)
            : base(Client.Game.GraphicsDevice, width, height, false, SurfaceFormat.Color)
        {
            Ticks = Time.Ticks + 3000;
        }

        public long Ticks { get; set; }
        public uint[] Data { get; private set; }

        public void PushData(uint[] data)
        {
            Data = data;
            SetData(data);
        }

        public bool Contains(int x, int y, bool pixelCheck = true)
        {
            if (Data != null && x >= 0 && y >= 0 && x < Width && y < Height)
            {
                if (!pixelCheck)
                {
                    return true;
                }

                int pos = y * Width + x;

                if (pos < Data.Length)
                {
                    return Data[pos] != 0;
                }
            }

            return false;
        }
    }
}