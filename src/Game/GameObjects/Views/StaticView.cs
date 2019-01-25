#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Static
    {
        private readonly bool _isFoliage, _isPartialHue, _isTransparent;
        private float _alpha;
        private float _timeToProcessAlpha;
        private readonly int _canBeTransparent;

        private Graphic _oldGraphic;

        public bool CharacterIsBehindFoliage { get; set; }
   
        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || IsDisposed)
                return false;

            if (Texture == null || Texture.IsDisposed || _oldGraphic != Graphic)
            {
                _oldGraphic = Graphic;

                ArtTexture texture = FileManager.Art.GetTexture(Graphic);
                Texture = texture;
                Bounds = new Rectangle((Texture.Width >> 1) - 22, Texture.Height - 44, Texture.Width, Texture.Height);

                //FrameInfo.X = texture.ImageRectangle.X;
                //FrameInfo.Y = texture.ImageRectangle.Y;
                FrameInfo.Width = texture.ImageRectangle.Width;
                FrameInfo.Height = texture.ImageRectangle.Height;

                FrameInfo.X = Bounds.X + texture.ImageRectangle.X;
                FrameInfo.Y = Bounds.Y + texture.ImageRectangle.Y;
            }

            if (_isFoliage)
            {
                if (CharacterIsBehindFoliage)
                {
                    
                        ProcessAlpha(76);
                        //CharacterIsBehindFoliage = false;
                    
                }
                else
                {
                    ProcessAlpha(0xFF);
                }
            }

            //if (_isFoliage)
            //{
            //    bool check = World.Player.X <= X && World.Player.Y <= Y;
            //    bool isnrect = false;

            //    if (!check)
            //    {
            //        check = World.Player.Y <= Y && World.Player.Position.X <= X + 1;

            //        if (!check)
            //            check = World.Player.X <= X && World.Player.Y <= Y + 1;
            //    }

            //    if (check)
            //    {
            //        Rectangle fol = Rectangle.Empty;
            //        fol.X = (int) position.X - Bounds.X + 22;
            //        fol.Y = (int) position.Y - Bounds.Y + 22;

            //        fol.Width = FrameInfo.Width;
            //        fol.Height = FrameInfo.Height;

            //        if (fol.InRect(World.Player.GetOnScreenRectangle()))
            //        {
            //            isnrect = true;

            //            if (_timeToProcessAlpha < Engine.Ticks)
            //            {
            //                _timeToProcessAlpha = Engine.Ticks + Constants.ALPHA_TIME;

            //                _alpha += .1f;

            //                if (_alpha >= Constants.FOLIAGE_ALPHA)
            //                {
            //                    _alpha = Constants.FOLIAGE_ALPHA;
            //                }
            //            }
            //        }
            //    }

            //    if (_alpha > 0.0f && !isnrect)
            //    {
            //        if (_timeToProcessAlpha < Engine.Ticks)
            //        {
            //            _timeToProcessAlpha = Engine.Ticks + Constants.ALPHA_TIME;

            //            _alpha -= .1f;

            //            if (_alpha < 0.0f)
            //                _alpha = 0;
            //        }
            //    }
            //}
            //else if (_isTransparent && _alpha != 0.5f)
            //    _alpha = 0.5f;

            
            //if (Engine.Profile.Current.UseCircleOfTransparency)
            //{
            //    int z = World.Player.Z + 5;

            //    bool r = true;

            //    if (!_isFoliage)
            //    {
            //        if (Z <= z - ItemData.Height)
            //            r = false;
            //        else if (z < Z && (_canBeTransparent & 0xFF) == 0)
            //            r = false;
            //    }

            //    if (r)
            //    {
            //        int distanceMax = Engine.Profile.Current.CircleOfTransparencyRadius + 1;
            //        int distance = Distance;

            //        if (distance <= distanceMax)
            //            _alpha = 1.0f - 1.0f / (distanceMax / (float) distance);
            //        else if (_alpha != 0)
            //            _alpha = _isTransparent ? .5f : 0;
            //    }
            //    else if (_alpha != 0)
            //        _alpha = _isTransparent ? .5f : 0;
            //}
            //else if (!_isFoliage && _alpha != 0)
            //    _alpha = _isTransparent ? .5f : 0;
            

            if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
                HueVector = new Vector3(0x038E, 1, HueVector.Z);
            else
                HueVector = ShaderHuesTraslator.GetHueVector(Hue, _isPartialHue, _alpha, false);
            MessageOverHead(batcher, position, Bounds.Y - 44);
            Engine.DebugInfo.StaticsRendered++;
            return base.Draw(batcher, position, objectList);
        }

        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex)
        {
            int x = list.MousePosition.X - (int) vertex[0].Position.X;
            int y = list.MousePosition.Y - (int) vertex[0].Position.Y;
            if (Texture.Contains(x, y))
                list.Add(this, vertex[0].Position);
        }
    }
}