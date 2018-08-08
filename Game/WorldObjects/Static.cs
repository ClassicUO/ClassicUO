using ClassicUO.AssetsLoader;
using ClassicUO.Game.Renderer.Views;
using ClassicUO.Game.WorldObjects.Interfaces;

namespace ClassicUO.Game.WorldObjects
{
    public class Static : GameObject, IDynamicItem
    {
        private StaticTiles? _itemData;

        public Static(in Graphic tileID, in Hue hue, in int index) : base(World.Map)
        {
            Graphic = tileID;
            Hue = hue;
            Index = index;
        }

        public int Index { get; }
        public new StaticView View => (StaticView) base.View;
        public string Name { get; private set; }
        public override Position Position { get; set; }

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

        public bool IsAtWorld(in int x, in int y)
        {
            return Position.X == x && Position.Y == y;
        }

        protected override View CreateView()
        {
            return new StaticView(this);
        }
    }
}