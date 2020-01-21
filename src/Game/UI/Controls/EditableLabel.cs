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

using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class EditableLabel : Control
    {
        private readonly TextBox _label;

        public EditableLabel(string text, byte font, ushort hue, bool unicode, int width, FontStyle style)
        {
            BackupText = text;

            _label = new TextBox(font, 32, width, width, unicode, style, hue)
            {
                Text = text,
                Width = width,
                Height = 15,
                IsEditable = false,
                AllowDeleteKey = false
            };


            AcceptMouseInput = true;
            CanMove = true;
            WantUpdateSize = false;


            _label.MouseUp += (sender, e) => { InvokeMouseUp(e.Location, e.Button); };


            Add(_label);
            CalculateSize();
        }

        public bool Unicode => _label.Unicode;
        public byte Font => _label.Font;

        public string Text
        {
            get => _label.Text;
            set => _label.Text = value;
        }

        public string BackupText { get; private set; }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if (string.IsNullOrEmpty(_label.Text))
            {
                _label.Text = BackupText;
                CalculateSize();
                SetEditable(false);

                return;
            }

            CalculateSize();
            base.OnKeyboardReturn(textID, text);
            SetEditable(false);
            BackupText = _label.Text;
        }

        internal override void OnFocusLeft()
        {
            //if (string.IsNullOrEmpty(_label.Text))
            //    _label.Text = _backupText;
            //else
            //    _backupText = _label.Text;
            //CalculateSize();
            base.OnFocusLeft();
            SetEditable(false);
        }

        public void SetEditable(bool edit)
        {
            _label.IsEditable = edit;

            if (edit)
                _label.SetKeyboardFocus();
        }

        public bool GetEditable()
        {
            return _label.IsEditable;
        }

        private void CalculateSize()
        {
            //(int w, int h) = FileManager.Fonts.MeasureText(_label.Text, _label.Font, _label.Unicode, TEXT_ALIGN_TYPE.TS_LEFT,
            //    (ushort)_style, Width);

            int w, h;

            if (_label.Unicode)
                w = FontsLoader.Instance.GetWidthUnicode(_label.Font, _label.Text);
            else
                w = FontsLoader.Instance.GetWidthASCII(_label.Font, _label.Text);

            if (_label.Unicode)
                h = FontsLoader.Instance.GetHeightUnicode(_label.Font, _label.Text, w, TEXT_ALIGN_TYPE.TS_LEFT, 0x0);
            else
                h = FontsLoader.Instance.GetHeightASCII(_label.Font, _label.Text, w, TEXT_ALIGN_TYPE.TS_LEFT, 0x0);

            Width = w;
            Height = h;
            _label.Width = w;
            _label.Height = h;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            if (_label.IsEditable)
                batcher.Draw2D(Texture2DCache.GetTexture(Color.Wheat), x, y, _label.Width, _label.Height, ref _hueVector);

            return base.Draw(batcher, x, y);
        }
    }
}