using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.WorldObjects
{
    public abstract class WorldObject
    {
        private WorldRenderObject _viewObject;
        private Map.Tile _tile;

        public virtual Position Position { get; set; } = Position.Invalid;
        public virtual Hue Hue { get; set; }

        public WorldRenderObject ViewObject
        {
            get
            {
                if (_viewObject == null)
                    _viewObject = CreateView();
                return _viewObject;
            }
        }

        protected virtual WorldRenderObject CreateView()
        {
            return null;
        }

        public Map.Tile Tile
        {
            get => _tile;
            set
            {
                if (_tile != value)
                {
                    _tile?.RemoveWorldObject(this);

                    _tile = value;

                    if (_tile != null)
                        _tile.AddWorldObject(this);

                }
            }
        }

        protected void DisposeView()
        {
            if (_viewObject != null)
            {
                _viewObject = null;
            }
        }
    }
}
