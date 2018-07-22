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

        public virtual Position Position { get; set; } = Position.Invalid;
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
                _viewObject.Dispose();
                _viewObject = null;
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
