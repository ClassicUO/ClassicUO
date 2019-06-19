#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Managers
{
    internal class AuraManager
    {
        /*private readonly BlendState _blend = new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha
        };*/
        private Vector3 _auraHueVector = new Vector3(0, 13, 0);

        private int _saveAuraUnderFeetType;

        public bool IsEnabled
        {
            get
            {
                if (Engine.Profile.Current == null)
                    return false;

                switch (Engine.Profile.Current.AuraUnderFeetType)
                {
                    default:
                    case 0: return false;
                    case 1 when World.Player != null && World.Player.InWarMode: return true;
                    case 2 when Keyboard.Ctrl && Keyboard.Shift: return true;
                    case 3: return true;
                }
            }
        }

        public Texture2D AuraTexture { get; private set; }

        public void ToggleVisibility()
        {
            if (!IsEnabled)
            {
                _saveAuraUnderFeetType = Engine.Profile.Current.AuraUnderFeetType;
                Engine.Profile.Current.AuraUnderFeetType = 3;
            }
            else
                Engine.Profile.Current.AuraUnderFeetType = _saveAuraUnderFeetType;
        }

        public void CreateAuraTexture(int radius = 30)
        {
            AuraTexture?.Dispose();

            short w = 0;
            short h = 0;
            uint[] data = CircleOfTransparency.CreateTexture(radius, ref w, ref h);

            for (int i = 0; i < data.Length; i++)
            {
                ref uint pixel = ref data[i];

                if (pixel != 0)
                {
                    ushort value = (ushort) (pixel << 3);

                    if (value > 0xFF)
                        value = 0xFF;

                    pixel = (uint) ((value << 24) | (value << 16) | (value << 8) | value);
                }
            }

            AuraTexture = new Texture2D(Engine.Batcher.GraphicsDevice, w, h);
            AuraTexture.SetData(data);
        }

        public void Draw(UltimaBatcher2D batcher, int x, int y, Hue hue)
        {
            x -= AuraTexture.Width >> 1;
            y -= AuraTexture.Height >> 1;

            _auraHueVector.X = hue;

            //batcher.SetBlendState(_blend);
            batcher.Draw2D(AuraTexture, x, y, ref _auraHueVector);
            //batcher.SetBlendState(null);
        }
    }
}