#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class Checkbox : GumpControl
    {
        private const int INACTIVE = 0;
        private const int ACTIVE = 1;

        private readonly SpriteTexture[] _textures = new SpriteTexture[2];
        private RenderedText _text;
        private bool _isChecked;


        public Checkbox(ushort inactive, ushort active, string text = "", byte font = 0, ushort color = 0)
        {
            _textures[INACTIVE] = IO.Resources.Gumps.GetGumpTexture(inactive);
            _textures[ACTIVE] = IO.Resources.Gumps.GetGumpTexture(active);

            ref SpriteTexture t = ref _textures[INACTIVE];
            Width = t.Width;
            Height = t.Height;

            _text = new RenderedText()
            {
                Font = font,
                Hue = color,
                IsUnicode = true,
                Text = text
            };

            CanMove = false;
            AcceptMouseInput = true;
        }


        public Checkbox(string[] parts, string[] lines) : this(ushort.Parse(parts[3]), ushort.Parse(parts[4]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);

            IsChecked = parts[5] == "1";
            LocalSerial = Serial.Parse(parts[6]);
        }

        public event EventHandler ValueChanged;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    ValueChanged.Raise();
                }
            }
        }

        public string Text => _text.Text;

        public override void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _textures.Length; i++)
            {
                if (_textures[i] != null)
                    _textures[i].Ticks = World.Ticks;
            }

            base.Update(totalMS, frameMS);
        }


        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            bool ok = base.Draw(spriteBatch, position);

            spriteBatch.Draw2D(IsChecked ? _textures[ACTIVE] : _textures[INACTIVE], position, HueVector);

            _text.Draw(spriteBatch, new Vector3(position.X + _textures[ACTIVE].Width + 2, position.Y, 0));
           
            return ok;
        }

    

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            IsChecked = !IsChecked;
        }
    }
}