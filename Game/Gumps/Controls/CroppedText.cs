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
using ClassicUO.IO;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class CroppedText : GumpControl
    {
        private readonly RenderedText _gameText;
        private readonly int _index;

        public CroppedText() => _gameText = new RenderedText
        {
            IsUnicode = true,
            Font = (byte) (FileManager.ClientVersion >= ClientVersions.CV_305D ? 1 : 0),
            FontStyle = FontStyle.BlackBorder
        };

        public CroppedText(string[] parts, string[] lines) : this()
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            Hue = Hue.Parse(parts[5]);
            _index = int.Parse(parts[6]);

            _gameText.MaxWidth = Width;

            if (_gameText.MaxWidth > 0)
                _gameText.FontStyle |= FontStyle.Cropped;

            Text = lines[_index];

            CanMove = true;
        }


        public Hue Hue { get; set; }

        public string Text
        {
            get => _gameText.Text;
            set => _gameText.Text = value;
        }


        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            _gameText.Draw(spriteBatch, position);
            return base.Draw(spriteBatch, position, hue);
        }
    }
}