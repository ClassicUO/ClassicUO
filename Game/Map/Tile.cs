using ClassicUO.Game.WorldObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Map
{
    public sealed class Tile : WorldObject
    {
        private readonly List<WorldObject> _objectsOnTile;

        public Tile()
        {
            _objectsOnTile = new List<WorldObject>();
        }

        public Graphic TileID { get; set; }
        public IReadOnlyList<WorldObject> ObjectsOnTiles => _objectsOnTile;
        public override Position Position { get; set; }

        public void AddWorldObject(in WorldObject obj)
        {
            _objectsOnTile.Add(obj);
        }

        public void RemoveWorldObject(in WorldObject obj)
        {
            _objectsOnTile.Remove(obj);
        }

        public void Clear()
        {
            _objectsOnTile.Clear();
        }
    }

    public class TileView : WorldRenderObject
    {
        public TileView(Tile tile) : base(tile)
        {

        }
    }
}
