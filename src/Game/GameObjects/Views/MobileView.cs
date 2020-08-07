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
using System.Collections.Generic;

using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Mobile
    {
        private static ushort _viewHue;
        private static EquipConvData? _equipConvData;
        private static bool _transform;
        private static int _characterFrameStartY;
        private static int _startCharacterWaistY;
        private static int _startCharacterKneesY;
        private static int _startCharacterFeetY;
        private static int _characterFrameHeight;


       

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            ResetHueVector();

            _equipConvData = null;
            _transform = false;
            AnimationsLoader.Instance.SittingValue = 0;
            FrameInfo.X = 0;
            FrameInfo.Y = 0;
            FrameInfo.Width = 0;
            FrameInfo.Height = 0;

            posY -= 3;
            int drawX = posX + (int) Offset.X;
            int drawY = posY + (int) (Offset.Y - Offset.Z);

            drawX += 22;
            drawY += 22;

            bool hasShadow = !IsDead && !IsHidden && ProfileManager.Current.ShadowsEnabled;

            if (AuraManager.IsEnabled)
            {
                AuraManager.Draw(batcher, drawX, drawY, ProfileManager.Current.PartyAura && World.Party.Contains(this) ? ProfileManager.Current.PartyAuraHue : Notoriety.GetHue(NotorietyFlag));
            }

            bool isHuman = IsHuman;
            bool isGargoyle = Client.Version >= ClientVersion.CV_7000 && (Graphic == 666 || Graphic == 667 || Graphic == 0x02B7 || Graphic == 0x02B6);

            if (AlphaHue != 255)
                HueVector.Z = 1f - AlphaHue / 255f;

            if (ProfileManager.Current.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                _viewHue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                HueVector.Y = 1;
            }
            else if (SelectedObject.HealthbarObject == this)
            {
                _viewHue = Notoriety.GetHue(NotorietyFlag);
            }
            else if (ProfileManager.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                _viewHue = Constants.OUT_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (World.Player.IsDead && ProfileManager.Current.EnableBlackWhiteEffect)
            {
                _viewHue = Constants.DEAD_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (IsHidden)
                _viewHue = 0x038E;
            else
            {
                _viewHue = 0;

                if (IsDead)
                {
                    if (!isHuman)
                        _viewHue = 0x0386;
                }
                else if (ProfileManager.Current.HighlightMobilesByFlags)
                {
                    if (IsPoisoned)
                        _viewHue = ProfileManager.Current.PoisonHue;

                    if (IsParalyzed)
                        _viewHue = ProfileManager.Current.ParalyzedHue;

                    if (NotorietyFlag != NotorietyFlag.Invulnerable && IsYellowHits)
                        _viewHue = ProfileManager.Current.InvulnerableHue;
                }
            }


            bool isAttack = Serial == TargetManager.LastAttack;
            bool isUnderMouse = TargetManager.IsTargeting && SelectedObject.LastObject == this;
            //bool needHpLine = false;

            if (this != World.Player)
            {
                if (isAttack || isUnderMouse)
                    _viewHue = Notoriety.GetHue(NotorietyFlag);

                //if (this == TargetManager.LastTarget)
                //{
                //    UIManager.SetTargetLineGump(this);
                //    //needHpLine = true;
                //}
            }



            ProcessSteps(out byte dir);
            byte layerDir = dir;

            AnimationsLoader.Instance.GetAnimDirection(ref dir, ref IsFlipped);

            ushort graphic = GetGraphicForAnimation();
            byte animGroup = GetGroupForAnimation(this, graphic, true);
            sbyte animIndex = AnimIndex;

            AnimationsLoader.Instance.Direction = dir;
            AnimationsLoader.Instance.AnimGroup = animGroup;

            Item mount = FindItemByLayer(Layer.Mount);

            if (isHuman && mount != null)
            {
                AnimationsLoader.Instance.SittingValue = 0;

                ushort mountGraphic = mount.GetGraphicForAnimation();

                if (mountGraphic != 0xFFFF) 
                {
                    if (hasShadow)
                    {
                        DrawInternal(batcher, this, null, drawX, drawY + 10, IsFlipped, animIndex, true, graphic, isHuman, alpha: HueVector.Z);
                        AnimationsLoader.Instance.AnimGroup = GetGroupForAnimation(this, mountGraphic);
                        DrawInternal(batcher, this, mount, drawX, drawY, IsFlipped, animIndex, true, mountGraphic, isHuman, alpha: HueVector.Z);
                    }
                    else
                        AnimationsLoader.Instance.AnimGroup = GetGroupForAnimation(this, mountGraphic);

                    drawY += DrawInternal(batcher, this, mount, drawX, drawY, IsFlipped, animIndex, false, mountGraphic, isHuman, isMount: true, alpha: HueVector.Z);
                }
            }
            else
            {
                if ((AnimationsLoader.Instance.SittingValue = IsSitting()) != 0)
                {
                    animGroup = (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND;
                    animIndex = 0;

                    ProcessSteps(out dir);
                    AnimationsLoader.Instance.Direction = dir;
                    AnimationsLoader.Instance.FixSittingDirection(ref dir, ref IsFlipped, ref drawX, ref drawY);

                    if (AnimationsLoader.Instance.Direction == 3)
                    {
                        if (IsGargoyle)
                        {
                            drawY -= 30;
                            animGroup = 42;
                        }
                        else
                            animGroup = 25;
                    }
                    else if (IsGargoyle)
                    {
                        animGroup = 42;
                    }
                    else
                        _transform = true;
                }
                else if (hasShadow)
                    DrawInternal(batcher, this, null, drawX, drawY, IsFlipped, animIndex, true, graphic, isHuman, alpha: HueVector.Z);
            }

            AnimationsLoader.Instance.AnimGroup = animGroup;


            DrawInternal(batcher, this, null, drawX, drawY, IsFlipped, animIndex, false, graphic, isHuman, alpha: HueVector.Z);

            for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[layerDir, i];

                Item item = FindItemByLayer(layer);

                if (item == null)
                    continue;

                if (IsDead && (layer == Layer.Hair || layer == Layer.Beard))
                    continue;

                if (isHuman)
                {
                    if (IsCovered(this, layer))
                        continue;

                    if (item.ItemData.AnimID != 0)
                    {
                        graphic = item.ItemData.AnimID;

                        if (isGargoyle)
                        {
                            if (graphic == 469)
                            {
                                // gargoyle robe
                                graphic = 342;
                            }
                            else if (graphic == 0x03CA)
                            {
                                // gargoyle dead shroud
                                graphic = 0x0223;
                            }
                        }

                        if (AnimationsLoader.Instance.EquipConversions.TryGetValue(Graphic, out Dictionary<ushort, EquipConvData> map))
                        {
                            if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
                            {
                                _equipConvData = data;
                                graphic = data.Graphic;
                            }
                        }

                        if (AnimationsLoader.Instance.SittingValue == 0 && IsGargoyle && item.ItemData.IsWeapon)
                        {
                            AnimationsLoader.Instance.AnimGroup = GetGroupForAnimation(this, graphic);
                            DrawInternal(batcher, this, item, drawX, drawY, IsFlipped, animIndex, false, graphic, isHuman, false, alpha: HueVector.Z);
                            AnimationsLoader.Instance.AnimGroup = animGroup;
                        }
                        else
                        {
                            DrawInternal(batcher, this, item, drawX, drawY, IsFlipped, animIndex, false, graphic, isHuman, false, alpha: HueVector.Z);
                        }
                    }
                    else
                    {
                        if (item.ItemData.IsLight)
                            Client.Game.GetScene<GameScene>().AddLight(this, this, drawX, drawY);
                    }

                    _equipConvData = null;
                }
                else
                {
                    if (item.ItemData.IsLight)
                    {
                        Client.Game.GetScene<GameScene>().AddLight(this, this, drawX, drawY);
                        break;
                    }
                }
            }

            //if (FileManager.Animations.SittingValue != 0)
            //{
            //    ref var sittingData = ref FileManager.Animations.SittingInfos[FileManager.Animations.SittingValue - 1];

            //    if (FileManager.Animations.Direction == 3 && sittingData.DrawBack &&
            //        HasEquipment && Equipment[(int) Layer.Cloak] == null)
            //    {

            //    }
            //}
            // 

            FrameInfo.X = Math.Abs(FrameInfo.X);
            FrameInfo.Y = Math.Abs(FrameInfo.Y);
            FrameInfo.Width = FrameInfo.X + FrameInfo.Width;
            FrameInfo.Height = FrameInfo.Y + FrameInfo.Height;

            return true;
        }

        private static sbyte DrawInternal(UltimaBatcher2D batcher,
                                           Mobile owner,
                                           Item entity,
                                           int x,
                                           int y,
                                           bool mirror,
                                           sbyte frameIndex,
                                           bool hasShadow,
                                           ushort id,
                                           bool isHuman,
                                           bool isParent = true,
                                           bool isMount = false,
                                           float alpha = 0)
        {
            if (id >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT || owner == null)
                return 0;

            ushort hueFromFile = _viewHue;
            byte animGroup = AnimationsLoader.Instance.AnimGroup;

            AnimationDirection direction = AnimationsLoader.Instance.GetBodyAnimationGroup(ref id, ref animGroup, ref hueFromFile, isParent).Direction[AnimationsLoader.Instance.Direction];
            AnimationsLoader.Instance.AnimID = id;

            if (direction == null || direction.Address == -1 || direction.FileIndex == -1)
            {
                if (!(_transform && entity == null && !hasShadow))
                    return 0;
            }

            if (direction == null || ((direction.FrameCount == 0 || direction.Frames == null) && !AnimationsLoader.Instance.LoadDirectionGroup(ref direction)))
            {
                if (!(_transform && entity == null && !hasShadow))
                    return 0;
            }

            if (direction == null)
                return 0;

            direction.LastAccessTime = Time.Ticks;

            int fc = direction.FrameCount;

            if ((fc > 0 && frameIndex >= fc) || frameIndex < 0)
                frameIndex = 0;

            if (frameIndex < direction.FrameCount)
            {
                var frame = direction.Frames[frameIndex];

                if (frame == null || frame.IsDisposed)
                {
                    if (!(_transform && entity == null && !hasShadow))
                        return 0;

                    goto SKIP;
                }

                frame.Ticks = Time.Ticks;

                if (mirror)
                    x -= frame.Width - frame.CenterX;
                else
                    x -= frame.CenterX;

                y -= frame.Height + frame.CenterY;

                SKIP:

                if (hasShadow)
                    batcher.DrawSpriteShadow(frame, x, y, mirror);
                else
                {
                    ushort hue = _viewHue;
                    bool partialHue = false;

                    if (hue == 0)
                    {
                        hue = entity?.Hue ?? owner.Hue;
                        partialHue = !isMount && entity != null && entity.ItemData.IsPartialHue;

                        if ((hue & 0x8000) != 0)
                        {
                            partialHue = true;
                            hue &= 0x7FFF;
                        }

                        if (hue == 0)
                        {
                            hue = hueFromFile;

                            if (hue == 0 && _equipConvData.HasValue)
                                hue = _equipConvData.Value.Color;

                            partialHue = false;
                        }
                    }
                    ResetHueVector();
                    ShaderHuesTraslator.GetHueVector(ref HueVector, hue, partialHue, alpha);

                    if (_transform)
                    {
                        const float UPPER_BODY_RATIO = 0.35f;
                        const float MID_BODY_RATIO = 0.60f;
                        const float LOWER_BODY_RATIO = 0.94f;

                        if (entity == null && isHuman)
                        {
                            int frameHeight = frame?.Height ?? 61;
                            _characterFrameStartY = y - (frame != null ? 0 : (frameHeight - 4));
                            _characterFrameHeight = frameHeight;
                            _startCharacterWaistY = (int) (frameHeight * UPPER_BODY_RATIO) + _characterFrameStartY;
                            _startCharacterKneesY = (int) (frameHeight * MID_BODY_RATIO) + _characterFrameStartY;
                            _startCharacterFeetY = (int) (frameHeight * LOWER_BODY_RATIO) + _characterFrameStartY;

                            if (frame == null)
                                return 0;
                        }

                        float h3mod = UPPER_BODY_RATIO;
                        float h6mod = MID_BODY_RATIO;
                        float h9mod = LOWER_BODY_RATIO;


                        if (entity != null)
                        {
                            float itemsEndY = y + frame.Height;

                            if (y >= _startCharacterWaistY)
                                h3mod = 0;
                            else if (itemsEndY <= _startCharacterWaistY)
                                h3mod = 1.0f;
                            else
                            {
                                float upperBodyDiff = _startCharacterWaistY - y;
                                h3mod = upperBodyDiff / frame.Height;

                                if (h3mod < 0)
                                    h3mod = 0;
                            }


                            if (_startCharacterWaistY >= itemsEndY || y >= _startCharacterKneesY)
                                h6mod = 0;
                            else if (_startCharacterWaistY <= y && itemsEndY <= _startCharacterKneesY)
                                h6mod = 1.0f;
                            else
                            {
                                float midBodyDiff;

                                if (y >= _startCharacterWaistY)
                                    midBodyDiff = _startCharacterKneesY - y;
                                else if (itemsEndY <= _startCharacterKneesY)
                                    midBodyDiff = itemsEndY - _startCharacterWaistY;
                                else
                                    midBodyDiff = _startCharacterKneesY - _startCharacterWaistY;

                                h6mod = h3mod + midBodyDiff / frame.Height;

                                if (h6mod < 0)
                                    h6mod = 0;
                            }


                            if (itemsEndY <= _startCharacterKneesY)
                                h9mod = 0;
                            else if (y >= _startCharacterKneesY)
                                h9mod = 1.0f;
                            else
                            {
                                float lowerBodyDiff = itemsEndY - _startCharacterKneesY;
                                h9mod = h6mod + lowerBodyDiff / frame.Height;

                                if (h9mod < 0)
                                    h9mod = 0;
                            }
                        }

                        batcher.DrawCharacterSitted(frame, x, y, mirror, h3mod, h6mod, h9mod, ref HueVector);
                    }
                    else if (frame != null)
                    {
                        batcher.DrawSprite(frame, x, y, mirror, ref HueVector);

                        int yy = -(frame.Height + frame.CenterY + 3);
                        int xx = -frame.CenterX;

                        if (mirror)
                            xx = -(frame.Width - frame.CenterX);

                        if (xx < owner.FrameInfo.X)
                            owner.FrameInfo.X = xx;

                        if (yy < owner.FrameInfo.Y)
                            owner.FrameInfo.Y = yy;

                        if (owner.FrameInfo.Width < xx + frame.Width)
                            owner.FrameInfo.Width = xx + frame.Width;

                        if (owner.FrameInfo.Height < yy + frame.Height)
                            owner.FrameInfo.Height = yy + frame.Height;
                    }

                    if (frame.Contains(mirror ? x + frame.Width - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - x, SelectedObject.TranslatedMousePositionByViewport.Y - y))
                    {
                        SelectedObject.Object = owner;
                    }

                    if (entity != null && entity.ItemData.IsLight)
                        Client.Game.GetScene<GameScene>().AddLight(owner, entity, mirror ? x + frame.Width : x, y);
                }

                return AnimationsLoader.Instance.DataIndex[id].MountedHeightOffset;
            }

            return 0;
        }

        internal static bool IsCovered(Mobile mobile, Layer layer)
        {
            if (mobile.IsEmpty)
                return false;

            switch (layer)
            {
                case Layer.Shoes:
                    Item pants = mobile.FindItemByLayer(Layer.Pants);
                    Item robe;

                    if (mobile.FindItemByLayer(Layer.Legs) != null || pants != null && (pants.Graphic == 0x1411 || pants.Graphic == 0x141A))
                        return true;
                    else
                    {
                        robe = mobile.FindItemByLayer(Layer.Robe);

                        if (pants != null && (pants.Graphic == 0x0513 || pants.Graphic == 0x0514) || robe != null && robe.Graphic == 0x0504)
                            return true;
                    }

                    break;

                case Layer.Pants:
                    Item skirt;
                    robe = mobile.FindItemByLayer(Layer.Robe);
                    pants = mobile.FindItemByLayer(Layer.Pants);

                    if (mobile.FindItemByLayer(Layer.Legs) != null || robe != null && robe.Graphic == 0x0504)
                        return true;

                    if (pants != null && (pants.Graphic == 0x01EB || pants.Graphic == 0x03E5 || pants.Graphic == 0x03eB))
                    {
                        skirt = mobile.FindItemByLayer(Layer.Skirt);

                        if (skirt != null && skirt.Graphic != 0x01C7 && skirt.Graphic != 0x01E4)
                            return true;

                        if (robe != null && robe.Graphic != 0x0229 && (robe.Graphic <= 0x04E7 || robe.Graphic > 0x04EB))
                            return true;
                    }

                    break;

                case Layer.Tunic:
                    robe = mobile.FindItemByLayer(Layer.Robe);
                    Item tunic = mobile.FindItemByLayer(Layer.Tunic);

                    /*if (robe != null && robe.Graphic != 0)
                        return true;
                    else*/
                    if (tunic != null && tunic.Graphic == 0x0238)
                        return robe != null && robe.Graphic != 0x9985 && robe.Graphic != 0x9986 && robe.Graphic != 0xA412;

                    break;

                case Layer.Torso:
                    robe = mobile.FindItemByLayer(Layer.Robe);

                    if (robe != null && robe.Graphic != 0 && robe.Graphic != 0x9985 && robe.Graphic != 0x9986 && robe.Graphic != 0xA412)
                        return true;
                    else
                    {
                        Item torso = mobile.FindItemByLayer(Layer.Torso);

                        if (torso != null && (torso.Graphic == 0x782A || torso.Graphic == 0x782B))
                            return true;
                    }

                    break;

                case Layer.Arms:
                    robe = mobile.FindItemByLayer(Layer.Robe);

                    return robe != null && robe.Graphic != 0 && robe.Graphic != 0x9985 && robe.Graphic != 0x9986 && robe.Graphic != 0xA412;

                case Layer.Helmet:
                case Layer.Hair:
                    robe = mobile.FindItemByLayer(Layer.Robe);

                    if (robe != null)
                    {
                        if (robe.Graphic > 0x3173)
                        {
                            if (robe.Graphic == 0x4B9D || robe.Graphic == 0x7816)
                                return true;
                        }
                        else
                        {
                            if (robe.Graphic <= 0x2687)
                            {
                                if (robe.Graphic < 0x2683)
                                    return robe.Graphic >= 0x204E && robe.Graphic <= 0x204F;

                                return true;
                            }

                            if (robe.Graphic == 0x2FB9 || robe.Graphic == 0x3173)
                                return true;
                        }
                    }

                    break;
                /*case Layer.Skirt:
                    skirt = mobile.FindItemByLayer( Layer.Skirt];

                    break;*/
            }

            return false;
        }
    }
}