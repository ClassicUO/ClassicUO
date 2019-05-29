﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    class EditableLabel : Control
    {
        private readonly TextBox _label;
        private string _backupText;
        
        public EditableLabel(string text, byte font, ushort hue, bool unicode, int width, FontStyle style)
        {
            _backupText = text;

            _label = new TextBox(font, 32, width, width, unicode, style, hue)
            {
                Text = text,
                Width = width,
                Height = 15,
                IsEditable = false,
            };


            AcceptMouseInput = true;
            CanMove = true;
            WantUpdateSize = false;


            _label.MouseClick += (sender, e) => { InvokeMouseClick(e.Location, e.Button); };
            

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

        public string BackupText => _backupText;
        
        private static Vector3 _hue = Vector3.Zero;

        public override void OnKeyboardReturn(int textID, string text)
        {
            if (string.IsNullOrEmpty(_label.Text))
            {
                _label.Text = _backupText;
                CalculateSize();
                SetEditable(false);
                return;
            }

            CalculateSize();
            base.OnKeyboardReturn(textID, text);
            SetEditable(false);
            _backupText = _label.Text;
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

        public bool GetEditable() => _label.IsEditable;

        private void CalculateSize()
        {
            //(int w, int h) = FileManager.Fonts.MeasureText(_label.Text, _label.Font, _label.Unicode, TEXT_ALIGN_TYPE.TS_LEFT,
            //    (ushort)_style, Width);

            int w, h;

            if (_label.Unicode)
                w = FileManager.Fonts.GetWidthUnicode(_label.Font, _label.Text);
            else
                w = FileManager.Fonts.GetWidthASCII(_label.Font, _label.Text);

            if (_label.Unicode)
                h = FileManager.Fonts.GetHeightUnicode(_label.Font, _label.Text, w, TEXT_ALIGN_TYPE.TS_LEFT, 0x0);
            else
                h = FileManager.Fonts.GetHeightASCII(_label.Font, _label.Text, w, TEXT_ALIGN_TYPE.TS_LEFT, 0x0);

            Width = w;
            Height = h;
            _label.Width = w;
            _label.Height = h;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (_label.IsEditable)
                batcher.Draw2D(Textures.GetTexture(Color.Wheat), x, y, _label.Width, _label.Height, ref _hue);
            return base.Draw(batcher, x, y);
        }
    }
}
