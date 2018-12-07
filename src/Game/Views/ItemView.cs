#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    public class ItemView : View
    {
        private Graphic _originalGraphic;

        public ItemView(Item item) : base(item)
        {
            //if (TileData.IsWet((long) item.ItemData.Flags)) SortZ++;

            if (!item.IsCorpse)
                AllowedToDraw = item.Graphic > 2 && item.DisplayedGraphic > 2 && !IsNoDrawable(item.Graphic);
            else
            {
                //item.AnimIndex = 99;

                if ((item.Direction & Direction.Running) != 0)
                {
                    item.UsedLayer = true;
                    item.Direction &= (Direction) 0x7F;
                }
                else
                    item.UsedLayer = false;

                item.Layer = (Layer) item.Direction;
                AllowedToDraw = true;
                //item.DisplayedGraphic = item.Amount;
            }
        }

        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
                return false;
            Item item = (Item) GameObject;

            if (item.IsCorpse)
                return DrawCorpse(batcher, position, objectList);

            if (item.Effect == null)
            {
                if (_originalGraphic != item.DisplayedGraphic || Texture == null || Texture.IsDisposed)
                {
                    _originalGraphic = item.DisplayedGraphic;
                    Texture = Art.GetStaticTexture(_originalGraphic);
                    Bounds = new Rectangle((Texture.Width >> 1) - 22, Texture.Height - 44, Texture.Width, Texture.Height);
                }

                HueVector = ShaderHuesTraslator.GetHueVector(GameObject.Hue, TileData.IsPartialHue( item.ItemData.Flags), TileData.IsTranslucent( item.ItemData.Flags) ? .5f : 0, false);

                if (item.Amount > 1 && TileData.IsStackable(item.ItemData.Flags) && item.DisplayedGraphic == GameObject.Graphic)
                {
                    Vector3 offsetDrawPosition = new Vector3(position.X - 5, position.Y - 5, 0);
                    base.Draw(batcher, offsetDrawPosition, objectList);
                }

                bool ok = base.Draw(batcher, position, objectList);
                MessageOverHead(batcher, position, Bounds.Y);

                return ok;
            }

            return !item.Effect.IsDisposed && item.Effect.View.Draw(batcher, position, objectList);
        }

        private bool DrawCorpse(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (GameObject.IsDisposed)
                return false;
            Item item = (Item) GameObject;

            byte dir = (byte) ((byte) item.Layer & 0x7F & 7);
            bool mirror = false;
            Animations.GetAnimDirection(ref dir, ref mirror);
            IsFlipped = mirror;
            Animations.Direction = dir;
            byte animIndex = (byte) GameObject.AnimIndex;


            for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[dir, i];

                if (layer == Layer.Mount) continue;
                Graphic graphic;
                Hue color;

                if (layer == Layer.Invalid)
                {
                    graphic = item.GetMountAnimation();
                    //graphic = item.DisplayedGraphic;
                    Animations.AnimGroup = Animations.GetDieGroupIndex(item.GetMountAnimation(), item.UsedLayer);
                    color = GameObject.Hue;
                }
                else
                {
                    Item itemEquip = item.Equipment[(int) layer];

                    if (itemEquip == null) continue;
                    graphic = itemEquip.ItemData.AnimID;

                    if (Animations.EquipConversions.TryGetValue(itemEquip.Graphic, out Dictionary<ushort, EquipConvData> map))
                    {
                        if (map.TryGetValue(itemEquip.ItemData.AnimID, out EquipConvData data))
                            graphic = data.Graphic;
                    }

                    color = itemEquip.Hue;
                }

                Animations.AnimID = graphic;
                ref AnimationDirection direction = ref Animations.DataIndex[Animations.AnimID].Groups[Animations.AnimGroup].Direction[Animations.Direction];

                if ((direction.FrameCount == 0 || direction.Frames == null) && !Animations.LoadDirectionGroup(ref direction))
                    return false;
                direction.LastAccessTime = Engine.Ticks;
                int fc = direction.FrameCount;
                if (fc > 0 && animIndex >= fc)
                    animIndex = (byte) (fc - 1);

                if (animIndex < direction.FrameCount)
                {
                    AnimationFrameTexture frame = direction.Frames[animIndex];

                    if (frame == null || frame.IsDisposed) return false;

                    int drawCenterY = frame.CenterY;
                    const int drawX = -22;
                    int drawY = drawCenterY - 22 - 3;
                    int x = drawX + frame.CenterX;
                    int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY;
                    Texture = frame;
                    Bounds = new Rectangle(x, -y, frame.Width, frame.Height);
                    HueVector = ShaderHuesTraslator.GetHueVector(color);
                    base.Draw(batcher, position, objectList);
                    Pick(frame, Bounds, position, objectList);
                }

                break;
            }

            return true;
        }

        private void Pick(SpriteTexture texture, Rectangle area, Vector3 drawPosition, MouseOverList list)
        {
            int x;

            if (IsFlipped)
                x = (int) drawPosition.X + area.X + 44 - list.MousePosition.X;
            else
                x = list.MousePosition.X - (int) drawPosition.X + area.X;
            int y = list.MousePosition.Y - ((int) drawPosition.Y - area.Y);
            //if (Animations.Contains(id, x, y))
            if (texture.Contains(x, y))
                list.Add(GameObject, drawPosition);
        }

        protected override void MousePick(MouseOverList objectList, SpriteVertex[] vertex)
        {
            int x = objectList.MousePosition.X - (int) vertex[0].Position.X;
            int y = objectList.MousePosition.Y - (int) vertex[0].Position.Y;

            //if (Texture.Contains(x, y))
            //{
            //    objectList.Add(GameObject, vertex[0].Position);
            //}

            //if (Art.Contains(((Item)GameObject).DisplayedGraphic, x, y))
            if (Texture.Contains(x, y))
                objectList.Add(GameObject, vertex[0].Position);
        }
    }
}