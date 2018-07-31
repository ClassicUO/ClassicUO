using ClassicUO.AssetsLoader;
using ClassicUO.Game.Renderer.Views;

namespace ClassicUO.Game.WorldObjects
{
    public class Static : WorldObject
    {
        private StaticTiles? _itemData;
        public Static(in Graphic tileID, in Hue hue, in int index) : base(World.Map)
        {
            Graphic = tileID;
            Hue = hue;
            Index = index;
        }

        public int Index { get; }
        public override Position Position { get; set; }
        public new StaticView ViewObject => (StaticView) base.ViewObject;
        public string Name { get; private set; }

        public StaticTiles ItemData
        {
            get
            {
                if (!_itemData.HasValue)
                {
                    _itemData = TileData.StaticData[Graphic];
                    Name = _itemData.Value.Name;
                }

                return _itemData.Value;
            }
        } 

        protected override View CreateView()
        {
            return new StaticView(this);
        }
    }
}