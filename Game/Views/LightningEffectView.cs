using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    class LightningEffectView : View
    {
        private static readonly Point[] _offsets = {
            new Point(48, 0),
            new Point(68, 0),
            new Point(92, 0),
            new Point(72, 0),
            new Point(48, 0),
            new Point(56, 0),
            new Point(76, 0),
            new Point(76, 0),
            new Point(92, 0),
            new Point(80, 0)
        };

        private Graphic _displayedGraphic = Graphic.Invalid;

        public LightningEffectView(LightningEffect effect) : base(effect)
        {

        }

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList list)
        {
            PreDraw(position);

            return base.DrawInternal(spriteBatch, position, list);
        }

        public override bool DrawInternal(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList objectList)
        {
            LightningEffect effect = (LightningEffect) GameObject;

            if (effect.AnimationGraphic != _displayedGraphic || Texture == null || Texture.IsDisposed)
            {
                if (effect.AnimationGraphic > 0x4E29)
                    return false;

                _displayedGraphic = effect.AnimationGraphic;
                Texture = Art.GetStaticTexture(effect.AnimationGraphic);
                Point offset = _offsets[_displayedGraphic - 20000];
                Bounds = new Rectangle(offset.X, Texture.Height - 33 + (effect.Position.Z * 4)+ offset.Y, Texture.Width, Texture.Height);
            }

            HueVector = RenderExtentions.GetHueVector(effect.Hue);

            return base.Draw(spriteBatch, position, objectList);
        }
    }
}
