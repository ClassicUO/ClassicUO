using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.WorldObjects
{
    public abstract class WorldObject
    {
        private WorldRenderObject _viewObject;

        public abstract Position Position { get; set; }

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

        protected void DisposeView()
        {
            if (_viewObject != null)
                _viewObject = null;
        }
    }
}
