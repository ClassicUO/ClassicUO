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

using System.Runtime.CompilerServices;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game
{
    internal static class SelectedObject
    {
        public static Point TranslatedMousePositionByViewport;
        public static BaseGameObject Object;
        public static BaseGameObject LastObject;
        public static BaseGameObject LastLeftDownObject;
        public static Entity HealthbarObject;
        public static Item SelectedContainer;
        public static Item CorpseObject;

        private static readonly bool[,] _InternalArea = new bool[44, 44];

        static SelectedObject()
        {
            for (int y = 21, i = 0; y >= 0; --y, i++)
            {
                for (int x = 0; x < 22; x++)
                {
                    if (x < i)
                    {
                        continue;
                    }

                    _InternalArea[x, y] = _InternalArea[43 - x, 43 - y] = _InternalArea[43 - x, y] = _InternalArea[x, 43 - y] = true;
                }
            }
        }


        public static bool IsPointInMobile(Mobile mobile, int xx, int yy)
        {
            /*
            bool mirror = false;
            byte dir = (byte) mobile.GetDirectionForAnimation();
            AnimationsLoader.Instance.GetAnimDirection(ref dir, ref mirror);


            sbyte animIndex = mobile.AnimIndex;

            for (int i = -2; i < Constants.USED_LAYER_COUNT; i++)
            {
                Layer layer = i == -2 ? Layer.Invalid : i == -1 ? Layer.Mount : LayerOrder.UsedLayers[dir, i];

                ushort graphic;

                if (layer == Layer.Invalid)
                    graphic = mobile.GetGraphicForAnimation();
                else if (mobile.HasEquipment)
                {
                    Item item = mobile.FindItemByLayer( layer];

                    if (item == null)
                        continue;

                    if (layer == Layer.Mount)
                        graphic = item.GetGraphicForAnimation();
                    else if (item.ItemData.AnimID != 0)
                    {
                        if (Mobile.IsCovered(mobile, layer))
                            continue;

                        graphic = item.ItemData.AnimID;

                        if (AnimationsLoader.Instance.EquipConversions.TryGetValue(mobile.Graphic, out Dictionary<ushort, EquipConvData> map))
                        {
                            if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
                                graphic = data.Graphic;
                        }
                    }
                    else
                        continue;
                }
                else
                    continue;


                byte animGroup = Mobile.GetGroupForAnimation(mobile, graphic, layer == Layer.Invalid);

                ushort hue = 0;
                AnimationForwardDirection direction = AnimationsLoader.Instance.GetBodyAnimationGroup(ref graphic, ref animGroup, ref hue, true).Direction[dir];

                AnimationsLoader.Instance.AnimID = graphic;
                AnimationsLoader.Instance.AnimGroup = animGroup;
                AnimationsLoader.Instance.Direction = dir;

                if (direction == null || ((direction.FrameCount == 0 || direction.Frames == null) && !AnimationsLoader.Instance.LoadDirectionGroup(ref direction)))
                    continue;

                int fc = direction.FrameCount;

                if (fc != 0 && animIndex >= fc)
                    animIndex = 0;

                if (animIndex < direction.FrameCount)
                {
                    AnimationFrameTexture frame = direction.Frames[animIndex];

                    if (frame == null || frame.IsDisposed)
                        continue;

                    int drawX;

                    int drawCenterY = frame.CenterY;
                    int yOff = ((int) mobile.Offset.Z >> 2) - 22 - (int) (mobile.Offset.Y - mobile.Offset.Z - 3);
                    int drawY = drawCenterY + yOff;

                    if (mirror)
                        drawX = -22 + (int) mobile.Offset.X;
                    else
                        drawX = -22 - (int) mobile.Offset.X;

                    int x = drawX + frame.CenterX;
                    int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY;

                    if (mirror)
                        x = xx + x + 44 - TranslatedMousePositionByViewport.X;
                    else
                        x = TranslatedMousePositionByViewport.X - xx + x;

                    y = TranslatedMousePositionByViewport.Y - yy - y;

                    if (frame.Contains(x, y))
                        return true;
                }
            }

            */
            return false;
        }

        public static bool IsPointInCorpse(Item corpse, int xx, int yy)
        {
            /*if (corpse == null || World.CorpseManager.Exists(corpse.Serial, 0))
                return false;

            byte dir = (byte) ((byte) corpse.Layer & 0x7F & 7);
            bool mirror = false;
            AnimationsLoader.Instance.GetAnimDirection(ref dir, ref mirror);
            AnimationsLoader.Instance.Direction = dir;
            byte animIndex = (byte) corpse.AnimIndex;

            for (int i = -1; i < Constants.USED_LAYER_COUNT; i++)
            {
                Layer layer = i == -1 ? Layer.Mount : LayerOrder.UsedLayers[dir, i];

                ushort graphic;
                ushort color = 0;

                if (layer == Layer.Invalid)
                {
                    graphic = corpse.GetGraphicForAnimation();
                    AnimationsLoader.Instance.AnimGroup = AnimationsLoader.Instance.GetDieGroupIndex(graphic, corpse.UsedLayer);
                }
                else if (corpse.HasEquipment && MathHelper.InRange(corpse.Amount, 0x0190, 0x0193) ||
                         MathHelper.InRange(corpse.Amount, 0x00B7, 0x00BA) ||
                         MathHelper.InRange(corpse.Amount, 0x025D, 0x0260) ||
                         MathHelper.InRange(corpse.Amount, 0x029A, 0x029B) ||
                         MathHelper.InRange(corpse.Amount, 0x02B6, 0x02B7) ||
                         corpse.Amount == 0x03DB || corpse.Amount == 0x03DF || corpse.Amount == 0x03E2 || corpse.Amount == 0x02E8 || corpse.Amount == 0x02E9)
                {
                    Item itemEquip = corpse.FindItemByLayer( layer];

                    if (itemEquip == null)
                        continue;

                    graphic = itemEquip.ItemData.AnimID;

                    if (AnimationsLoader.Instance.EquipConversions.TryGetValue(corpse.Amount, out Dictionary<ushort, EquipConvData> map))
                    {
                        if (map.TryGetValue(graphic, out EquipConvData data))
                            graphic = data.Graphic;
                    }
                }
                else
                    continue;


                byte animGroup = AnimationsLoader.Instance.AnimGroup;

                AnimationGroup gr = layer == Layer.Invalid
                                        ? AnimationsLoader.Instance.GetCorpseAnimationGroup(ref graphic, ref animGroup, ref color)
                                        : AnimationsLoader.Instance.GetBodyAnimationGroup(ref graphic, ref animGroup, ref color);

                AnimationForwardDirection direction = gr.Direction[AnimationsLoader.Instance.Direction];

                if (direction == null || ((direction.FrameCount == 0 || direction.Frames == null) && !AnimationsLoader.Instance.LoadDirectionGroup(ref direction)))
                    continue;

                int fc = direction.FrameCount;

                if (fc > 0 && animIndex >= fc)
                    animIndex = (byte) (fc - 1);

                if (animIndex < direction.FrameCount)
                {
                    AnimationFrameTexture frame = direction.Frames[animIndex];

                    if (frame == null || frame.IsDisposed)
                        continue;

                    int drawCenterY = frame.CenterY;
                    const int drawX = -22;
                    int drawY = drawCenterY - 22;
                    drawY -= 3;
                    int x = drawX + frame.CenterX;
                    int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY;

                    if (mirror)
                        x = xx + x + 44 - TranslatedMousePositionByViewport.X;
                    else
                        x = TranslatedMousePositionByViewport.X - xx + x;

                    y = TranslatedMousePositionByViewport.Y - yy + y;

                    if (frame.Contains(x, y))
                        return true;
                }
            }            
            */

            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPointInStatic(UOTexture texture, int x, int y)
        {
            return texture != null && texture.Contains(TranslatedMousePositionByViewport.X - x, TranslatedMousePositionByViewport.Y - y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPointInLand(int x, int y)
        {
            x = TranslatedMousePositionByViewport.X - x;
            y = TranslatedMousePositionByViewport.Y - y;

            return x >= 0 && x < 44 && y >= 0 && y < 44 && _InternalArea[x, y];
        }

        public static bool IsPointInStretchedLand(ref Rectangle rect, int x, int y)
        {
            //y -= 22;
            x += 22;

            int testX = TranslatedMousePositionByViewport.X - x;
            int testY = TranslatedMousePositionByViewport.Y;

            int y0 = -rect.Left;
            int y1 = 22 - rect.Top;
            int y2 = 44 - rect.Right;
            int y3 = 22 - rect.Bottom;


            return testY >= testX * (y1 - y0) / -22 + y + y0 && testY >= testX * (y3 - y0) / 22 + y + y0 && testY <= testX * (y3 - y2) / 22 + y + y2 && testY <= testX * (y1 - y2) / -22 + y + y2;
        }
    }
}