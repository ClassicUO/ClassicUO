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

using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Item
    {
        private static EquipConvData? _equipConvData;

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, ref Vector3 hueVec)
        {
            if (!AllowedToDraw || IsDestroyed)
            {
                return false;
            }

            //Engine.DebugInfo.ItemsRendered++;

            hueVec = Vector3.Zero;

            DrawTransparent = false;

            posX += (int) Offset.X;
            posY += (int) (Offset.Y + Offset.Z);

            if (ItemData.IsTranslucent)
            {
                hueVec.Z = 0.5f;
            }

            if (AlphaHue != 255)
            {
                hueVec.Z = 1f - AlphaHue / 255f;
            }

            if (IsCorpse)
            {
                return DrawCorpse(batcher, posX, posY - 3, ref hueVec);
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

            ShaderHueTranslator.GetHueVector(ref hueVec, hue, partial, hueVec.Z);

            if (!IsMulti && !IsCoin && Amount > 1 && ItemData.IsStackable)
            {
                DrawStaticAnimated
                (
                    batcher,
                    graphic,
                    posX - 5,
                    posY - 5,
                    ref hueVec,
                    ref DrawTransparent,
                    false
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
                ref hueVec,
                ref DrawTransparent,
                false
            );

            if (ReferenceEquals(SelectedObject.Object, this) || TargetManager.TargetingState == CursorTarget.MultiPlacement)
            {
                return false;
            }

            ArtTexture texture = ArtLoader.Instance.GetTexture(graphic);

            if (texture != null)
            {
                ref UOFileIndex index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

                posX -= index.Width;
                posY -= index.Height;

                if (ArtLoader.Instance.PixelCheck
                (
                    graphic,
                    SelectedObject.TranslatedMousePositionByViewport.X - posX,
                    SelectedObject.TranslatedMousePositionByViewport.Y - posY
                ))
                {
                    SelectedObject.Object = this;
                }
                else if (!IsMulti && !IsCoin && Amount > 1 && ItemData.IsStackable)
                {
                    if (ArtLoader.Instance.PixelCheck
                    (
                        graphic,
                        SelectedObject.TranslatedMousePositionByViewport.X - posX + 5,
                        SelectedObject.TranslatedMousePositionByViewport.Y - posY + 5
                    ))
                    {
                        SelectedObject.Object = this;
                    }
                }
            }

            return true;
        }

        private bool DrawCorpse(UltimaBatcher2D batcher, int posX, int posY, ref Vector3 hueVec)
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
                ref hueVec
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
                    ref hueVec
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
            ref Vector3 hueVec
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

            if ((direction.FrameCount == 0 || direction.Frames == null) && !AnimationsLoader.Instance.LoadAnimationFrames(graphic, animGroup, dir, ref direction))
            {
                return;
            }

            direction.LastAccessTime = Time.Ticks;
            int fc = direction.FrameCount;

            if (fc > 0 && animIndex >= fc)
            {
                animIndex = (byte) (fc - 1);
            }

            if (animIndex < direction.FrameCount)
            {
                AnimationFrameTexture frame = direction.Frames[animIndex];

                if (frame == null || frame.IsDisposed)
                {
                    return;
                }

                frame.Ticks = Time.Ticks;

                if (flipped)
                {
                    posX -= frame.Width - frame.CenterX;
                }
                else
                {
                    posX -= frame.CenterX;
                }

                posY -= frame.Height + frame.CenterY;


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

                hueVec = Vector3.Zero;

                if (ProfileManager.CurrentProfile.NoColorObjectsOutOfRange && owner.Distance > World.ClientViewRange)
                {
                    hueVec.X = Constants.OUT_RANGE_COLOR;
                    hueVec.Y = 1;
                }
                else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
                {
                    hueVec.X = Constants.DEAD_RANGE_COLOR;
                    hueVec.Y = 1;
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

                    ShaderHueTranslator.GetHueVector(ref hueVec, color, ispartialhue, alpha);
                }

                batcher.DrawSprite
                (
                    frame,
                    posX,
                    posY,
                    flipped,
                    ref hueVec
                );

                if (!SerialHelper.IsValid(owner))
                {
                    return;
                }

                if (ReferenceEquals(SelectedObject.Object, owner))
                {
                    return;
                }

                if (AnimationsLoader.Instance.PixelCheck(graphic, animGroup, dir, direction.IsUOP, animIndex, flipped ? posX + frame.Width - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - posX, SelectedObject.TranslatedMousePositionByViewport.Y - posY))
                {
                    SelectedObject.Object = owner;
                }
            }
        }
    }
}