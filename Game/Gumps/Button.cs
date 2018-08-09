using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Renderer;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class Button : GumpControl
    {

        private const int NORMAL = 0;
        private const int PRESSED = 1;
        private const int OVER = 2;

        private readonly SpriteTexture[] _textures = new SpriteTexture[3];
        private int _curentState = NORMAL;


        public Button(in GumpControl parent, in ushort normal, in ushort pressed, in ushort over = 0) : base(parent)
        {
            _textures[NORMAL] = TextureManager.GetOrCreateGumpTexture(normal);
            _textures[PRESSED] = TextureManager.GetOrCreateGumpTexture(pressed);
            if (over > 0)
                _textures[OVER] = TextureManager.GetOrCreateGumpTexture(over);

            ref var t = ref _textures[NORMAL];

            Rectangle = t.Bounds;
        }


        public override void Update(in double frameMS)
        {

            base.Update(in frameMS);
        }

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            return base.Draw(in spriteBatch, in position);
        }

        public override void OnMouseMove(in MouseEventArgs e)
        {
            base.OnMouseMove(in e);
        }
    }
}
