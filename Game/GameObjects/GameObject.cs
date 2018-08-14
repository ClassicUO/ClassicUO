using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Map;
using ClassicUO.Game.Renderer.Views;

namespace ClassicUO.Game.GameObjects
{
    public abstract class GameObject : IDisposable
    {
        private Tile _tile;
        private View _view;

        protected GameObject(in Facet map)
        {
            Map = map;
        }

        public virtual Position Position { get; set; } = Position.Invalid;
        public virtual Hue Hue { get; set; }
        public virtual Graphic Graphic { get; set; }
        public View View => _view ?? (_view = CreateView());
        public sbyte AnimIndex { get; set; }
        public List<GameText> OverHeads { get; } = new List<GameText>();


        public Tile Tile
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
                    else
                    {
                        if (this != World.Player && !IsDisposed)
                            Dispose();
                    }
                }
            }
        }

        public Facet Map { get; set; }
        public bool IsDisposed { get; private set; }


        protected virtual View CreateView()
        {
            return null;
        }

        public GameText AddGameText(in MessageType type, in string text, in byte font, in Hue hue, in bool isunicode)
        {
            GameText overhead;

            for (int i = 0; i < OverHeads.Count; i++)
            {
                overhead = OverHeads[i];

                if (type == MessageType.Label && overhead.Text == text && overhead.Font == font && overhead.Hue == hue && overhead.IsUnicode == isunicode && overhead.MessageType == type && !overhead.IsDisposed)
                {
                    Hue = hue;
                    OverHeads.RemoveAt(i);

                    return overhead;
                }
            }

            overhead = new GameText(this, text) {Hue = hue, Font = font, IsUnicode = isunicode};

            return overhead;
        }

        private void InsertGameText(in GameText gameText)
        {
            OverHeads.Insert(OverHeads.Count == 0 || OverHeads[0].MessageType != MessageType.Label ? 0 : 1, gameText);
        }

        protected void DisposeView()
        {
            if (_view != null)
            {
                //_view.Dispose();
                _view = null;
            }
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            //DisposeView();
            Tile = null;
        }
    }
}