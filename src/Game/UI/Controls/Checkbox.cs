#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;

using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class Checkbox : Control
    {
        private const int INACTIVE = 0;
        private const int ACTIVE = 1;
        private readonly RenderedText _text;
        private readonly UOTexture[] _textures = new UOTexture[2];
        private bool _isChecked;

        public Checkbox(ushort inactive, ushort active, string text = "", byte font = 0, ushort color = 0, bool isunicode = true, int maxWidth = 0)
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

        public override void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _textures.Length; i++)
            {
                UOTexture t = _textures[i];

                if (t != null)
                    t.Ticks = (long) totalMS;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            ResetHueVector();

            bool ok = base.Draw(batcher, x, y);
            batcher.Draw2D(IsChecked ? _textures[ACTIVE] : _textures[INACTIVE], x, y, ref _hueVector);
            _text.Draw(batcher, x + _textures[ACTIVE].Width + 2, y);

            return ok;
        }

        protected virtual void OnCheckedChanged()
        {
            ValueChanged.Raise(this);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
                IsChecked = !IsChecked;
        }

        public override void Dispose()
        {
            base.Dispose();
            _text?.Destroy();
        }
    }
}