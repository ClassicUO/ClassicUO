using System;
using System.Linq;
using ClassicUO.Game.Renderer;
using ClassicUO.UI;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IDrawable = ClassicUO.Game.GameObjects.Interfaces.IDrawable;
using IUpdateable = ClassicUO.Game.GameObjects.Interfaces.IUpdateable;

namespace ClassicUO.Game.Gumps
{
    public abstract class GumpControl : Control, IDrawable, IUpdateable
    {
        protected GumpControl(in GumpControl parent) : base(parent)
        {
            AllowedToDraw = true;
        }


        public bool AllowedToDraw { get; set; }
        [Obsolete("Don't use! Every class that inherits this class has own texture/textures", true)]
        public SpriteTexture Texture { get; set; }
        public Vector3 HueVector { get; set; }



        public virtual void Update(in double frameMS)
        {
            if (IsDisposed)
                return;

            foreach (Control c in Children)
            {
                if (c is GumpControl gump)
                    gump.Update(frameMS);
                else
                    Log.Message(LogTypes.Warning, $"{c} is not a GumpControl!!");
            }
        }

        public virtual bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (IsDisposed)
                return false;

            foreach (Control c in Children)
            {
                if (c is GumpControl gump)
                {
                    if (gump.IsVisible)
                    {
                        Vector3 offset = new Vector3(gump.X + position.X, gump.Y + position.Y, position.Z);
                        gump.Draw(spriteBatch, offset);
                    }
                }
                else
                    Log.Message(LogTypes.Warning, $"{c} is not a GumpControl!!");
            }

            return true;
        }

        public override void Dispose()
        {        
            for (int i = 0; i < Children.Count; i++)
            {
                var c = Children[i];                
                c.Dispose();              
            }

            base.Dispose();
        }
    }
}