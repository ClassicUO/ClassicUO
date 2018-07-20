using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Renderer.Views;
using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.WorldObjects
{
    public class Static : WorldObject
    {
        public Static(Graphic tileID, Hue hue, int index)
        {
            Graphic = tileID; Hue = hue; Index = index;
        }

        public int Index { get; }
        public override Position Position { get; set; }
        public new StaticView ViewObject => (StaticView)base.ViewObject;
        public AssetsLoader.StaticTiles ItemData => AssetsLoader.TileData.StaticData[Graphic];

        protected override View CreateView() => new StaticView(this);
    }
}
