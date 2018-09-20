using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls.InGame
{
    public class WorldViewportGump : Gump
    {
        private int _worldWidth = 800, _worldHeight = 600;
        private WorldViewport _viewport;
        private ChatControl _chatControl;
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


        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            return base.Draw(spriteBatch, position, hue);
        }

        protected override void OnMove()
        {
            base.OnMove();
        }

        private void OnResize()
        {
            if (Service.Has<ChatControl>())
                Service.Unregister<ChatControl>();

            Clear();

            Width = _worldWidth;
            Height = _worldHeight;

            AddChildren(_viewport = new WorldViewport(_scene, 0, 0, Width, Height));
            AddChildren(_chatControl = new ChatControl(0, 0, Width, Height));

            Service.Register(_chatControl);
        }
    }
}
