#region license

// Copyright (C) 2020 project dust765
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
using System.IO;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.InteropServices.Runtime.UOClassicCombat
{
    internal class TextureManager
    {
        public bool IsEnabled => ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.TextureManagerEnabled;
        public bool IsArrowEnabled => ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.TextureManagerArrows;
        public bool IsHalosEnabled => ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.TextureManagerHalos;


        private static BlendState _blend = new BlendState {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha
        };
        
        private static Vector3 _hueVector = Vector3.Zero;

        //TEXTURES ARROW
        private static UOTexture _arrowGreen;
        public static UOTexture ArrowGreen
        {
            get
            {
                if(_arrowGreen == null || _arrowGreen.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.arrow_green.png");
                    Texture2D.TextureDataFromStreamEXT(stream, out int w, out int h, out byte[] pixels);

                    _arrowGreen = new UOTexture(w, h);
                    _arrowGreen.SetData(pixels);
                }

                return _arrowGreen;
            }
        }

        private static UOTexture _arrowPurple;
        public static UOTexture ArrowPurple
        {
            get
            {
                if (_arrowPurple == null || _arrowPurple.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.arrow_purple.png");
                    Texture2D.TextureDataFromStreamEXT(stream, out int w, out int h, out byte[] pixels);

                    _arrowPurple = new UOTexture(w, h);
                    _arrowPurple.SetData(pixels);
                }

                return _arrowPurple;
            }
        }

        private static UOTexture _arrowRed;
        public static UOTexture ArrowRed
        {
            get
            {
                if(_arrowRed == null || _arrowRed.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.arrow_red.png");
                    Texture2D.TextureDataFromStreamEXT(stream, out int w, out int h, out byte[] pixels);

                    _arrowRed = new UOTexture(w, h);
                    _arrowRed.SetData(pixels);
                }

                return _arrowRed;
            }
        }

        private static UOTexture _arrowYellow;
        public static UOTexture ArrowYellow
        {
            get
            {
                if (_arrowYellow == null || _arrowYellow.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.arrow_yellow.png");
                    Texture2D.TextureDataFromStreamEXT(stream, out int w, out int h, out byte[] pixels);

                    _arrowYellow = new UOTexture(w, h);
                    _arrowYellow.SetData(pixels);
                }

                return _arrowYellow;
            }
        }

        private static UOTexture _arrowOrange;
        public static UOTexture ArrowOrange
        {
            get
            {
                if (_arrowOrange == null || _arrowOrange.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.arrow_orange.png");
                    Texture2D.TextureDataFromStreamEXT(stream, out int w, out int h, out byte[] pixels);

                    _arrowOrange = new UOTexture(w, h);
                    _arrowOrange.SetData(pixels);
                }

                return _arrowOrange;
            }
        }
        //TEXTURES HALO
        private static UOTexture _haloGreen;
        private static UOTexture HaloGreen
        {
            get
            {
                if (_haloGreen == null || _haloGreen.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.halo_green.png");
                    Texture2D.TextureDataFromStreamEXT(stream, out int w, out int h, out byte[] pixels, 50, 27);

                    _haloGreen = new UOTexture(w, h);
                    _haloGreen.SetData(pixels);
                }

                return _haloGreen;
            }
        }

        private static UOTexture _haloPurple;
        private static UOTexture HaloPurple
        {
            get
            {
                if (_haloPurple == null || _haloPurple.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.halo_purple.png");
                    Texture2D.TextureDataFromStreamEXT(stream, out int w, out int h, out byte[] pixels, 50, 27);

                    _haloPurple = new UOTexture(w, h);
                    _haloPurple.SetData(pixels);
                }

                return _haloPurple;
            }
        }

        private static UOTexture _haloRed;
        private static UOTexture HaloRed
        {
            get
            {
                if (_haloRed == null || _haloRed.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.halo_red.png");
                    Texture2D.TextureDataFromStreamEXT(stream, out int w, out int h, out byte[] pixels, 50, 27);

                    _haloRed = new UOTexture(w, h);
                    _haloRed.SetData(pixels);
                }

                return _haloRed;
            }
        }

        private static UOTexture _haloWhite;
        private static UOTexture HaloWhite
        {
            get
            {
                if (_haloWhite == null || _haloWhite.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.halo_white.png");
                    Texture2D.TextureDataFromStreamEXT(stream, out int w, out int h, out byte[] pixels, 50, 27);

                    _haloWhite = new UOTexture(w, h);
                    _haloWhite.SetData(pixels);
                }

                return _haloWhite;
            }
        }

        private static UOTexture _haloYellow;
        private static UOTexture HaloYellow
        {
            get
            {
                if (_haloYellow == null || _haloYellow.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.halo_yellow.png");
                    Texture2D.TextureDataFromStreamEXT(stream, out int w, out int h, out byte[] pixels, 50, 27);

                    _haloYellow = new UOTexture(w, h);
                    _haloYellow.SetData(pixels);
                }

                return _haloYellow;
            }
        }


        private static UOTexture _haloOrange;
        private static UOTexture HaloOrange
        {
            get
            {
                if (_haloOrange == null || _haloOrange.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.halo_orange.png");
                    Texture2D.TextureDataFromStreamEXT(stream, out int w, out int h, out byte[] pixels, 50, 27);

                    _haloOrange = new UOTexture(w, h);
                    _haloOrange.SetData(pixels);
                }

                return _haloOrange;
            }
        }
        //TEXTURES END
        public void Draw(UltimaBatcher2D batcher)
        {
           if (!IsEnabled)
            return;

           if (World.Player == null)
            return;
            
            foreach (Mobile mobile in World.Mobiles)
            {
                //SKIP FOR PLAYER
                if (mobile == World.Player)
                    continue;

                _hueVector.Z = 0f;

                //CALC WHERE MOBILE IS
                Point pm = mobile.RealScreenPosition;
                pm.X += (int) mobile.Offset.X + 22;
                pm.Y += (int) (mobile.Offset.Y - mobile.Offset.Z) + 22;
                Point p1 = pm;

                if (IsHalosEnabled && (HaloOrange != null || HaloGreen != null || HaloPurple != null || HaloRed != null))
                {
                    _hueVector.Y = ShaderHueTranslator.SHADER_LIGHTS;

                    //Move from center by texture width
                    pm.X = pm.X - HaloOrange.Width / 2;

                    batcher.SetBlendState(_blend);

                    //PURPLE HALO FOR: LAST ATTACK, LASTTARGET
                    if (TargetManager.LastAttack == mobile.Serial || TargetManager.LastTargetInfo.Serial == mobile.Serial)
                        batcher.Draw2D(HaloPurple, pm.X, pm.Y - 15, HaloPurple.Width, HaloPurple.Height, ref _hueVector);

                    //GREEN HALO FOR: ALLYS AND PARTY
                    else if ((mobile.NotorietyFlag == NotorietyFlag.Ally || World.Party.Contains(mobile.Serial)) && mobile != World.Player)
                        batcher.Draw2D(HaloGreen, pm.X, pm.Y - 15, HaloGreen.Width, HaloGreen.Height, ref _hueVector);

                    //RED HALO FOR: CRIMINALS, GRAY, MURDERER
                    else if (mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray || mobile.NotorietyFlag == NotorietyFlag.Murderer)
                        batcher.Draw2D(HaloRed, pm.X, pm.Y - 15, HaloRed.Width, HaloRed.Height, ref _hueVector);

                    //ORANGE HALO FOR: ENEMY
                    else if (mobile.NotorietyFlag == NotorietyFlag.Enemy)
                        batcher.Draw2D(HaloOrange, pm.X, pm.Y - 15, HaloOrange.Width, HaloOrange.Height, ref _hueVector);

                    batcher.SetBlendState(null);
                }
                //HALO TEXTURE

                //ARROW TEXTURE
                if (!IsArrowEnabled)
                    return;
                
                //CALC MOBILE HEIGHT FROM ANIMATION
                AnimationsLoader.Instance.GetAnimationDimensions(mobile.AnimIndex,
                                                              mobile.GetGraphicForAnimation(),
                                                              /*(byte) m.GetDirectionForAnimation()*/ 0,
                                                              /*Mobile.GetGroupForAnimation(m, isParent:true)*/ 0,
                                                              mobile.IsMounted,
                                                              /*(byte) m.AnimIndex*/ 0,
                                                              out int centerX,
                                                              out int centerY,
                                                              out int width,
                                                              out int height);

                p1.Y -= height + centerY + 8 + 22;

                if (mobile.IsGargoyle && mobile.IsFlying)
                {
                    p1.Y -= 22;
                }
                else if (!mobile.IsMounted)
                {
                    p1.Y += 22;
                }
                
                p1.X -= ArrowRed.Width / 2;
                p1.Y -= ArrowRed.Height / 1;

                if (mobile.ObjectHandlesOpened)
                {
                    p1.Y -= 22;
                }

                /* MAYBE USE THIS INCASE IT SHOWS OUTSIDE OF GAMESCREEN?
                if (!(p1.X < 0 || p1.X > screenW - mobile.HitsTexture.Width || p1.Y < 0 || p1.Y > screenH))
                            {
                                mobile.HitsTexture.Draw(batcher, p1.X, p1.Y);
                            }
                */

                //ARROW TEXTURE
                if (IsArrowEnabled && (ArrowGreen != null || ArrowRed != null || ArrowPurple != null || ArrowOrange != null))
                {
                    _hueVector.Y = ShaderHueTranslator.SHADER_LIGHTS;

                    batcher.SetBlendState(_blend);

                    //PURPLE ARROW FOR: LAST ATTACK, LASTTARGET
                    if (TargetManager.LastAttack == mobile.Serial || TargetManager.LastTargetInfo.Serial == mobile.Serial)
                        batcher.Draw2D(ArrowPurple, p1.X, p1.Y, ArrowPurple.Width, ArrowPurple.Height, ref _hueVector);
                    
                    //GREEN ARROW FOR: ALLYS AND PARTY
                    else if ((mobile.NotorietyFlag == NotorietyFlag.Ally || World.Party.Contains(mobile.Serial)) && mobile != World.Player)
                        batcher.Draw2D(ArrowGreen, p1.X, p1.Y, ArrowGreen.Width, ArrowGreen.Height, ref _hueVector);
                    
                    //RED ARROW FOR: CRIMINALS, GRAY, MURDERER
                    else if (mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray || mobile.NotorietyFlag == NotorietyFlag.Murderer)
                        batcher.Draw2D(ArrowRed, p1.X, p1.Y, ArrowRed.Width, ArrowRed.Height, ref _hueVector);

                    //ORANGE ARROW FOR: ENEMY
                    else if (mobile.NotorietyFlag == NotorietyFlag.Enemy)
                        batcher.Draw2D(ArrowOrange, p1.X, p1.Y, ArrowOrange.Width, ArrowOrange.Height, ref _hueVector);

                    batcher.SetBlendState(null);
                }
                //ARROW TEXTURE
            }
        }
    }
}