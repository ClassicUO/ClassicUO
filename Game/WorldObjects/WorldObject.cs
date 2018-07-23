using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Renderer.Views;

namespace ClassicUO.Game.WorldObjects
{
    public abstract class WorldObject //: IDisposable
    {
        private View _viewObject;
        private Map.Tile _tile;

        public WorldObject(in Map.Facet map)
        {
            Map = map;
            Position = Position.Invalid;
        }

        public virtual Position Position { get; set; }
        public virtual Hue Hue { get; set; }
        public virtual Graphic Graphic { get; set; }

        public View ViewObject
        {
            get
            {
                if (_viewObject == null)
                    _viewObject = CreateView();
                return _viewObject;
            }
        }

        protected virtual View CreateView()
        {
            return null;
        }

        public sbyte AnimIndex { get; set; }

        public Map.Tile Tile
        {
            get => _tile;
            set
            {
                if (_tile != value)
                {
                    _tile?.RemoveWorldObject(this);

                    _tile = value;

                    _tile?.AddWorldObject(this);

                }
            }
        }


        public Map.Facet Map { get; private set; }

        public void SetMap(in Map.Facet map)
        {
            if (map != Map)
            {
                Map = map;
                //Position.Tile = Position.NullTile;
            }
        }

        protected void DisposeView()
        {
            if (_viewObject != null)
            {
                _viewObject.Dispose();
                _viewObject = null;
            }
        }

        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {           
            IsDisposed = true;
            Tile = null;
        }

        protected virtual void OnTileChanged(int x, int y)
        {
            if (Map != null)
            {
                if (this == World.Player && Map.Index >= 0)
                {
                    Map.Center = new Microsoft.Xna.Framework.Point(x, y);
                    Utility.Log.Message(Utility.LogTypes.Info, Map.Center.ToString());
                }
                Tile = Map.GetTile(x, y);
            }
            else
            {
                if (this != World.Player)
                    Dispose();
            }
        }

        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

        //protected virtual void Dispose(in bool disposing)
        //{
        //    if (disposing)
        //    {
        //        DisposeView();
        //    }
        //}

        //~WorldObject()
        //{
        //    Dispose();
        //}

    }
}
