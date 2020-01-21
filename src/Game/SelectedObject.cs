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

using System.Collections.Generic;
using System.Runtime.CompilerServices;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game
{
    internal static class SelectedObject
    {
        public static Point TranslatedMousePositionByViewport;
        public static BaseGameObject Object;
        public static BaseGameObject LastObject;
        public static GameObject HealthbarObject;
        public static GameObject CorpseObject;


        public static bool IsPointInMobile(Mobile mobile, int xx, int yy)
        {
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
                    Item item = mobile.Equipment[(int) layer];

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
                AnimationDirection direction = AnimationsLoader.Instance.GetBodyAnimationGroup(ref graphic, ref animGroup, ref hue, true).Direction[dir];

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


            return false;
        }

        public static bool IsPointInCorpse(Item corpse, int xx, int yy)
        {
            if (corpse == null || World.CorpseManager.Exists(corpse.Serial, 0))
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
                    Item itemEquip = corpse.Equipment[(int) layer];

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

                AnimationDirection direction = gr.Direction[AnimationsLoader.Instance.Direction];

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

            return false;
        }


        [MethodImpl(256)]
        public static bool IsPointInStatic(UOTexture texture, int x, int y)
        {
            return texture != null && texture.Contains(TranslatedMousePositionByViewport.X - x, TranslatedMousePositionByViewport.Y - y);
        }

        [MethodImpl(256)]
        public static bool IsPointInLand(UOTexture texture, int x, int y)
        {
            x = TranslatedMousePositionByViewport.X - x;
            y = TranslatedMousePositionByViewport.Y - y;
            return texture != null && x >= 0 && x < 44 && y >= 0 && y < 44 && _InternalArea[x, y];
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


            return testY >= testX * (y1 - y0) / -22 + y + y0 &&
                   testY >= testX * (y3 - y0) / 22 + y + y0 && testY <= testX * (y3 - y2) / 22 + y + y2 &&
                   testY <= testX * (y1 - y2) / -22 + y + y2;
        }

        private static readonly bool[,] _InternalArea = new bool[44, 44];
        static SelectedObject()
        {
            for (int y = 21, i = 0; y >= 0; --y, i++)
            {
                for (int x = 0; x < 22; x++)
                {
                    if (x < i)
                        continue;
                    _InternalArea[x, y] = _InternalArea[43 - x, 43 - y] = _InternalArea[43 - x, y] = _InternalArea[x, 43 - y] = true;
                }
            }
        }
    }
}