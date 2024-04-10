#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    internal class HealthLinesManager
    {
        private const int BAR_WIDTH = 34; //28;
        private const int BAR_HEIGHT = 8;
        private const int BAR_WIDTH_HALF = BAR_WIDTH >> 1;
        private const int BAR_HEIGHT_HALF = BAR_HEIGHT >> 1;

        const ushort BACKGROUND_GRAPHIC = 0x1068;
        const ushort HP_GRAPHIC = 0x1069;

        public bool IsEnabled =>
            ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ShowMobilesHP;

        public void Draw(UltimaBatcher2D batcher)
        {
            var camera = Client.Game.Scene.Camera;

            if (SerialHelper.IsMobile(TargetManager.LastTargetInfo.Serial))
            {
                DrawHealthLineWithMath(
                    batcher,
                    TargetManager.LastTargetInfo.Serial,
                    camera.Bounds.Width,
                    camera.Bounds.Height
                );
                DrawTargetIndicator(batcher, TargetManager.LastTargetInfo.Serial);
            }

            if (SerialHelper.IsMobile(TargetManager.SelectedTarget))
            {
                DrawHealthLineWithMath(
                    batcher,
                    TargetManager.SelectedTarget,
                    camera.Bounds.Width,
                    camera.Bounds.Height
                );
                DrawTargetIndicator(batcher, TargetManager.SelectedTarget);
            }

            if (SerialHelper.IsMobile(TargetManager.LastAttack))
            {
                DrawHealthLineWithMath(
                    batcher,
                    TargetManager.LastAttack,
                    camera.Bounds.Width,
                    camera.Bounds.Height
                );
                DrawTargetIndicator(batcher, TargetManager.LastAttack);
            }

            if (!IsEnabled)
            {
                return;
            }

            int mode = ProfileManager.CurrentProfile.MobileHPType;

            if (mode < 0)
            {
                return;
            }

            int showWhen = ProfileManager.CurrentProfile.MobileHPShowWhen;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile.IsDestroyed)
                {
                    continue;
                }

                int current = mobile.Hits;
                int max = mobile.HitsMax;

                if (max == 0)
                {
                    continue;
                }

                if (showWhen == 1 && current == max)
                {
                    continue;
                }

                Point p = mobile.RealScreenPosition;
                p.X += (int)mobile.Offset.X + 22 + 5;
                p.Y += (int)(mobile.Offset.Y - mobile.Offset.Z) + 22 + 5;

                if (mode != 1 && !mobile.IsDead)
                {
                    if (showWhen == 2 && current != max || showWhen <= 1)
                    {
                        if (mobile.HitsPercentage != 0)
                        {
                            Client.Game.Animations.GetAnimationDimensions(
                                mobile.AnimIndex,
                                mobile.GetGraphicForAnimation(),
                                /*(byte) m.GetDirectionForAnimation()*/
                                0,
                                /*Mobile.GetGroupForAnimation(m, isParent:true)*/
                                0,
                                mobile.IsMounted,
                                /*(byte) m.AnimIndex*/
                                0,
                                out int centerX,
                                out int centerY,
                                out int width,
                                out int height
                            );

                            Point p1 = p;
                            p1.Y -= height + centerY + 8 + 22;

                            if (mobile.IsGargoyle && mobile.IsFlying)
                            {
                                p1.Y -= 22;
                            }
                            else if (!mobile.IsMounted)
                            {
                                p1.Y += 22;
                            }

                            p1 = Client.Game.Scene.Camera.WorldToScreen(p1);
                            p1.X -= (mobile.HitsTexture.Width >> 1) + 5;
                            p1.Y -= mobile.HitsTexture.Height;

                            if (mobile.ObjectHandlesStatus == ObjectHandlesStatus.DISPLAYING)
                            {
                                p1.Y -= Constants.OBJECT_HANDLES_GUMP_HEIGHT + 5;
                            }

                            if (
                                !(
                                    p1.X < 0
                                    || p1.X > camera.Bounds.Width - mobile.HitsTexture.Width
                                    || p1.Y < 0
                                    || p1.Y > camera.Bounds.Height
                                )
                            )
                            {
                                mobile.HitsTexture.Draw(batcher, p1.X, p1.Y);
                            }
                        }
                    }
                }

                if (
                    mobile.Serial == TargetManager.LastTargetInfo.Serial
                    || mobile.Serial == TargetManager.SelectedTarget
                    || mobile.Serial == TargetManager.LastAttack
                )
                {
                    continue;
                }

                p.X -= 5;
                p = Client.Game.Scene.Camera.WorldToScreen(p);
                p.X -= BAR_WIDTH_HALF;
                p.Y -= BAR_HEIGHT_HALF;

                if (p.X < 0 || p.X > camera.Bounds.Width - BAR_WIDTH)
                {
                    continue;
                }

                if (p.Y < 0 || p.Y > camera.Bounds.Height - BAR_HEIGHT)
                {
                    continue;
                }

                if (mode >= 1)
                {
                    DrawHealthLine(batcher, mobile, p.X, p.Y, mobile.Serial != World.Player.Serial);
                }
            }
        }

        private void DrawTargetIndicator(UltimaBatcher2D batcher, uint serial)
        {
            Entity entity = World.Get(serial);

            if (entity == null)
            {
                return;
            }
            if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.ShowTargetIndicator)
            {
                return;
            }
            ref readonly var indicatorInfo = ref Client.Game.Gumps.GetGump(0x756F);
            if (indicatorInfo.Texture != null)
            {
                Point p = entity.RealScreenPosition;
                p.Y += (int)(entity.Offset.Y - entity.Offset.Z) + 22 + 5;

                p = Client.Game.Scene.Camera.WorldToScreen(p);
                p.Y -= entity.FrameInfo.Height + 25;

                batcher.Draw(
                indicatorInfo.Texture,
                new Rectangle(p.X - 24, p.Y, indicatorInfo.UV.Width, indicatorInfo.UV.Height),
                indicatorInfo.UV,
                ShaderHueTranslator.GetHueVector(0, false, 1.0f)
                );
            }
            else
            {
                ProfileManager.CurrentProfile.ShowTargetIndicator = false; //This sprite doesn't exist for this client, lets avoid checking for it every frame.
            }
        }
        private void DrawHealthLineWithMath(
            UltimaBatcher2D batcher,
            uint serial,
            int screenW,
            int screenH
        )
        {
            Entity entity = World.Get(serial);

            if (entity == null)
            {
                return;
            }

            Point p = entity.RealScreenPosition;
            p.X += (int)entity.Offset.X + 22;
            p.Y += (int)(entity.Offset.Y - entity.Offset.Z) + 22 + 5;

            p = Client.Game.Scene.Camera.WorldToScreen(p);
            p.X -= BAR_WIDTH_HALF;
            p.Y -= BAR_HEIGHT_HALF;

            if (p.X < 0 || p.X > screenW - BAR_WIDTH)
            {
                return;
            }

            if (p.Y < 0 || p.Y > screenH - BAR_HEIGHT)
            {
                return;
            }

            DrawHealthLine(batcher, entity, p.X, p.Y, false);
        }

        private void DrawHealthLine(
            UltimaBatcher2D batcher,
            Entity entity,
            int x,
            int y,
            bool passive
        )
        {
            if (entity == null)
            {
                return;
            }

            int multiplier = 1;
            if (ProfileManager.CurrentProfile != null)
                multiplier = ProfileManager.CurrentProfile.HealthLineSizeMultiplier;

            int per = (BAR_WIDTH * multiplier) * entity.HitsPercentage / 100;
            int offset = 2;

            if (per >> 2 == 0)
            {
                offset = per;
            }

            Mobile mobile = entity as Mobile;

            float alpha = passive ? 0.5f : 1.0f;
            ushort hue =
                mobile != null
                    ? Notoriety.GetHue(mobile.NotorietyFlag)
                    : Notoriety.GetHue(NotorietyFlag.Gray);

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue, false, alpha);

            if (mobile == null)
            {
                y += 22;
            }


            ref readonly var gumpInfo = ref Client.Game.Gumps.GetGump(BACKGROUND_GRAPHIC);
            Rectangle bounds = gumpInfo.UV;

            if (multiplier > 1)
                x -= (int)(((BAR_WIDTH * multiplier) / 2) - (BAR_WIDTH / 2));

            batcher.Draw(
                gumpInfo.Texture,
                new Rectangle(x, y, gumpInfo.UV.Width * multiplier, gumpInfo.UV.Height * multiplier),
                gumpInfo.UV,
                hueVec
            );

            hueVec.X = 90;

            if (mobile != null)
            {
                if (mobile.IsPoisoned)
                {
                    hueVec.X = 63;
                }
                else if (mobile.IsYellowHits)
                {
                    hueVec.X = 53;
                }
            }

            float hitPerecentage = (float)entity.Hits / (float)entity.HitsMax;

            if (entity.HitsMax == 0)
                hitPerecentage = 1;

            batcher.Draw(
                SolidColorTextureCache.GetTexture(Color.White),
                new Vector2(x + (3 * multiplier), y + (4 * multiplier)),
                new Rectangle(0, 0, (int)(((BAR_WIDTH * multiplier) - (6 * multiplier)) * hitPerecentage), (bounds.Height * multiplier) - (6 * multiplier)),
                hueVec
                );
        }
    }
}
