using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.WorldObjects.Views
{
    public class MobileView : WorldRenderObject
    {
        public MobileView(in Mobile mobile) : base(mobile)
        {
            Texture = TextureManager.GetOrCreateStaticTexture(567);
            Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + (WorldObject.Position.Z * 4), Texture.Width, Texture.Height);
        }

        public sbyte AnimIndex { get; set; }

        public new Mobile WorldObject => (Mobile)base.WorldObject;




        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {

            bool mirror = false;
            byte dir = (byte)WorldObject.Direction;
            
            AssetsLoader.Animations.GetAnimDirection(ref dir, ref mirror);

            // var direction = AssetsLoader.Animations.DataIndex[WorldObject.Graphic].Groups[]


            WorldObject.ProcessAnimation();

            return base.Draw(spriteBatch, position);
        }

        public override void Update(in double frameMS)
        {
            //WorldObject.DoMovements(frameMS);


            base.Update(frameMS);
        }
    }
}
