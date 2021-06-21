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

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Mobile
    {
        private const int SIT_OFFSET_Y = 4;
        private static ushort _viewHue;
        private static EquipConvData? _equipConvData;
        private static bool _transform;
        private static int _characterFrameStartY;
        private static int _startCharacterWaistY;
        private static int _startCharacterKneesY;
        private static int _startCharacterFeetY;
        private static int _characterFrameHeight;

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, ref Vector3 hueVec)
        {
            hueVec = Vector3.Zero;

            AnimationsLoader.SittingInfoData seatData = AnimationsLoader.SittingInfoData.Empty;
            _equipConvData = null;
            _transform = false;
            FrameInfo.X = 0;
            FrameInfo.Y = 0;
            FrameInfo.Width = 0;
            FrameInfo.Height = 0;

            posY -= 3;
            int drawX = posX + (int) Offset.X;
            int drawY = posY + (int) (Offset.Y - Offset.Z);

            drawX += 22;
            drawY += 22;

            bool hasShadow = !IsDead && !IsHidden && ProfileManager.CurrentProfile.ShadowsEnabled;

            if (AuraManager.IsEnabled)
            {
                AuraManager.Draw(batcher, drawX, drawY, ProfileManager.CurrentProfile.PartyAura && World.Party.Contains(this) ? ProfileManager.CurrentProfile.PartyAuraHue : Notoriety.GetHue(NotorietyFlag));
            }

            bool isHuman = IsHuman;

            bool isGargoyle = Client.Version >= ClientVersion.CV_7000 && (Graphic == 666 || Graphic == 667 || Graphic == 0x02B7 || Graphic == 0x02B6);

            if (AlphaHue != 255)
            {
                hueVec.Z = 1f - AlphaHue / 255f;
            }

            if (ProfileManager.CurrentProfile.HighlightGameObjects && ReferenceEquals(SelectedObject.LastObject, this))
            {
                _viewHue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                hueVec.Y = 1;
            }
            else if (SelectedObject.HealthbarObject == this)
            {
                _viewHue = Notoriety.GetHue(NotorietyFlag);
            }
            else if (ProfileManager.CurrentProfile.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                _viewHue = Constants.OUT_RANGE_COLOR;
                hueVec.Y = 1;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                _viewHue = Constants.DEAD_RANGE_COLOR;
                hueVec.Y = 1;
            }
            else if (IsHidden)
            {
                _viewHue = 0x038E;
            }
            else
            {
                _viewHue = 0;

                if (IsDead)
                {
                    if (!isHuman)
                    {
                        _viewHue = 0x0386;
                    }
                }
                else if (ProfileManager.CurrentProfile.HighlightMobilesByFlags)
                {
                    if (IsPoisoned)
                    {
                        _viewHue = ProfileManager.CurrentProfile.PoisonHue;
                    }

                    if (IsParalyzed)
                    {
                        _viewHue = ProfileManager.CurrentProfile.ParalyzedHue;
                    }

                    if (NotorietyFlag != NotorietyFlag.Invulnerable && IsYellowHits)
                    {
                        _viewHue = ProfileManager.CurrentProfile.InvulnerableHue;
                    }
                }
            }


            bool isAttack = Serial == TargetManager.LastAttack;
            bool isUnderMouse = TargetManager.IsTargeting && ReferenceEquals(SelectedObject.LastObject, this);

            if (Serial != World.Player.Serial)
            {
                if (isAttack || isUnderMouse)
                {
                    _viewHue = Notoriety.GetHue(NotorietyFlag);
                }
            }


            ProcessSteps(out byte dir);
            byte layerDir = dir;

            AnimationsLoader.Instance.GetAnimDirection(ref dir, ref IsFlipped);

            ushort graphic = GetGraphicForAnimation();
            byte animGroup = GetGroupForAnimation(this, graphic, true);
            sbyte animIndex = AnimIndex;

            Item mount = FindItemByLayer(Layer.Mount);

            if (isHuman && mount != null)
            {
                ushort mountGraphic = mount.GetGraphicForAnimation();
                byte animGroupMount = 0;

                if (mountGraphic != 0xFFFF)
                {
                    if (hasShadow)
                    {
                        DrawInternal
                        (
                            batcher,
                            this,
                            null,
                            drawX,
                            drawY + 10,
                            ref hueVec,
                            IsFlipped,
                            animIndex,
                            true,
                            graphic,
                            animGroup,
                            dir,
                            isHuman,
                            alpha: hueVec.Z
                        );

                        animGroupMount = GetGroupForAnimation(this, mountGraphic);

                        DrawInternal
                        (
                            batcher,
                            this,
                            mount,
                            drawX,
                            drawY,
                            ref hueVec,
                            IsFlipped,
                            animIndex,
                            true,
                            mountGraphic,
                            animGroupMount,
                            dir,
                            isHuman,
                            alpha: hueVec.Z
                        );
                    }
                    else
                    {
                        animGroupMount = GetGroupForAnimation(this, mountGraphic);
                    }

                    drawY += DrawInternal
                    (
                        batcher,
                        this,
                        mount,
                        drawX,
                        drawY,
                        ref hueVec,
                        IsFlipped,
                        animIndex,
                        false,
                        mountGraphic,
                        animGroupMount,
                        dir,
                        isHuman,
                        isMount: true,
                        alpha: hueVec.Z
                    );
                }
            }
            else
            {
                if (TryGetSittingInfo(out seatData))
                {
                    animGroup = (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND;
                    animIndex = 0;

                    ProcessSteps(out dir);

                    AnimationsLoader.Instance.FixSittingDirection
                    (
                        ref dir,
                        ref IsFlipped,
                        ref drawX,
                        ref drawY,
                        ref seatData
                    );

                    drawY += SIT_OFFSET_Y;

                    if (dir == 3)
                    {
                        if (IsGargoyle)
                        {
                            drawY -= 30 - SIT_OFFSET_Y;
                            animGroup = 42;
                        }
                        else
                        {
                            animGroup = 25;
                        }
                    }
                    else if (IsGargoyle)
                    {
                        animGroup = 42;
                    }
                    else
                    {
                        _transform = true;
                    }
                }
                else if (hasShadow)
                {
                    DrawInternal
                    (
                        batcher,
                        this,
                        null,
                        drawX,
                        drawY,
                        ref hueVec,
                        IsFlipped,
                        animIndex,
                        true,
                        graphic,
                        animGroup,
                        dir,
                        isHuman,
                        alpha: hueVec.Z
                    );
                }
            }

            DrawInternal
            (
                batcher,
                this,
                null,
                drawX,
                drawY,
                ref hueVec,
                IsFlipped,
                animIndex,
                false,
                graphic,
                animGroup,
                dir,
                isHuman,
                alpha: hueVec.Z,
                forceUOP: isGargoyle
            );

            if (!IsEmpty)
            {
                for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
                {
                    Layer layer = LayerOrder.UsedLayers[layerDir, i];

                    Item item = FindItemByLayer(layer);

                    if (item == null)
                    {
                        continue;
                    }

                    if (IsDead && (layer == Layer.Hair || layer == Layer.Beard))
                    {
                        continue;
                    }

                    if (isHuman)
                    {
                        if (IsCovered(this, layer))
                        {
                            continue;
                        }

                        if (item.ItemData.AnimID != 0)
                        {
                            graphic = item.ItemData.AnimID;

                            if (isGargoyle)
                            {
                                switch (graphic)
                                {
                                    // gargoyle robe
                                    case 0x01D5:
                                        graphic = 0x0156;

                                        break;

                                    // gargoyle dead shroud
                                    case 0x03CA:
                                        graphic = 0x0223;

                                        break;

                                    // gargoyle spellbook
                                    case 0x03D8:
                                        graphic = 329;

                                        break;

                                    // gargoyle necrobook
                                    case 0x0372:
                                        graphic = 330;

                                        break;

                                    // gargoyle chivalry book
                                    case 0x0374:
                                        graphic = 328;

                                        break;

                                    // gargoyle bushido book
                                    case 0x036F:
                                        graphic = 327;

                                        break;

                                    // gargoyle ninjitsu book
                                    case 0x036E:
                                        graphic = 328;

                                        break;

                                    // gargoyle masteries book
                                    case 0x0426:
                                        graphic = 0x042B;

                                        break;


                                    // gargoyle mysticism book seems ok. Mha!
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

                            DrawInternal
                            (
                                batcher,
                                this,
                                item,
                                drawX,
                                drawY,
                                ref hueVec,
                                IsFlipped, 
                                animIndex,
                                false,
                                graphic,
                                isGargoyle /*&& item.ItemData.IsWeapon*/ && seatData.Graphic == 0 ? GetGroupForAnimation(this, graphic, true) : animGroup,
                                dir,
                                isHuman,
                                false,
                                false,
                                isGargoyle,
                                hueVec.Z
                            );
                        }
                        else
                        {
                            if (item.ItemData.IsLight)
                            {
                                Client.Game.GetScene<GameScene>().AddLight(this, this, drawX, drawY);
                            }
                        }

                        _equipConvData = null;
                    }
                    else
                    {
                        if (item.ItemData.IsLight)
                        {
                            Client.Game.GetScene<GameScene>().AddLight(this, this, drawX, drawY);

                            /*DrawInternal
                            (
                                batcher,
                                this,
                                item,
                                drawX,
                                drawY,
                                IsFlipped,
                                animIndex,
                                false,
                                graphic,
                                animGroup,
                                dir,
                                isHuman,
                                false,
                                alpha: HueVector.Z
                            );
                            */
                            //break;
                        }
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

        private static sbyte DrawInternal
        (
            UltimaBatcher2D batcher,
            Mobile owner,
            Item entity,
            int x,
            int y,
            ref Vector3 hueVec,
            bool mirror,
            sbyte frameIndex,
            bool hasShadow,
            ushort id,
            byte animGroup,
            byte dir,
            bool isHuman,
            bool isParent = true,
            bool isMount = false,
            bool forceUOP = false,
            float alpha = 0
        )
        {
            if (id >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT || owner == null)
            {
                return 0;
            }

            ushort hueFromFile = _viewHue;


            AnimationDirection direction = AnimationsLoader.Instance.GetBodyAnimationGroup
                                                           (
                                                               ref id,
                                                               ref animGroup,
                                                               ref hueFromFile,
                                                               isParent,
                                                               forceUOP
                                                           )
                                                           .Direction[dir];

            if (direction == null || direction.Address == -1 || direction.FileIndex == -1)
            {
                if (!(_transform && entity == null && !hasShadow))
                {
                    return 0;
                }
            }

            if (direction == null || (direction.FrameCount == 0 || direction.Frames == null) && !AnimationsLoader.Instance.LoadAnimationFrames(id, animGroup, dir, ref direction))
            {
                if (!(_transform && entity == null && !hasShadow))
                {
                    return 0;
                }
            }

            if (direction == null)
            {
                return 0;
            }

            direction.LastAccessTime = Time.Ticks;

            int fc = direction.FrameCount;

            if (fc > 0 && frameIndex >= fc)
            {
                frameIndex = (sbyte) (fc - 1);
            }
            else if (frameIndex < 0)
            {
                frameIndex = 0;
            }

            if (frameIndex < direction.FrameCount)
            {
                AnimationFrameTexture frame = direction.Frames[frameIndex];

                if (frame == null || frame.IsDisposed)
                {
                    if (!(_transform && entity == null && !hasShadow))
                    {
                        return 0;
                    }

                    goto SKIP;
                }

                frame.Ticks = Time.Ticks;

                if (mirror)
                {
                    x -= frame.Width - frame.CenterX;
                }
                else
                {
                    x -= frame.CenterX;
                }

                y -= frame.Height + frame.CenterY;

                SKIP:

                if (hasShadow)
                {
                    batcher.DrawSpriteShadow(frame, x, y, mirror);
                }
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
                            {
                                hue = _equipConvData.Value.Color;
                            }

                            partialHue = false;
                        }
                    }

                    hueVec = Vector3.Zero;
                    ShaderHueTranslator.GetHueVector(ref hueVec, hue, partialHue, alpha);

                    // this is an hack to make entities partially hued. OG client seems to ignore this.
                    /*if (entity != null && entity.ItemData.AnimID == 0 && entity.ItemData.IsLight)
                    {
                        HueVector.X = entity.Hue == 0 ? owner.Hue : entity.Hue;
                        HueVector.Y = ShaderHueTranslator.SHADER_LIGHTS;
                        HueVector.Z = alpha;
                    }
                    */

                    if (_transform)
                    {
                        const float UPPER_BODY_RATIO = 0.35f;
                        const float MID_BODY_RATIO = 0.60f;
                        const float LOWER_BODY_RATIO = 0.94f;

                        if (entity == null && isHuman)
                        {
                            int frameHeight = frame?.Height ?? 61;
                            _characterFrameStartY = y - (frame != null ? 0 : frameHeight - SIT_OFFSET_Y);
                            _characterFrameHeight = frameHeight;
                            _startCharacterWaistY = (int) (frameHeight * UPPER_BODY_RATIO) + _characterFrameStartY;
                            _startCharacterKneesY = (int) (frameHeight * MID_BODY_RATIO) + _characterFrameStartY;
                            _startCharacterFeetY = (int) (frameHeight * LOWER_BODY_RATIO) + _characterFrameStartY;

                            if (frame == null)
                            {
                                return 0;
                            }
                        }

                        float h3mod = UPPER_BODY_RATIO;
                        float h6mod = MID_BODY_RATIO;
                        float h9mod = LOWER_BODY_RATIO;


                        if (entity != null)
                        {
                            float itemsEndY = y + frame.Height;

                            if (y >= _startCharacterWaistY)
                            {
                                h3mod = 0;
                            }
                            else if (itemsEndY <= _startCharacterWaistY)
                            {
                                h3mod = 1.0f;
                            }
                            else
                            {
                                float upperBodyDiff = _startCharacterWaistY - y;
                                h3mod = upperBodyDiff / frame.Height;

                                if (h3mod < 0)
                                {
                                    h3mod = 0;
                                }
                            }


                            if (_startCharacterWaistY >= itemsEndY || y >= _startCharacterKneesY)
                            {
                                h6mod = 0;
                            }
                            else if (_startCharacterWaistY <= y && itemsEndY <= _startCharacterKneesY)
                            {
                                h6mod = 1.0f;
                            }
                            else
                            {
                                float midBodyDiff;

                                if (y >= _startCharacterWaistY)
                                {
                                    midBodyDiff = _startCharacterKneesY - y;
                                }
                                else if (itemsEndY <= _startCharacterKneesY)
                                {
                                    midBodyDiff = itemsEndY - _startCharacterWaistY;
                                }
                                else
                                {
                                    midBodyDiff = _startCharacterKneesY - _startCharacterWaistY;
                                }

                                h6mod = h3mod + midBodyDiff / frame.Height;

                                if (h6mod < 0)
                                {
                                    h6mod = 0;
                                }
                            }


                            if (itemsEndY <= _startCharacterKneesY)
                            {
                                h9mod = 0;
                            }
                            else if (y >= _startCharacterKneesY)
                            {
                                h9mod = 1.0f;
                            }
                            else
                            {
                                float lowerBodyDiff = itemsEndY - _startCharacterKneesY;
                                h9mod = h6mod + lowerBodyDiff / frame.Height;

                                if (h9mod < 0)
                                {
                                    h9mod = 0;
                                }
                            }
                        }

                        batcher.DrawCharacterSitted
                        (
                            frame,
                            x,
                            y,
                            mirror,
                            h3mod,
                            h6mod,
                            h9mod,
                            ref hueVec
                        );
                    }
                    else if (frame != null)
                    {
                        batcher.DrawSprite
                        (
                            frame,
                            x,
                            y,
                            mirror,
                            ref hueVec
                        );

                        int yy = -(frame.Height + frame.CenterY + 3);
                        int xx = -frame.CenterX;

                        if (mirror)
                        {
                            xx = -(frame.Width - frame.CenterX);
                        }

                        if (xx < owner.FrameInfo.X)
                        {
                            owner.FrameInfo.X = xx;
                        }

                        if (yy < owner.FrameInfo.Y)
                        {
                            owner.FrameInfo.Y = yy;
                        }

                        if (owner.FrameInfo.Width < xx + frame.Width)
                        {
                            owner.FrameInfo.Width = xx + frame.Width;
                        }

                        if (owner.FrameInfo.Height < yy + frame.Height)
                        {
                            owner.FrameInfo.Height = yy + frame.Height;
                        }
                    }


                    if (AnimationsLoader.Instance.PixelCheck(id, animGroup, dir, direction.IsUOP, frameIndex, mirror ? x + frame.Width - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - x, SelectedObject.TranslatedMousePositionByViewport.Y - y))
                    {
                        SelectedObject.Object = owner;
                    }

                    if (entity != null && entity.ItemData.IsLight)
                    {
                        Client.Game.GetScene<GameScene>().AddLight(owner, entity, mirror ? x + frame.Width : x, y);
                    }
                }

                return AnimationsLoader.Instance.DataIndex[id].MountedHeightOffset;
            }

            return 0;
        }

        internal static bool IsCovered(Mobile mobile, Layer layer)
        {
            if (mobile.IsEmpty)
            {
                return false;
            }

            switch (layer)
            {
                case Layer.Shoes:
                    Item pants = mobile.FindItemByLayer(Layer.Pants);
                    Item robe;

                    if (mobile.FindItemByLayer(Layer.Legs) != null || pants != null && (pants.Graphic == 0x1411 /*|| pants.Graphic == 0x141A*/))
                    {
                        return true;
                    }
                    else
                    {
                        robe = mobile.FindItemByLayer(Layer.Robe);

                        if (pants != null && (pants.Graphic == 0x0513 || pants.Graphic == 0x0514) || robe != null && robe.Graphic == 0x0504)
                        {
                            return true;
                        }
                    }

                    break;

                case Layer.Pants:

                    robe = mobile.FindItemByLayer(Layer.Robe);
                    pants = mobile.FindItemByLayer(Layer.Pants);

                    if (mobile.FindItemByLayer(Layer.Legs) != null || robe != null && robe.Graphic == 0x0504)
                    {
                        return true;
                    }

                    if (pants != null && (pants.Graphic == 0x01EB || pants.Graphic == 0x03E5 || pants.Graphic == 0x03eB))
                    {
                        Item skirt = mobile.FindItemByLayer(Layer.Skirt);

                        if (skirt != null && skirt.Graphic != 0x01C7 && skirt.Graphic != 0x01E4)
                        {
                            return true;
                        }

                        if (robe != null && robe.Graphic != 0x0229 && (robe.Graphic <= 0x04E7 || robe.Graphic > 0x04EB))
                        {
                            return true;
                        }
                    }

                    break;

                case Layer.Tunic:
                    robe = mobile.FindItemByLayer(Layer.Robe);
                    Item tunic = mobile.FindItemByLayer(Layer.Tunic);

                    /*if (robe != null && robe.Graphic != 0)
                        return true;
                    else*/
                    if (tunic != null && tunic.Graphic == 0x0238)
                    {
                        return robe != null && robe.Graphic != 0x9985 && robe.Graphic != 0x9986 && robe.Graphic != 0xA412;
                    }

                    break;

                case Layer.Torso:
                    robe = mobile.FindItemByLayer(Layer.Robe);

                    if (robe != null && robe.Graphic != 0 && robe.Graphic != 0x9985 && robe.Graphic != 0x9986 && robe.Graphic != 0xA412 && robe.Graphic != 0xA2CA)
                    {
                        return true;
                    }
                    else
                    {
                        Item torso = mobile.FindItemByLayer(Layer.Torso);

                        if (torso != null && (torso.Graphic == 0x782A || torso.Graphic == 0x782B))
                        {
                            return true;
                        }
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
                            {
                                return true;
                            }
                        }
                        else
                        {
                            if (robe.Graphic <= 0x2687)
                            {
                                if (robe.Graphic < 0x2683)
                                {
                                    return robe.Graphic >= 0x204E && robe.Graphic <= 0x204F;
                                }

                                return true;
                            }

                            if (robe.Graphic == 0x2FB9 || robe.Graphic == 0x3173)
                            {
                                return true;
                            }
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