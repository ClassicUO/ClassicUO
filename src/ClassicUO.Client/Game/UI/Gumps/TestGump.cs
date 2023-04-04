using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace ClassicUO.Game.UI.Gumps
{
    internal class TestGump : Gump
    {
        private Texture2D image = PNGLoader.Instance.GetImageTexture(Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "tazuo.png"));

        public TestGump() : base(0, 0)
        {
            Width = 512;
            Height = 512;
            X = 200;
            Y = 200;
            CanCloseWithRightClick = true;
            CanMove = true;
            AcceptMouseInput = true;
            //Add(new HitBox(0, 0, Width, Height, null, 0f) { AcceptMouseInput = true, CanMove = true });
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (image != null)
            {
                batcher.Draw(
                    image,
                    new Rectangle(x, y, image.Bounds.Width, image.Bounds.Height),
                    new Vector3(0, 0, 1)
                    );
            }
            base.Draw(batcher, x, y);
            return true;
        }
    }
}
