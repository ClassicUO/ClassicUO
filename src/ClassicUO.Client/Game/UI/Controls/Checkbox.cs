// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class Checkbox : Control
    {
        private bool _isChecked;
        private readonly RenderedText _text;
        private ushort _inactive,
            _active;

        public Checkbox(
            ushort inactive,
            ushort active,
            string text = "",
            byte font = 0,
            ushort color = 0,
            bool isunicode = true,
            int maxWidth = 0
        )
        {
            _inactive = inactive;
            _active = active;

            ref readonly var gumpInfoInactive = ref Client.Game.UO.Gumps.GetGump(inactive);
            ref readonly var gumpInfoActive = ref Client.Game.UO.Gumps.GetGump(active);

            if (gumpInfoInactive.Texture == null || gumpInfoActive.Texture == null)
            {
                Dispose();

                return;
            }

            Width = gumpInfoInactive.UV.Width;

            _text = RenderedText.Create(text, color, font, isunicode, maxWidth: maxWidth);

            Width += _text.Width;

            Height = Math.Max(gumpInfoInactive.UV.Width, _text.Height);
            CanMove = false;
            AcceptMouseInput = true;
        }

        public Checkbox(List<string> parts, string[] lines)
            : this(ushort.Parse(parts[3]), ushort.Parse(parts[4]))
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

        public override ClickPriority Priority => ClickPriority.High;

        public string Text => _text.Text;

        public event EventHandler ValueChanged;

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            var ok = base.Draw(batcher, x, y);

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(
                IsChecked ? _active : _inactive
            );

            batcher.Draw(
                gumpInfo.Texture,
                new Vector2(x, y),
                gumpInfo.UV,
                ShaderHueTranslator.GetHueVector(0)
            );

            _text.Draw(batcher, x + gumpInfo.UV.Width + 2, y);

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
