using System.Collections.Generic;
using System.Linq;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.Renderer.Views;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.GameObjects.Interfaces;
using ClassicUO.Utility;

namespace ClassicUO.Game.Map
{
    public sealed class Tile : GameObject
    {
        private static readonly List<GameObject> _itemsAtZ = new List<GameObject>();
        private readonly List<GameObject> _objectsOnTile;
        private bool _needSort;
        private LandTiles? _tileData;

        public Tile() : base(World.Map)
        {
            _objectsOnTile = new List<GameObject>(1);
            _objectsOnTile.Add(this);
        }


        public IReadOnlyList<GameObject> ObjectsOnTiles
        {
            get
            {
                if (_needSort)
                {
                    RemoveDuplicates();
                    TileSorter.Sort(_objectsOnTile);
                    _needSort = false;
                }

                return _objectsOnTile;
            }
        }


        public override Position Position { get; set; }
        public new TileView View => (TileView) base.View;
        public bool IsIgnored => Graphic < 3 || Graphic == 0x1DB || Graphic >= 0x1AE && Graphic <= 0x1B5;

        public LandTiles TileData
        {
            get
            {
                if (!_tileData.HasValue)
                    _tileData = AssetsLoader.TileData.LandData[Graphic];
                return _tileData.Value;
            }
        }

        public void AddWorldObject(in GameObject obj)
        {
            if (obj is IDynamicItem)
            {
                for (int i = 0; i < _objectsOnTile.Count; i++)
                {
                    if (_objectsOnTile[i] is Item || _objectsOnTile[i] is Static)
                    {
                        if (obj.Graphic == _objectsOnTile[i].Graphic && obj.Position.Z == _objectsOnTile[i].Position.Z)
                        {
                            _objectsOnTile.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            _objectsOnTile.Add(obj);

            _needSort = true;
        }

        public void RemoveWorldObject(in GameObject obj)
        {
            _objectsOnTile.Remove(obj);
        }

        public void ForceSort()
        {
            _needSort = true;
        }

        public void Clear()
        {
            for (int i = 0; i < _objectsOnTile.Count; i++)
            {
                var obj = _objectsOnTile[i];

                if (obj != World.Player && !(obj is Tile))
                {
                    int count = _objectsOnTile.Count;


                    obj.Dispose();

                    if (count == _objectsOnTile.Count) _objectsOnTile.RemoveAt(i);


                    i--;
                }
            }

            //_objectsOnTile.Clear();
            //_objectsOnTile.Add(this);
            DisposeView();
            Graphic = 0;
            Position = Position.Invalid;
            _needSort = false;
        }

        private void RemoveDuplicates()
        {
            int[] toremove = new int[0x100];
            int index = 0;

            for (int i = 0; i < _objectsOnTile.Count; i++)
            {
                if (_objectsOnTile[i] is Static st)
                {
                    for (int j = i + 1; j < _objectsOnTile.Count; j++)
                    {
                        if (_objectsOnTile[i].Position.Z == _objectsOnTile[j].Position.Z)
                        {
                            if (_objectsOnTile[j] is Static stj && st.Graphic == stj.Graphic)
                            {
                                toremove[index++] = i;
                                break;
                            }
                        }
                    }
                }
                else if (_objectsOnTile[i] is Item item)
                {
                    for (int j = i + 1; j < _objectsOnTile.Count; j++)
                    {
                        if (_objectsOnTile[i].Position.Z == _objectsOnTile[j].Position.Z)
                        {
                            if (_objectsOnTile[j] is Static stj && item.ItemData.Name == stj.ItemData.Name || _objectsOnTile[j] is Item itemj && item.Serial == itemj.Serial)
                                toremove[index++] = j;
                        }
                    }
                }
            }

            for (int i = 0; i < index; i++)
                _objectsOnTile.RemoveAt(toremove[i] - i);
        }


        public List<GameObject> GetItemsBetweenZ(in int z0, in int z1)
        {
            var items = _itemsAtZ;
            _itemsAtZ.Clear();

            for (int i = 0; i < ObjectsOnTiles.Count; i++)
            {
                if (MathHelper.InRange(ObjectsOnTiles[i].Position.Z, z0, z1))
                {
                    if (ObjectsOnTiles[i] is IDynamicItem)
                        items.Add(ObjectsOnTiles[i]);
                }
            }

            return items;
        }

        public bool IsZUnderObjectOrGround(in sbyte z, out GameObject entity, out GameObject ground)
        {
            var list = _objectsOnTile;

            entity = null;
            ground = null;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Position.Z <= z)
                    continue;

                if (list[i] is IDynamicItem dyn)
                {
                    StaticTiles itemdata = dyn.ItemData;
                    if (AssetsLoader.TileData.IsRoof((long) itemdata.Flags) || AssetsLoader.TileData.IsSurface((long) itemdata.Flags) || AssetsLoader.TileData.IsWall((long) itemdata.Flags) && AssetsLoader.TileData.IsImpassable((long) itemdata.Flags))
                    {
                        if (entity == null || list[i].Position.Z < entity.Position.Z)
                            entity = list[i];
                    }
                }

                else if (list[i] is Tile tile && tile.View.SortZ >= z + 12) ground = list[i];
            }

            return entity != null || ground != null;
        }

        public T[] GetWorldObjects<T>() where T : GameObject
        {
            return _objectsOnTile.OfType<T>().ToArray();
        }

        // create view only when TileID is initialized
        protected override View CreateView()
        {
            return Graphic <= 0 || Position == Position.Invalid ? null : new TileView(this);
        }
    }
}