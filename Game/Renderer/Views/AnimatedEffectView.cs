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
        private bool _animated;
        private AnimDataFrame _data;
        private Graphic _displayedGraphic = Graphic.Invalid;

        public AnimatedEffectView(in AnimatedItemEffect effect) : base(effect)
        {
            _animated = true;
            _data = AnimData.CalculateCurrentGraphic(effect.Graphic);

            WorldObject.AnimIndex = (sbyte)_data.FrameStart;
            WorldObject.Speed = _data.FrameInterval * 45;
        }

        public new AnimatedItemEffect WorldObject => (AnimatedItemEffect)base.WorldObject;



        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            Graphic graphic = _displayedGraphic;

            if (_animated)
            {
                if (WorldObject.LastChangeFrameTime < World.Ticks)
                {
                    graphic = (Graphic) (WorldObject.Graphic + _data.FrameData[WorldObject.AnimIndex]);
                

                    WorldObject.AnimIndex++;

                    if (WorldObject.AnimIndex >= _data.FrameCount)
                            WorldObject.AnimIndex = (sbyte)_data.FrameStart;

                    WorldObject.LastChangeFrameTime = World.Ticks + WorldObject.Speed;
                }
            }
            else
            {
                graphic = WorldObject.Graphic;
            }


            if (graphic != _displayedGraphic)
            {
                _displayedGraphic = graphic;
                Texture = TextureManager.GetOrCreateStaticTexture(graphic);
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
