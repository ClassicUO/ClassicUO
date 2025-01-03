// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using ClassicUO.Game.Scenes;
using ClassicUO.Renderer.Animations;

namespace ClassicUO.Game.Managers
{
    internal sealed class HealthLinesManager
    {
        private const int BAR_WIDTH = 34; //28;
        private const int BAR_HEIGHT = 8;
        private const int BAR_WIDTH_HALF = BAR_WIDTH >> 1;
        private const int BAR_HEIGHT_HALF = BAR_HEIGHT >> 1;

        const ushort BACKGROUND_GRAPHIC = 0x1068;
        const ushort HP_GRAPHIC = 0x1069;

        private readonly World _world;

        public HealthLinesManager(World world) { _world = world; }

        public bool IsEnabled =>
            ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ShowMobilesHP;

        public void Draw(UltimaBatcher2D batcher)
        {
            var camera = Client.Game.Scene.Camera;
            int mode = ProfileManager.CurrentProfile.MobileHPType;

            if (mode < 0)
            {
                return;
            }

            int showWhen = ProfileManager.CurrentProfile.MobileHPShowWhen;
            var useNewTargetSystem = ProfileManager.CurrentProfile.UseNewTargetSystem;
            var animations = Client.Game.UO.Animations;
            var isEnabled = IsEnabled;

            foreach (Mobile mobile in _world.Mobiles.Values)
            {
                if (mobile.IsDestroyed)
                {
                    continue;
                }

                var newTargSystem = false;
                var forceDraw = false;
                var passive = mobile.Serial != _world.Player.Serial;

                if (_world.TargetManager.LastTargetInfo.Serial == mobile ||
                    _world.TargetManager.LastAttack == mobile ||
                    _world.TargetManager.SelectedTarget == mobile ||
                    _world.TargetManager.NewTargetSystemSerial == mobile)
                {
                    newTargSystem = useNewTargetSystem && _world.TargetManager.NewTargetSystemSerial == mobile;
                    passive = false;
                    forceDraw = true;
                }

                int current = mobile.Hits;
                int max = mobile.HitsMax;

                if (!newTargSystem)
                {
                    if (max == 0)
                    {
                        continue;
                    }

                    if (showWhen == 1 && current == max)
                    {
                        continue;
                    }
                }

                Point p = mobile.RealScreenPosition;
                p.X += (int)mobile.Offset.X + 22 + 5;
                p.Y += (int)(mobile.Offset.Y - mobile.Offset.Z) + 22 + 5;
                var offsetY = 0;

                if (isEnabled)
                {
                    if (mode != 1 && !mobile.IsDead)
                    {
                        if (showWhen == 2 && current != max || showWhen <= 1)
                        {
                            if (mobile.HitsPercentage != 0)
                            {
                                animations.GetAnimationDimensions(
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
                                    offsetY += Constants.OBJECT_HANDLES_GUMP_HEIGHT + 5;
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

                                if (newTargSystem)
                                {
                                    offsetY += mobile.HitsTexture.Height;
                                }
                            }
                        }
                    }
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

                if ((isEnabled && mode >= 1) || newTargSystem || forceDraw)
                {
                    DrawHealthLine(batcher, mobile, p.X, p.Y, offsetY, passive, newTargSystem);
                }
            }
        }

        private void DrawHealthLine(
            UltimaBatcher2D batcher,
            Entity entity,
            int x,
            int y,
            int offsetY,
            bool passive,
            bool newTargetSystem
        )
        {
            if (entity == null)
            {
                return;
            }

            int per = BAR_WIDTH * entity.HitsPercentage / 100;

            Mobile mobile = entity as Mobile;

            float alpha = passive && !newTargetSystem ? 0.5f : 1.0f;
            ushort hue =
                mobile != null
                    ? Notoriety.GetHue(mobile.NotorietyFlag)
                    : Notoriety.GetHue(NotorietyFlag.Gray);

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue, false, alpha);

            if (mobile == null)
            {
                y += 22;
            }

            const int MULTIPLER = 1;

            if (newTargetSystem && mobile != null && mobile.Serial != _world.Player.Serial)
            {
                Client.Game.UO.Animations.GetAnimationDimensions(
                    mobile.AnimIndex,
                    mobile.GetGraphicForAnimation(),
                    (byte) mobile.GetDirectionForAnimation(),
                    Mobile.GetGroupForAnimation(mobile, isParent: true),
                    mobile.IsMounted,
                    0, //mobile.AnimIndex,
                    out int centerX,
                    out int centerY,
                    out int width,
                    out int height
                );

                uint topGump;
                uint bottomGump;
                uint gumpHue = 0x7570;
                if (width >= 80)
                {
                    topGump = 0x756D;
                    bottomGump = 0x756A;
                }
                else if (width >= 40)
                {
                    topGump = 0x756E;
                    bottomGump = 0x756B;
                }
                else
                {
                    topGump = 0x756F;
                    bottomGump = 0x756C;
                }

                ref readonly var hueGumpInfo = ref Client.Game.UO.Gumps.GetGump(gumpHue);
                var targetX = x + BAR_WIDTH_HALF - hueGumpInfo.UV.Width / 2f;
                var topTargetY = height + centerY + 8 + 22 + offsetY;

                ref readonly var newTargGumpInfo = ref Client.Game.UO.Gumps.GetGump(topGump);
                if (newTargGumpInfo.Texture != null)
                    batcher.Draw(
                        newTargGumpInfo.Texture,
                        new Vector2(targetX, y - topTargetY),
                        newTargGumpInfo.UV,
                        hueVec
                    );

                if (hueGumpInfo.Texture != null)
                    batcher.Draw(
                        hueGumpInfo.Texture,
                        new Vector2(targetX, y - topTargetY),
                        hueGumpInfo.UV,
                        hueVec
                    );

                y += 7 + newTargGumpInfo.UV.Height / 2 - centerY;

                newTargGumpInfo = ref Client.Game.UO.Gumps.GetGump(bottomGump);
                if (newTargGumpInfo.Texture != null)
                    batcher.Draw(
                        newTargGumpInfo.Texture,
                        new Vector2(targetX, y - 1 - newTargGumpInfo.UV.Height / 2f),
                        newTargGumpInfo.UV,
                        hueVec
                    );
            }


            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(BACKGROUND_GRAPHIC);

            batcher.Draw(
                gumpInfo.Texture,
                new Rectangle(x, y, gumpInfo.UV.Width * MULTIPLER, gumpInfo.UV.Height * MULTIPLER),
                gumpInfo.UV,
                hueVec
            );

            hueVec.X = 0x21;

            if (entity.Hits != entity.HitsMax || entity.HitsMax == 0)
            {
                int offset = 2;

                if (per >> 2 == 0)
                {
                    offset = per;
                }

                gumpInfo = ref Client.Game.UO.Gumps.GetGump(HP_GRAPHIC);

                batcher.DrawTiled(
                    gumpInfo.Texture,
                    new Rectangle(
                        x + per * MULTIPLER - offset,
                        y,
                        (BAR_WIDTH - per) * MULTIPLER - offset / 2,
                        gumpInfo.UV.Height * MULTIPLER
                    ),
                    gumpInfo.UV,
                    hueVec
                );
            }

            hue = 90;

            if (per > 0)
            {
                if (mobile != null)
                {
                    if (mobile.IsPoisoned)
                    {
                        hue = 63;
                    }
                    else if (mobile.IsYellowHits)
                    {
                        hue = 53;
                    }
                }

                hueVec.X = hue;

                gumpInfo = ref Client.Game.UO.Gumps.GetGump(HP_GRAPHIC);
                batcher.DrawTiled(
                    gumpInfo.Texture,
                    new Rectangle(x, y, per * MULTIPLER, gumpInfo.UV.Height * MULTIPLER),
                    gumpInfo.UV,
                    hueVec
                );
            }
        }
    }
}
