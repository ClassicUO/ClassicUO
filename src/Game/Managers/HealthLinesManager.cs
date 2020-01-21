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

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Managers
{
    internal class HealthLinesManager
    {
        public bool IsEnabled => ProfileManager.Current != null && ProfileManager.Current.ShowMobilesHP;


        private Vector3 _vectorHue = Vector3.Zero;


        private static readonly Texture2D _edge, _back; 
        static HealthLinesManager()
        {
            _edge = Texture2DCache.GetTexture(Color.Black);
            _back = Texture2DCache.GetTexture(Color.Red);
        }


        public void Draw(UltimaBatcher2D batcher, float scale)
        {
            if (!IsEnabled)
                return;

            const int BAR_WIDTH = 28;
            const int BAR_HEIGHT = 3;
            const int BAR_WIDTH_HALF = BAR_WIDTH >> 1;
            const int BAR_HEIGHT_HALF = BAR_HEIGHT >> 1;

            int screenX = ProfileManager.Current.GameWindowPosition.X;
            int screenY = ProfileManager.Current.GameWindowPosition.Y;
            int screenW = ProfileManager.Current.GameWindowSize.X;
            int screenH = ProfileManager.Current.GameWindowSize.Y;

            

            Color color;

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


                        int ww = mobile.HitsMax;

                        if (ww > 0)
                        {
                            ww = mobile.Hits * 100 / ww;

                            if (ww > 100)
                                ww = 100;
                            else if (ww < 1)
                                ww = 0;

                            mobile.UpdateHits((byte) ww);
                        }

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

                if (mode >= 1 && TargetManager.LastTarget != mobile)
                {
                    if (max > 0)
                    {
                        max = current * 100 / max;

                        if (max > 100)
                            max = 100;

                        if (max > 1)
                            max = BAR_WIDTH * max / 100;
                    }

                    batcher.Draw2D(_edge, x - 1, y - 1, BAR_WIDTH + 2, BAR_HEIGHT + 2, ref _vectorHue);
                    batcher.Draw2D(_back, x, y, BAR_WIDTH, BAR_HEIGHT, ref _vectorHue);

                    if (mobile.IsParalyzed)
                        color = Color.AliceBlue;
                    else if (mobile.IsYellowHits)
                        color = Color.Orange;
                    else if (mobile.IsPoisoned)
                        color = Color.LimeGreen;
                    else
                        color = Color.CornflowerBlue;

                    batcher.Draw2D(Texture2DCache.GetTexture(color), x, y, max, BAR_HEIGHT, ref _vectorHue);
                }

               
            }
        }
    }
}