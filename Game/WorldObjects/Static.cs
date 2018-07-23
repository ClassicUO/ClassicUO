using ClassicUO.AssetsLoader;
using ClassicUO.Game.Renderer.Views;

namespace ClassicUO.Game.WorldObjects
{
    public class Static : WorldObject
    {
        public Static(Graphic tileID, Hue hue, int index) : base(World.Map)
        {
            Graphic = tileID;
            Hue = hue;
            Index = index;
        }

        public int Index { get; }
        public override Position Position { get; set; }
        public new StaticView ViewObject => (StaticView) base.ViewObject;
        public StaticTiles ItemData => TileData.StaticData[Graphic];

        protected override View CreateView()
        {
            return new StaticView(this);
        }
    }
}