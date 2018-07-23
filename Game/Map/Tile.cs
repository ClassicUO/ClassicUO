using System.Collections.Generic;
using System.Linq;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.Renderer.Views;
using ClassicUO.Game.WorldObjects;
using ClassicUO.Utility;

namespace ClassicUO.Game.Map
{
    public sealed class Tile : WorldObject
    {
        private static readonly List<WorldObject> _itemsAtZ = new List<WorldObject>();
        private readonly List<WorldObject> _objectsOnTile;

        public Tile() : base(World.Map)
        {
            _objectsOnTile = new List<WorldObject>();
            _objectsOnTile.Add(this);
        }


        public IReadOnlyList<WorldObject> ObjectsOnTiles => _objectsOnTile;
        public override Position Position { get; set; }
        public new TileView ViewObject => (TileView) base.ViewObject;
        public bool IsIgnored => Graphic < 3 || Graphic == 0x1DB || Graphic >= 0x1AE && Graphic <= 0x1B5;

        public LandTiles TileData => AssetsLoader.TileData.LandData[Graphic];

        public void AddWorldObject(in WorldObject obj)
        {
            _objectsOnTile.Add(obj);

            Sort();
        }

        public void RemoveWorldObject(in WorldObject obj)
        {
            _objectsOnTile.Remove(obj);
        }

        public void Clear()
        {
            _objectsOnTile.Clear();
            _objectsOnTile.Add(this);
            DisposeView();
            Graphic = 0;
            Position = Position.Invalid;
        }

        public void Sort()
        {
            for (var i = 0; i < _objectsOnTile.Count - 1; i++)
            {
                var j = i + 1;
                while (j > 0)
                {
                    var result = Compare(_objectsOnTile[j - 1], _objectsOnTile[j]);
                    if (result > 0)
                    {
                        var temp = _objectsOnTile[j - 1];
                        _objectsOnTile[j - 1] = _objectsOnTile[j];
                        _objectsOnTile[j] = temp;
                    }

                    j--;
                }
            }
        }

        public List<WorldObject> GetItemsBetweenZ(in int z0, in int z1)
        {
            var items = _itemsAtZ;
            _itemsAtZ.Clear();

            for (var i = 0; i < ObjectsOnTiles.Count; i++)
                if (MathHelper.InRange(ObjectsOnTiles[i].Position.Z, z0, z1))
                    if (ObjectsOnTiles[i] is Item || ObjectsOnTiles[i] is Static)
                        items.Add(ObjectsOnTiles[i]);
            return items;
        }

        public bool IsZUnderObjectOrGround(in sbyte z, out WorldObject entity, out WorldObject ground)
        {
            var list = _objectsOnTile;

            entity = null;
            ground = null;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Position.Z <= z)
                    continue;

                if (list[i] is Item it)
                {
                    var itemdata = it.ItemData;

                    if (AssetsLoader.TileData.IsRoof((long) itemdata.Flags) ||
                        AssetsLoader.TileData.IsSurface((long) itemdata.Flags) ||
                        AssetsLoader.TileData.IsWall((long) itemdata.Flags) &&
                        AssetsLoader.TileData.IsImpassable((long) itemdata.Flags))
                        if (entity == null || list[i].Position.Z < entity.Position.Z)
                            entity = list[i];
                }
                else if (list[i] is Static st)
                {
                    var itemdata = st.ItemData;

                    if (AssetsLoader.TileData.IsRoof((long) itemdata.Flags) ||
                        AssetsLoader.TileData.IsSurface((long) itemdata.Flags) ||
                        AssetsLoader.TileData.IsWall((long) itemdata.Flags) &&
                        AssetsLoader.TileData.IsImpassable((long) itemdata.Flags))
                        if (entity == null || list[i].Position.Z < entity.Position.Z)
                            entity = list[i];
                }
                else if (list[i] is Tile tile && tile.ViewObject.SortZ >= z + 12)
                {
                    ground = list[i];
                }
            }

            return entity != null || ground != null;
        }

        public T[] GetWorldObjects<T>() where T : WorldObject
        {
            return _objectsOnTile.OfType<T>().Cast<T>().ToArray();
        }

        // create view only when TileID is initialized
        protected override View CreateView()
        {
            return Graphic <= 0 ? null : new TileView(this);
        }


        private static int Compare(in WorldObject x, in WorldObject y)
        {
            var (xZ, xType, xThreshold, xTierbreaker) = GetSortValues(x);
            var (yZ, yType, yThreshold, yTierbreaker) = GetSortValues(y);

            xZ += xThreshold;
            yZ += yThreshold;

            var comparison = xZ - yZ;
            if (comparison == 0)
                comparison = xType - yType;
            if (comparison == 0)
                comparison = xThreshold - yThreshold;
            if (comparison == 0)
                comparison = xTierbreaker - yTierbreaker;

            return comparison;
        }

        private static (int, int, int, int) GetSortValues(in WorldObject e)
        {
            if (e is Tile tile)
                return (tile.ViewObject.SortZ,
                    0,
                    0,
                    0);

            if (e is Static staticitem)
            {
                var itemdata = AssetsLoader.TileData.StaticData[staticitem.Graphic];

                return (staticitem.Position.Z,
                    1,
                    (itemdata.Height > 0 ? 1 : 0) + (AssetsLoader.TileData.IsBackground((long) itemdata.Flags) ? 0 : 1),
                    staticitem.Index);
            }

            if (e is Item item)
                return (item.Position.Z,
                    item.IsCorpse ? 4 : 2,
                    (item.ItemData.Height > 0 ? 1 : 0) +
                    (AssetsLoader.TileData.IsBackground((long) item.ItemData.Flags) ? 0 : 1),
                    (int) item.Serial.Value);
            if (e is Mobile mobile)
                return (mobile.Position.Z,
                    3 /* is sitting */,
                    2,
                    mobile == World.Player ? 0x40000000 : (int) mobile.Serial.Value);

            return (0, 0, 0, 0);
        }
    }
}