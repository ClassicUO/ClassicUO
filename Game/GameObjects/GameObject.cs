using ClassicUO.Game.Map;
using ClassicUO.Game.Renderer.Views;
using ClassicUO.IO.Resources;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.GameObjects
{
    public abstract class GameObject : IDisposable
    {
        private Tile _tile;
        private View _view;
        private List<GameText> _overHeads;

        protected GameObject(Facet map)
        {
            Map = map;
            _overHeads = new List<GameText>();
        }

        public virtual Position Position { get; set; } = Position.Invalid;
        public virtual Hue Hue { get; set; }
        public virtual Graphic Graphic { get; set; }
        //public View View => _view ?? (_view = CreateView());
        public sbyte AnimIndex { get; set; }
        public IReadOnlyList<GameText> OverHeads => _overHeads;


        public Tile Tile
        {
            get => _tile;
            set
            {
                if (_tile != value)
                {
                    _tile?.RemoveGameObject(this);

                    _tile = value;

                    if (_tile != null)
                    {
                        _tile.AddGameObject(this);
                    }
                    else
                    {
                        if (this != World.Player && !IsDisposed)
                        {
                            Dispose();
                        }
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

        public View GetView()
        {
            if (_view == null)
                _view = CreateView();
            return _view;
        }

        public GameText AddGameText(MessageType type,  string text,  byte font,  Hue hue,  bool isunicode)
        {
            GameText overhead;

            for (int i = 0; i < OverHeads.Count; i++)
            {
                overhead = OverHeads[i];

                if (type == MessageType.Label && overhead.Text == text && overhead.Font == font && overhead.Hue == hue && overhead.IsUnicode == isunicode && overhead.MessageType == type && !overhead.IsDisposed)
                {
                    overhead.Hue = hue;
                    _overHeads.RemoveAt(i);
                    InsertGameText(overhead);
                    return overhead;
                }
            }

            int width = isunicode ? Fonts.GetWidthUnicode(font, text) : Fonts.GetWidthASCII(font, text);

            if (width > 200)
            {
                width = isunicode ? Fonts.GetWidthExUnicode(font, text, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort)FontStyle.BlackBorder) : Fonts.GetWidthExASCII(font, text, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort)FontStyle.BlackBorder);
            }
            else
            {
                width = 0;
            }

            overhead = new GameText(this, text) { MaxWidth = width, Hue = hue, Font = font, IsUnicode = isunicode, FontStyle = FontStyle.BlackBorder };
            InsertGameText(overhead);
            return overhead;
        }

        public void RemoveGameTextAt(int idx) => _overHeads.RemoveAt(idx);

        private void InsertGameText(GameText gameText)
        {
            _overHeads.Insert(OverHeads.Count == 0 || OverHeads[0].MessageType != MessageType.Label ? 0 : 1, gameText);
        }

        protected void DisposeView()
        {
            if (_view != null)
            {
                if (_view.Texture != null /*&& !_view.Texture.IsDisposed*/)
                {
                    _view.Texture.Dispose();
                    _view.Texture = null;
                }
                _view = null;
            }
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            DisposeView();
            Tile = null;
        }
    }
}