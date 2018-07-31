using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.WorldObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class AnimatedEffectView : View
    {
        private Graphic _displayedGraphic = Graphic.Invalid;

        public AnimatedEffectView(in AnimatedItemEffect effect) : base(effect)
        {
        }

        public new AnimatedItemEffect WorldObject => (AnimatedItemEffect)base.WorldObject;



        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            PreDraw(position);
            return DrawInternal(spriteBatch, position);
        }

        public override bool DrawInternal(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (WorldObject.AnimationGraphic != _displayedGraphic)
            {
                _displayedGraphic = WorldObject.AnimationGraphic;
                Texture = TextureManager.GetOrCreateStaticTexture(WorldObject.AnimationGraphic);
                Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + WorldObject.Position.Z * 4, Texture.Width, Texture.Height);
            }

            HueVector = RenderExtentions.GetHueVector(WorldObject.Hue);

            return base.Draw(in spriteBatch, in position);
        }

        public override void Update(in double frameMS)
        {
            WorldObject.UpdateAnimation(frameMS);
        }
    }
}
