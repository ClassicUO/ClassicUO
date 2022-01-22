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
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Mobile
    {
        private const int SIT_OFFSET_Y = 4;
        private static EquipConvData? _equipConvData;
        private static int _characterFrameStartY;
        private static int _startCharacterWaistY;
        private static int _startCharacterKneesY;
        private static int _startCharacterFeetY;
        private static int _characterFrameHeight;

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth)
        {
            if (IsDestroyed || !AllowedToDraw)
            {
                return false;
            }

            bool charSitting = false;
            ushort overridedHue = 0;

            AnimationsLoader.SittingInfoData seatData = AnimationsLoader.SittingInfoData.Empty;
            _equipConvData = null;
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
                AuraManager.Draw
                (
                    batcher, 
                    drawX, 
                    drawY,
                    ProfileManager.CurrentProfile.PartyAura && World.Party.Contains(this) ? ProfileManager.CurrentProfile.PartyAuraHue : Notoriety.GetHue(NotorietyFlag),
                    depth + 1f
                );
            }

            bool isHuman = IsHuman;

            bool isGargoyle = Client.Version >= ClientVersion.CV_7000 && (Graphic == 666 || Graphic == 667 || Graphic == 0x02B7 || Graphic == 0x02B6);

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(0, false, AlphaHue / 255f);

            if (ProfileManager.CurrentProfile.HighlightGameObjects && ReferenceEquals(SelectedObject.LastObject, this))
            {
                overridedHue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                hueVec.Y = 1;
            }
            else if (SelectedObject.HealthbarObject == this)
            {
                overridedHue = Notoriety.GetHue(NotorietyFlag);
            }
            else if (ProfileManager.CurrentProfile.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                overridedHue = Constants.OUT_RANGE_COLOR;
                hueVec.Y = 1;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                overridedHue = Constants.DEAD_RANGE_COLOR;
                hueVec.Y = 1;
            }
            else if (IsHidden)
            {
                overridedHue = 0x038E;
            }
            else
            {
                overridedHue = 0;

                if (IsDead)
                {
                    if (!isHuman)
                    {
                        overridedHue = 0x0386;
                    }
                }
                else
                {
                    if (ProfileManager.CurrentProfile.HighlightMobilesByPoisoned)
                    {
                        if (IsPoisoned)
                        {
                            overridedHue = ProfileManager.CurrentProfile.PoisonHue;
                        }
                    }
                    if (ProfileManager.CurrentProfile.HighlightMobilesByParalize)
                    {
                        if (IsParalyzed && NotorietyFlag != NotorietyFlag.Invulnerable)
                        {
                            overridedHue = ProfileManager.CurrentProfile.ParalyzedHue;
                        }
                    }
                    if (ProfileManager.CurrentProfile.HighlightMobilesByInvul)
                    {
                        if (NotorietyFlag != NotorietyFlag.Invulnerable && IsYellowHits)
                        {
                            overridedHue = ProfileManager.CurrentProfile.InvulnerableHue;
                        }
                    }
                }
            }


            bool isAttack = Serial == TargetManager.LastAttack;
            bool isUnderMouse = TargetManager.IsTargeting && ReferenceEquals(SelectedObject.LastObject, this);

            if (Serial != World.Player.Serial)
            {
                if (isAttack || isUnderMouse)
                {
                    overridedHue = Notoriety.GetHue(NotorietyFlag);
                }
            }


            ProcessSteps(out byte dir);
            byte layerDir = dir;

            AnimationsLoader.Instance.GetAnimDirection(ref dir, ref IsFlipped);

            ushort graphic = GetGraphicForAnimation();
            byte animGroup = GetGroupForAnimation(this, graphic, true);
            byte animIndex = AnimIndex;

            Item mount = FindItemByLayer(Layer.Mount);
            sbyte mountOffsetY = 0;

            if (isHuman && mount != null && mount.Graphic != 0x3E96)
            {
                ushort mountGraphic = mount.GetGraphicForAnimation();
                byte animGroupMount = 0;

                if (mountGraphic != 0xFFFF && mountGraphic < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                {
                    mountOffsetY = AnimationsLoader.Instance.DataIndex[mountGraphic].MountedHeightOffset;

                    if (hasShadow)
                    {
                        DrawInternal
                        (
                            batcher,
                            this,
                            null,
                            drawX,
                            drawY + 10,
                            hueVec,
                            IsFlipped,
                            animIndex,
                            true,
                            graphic,
                            animGroup,
                            dir,
                            isHuman,
                            true,
                            false,
                            false,
                            depth,
                            mountOffsetY,
                            overridedHue,
                            charSitting
                        );

                        animGroupMount = GetGroupForAnimation(this, mountGraphic);

                        DrawInternal
                        (
                            batcher,
                            this,
                            mount,
                            drawX,
                            drawY,
                            hueVec,
                            IsFlipped,
                            animIndex,
                            true,
                            mountGraphic,
                            animGroupMount,
                            dir,
                            isHuman,
                            true,
                            false,
                            false,
                            depth,
                            mountOffsetY,
                            overridedHue,
                            charSitting
                        );
                    }
                    else
                    {
                        animGroupMount = GetGroupForAnimation(this, mountGraphic);
                    }

                    DrawInternal
                    (
                        batcher,
                        this,
                        mount,
                        drawX,
                        drawY,
                        hueVec,
                        IsFlipped,
                        animIndex,
                        false,
                        mountGraphic,
                        animGroupMount,
                        dir,
                        isHuman,
                        true,
                        true,
                        false,
                        depth,
                        mountOffsetY,
                        overridedHue,
                        charSitting
                    );

                    drawY += mountOffsetY;
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
                        charSitting = true;
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
                        hueVec,
                        IsFlipped,
                        animIndex,
                        true,
                        graphic,
                        animGroup,
                        dir,
                        isHuman,
                        true,
                        false,
                        false,
                        depth,
                        mountOffsetY,
                        overridedHue,
                        charSitting
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
                hueVec,
                IsFlipped,
                animIndex,
                false,
                graphic,
                animGroup,
                dir,
                isHuman,
                true,
                false,
                isGargoyle,
                depth,
                mountOffsetY,
                overridedHue,
                charSitting
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
                                FixGargoyleEquipments(ref graphic);
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
                                hueVec,
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
                                depth,
                                mountOffsetY,
                                overridedHue,
                                charSitting
                            );
                        }
                        else
                        {
                            if (item.ItemData.IsLight)
                            {
                                Client.Game.GetScene<GameScene>().AddLight(this, item, drawX, drawY);
                            }
                        }

                        _equipConvData = null;
                    }
                    else
                    {
                        if (item.ItemData.IsLight)
                        {
                            Client.Game.GetScene<GameScene>().AddLight(this, item, drawX, drawY);

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

        private static ushort GetAnimationInfo
        (
            Mobile owner, 
            Item item, 
            bool isGargoyle
        )
        {
            if (item.ItemData.AnimID != 0)
            {
                var graphic = item.ItemData.AnimID;

                if (isGargoyle)
                {
                    FixGargoyleEquipments(ref graphic);
                }

                if (AnimationsLoader.Instance.EquipConversions.TryGetValue(owner.Graphic, out Dictionary<ushort, EquipConvData> map))
                {
                    if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
                    {
                        _equipConvData = data;
                        graphic = data.Graphic;
                    }
                }

                return graphic;
            }

            return 0xFFFF;
        }

        private static void FixGargoyleEquipments(ref ushort graphic)
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
                //NOTE: gargoyle mysticism book seems ok. Mha!


                /* into the mobtypes.txt file of 7.0.90+ client version we have:
                 *
                 *   1529 	EQUIPMENT	0		# EQUIP_Shield_Pirate_Male_H
                 *   1530 	EQUIPMENT	0		# EQUIP_Shield_Pirate_Female_H
                 *   1531 	EQUIPMENT	10000	# Equip_Shield_Pirate_Male_G
                 *   1532 	EQUIPMENT	10000	# Equip_Shield_Pirate_Female_G
                 *   
                 *   This means that graphic 0xA649 [pirate shield] has 4 tiledata infos.
                 *   Standard client handles it automatically without any issue. 
                 *   Maybe it's hardcoded into the client
                 */

                // EQUIP_Shield_Pirate_Male_H
                case 1529:
                    graphic = 1531;

                    break;

                // EQUIP_Shield_Pirate_Female_H
                case 1530:
                    graphic = 1532;

                    break;
            }
        }

        private static bool GetTexture(ref ushort graphic, ref byte animGroup, ref byte animIndex, byte direction, out SpriteInfo spriteInfo, out bool isUOP)
        {
            spriteInfo = default;
            isUOP = false;

            ushort hue = 0;

            AnimationDirection animationSet = AnimationsLoader.Instance.GetBodyAnimationGroup
            (
                ref graphic,
                ref animGroup,
                ref hue,
                true,
                false
            )
            .Direction[direction];

            if (animationSet == null ||
                animationSet.Address == -1 ||
                animationSet.FileIndex == -1 ||
                animationSet.FrameCount == 0 || 
                animationSet.SpriteInfos == null
               )
            {
                return false;
            }

            int fc = animationSet.FrameCount;

            if (fc > 0 && animIndex >= fc)
            {
                animIndex = (byte)(fc - 1);
            }
            else if (animIndex < 0)
            {
                animIndex = 0;
            }

            if (animIndex >= animationSet.FrameCount)
            {
                return false;
            }

            spriteInfo = animationSet.SpriteInfos[animIndex % animationSet.FrameCount];

            if (spriteInfo.Texture == null)
            {
                return false;
            }

            isUOP = animationSet.IsUOP;

            return true;
        }

        private static void DrawInternal
        (
            UltimaBatcher2D batcher,
            Mobile owner,
            Item entity,
            int x,
            int y,
            Vector3 hueVec,
            bool mirror,
            byte frameIndex,
            bool hasShadow,
            ushort id,
            byte animGroup,
            byte dir,
            bool isHuman,
            bool isParent,
            bool isMount,
            bool forceUOP,
            float depth,
            sbyte mountOffset,
            ushort overridedHue,
            bool charIsSitting
        )
        {
            if (id >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT || owner == null)
            {
                return;
            }

            ushort hueFromFile = overridedHue;
            
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
                if (!(charIsSitting && entity == null && !hasShadow))
                {
                    return;
                }
            }

            if (direction == null || (direction.FrameCount == 0 || direction.SpriteInfos == null) && !AnimationsLoader.Instance.LoadAnimationFrames(id, animGroup, dir, ref direction))
            {
                if (!(charIsSitting && entity == null && !hasShadow))
                {
                    return;
                }
            }

            if (direction == null)
            {
                return;
            }

            int fc = direction.FrameCount;

            if (fc > 0 && frameIndex >= fc)
            {
                frameIndex = (byte) (fc - 1);
            }
            else if (frameIndex < 0)
            {
                frameIndex = 0;
            }

            if (frameIndex < direction.FrameCount)
            {
                ref var spriteInfo = ref direction.SpriteInfos[frameIndex % direction.FrameCount];

                if (spriteInfo.Texture == null)
                {
                    if (!(charIsSitting && entity == null && !hasShadow))
                    {
                        return;
                    }

                    goto SKIP;
                }

                if (mirror)
                {
                    x -= spriteInfo.UV.Width - spriteInfo.Center.X;
                }
                else
                {
                    x -= spriteInfo.Center.X;
                }

                y -= spriteInfo.UV.Height + spriteInfo.Center.Y;

                SKIP:

                if (hasShadow)
                {
                    batcher.DrawShadow(spriteInfo.Texture, new Vector2(x, y), spriteInfo.UV, mirror, depth);
                }
                else
                {
                    ushort hue = overridedHue;
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

                    hueVec = ShaderHueTranslator.GetHueVector(hue, partialHue, hueVec.Z);

                    if (spriteInfo.Texture != null)
                    {
                        Vector2 pos = new Vector2(x, y);
                        Rectangle rect = spriteInfo.UV;

                        if (charIsSitting)
                        {
                            Vector3 mod = CalculateSitAnimation(y, entity, isHuman, ref spriteInfo);

                            batcher.DrawCharacterSitted
                            (
                                spriteInfo.Texture,
                                pos,
                                rect,
                                mod,
                                hueVec,
                                mirror,
                                depth + 1f
                            );
                        }
                        else
                        {

                            //bool isMounted = isHuman && owner.IsMounted;


                            //int diffX = spriteInfo.UV.Width /*- spriteInfo.Center.X*/;

                            //if (isMounted)
                            //{
                            //if (mountOffset != 0)
                            //{
                            //    mountOffset += 10;
                            //}
                            //else
                            //{
                            //mountOffset = (sbyte)Math.Abs(spriteInfo.Center.Y);
                            //}                          
                            //}

                            //var flags = AnimationsLoader.Instance.DataIndex[id].Flags;
                            //if (AnimationsLoader.Instance.DataIndex[id].Type == ANIMATION_GROUPS_TYPE.HUMAN)
                            //{

                            //}


                            int diffY = (spriteInfo.UV.Height + spriteInfo.Center.Y) - mountOffset;

                            //if (owner.Serial == World.Player.Serial && entity == null)
                            //{

                            //}

                            int value = /*!isMounted && diffX <= 44 ? spriteInfo.UV.Height * 2 :*/ Math.Max(1, diffY);
                            int count = Math.Max((spriteInfo.UV.Height / value) + 1, 2);

                            rect.Height = Math.Min(value, rect.Height);
                            int remains = spriteInfo.UV.Height - rect.Height;

                            int tiles = (byte)owner.Direction % 2 == 0 ? 2 : 2;
                            //tiles = 999;

                            for (int i = 0; i < count; ++i)
                            {
                                //hueVec.Y = 1;
                                //hueVec.X = 0x44 + (i * 20);

                                batcher.Draw
                                (
                                    spriteInfo.Texture,
                                    pos,
                                    rect,
                                    hueVec,
                                    0f,
                                    Vector2.Zero,
                                    1f,
                                    mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                                    depth + 1f + (i * tiles)
                                //depth + (i * tiles) + (owner.PriorityZ * 0.001f)
                                );

                                pos.Y += rect.Height;
                                rect.Y += rect.Height;
                                rect.Height = remains; // Math.Min(value, remains);
                                remains -= rect.Height;
                            }
                        }

                        int xx = -spriteInfo.Center.X;
                        int yy = -(spriteInfo.UV.Height + spriteInfo.Center.Y + 3);

                        if (mirror)
                        {
                            xx = -(spriteInfo.UV.Width - spriteInfo.Center.X);
                        }

                        if (xx < owner.FrameInfo.X)
                        {
                            owner.FrameInfo.X = xx;
                        }

                        if (yy < owner.FrameInfo.Y)
                        {
                            owner.FrameInfo.Y = yy;
                        }

                        if (owner.FrameInfo.Width < xx + spriteInfo.UV.Width)
                        {
                            owner.FrameInfo.Width = xx + spriteInfo.UV.Width;
                        }

                        if (owner.FrameInfo.Height < yy + spriteInfo.UV.Height)
                        {
                            owner.FrameInfo.Height = yy + spriteInfo.UV.Height;
                        }
                    }

                    if (entity != null && entity.ItemData.IsLight)
                    {
                        Client.Game.GetScene<GameScene>().AddLight(owner, entity, mirror ? x + spriteInfo.UV.Width : x, y);
                    }
                }
            }
        }

        private static Vector3 CalculateSitAnimation(int y, Item entity, bool isHuman, ref SpriteInfo spriteInfo)
        {
            Vector3 mod = new Vector3();

            const float UPPER_BODY_RATIO = 0.35f;
            const float MID_BODY_RATIO = 0.60f;
            const float LOWER_BODY_RATIO = 0.94f;

            if (entity == null && isHuman)
            {
                int frameHeight = spriteInfo.UV.Height;
                if (frameHeight == 0)
                {
                    frameHeight = 61;
                }

                _characterFrameStartY = y - (spriteInfo.Texture != null ? 0 : frameHeight - SIT_OFFSET_Y);
                _characterFrameHeight = frameHeight;
                _startCharacterWaistY = (int)(frameHeight * UPPER_BODY_RATIO) + _characterFrameStartY;
                _startCharacterKneesY = (int)(frameHeight * MID_BODY_RATIO) + _characterFrameStartY;
                _startCharacterFeetY = (int)(frameHeight * LOWER_BODY_RATIO) + _characterFrameStartY;

                if (spriteInfo.Texture == null)
                {
                    return mod;
                }
            }

            mod.X = UPPER_BODY_RATIO;
            mod.Y = MID_BODY_RATIO;
            mod.Z = LOWER_BODY_RATIO;


            if (entity != null)
            {
                float itemsEndY = y + spriteInfo.UV.Height;

                if (y >= _startCharacterWaistY)
                {
                    mod.X = 0;
                }
                else if (itemsEndY <= _startCharacterWaistY)
                {
                    mod.X = 1.0f;
                }
                else
                {
                    float upperBodyDiff = _startCharacterWaistY - y;
                    mod.X = upperBodyDiff / spriteInfo.UV.Height;

                    if (mod.X < 0)
                    {
                        mod.X = 0;
                    }
                }


                if (_startCharacterWaistY >= itemsEndY || y >= _startCharacterKneesY)
                {
                    mod.Y = 0;
                }
                else if (_startCharacterWaistY <= y && itemsEndY <= _startCharacterKneesY)
                {
                    mod.Y = 1.0f;
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

                    mod.Y = mod.X + midBodyDiff / spriteInfo.UV.Height;

                    if (mod.Y < 0)
                    {
                        mod.Y = 0;
                    }
                }


                if (itemsEndY <= _startCharacterKneesY)
                {
                    mod.Z = 0;
                }
                else if (y >= _startCharacterKneesY)
                {
                    mod.Z = 1.0f;
                }
                else
                {
                    float lowerBodyDiff = itemsEndY - _startCharacterKneesY;
                    mod.Z = mod.Y + lowerBodyDiff / spriteInfo.UV.Height;

                    if (mod.Z < 0)
                    {
                        mod.Z = 0;
                    }
                }
            }

            return mod;
        }

        public override bool CheckMouseSelection()
        {
            Point position = RealScreenPosition;
            position.Y -= 3;
            position.X += (int)Offset.X + 22;
            position.Y += (int)(Offset.Y - Offset.Z) + 22;

            Rectangle r = FrameInfo;
            r.X = position.X - r.X;
            r.Y = position.Y - r.Y;

            if (!r.Contains(SelectedObject.TranslatedMousePositionByViewport))
            {
                return false;
            }


            bool isHuman = IsHuman;
            bool isGargoyle = Client.Version >= ClientVersion.CV_7000 && 
                              (Graphic == 666 || 
                              Graphic == 667 ||
                              Graphic == 0x02B7 || 
                              Graphic == 0x02B6);


            ProcessSteps(out byte dir);
            bool isFlipped = IsFlipped;
            AnimationsLoader.Instance.GetAnimDirection(ref dir, ref isFlipped);

            ushort graphic = GetGraphicForAnimation();
            byte animGroup = GetGroupForAnimation(this, graphic, true);
            byte animIndex = AnimIndex;

            byte animGroupBackup = animGroup;
            byte animIndexBackup = animIndex;

            SpriteInfo spriteInfo;
            bool isUop;


            if (isHuman)
            {
                Item mount = FindItemByLayer(Layer.Mount);
                if (mount != null)
                {
                    var mountGraphic = mount.GetGraphicForAnimation();
                   
                    if (mountGraphic != 0xFFFF)
                    {
                        var animGroupMount = GetGroupForAnimation(this, mountGraphic);

                        if (GetTexture(ref mountGraphic, ref animGroupMount, ref animIndex, dir, out spriteInfo, out isUop))
                        {
                            int x = position.X - (isFlipped ? spriteInfo.UV.Width - spriteInfo.Center.X : spriteInfo.Center.X);
                            int y = position.Y - (spriteInfo.UV.Height + spriteInfo.Center.Y);

                            if (AnimationsLoader.Instance.PixelCheck
                            (
                                mountGraphic,
                                animGroupMount,
                                dir,
                                isUop,
                                animIndex,
                                isFlipped ? x + spriteInfo.UV.Width - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - x,
                                SelectedObject.TranslatedMousePositionByViewport.Y - y
                            ))
                            {
                                return true;
                            }

                            position.Y += AnimationsLoader.Instance.DataIndex[mountGraphic].MountedHeightOffset;
                        }
                    }
                }
            }
            

            if (GetTexture(ref graphic, ref animGroup, ref animIndex, dir, out spriteInfo, out isUop))
            {
                int x = position.X - (isFlipped ? spriteInfo.UV.Width - spriteInfo.Center.X : spriteInfo.Center.X);
                int y = position.Y - (spriteInfo.UV.Height + spriteInfo.Center.Y);

                if (AnimationsLoader.Instance.PixelCheck
                (
                    graphic,
                    animGroup,
                    dir,
                    isUop,
                    animIndex,
                    isFlipped ? x + spriteInfo.UV.Width - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - x,
                    SelectedObject.TranslatedMousePositionByViewport.Y - y
                ))
                {
                    return true;
                }
            }


            if (!IsEmpty && isHuman)
            {
                for (Layer layer = Layer.Invalid + 1; layer < Layer.Mount; ++layer)
                {
                    Item item = FindItemByLayer(layer);

                    if (item == null || (IsDead && (layer == Layer.Hair || layer == Layer.Beard)) || IsCovered(this, layer))
                    {
                        continue;
                    }

                    graphic = GetAnimationInfo(this, item, isGargoyle);

                    if (graphic != 0xFFFF)
                    {
                        animGroup = animGroupBackup;
                        animIndex = animIndexBackup;

                        if (GetTexture(ref graphic, ref animGroup, ref animIndex, dir, out spriteInfo, out isUop))
                        {
                            int x = position.X - (isFlipped ? spriteInfo.UV.Width - spriteInfo.Center.X : spriteInfo.Center.X);
                            int y = position.Y - (spriteInfo.UV.Height + spriteInfo.Center.Y);

                            if (AnimationsLoader.Instance.PixelCheck
                            (
                                graphic,
                                animGroup,
                                dir,
                                isUop,
                                animIndex,
                                isFlipped ? x + spriteInfo.UV.Width - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - x,
                                SelectedObject.TranslatedMousePositionByViewport.Y - y
                            ))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
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
