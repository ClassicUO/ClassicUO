using ClassicUO.Game.Renderer;
using ClassicUO.UI;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Gumps
{
    public abstract class GumpControl : Control
    {
        protected GumpControl(in GumpControl parent, in ushort graphic) : base(parent)
        {
            Texture = TextureManager.GetOrCreateGumpTexture(graphic);
        }


        public Texture2D Texture { get; }


        public virtual void Update()
        {
            foreach (Control c in Children)
            {
                if (c is GumpControl gump)
                    gump.Update();
                else
                    Log.Message(LogTypes.Warning, $"{c} is not a GumpControl!!");
            }
        }

        public virtual void Draw(in SpriteBatch3D spriteBatch, in Point position)
        {
            foreach (Control c in Children)
            {
                if (c is GumpControl gump)
                {
                    if (gump.IsVisible)
                    {
                        Point offset = new Point(gump.X + position.X, gump.Y + position.Y);
                        gump.Draw(spriteBatch, offset);
                    }
                }
                else
                    Log.Message(LogTypes.Warning, $"{c} is not a GumpControl!!");
            }
        }
    }
}