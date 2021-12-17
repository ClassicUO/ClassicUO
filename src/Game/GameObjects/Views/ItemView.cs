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
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Item
    {
        private static EquipConvData? _equipConvData;

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth)
        {
            if (!AllowedToDraw || IsDestroyed)
            {
                return false;
            }

            //Engine.DebugInfo.ItemsRendered++;
            Vector3 hueVec;

            posX += (int) Offset.X;
            posY += (int) (Offset.Y + Offset.Z);

            float alpha = AlphaHue / 255f;

            if (IsCorpse)
            {
                hueVec = ShaderHueTranslator.GetHueVector(0, false, alpha);
                return DrawCorpse(batcher, posX, posY - 3, hueVec, depth);
            }


            ushort hue = Hue;
            ushort graphic = DisplayedGraphic;
            bool partial = ItemData.IsPartialHue;

            if (OnGround)
            {
                if (ItemData.IsAnimated)
                {
                    if (ProfileManager.CurrentProfile.FieldsType == 2)
                    {
                        if (StaticFilters.IsFireField(Graphic))
                        {
                            graphic = Constants.FIELD_REPLACE_GRAPHIC;
                            hue = 0x0020;
                        }
                        else if (StaticFilters.IsParalyzeField(Graphic))
                        {
                            graphic = Constants.FIELD_REPLACE_GRAPHIC;
                            hue = 0x0058;
                        }
                        else if (StaticFilters.IsEnergyField(Graphic))
                        {
                            graphic = Constants.FIELD_REPLACE_GRAPHIC;
                            hue = 0x0070;
                        }
                        else if (StaticFilters.IsPoisonField(Graphic))
                        {
                            graphic = Constants.FIELD_REPLACE_GRAPHIC;
                            hue = 0x0044;
                        }
                        else if (StaticFilters.IsWallOfStone(Graphic))
                        {
                            graphic = Constants.FIELD_REPLACE_GRAPHIC;
                            hue = 0x038A;
                        }
                    }
                }

                if (ItemData.IsContainer && SelectedObject.SelectedContainer == this)
                {
                    hue = 0x0035;
                    partial = false;
                }
            }

            if (ProfileManager.CurrentProfile.HighlightGameObjects && ReferenceEquals(SelectedObject.LastObject, this))
            {
                hue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                partial = false;
            }
            else if (ProfileManager.CurrentProfile.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }
            else
            {
                if (!IsLocked && !IsMulti && ReferenceEquals(SelectedObject.LastObject, this))
                {
                    // TODO: check why i put this.
                    //isPartial = ItemData.Weight == 0xFF;
                    hue = 0x0035;
                }
                else if (IsHidden)
                {
                    hue = 0x038E;
                }
            }

            hueVec = ShaderHueTranslator.GetHueVector(hue, partial, alpha);

            if (!IsMulti && !IsCoin && Amount > 1 && ItemData.IsStackable)
            {
                DrawStaticAnimated
                (
                    batcher,
                    graphic,
                    posX - 5,
                    posY - 5,
                    hueVec,
                    false,
                    depth
                );
            }

            if (ItemData.IsLight || graphic >= 0x3E02 && graphic <= 0x3E0B || graphic >= 0x3914 && graphic <= 0x3929)
            {
                Client.Game.GetScene<GameScene>().AddLight(this, this, posX + 22, posY + 22);
            }

            if (!SerialHelper.IsValid(Serial) && IsMulti && TargetManager.TargetingState == CursorTarget.MultiPlacement)
            {
                hueVec.Z = 0.5f;
            }

            DrawStaticAnimated
            (
                batcher,
                graphic,
                posX,
                posY,
                hueVec,
                false,
                depth
            );

            return true;
        }

        private bool DrawCorpse(UltimaBatcher2D batcher, int posX, int posY, Vector3 hueVec, float depth)
        {
            if (IsDestroyed || World.CorpseManager.Exists(Serial, 0))
            {
                return false;
            }

            posX += 22;
            posY += 22;

            byte direction = (byte) ((byte) Layer & 0x7F & 7);
            AnimationsLoader.Instance.GetAnimDirection(ref direction, ref IsFlipped);

            byte animIndex = (byte) AnimIndex;
            ushort graphic = GetGraphicForAnimation();
            AnimationsLoader.Instance.ConvertBodyIfNeeded(ref graphic);
            byte group = AnimationsLoader.Instance.GetDieGroupIndex(graphic, UsedLayer);

            bool ishuman = MathHelper.InRange(Amount, 0x0190, 0x0193) || MathHelper.InRange(Amount, 0x00B7, 0x00BA) || MathHelper.InRange(Amount, 0x025D, 0x0260) || MathHelper.InRange(Amount, 0x029A, 0x029B) || MathHelper.InRange(Amount, 0x02B6, 0x02B7) || Amount == 0x03DB || Amount == 0x03DF || Amount == 0x03E2 || Amount == 0x02E8 || Amount == 0x02E9;

            DrawLayer
            (
                batcher,
                posX,
                posY,
                this,
                Layer.Invalid,
                animIndex,
                ishuman,
                Hue,
                IsFlipped,
                hueVec.Z,
                group,
                direction,
                hueVec,
                depth
            );

            for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[direction, i];

                DrawLayer
                (
                    batcher,
                    posX,
                    posY,
                    this,
                    layer,
                    animIndex,
                    ishuman,
                    0,
                    IsFlipped,
                    hueVec.Z,
                    group,
                    direction,
                    hueVec,
                    depth
                );
            }

            return true;
        }

        private static void DrawLayer
        (
            UltimaBatcher2D batcher,
            int posX,
            int posY,
            Item owner,
            Layer layer,
            byte animIndex,
            bool ishuman,
            ushort color,
            bool flipped,
            float alpha,
            byte animGroup,
            byte dir,
            Vector3 hueVec,
            float depth
        )
        {
            _equipConvData = null;
            bool ispartialhue = false;

            ushort graphic;

            if (layer == Layer.Invalid)
            {
                graphic = owner.GetGraphicForAnimation();
            }
            else if (ishuman)
            {
                Item itemEquip = owner.FindItemByLayer(layer);

                if (itemEquip == null)
                {
                    return;
                }

                graphic = itemEquip.ItemData.AnimID;
                ispartialhue = itemEquip.ItemData.IsPartialHue;

                if (AnimationsLoader.Instance.EquipConversions.TryGetValue(graphic, out Dictionary<ushort, EquipConvData> map))
                {
                    if (map.TryGetValue(graphic, out EquipConvData data))
                    {
                        _equipConvData = data;
                        graphic = data.Graphic;
                    }
                }

                color = itemEquip.Hue;
            }
            else
            {
                return;
            }

            ushort newHue = 0;

            AnimationGroup gr = layer == Layer.Invalid ? AnimationsLoader.Instance.GetCorpseAnimationGroup(ref graphic, ref animGroup, ref newHue) : AnimationsLoader.Instance.GetBodyAnimationGroup(ref graphic, ref animGroup, ref newHue);

            if (color == 0)
            {
                color = newHue;
            }

            AnimationDirection direction = gr.Direction[dir];

            if (direction == null)
            {
                return;
            }

            if ((direction.FrameCount == 0 || direction.SpriteInfos == null) && !AnimationsLoader.Instance.LoadAnimationFrames(graphic, animGroup, dir, ref direction))
            {
                return;
            }

            int fc = direction.FrameCount;

            if (fc > 0 && animIndex >= fc)
            {
                animIndex = (byte) (fc - 1);
            }

            if (animIndex < direction.FrameCount)
            {
                ref var spriteInfo = ref direction.SpriteInfos[animIndex];

                if (spriteInfo.Texture == null)
                {
                    return;
                }

                if (flipped)
                {
                    posX -= spriteInfo.UV.Width - spriteInfo.Center.X;
                }
                else
                {
                    posX -= spriteInfo.Center.X;
                }

                posY -= spriteInfo.UV.Height + spriteInfo.Center.Y;


                if (color == 0)
                {
                    if ((color & 0x8000) != 0)
                    {
                        ispartialhue = true;
                        color &= 0x7FFF;
                    }

                    if (color == 0 && _equipConvData.HasValue)
                    {
                        color = _equipConvData.Value.Color;
                        ispartialhue = false;
                    }
                }

                if (ProfileManager.CurrentProfile.NoColorObjectsOutOfRange && owner.Distance > World.ClientViewRange)
                {
                    hueVec = ShaderHueTranslator.GetHueVector(Constants.OUT_RANGE_COLOR + 1, false, 1);
                }
                else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
                {
                    hueVec = ShaderHueTranslator.GetHueVector(Constants.DEAD_RANGE_COLOR + 1, false, 1);
                }
                else
                {
                    if (ProfileManager.CurrentProfile.GridLootType > 0 && SelectedObject.CorpseObject == owner)
                    {
                        color = 0x0034;
                    }
                    else if (ProfileManager.CurrentProfile.HighlightGameObjects && ReferenceEquals(SelectedObject.LastObject, owner))
                    {
                        color = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                    }

                    hueVec = ShaderHueTranslator.GetHueVector(color, ispartialhue, alpha);
                }

                Vector2 pos = new Vector2(posX, posY);
                Rectangle rect = spriteInfo.UV;

                int diffY = (spriteInfo.UV.Height + spriteInfo.Center.Y);
                int value = /*!isMounted && diffX <= 44 ? spriteInfo.UV.Height * 2 :*/ Math.Max(1, diffY);
                int count = Math.Max((spriteInfo.UV.Height / value) + 1, 2);

                rect.Height = Math.Min(value, rect.Height);
                int remains = spriteInfo.UV.Height - rect.Height;

                int tiles = (byte)owner.Direction % 2 == 0 ? 2 : 2;


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
                        flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                        depth + 1f + (i * tiles)
                    //depth + (i * tiles) + (owner.PriorityZ * 0.001f)
                    );

                    pos.Y += rect.Height;
                    rect.Y += rect.Height;
                    rect.Height = remains; // Math.Min(value, remains);
                    remains -= rect.Height;
                }
            }
        }

        public override bool CheckMouseSelection()
        {
            if (!IsCorpse)
            {
                if (ReferenceEquals(SelectedObject.Object, this) || TargetManager.TargetingState == CursorTarget.MultiPlacement)
                {
                    return false;
                }

                ushort graphic = DisplayedGraphic;

                if (OnGround && ItemData.IsAnimated)
                {
                    if (ProfileManager.CurrentProfile.FieldsType == 2)
                    {
                        if (StaticFilters.IsFireField(Graphic) ||
                            StaticFilters.IsParalyzeField(Graphic) ||
                            StaticFilters.IsEnergyField(Graphic) ||
                            StaticFilters.IsPoisonField(Graphic) ||
                            StaticFilters.IsWallOfStone(Graphic))
                        {
                            graphic = Constants.FIELD_REPLACE_GRAPHIC;
                        }
                    }
                }

                if (ArtLoader.Instance.GetStaticTexture(graphic, out _) != null)
                {
                    ref UOFileIndex index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

                    Point position = RealScreenPosition;
                    position.X += (int)Offset.X;
                    position.Y += (int)(Offset.Y + Offset.Z);
                    position.X -= index.Width;
                    position.Y -= index.Height;

                    if (ArtLoader.Instance.PixelCheck
                    (
                        graphic,
                        SelectedObject.TranslatedMousePositionByViewport.X - position.X,
                        SelectedObject.TranslatedMousePositionByViewport.Y - position.Y
                    ))
                    {
                        return true;
                    }
                    else if (!IsMulti && !IsCoin && Amount > 1 && ItemData.IsStackable)
                    {
                        if (ArtLoader.Instance.PixelCheck
                        (
                            graphic,
                            SelectedObject.TranslatedMousePositionByViewport.X - position.X + 5,
                            SelectedObject.TranslatedMousePositionByViewport.Y - position.Y + 5
                        ))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                if (!SerialHelper.IsValid(Serial))
                {
                    return false;
                }

                if (ReferenceEquals(SelectedObject.Object, this))
                {
                    return true;
                }

                Point position = RealScreenPosition;
                position.X += 22;
                position.Y += 22;

                byte direction = (byte)((byte)Layer & 0x7F & 7);
                AnimationsLoader.Instance.GetAnimDirection(ref direction, ref IsFlipped);
                byte animIndex = AnimIndex;
                bool ishuman = MathHelper.InRange(Amount, 0x0190, 0x0193) ||
                    MathHelper.InRange(Amount, 0x00B7, 0x00BA) ||
                    MathHelper.InRange(Amount, 0x025D, 0x0260) ||
                    MathHelper.InRange(Amount, 0x029A, 0x029B) ||
                    MathHelper.InRange(Amount, 0x02B6, 0x02B7) ||
                    Amount == 0x03DB || Amount == 0x03DF || Amount == 0x03E2 ||
                    Amount == 0x02E8 || Amount == 0x02E9;


                for (int i = -1; i < Constants.USED_LAYER_COUNT; i++)
                {
                    // yes im lazy
                    Layer layer = i == -1 ? Layer.Invalid : LayerOrder.UsedLayers[direction, i];

                    ushort graphic;

                    if (layer == Layer.Invalid)
                    {
                        graphic = GetGraphicForAnimation();
                        AnimationsLoader.Instance.ConvertBodyIfNeeded(ref graphic);
                    }
                    else if (ishuman)
                    {
                        Item itemEquip = FindItemByLayer(layer);

                        if (itemEquip == null)
                        {
                            continue;
                        }

                        graphic = itemEquip.ItemData.AnimID;

                        if (AnimationsLoader.Instance.EquipConversions.TryGetValue(graphic, out Dictionary<ushort, EquipConvData> map))
                        {
                            if (map.TryGetValue(graphic, out EquipConvData data))
                            {
                                _equipConvData = data;
                                graphic = data.Graphic;
                            }
                        }
                    }
                    else
                    {
                        continue;
                    }

                    byte group = AnimationsLoader.Instance.GetDieGroupIndex(graphic, UsedLayer);

                    if (GetTexture(ref graphic, ref group, ref animIndex, direction, out var spriteInfo, out var isUop))
                    {
                        int x = position.X - (IsFlipped ? spriteInfo.UV.Width - spriteInfo.Center.X : spriteInfo.Center.X);
                        int y = position.Y - (spriteInfo.UV.Height + spriteInfo.Center.Y);

                        if (AnimationsLoader.Instance.PixelCheck
                        (
                            graphic,
                            group,
                            direction,
                            isUop,
                            animIndex,
                            IsFlipped ? x + spriteInfo.UV.Width - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - x,
                            SelectedObject.TranslatedMousePositionByViewport.Y - y
                        ))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
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
    }
}