using ClassicUO.Game.Map;
using ClassicUO.Game.Renderer.Views;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.WorldObjects
{
    public abstract class WorldObject //: IDisposable
    {
        private Tile _tile;
        private View _viewObject;

        public WorldObject(in Facet map)
        {
            Map = map;
            Position = Position.Invalid;
        }

        public virtual Position Position { get; set; }
        public virtual Hue Hue { get; set; }
        public virtual Graphic Graphic { get; set; }

        public View ViewObject => _viewObject ?? (_viewObject = CreateView());

        public sbyte AnimIndex { get; set; }

        public Tile Tile
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


        public Facet Map { get; set; }

        public bool IsDisposed { get; private set; }

        protected virtual View CreateView()
        {
            return null;
        }

        protected void DisposeView()
        {
            if (_viewObject != null)
            {
                _viewObject.Dispose();
                _viewObject = null;
            }
        }

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
                    Map.Center = new Point(x, y);
                    Log.Message(LogTypes.Info, Map.Center.ToString());
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