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

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Managers
{
    internal class HealthLinesManager
    {
        const int BAR_WIDTH = 34; //28;
        const int BAR_HEIGHT = 8;
        const int BAR_WIDTH_HALF = BAR_WIDTH >> 1;
        const int BAR_HEIGHT_HALF = BAR_HEIGHT >> 1;

        public bool IsEnabled => ProfileManager.Current != null && ProfileManager.Current.ShowMobilesHP;
        private Vector3 _vectorHue = Vector3.Zero;



        private readonly UOTexture16 _background_texture, _hp_texture;

        public HealthLinesManager()
        {
            _background_texture = GumpsLoader.Instance.GetTexture(0x1068);
            _hp_texture = GumpsLoader.Instance.GetTexture(0x1069);
        }


        public void Update()
        {
            if (_background_texture != null)
                _background_texture.Ticks = Time.Ticks;

            if (_hp_texture != null)
                _hp_texture.Ticks = Time.Ticks;
        }

        public void Draw(UltimaBatcher2D batcher, float scale)
        {
            int screenX = ProfileManager.Current.GameWindowPosition.X;
            int screenY = ProfileManager.Current.GameWindowPosition.Y;
            int screenW = ProfileManager.Current.GameWindowSize.X;
            int screenH = ProfileManager.Current.GameWindowSize.Y;

            if (SerialHelper.IsMobile(TargetManager.LastTargetInfo.Serial))
                DrawHealthLineWithMath(batcher, TargetManager.LastTargetInfo.Serial, screenX, screenY, screenW, screenH, scale);
            if (SerialHelper.IsMobile(TargetManager.SelectedTarget))
                DrawHealthLineWithMath(batcher, TargetManager.SelectedTarget, screenX, screenY, screenW, screenH, scale);
            if (SerialHelper.IsMobile(TargetManager.LastAttack))
                DrawHealthLineWithMath(batcher, TargetManager.LastAttack, screenX, screenY, screenW, screenH, scale);

            if (!IsEnabled)
            {
                return;
            }


            int mode = ProfileManager.Current.MobileHPType;

            if (mode < 0)
                return;

            int showWhen = ProfileManager.Current.MobileHPShowWhen;

            foreach (Mobile mobile in World.Mobiles)
            {
                //if (World.Party.Contains(mobile) && mobile.Tile == null)
                //    continue;

                int current = mobile.Hits;
                int max = mobile.HitsMax;

                if (showWhen == 1 && current == max)
                    continue;

                int x = screenX + mobile.RealScreenPosition.X;
                int y = screenY + mobile.RealScreenPosition.Y;

                x += (int) mobile.Offset.X + 22 ;
                y += (int) (mobile.Offset.Y - mobile.Offset.Z) + 22;

                x = (int) (x / scale);
                y = (int) (y / scale);
                x -= (int) (screenX / scale);
                y -= (int) (screenY / scale);
                x += screenX;
                y += screenY;

                x += 5;
                y += 5;

                x -= BAR_WIDTH_HALF;
                y -= BAR_HEIGHT_HALF;

                if (mode != 1 && !mobile.IsDead)
                {
                    if ((showWhen == 2 && current != max) || showWhen <= 1)
                    {
                        int xx = x;
                        int yy = y;

                        if (mobile.IsGargoyle && mobile.IsFlying)
                            yy -= (int) (22 / scale);
                        else if (!mobile.IsMounted)
                            yy += (int) (22 / scale);


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

                       
                        yy -= (int) ((height + centerY + 28) / scale);


                       

                        if (mobile.HitsPercentage != 0)
                        {
                            xx -= (mobile.HitsTexture.Width >> 1) + 3;
                            xx += 22;
                            yy -= mobile.HitsTexture.Height / 1;
                            if (mobile.ObjectHandlesOpened)
                                yy -= 22;

                            if (!(xx < screenX || xx > screenX + screenW - mobile.HitsTexture.Width || yy < screenY || yy > screenY + screenH))
                                mobile.HitsTexture.Draw(batcher, xx, yy);
                        }
                    }
                }

                if (x < screenX || x > screenX + screenW - BAR_WIDTH)
                    continue;

                if (y < screenY || y > screenY + screenH - BAR_HEIGHT)
                    continue;

                if (mode >= 1 && TargetManager.LastTargetInfo.Serial != mobile)
                {
                    // already done
                    if (mobile == TargetManager.LastTargetInfo.Serial || 
                        mobile == TargetManager.SelectedTarget ||
                        mobile == TargetManager.LastAttack)
                        continue;


                    DrawHealthLine(batcher, mobile, x, y, mobile != World.Player);
                }
            }
        }

        private void DrawHealthLineWithMath(UltimaBatcher2D batcher, uint serial, int screenX, int screenY, int screenW, int screenH, float scale)
        {
            Mobile mobile = World.Mobiles.Get(serial);
            if (mobile == null)
                return;

            int x = screenX + mobile.RealScreenPosition.X;
            int y = screenY + mobile.RealScreenPosition.Y;

            x += (int) mobile.Offset.X + 22;
            y += (int) (mobile.Offset.Y - mobile.Offset.Z) + 22;

            x = (int) (x / scale);
            y = (int) (y / scale);
            x -= (int) (screenX / scale);
            y -= (int) (screenY / scale);
            x += screenX;
            y += screenY;

            x += 5;
            y += 5;

            x -= BAR_WIDTH_HALF;
            y -= BAR_HEIGHT_HALF;

            if (x < screenX || x > screenX + screenW - BAR_WIDTH)
                return;

            if (y < screenY || y > screenY + screenH - BAR_HEIGHT)
                return;

            DrawHealthLine(batcher, mobile, x, y, false);
        }

        private void DrawHealthLine(UltimaBatcher2D batcher, Mobile mobile, int x, int y, bool passive)
        {
            if (mobile == null)
                return;

            int per = BAR_WIDTH * mobile.HitsPercentage / 100;


            float alpha = passive ? 0.5f : 0.0f;

            _vectorHue.X = Notoriety.GetHue(mobile.NotorietyFlag);
            _vectorHue.Y = 1;
            _vectorHue.Z = alpha;


            const int MULTIPLER = 1;

            batcher.Draw2D(_background_texture,
                           x, y,
                           _background_texture.Width * MULTIPLER,
                           _background_texture.Height * MULTIPLER,
                           ref _vectorHue);


            _vectorHue.X = 0x21;



            if (mobile.Hits != mobile.HitsMax || mobile.HitsMax == 0)
            {
                int offset = 2;

                if (per >> 2 == 0)
                {
                    offset = per;
                }

                batcher.Draw2DTiled(_hp_texture,
                                    x + per * MULTIPLER - offset, y,
                                    (BAR_WIDTH - per) * MULTIPLER - offset / 2,
                                    _hp_texture.Height * MULTIPLER,
                                    ref _vectorHue);
            }
            
            ushort hue = 90;

            if (per > 0)
            {
                if (mobile.IsPoisoned)
                {
                    hue = 63;
                }
                else if (mobile.IsYellowHits)
                {
                    hue = 53;
                }

                _vectorHue.X = hue;


                batcher.Draw2DTiled(_hp_texture,
                               x, y,
                               per * MULTIPLER,
                               _hp_texture.Height * MULTIPLER,
                               ref _vectorHue);
            }
           
        }
    }
}