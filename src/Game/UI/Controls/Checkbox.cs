using System;
using System.Collections.Generic;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    internal class Checkbox : Control
    {
        private const int INACTIVE = 0;
        private const int ACTIVE = 1;
        private bool _isChecked;
        private readonly RenderedText _text;
        private readonly UOTexture[] _textures = new UOTexture[2];

        public Checkbox
        (
            ushort inactive,
            ushort active,
            string text = "",
            byte font = 0,
            ushort color = 0,
            bool isunicode = true,
            int maxWidth = 0
        )
        {
            _textures[INACTIVE] = GumpsLoader.Instance.GetTexture(inactive);
            _textures[ACTIVE] = GumpsLoader.Instance.GetTexture(active);

            if (_textures[0] == null || _textures[1] == null)
            {
                Dispose();

                return;
            }

            UOTexture t = _textures[INACTIVE];
            Width = t.Width;

            _text = RenderedText.Create(text, color, font, isunicode, maxWidth: maxWidth);
            Width += _text.Width;

            Height = Math.Max(t.Width, _text.Height);
            CanMove = false;
            AcceptMouseInput = true;
        }

        public Checkbox(List<string> parts, string[] lines) : this(ushort.Parse(parts[3]), ushort.Parse(parts[4]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            IsChecked = parts[5] == "1";
            LocalSerial = SerialHelper.Parse(parts[6]);
            IsFromServer = true;
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnCheckedChanged();
                }
            }
        }

        public string Text => _text.Text;

        public event EventHandler ValueChanged;

        public override void Update(double totalTime, double frameTime)
        {
            for (int i = 0; i < _textures.Length; i++)
            {
                UOTexture t = _textures[i];

                if (t != null)
                {
                    t.Ticks = (long) totalTime;
                }
            }

            base.Update(totalTime, frameTime);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            ResetHueVector();

            bool ok = base.Draw(batcher, x, y);
            batcher.Draw2D(IsChecked ? _textures[ACTIVE] : _textures[INACTIVE], x, y, ref HueVector);

            _text.Draw(batcher, x + _textures[ACTIVE].Width + 2, y);

            return ok;
        }

        protected virtual void OnCheckedChanged()
        {
            ValueChanged.Raise(this);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && MouseIsOver)
            {
                IsChecked = !IsChecked;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _text?.Destroy();
        }
    }
}