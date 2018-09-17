using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls.InGame
{
    public class WorldViewportGump : Gump
    {
        private int _worldWidth = 800, _worldHeight = 600;
        private WorldViewport _viewport;
        private Scenes.GameScene _scene;

        public WorldViewportGump(Scenes.GameScene scene) : base(0, 0)
        {
            AcceptMouseInput = false;
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            ControlInfo.Layer = UILayer.Under;

            X = 0; Y = 0;

            _scene = scene;

            OnResize();
        }


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }


        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position)
        {
            return base.Draw(spriteBatch, position);
        }

        protected override void OnMove()
        {
            base.OnMove();
        }

        private void OnResize()
        {
            Clear();

            Width = _worldWidth;
            Height = _worldHeight;

            AddChildren(_viewport = new WorldViewport(_scene, 0, 0, Width, Height));
        }
    }
}
