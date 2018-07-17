using ClassicUO.Game.WorldObjects;
using ClassicUO.Game.WorldObjects.Views;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ClassicUO.Game.Map
{
    public sealed class Tile : WorldObject
    {
        private readonly List<WorldObject> _objectsOnTile;

        public Tile()
        {
            _objectsOnTile = new List<WorldObject>();
            _objectsOnTile.Add(this);
        }


        public Graphic TileID { get; set; }
        public IReadOnlyList<WorldObject> ObjectsOnTiles => _objectsOnTile;
        public override Position Position { get; set; }
        public new TileView ViewObject => (TileView)base.ViewObject;
        public bool IsIgnored => TileID < 3 || TileID == 0x1DB || (TileID >= 0x1AE && TileID <= 0x1B5);


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
            TileID = 0;
            Position = new Position(0, 0);
            
        }

        public void Sort()
        {
            for (int i = 0; i < _objectsOnTile.Count - 1; i++)
            {
                int j = i + 1;
                while (j > 0)
                {
                    int result = Compare(_objectsOnTile[j - 1], _objectsOnTile[j]);
                    if (result > 0)
                    {
                        WorldObject temp = _objectsOnTile[j - 1];
                        _objectsOnTile[j - 1] = _objectsOnTile[j];
                        _objectsOnTile[j] = temp;
                    }
                    j--;
                }
            }
        }

        public T[] GetWorldObjects<T>() where T: WorldObject
            => _objectsOnTile.OfType<T>().Cast<T>().ToArray();

        // create view only when TileID is initialized
        protected override WorldRenderObject CreateView()
            => TileID <= 0 ? null : new TileView(this);


        private static int Compare(in WorldObject x, in WorldObject y)
        {
            (int xZ, int xType, int xThreshold, int xTierbreaker) = GetSortValues(x);
            (int yZ, int yType, int yThreshold, int yTierbreaker) = GetSortValues(y);

            xZ += xThreshold;
            yZ += yThreshold;

            int comparison = xZ - yZ;
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
            {
                return (tile.ViewObject.SortZ,
                        0, 
                        0, 
                        0);
            }
            else if (e is Static staticitem)
            {
                var itemdata = AssetsLoader.TileData.StaticData[staticitem.TileID];

                return (staticitem.Position.Z, 
                        1, 
                        (itemdata.Height > 0 ? 1 : 0) + (AssetsLoader.TileData.IsBackground((long)itemdata.Flags) ? 0 : 1), 
                        staticitem.Index);
            }
            else if (e is Item item)
            {
                return (item.Position.Z, 
                        ((item.Graphic & AssetsLoader.FileManager.GraphicMask) == 0x2006) ? 4 : 2, 
                        (item.ItemData.Height > 0 ? 1 : 0) + (AssetsLoader.TileData.IsBackground((long)item.ItemData.Flags) ? 0 : 1), 
                        (int)item.Serial.Value);
            }
            else if (e is Mobile mobile)
            {
                return (mobile.Position.Z, 
                    3 /* is sitting */, 
                    2,
                    mobile == World.Player ? 0x40000000 : (int)mobile.Serial.Value);
            }

            return (0, 0, 0, 0);        
        }
    }
}
