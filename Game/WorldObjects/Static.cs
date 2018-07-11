using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.WorldObjects.Views;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.WorldObjects
{
    public class Static : WorldObject
    {
        public Static(Graphic tileID, Hue hue, int index)
        {
            TileID = tileID; Hue = hue; Index = index;
        }

        public Graphic TileID { get; }
        public int Index { get; }
        public override Position Position { get; set; }
        public new StaticView ViewObject => (StaticView)base.ViewObject;

        protected override WorldRenderObject CreateView() => new StaticView(this);
    }  
}
