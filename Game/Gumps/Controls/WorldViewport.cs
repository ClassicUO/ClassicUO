using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    public class WorldViewport : GumpControl
    {
        private readonly GameScene _scene;
        private Rectangle _rect;

        public WorldViewport(GameScene scene, int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _scene = scene;
            AcceptMouseInput = true;
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            _rect.X = (int) position.X;
            _rect.Y = (int) position.Y;
            _rect.Width = Width;
            _rect.Height = Height;

            spriteBatch.Draw2D(_scene.ViewportTexture, _rect, Vector3.Zero);

            return base.Draw(spriteBatch, position, hue);
        }


        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            UIManager.KeyboardFocusControl = Service.Get<ChatControl>().GetFirstControlAcceptKeyboardInput();
        }
    }
}