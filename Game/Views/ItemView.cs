#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    public class ItemView : View
    {
        private Graphic _originalGraphic;

        public ItemView(Item item) : base(item)
        {
            if (TileData.IsWet((long) item.ItemData.Flags)) SortZ++;

            if (!item.IsCorpse)
                AllowedToDraw = item.Graphic > 2 && item.DisplayedGraphic > 2 && !IsNoDrawable(item.Graphic);
            else
            {
                item.AnimIndex = 99;
                if ((item.Direction & Direction.Running) != 0)
                {
                    item.UsedLayer = true;
                    item.Direction &= (Direction) 0x7F;
                }
                else
                    item.UsedLayer = false;

                item.Layer = (Layer) item.Direction;

                AllowedToDraw = true;
                item.DisplayedGraphic = item.Amount;
            }
        }


        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
                return false;

            Item item = (Item) GameObject;

            if (item.IsCorpse)
                return DrawInternal(spriteBatch, position, objectList);

            if (item.Effect == null)
            {
                if (_originalGraphic != item.DisplayedGraphic || Texture == null || Texture.IsDisposed)
                {
                    _originalGraphic = item.DisplayedGraphic;
                    Texture = Art.GetStaticTexture(_originalGraphic);
                    Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + GameObject.Position.Z * 4,
                        Texture.Width, Texture.Height);
                }

                HueVector = RenderExtentions.GetHueVector(GameObject.Hue,
                    TileData.IsPartialHue((long) item.ItemData.Flags),
                    TileData.IsTranslucent((long) item.ItemData.Flags) ? .5f : 0, false);

                if (item.Amount > 1 && TileData.IsStackable((long) item.ItemData.Flags) &&
                    item.DisplayedGraphic == GameObject.Graphic)
                {
                    Vector3 offsetDrawPosition = new Vector3(position.X - 5, position.Y - 5, 0);
                    base.Draw(spriteBatch, offsetDrawPosition, objectList);
                }
                bool ok = base.Draw(spriteBatch, position, objectList);
                MessageOverHead(spriteBatch, position, Bounds.Y - 22);
                return ok;
            }

            if (!item.Effect.IsDisposed)
                return item.Effect.View.Draw(spriteBatch, position, objectList);

            return false;
        }


        public override bool DrawInternal(SpriteBatch3D spriteBatch, Vector3 position,
            MouseOverList objectList)
        {
            Item item = (Item) GameObject;

            spriteBatch.GetZ();

            byte dir = (byte) ((byte) item.Layer & 0x7F & 7);
            bool mirror = false;

            Animations.GetAnimDirection(ref dir, ref mirror);

            IsFlipped = mirror;

            Animations.Direction = dir;

            byte animIndex = (byte) GameObject.AnimIndex;
            Graphic graphic = 0;
            EquipConvData? convertedItem = null;
            Hue color = 0;

            for (int i = 0; i < LayerOrder.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[dir, i];

                if (layer == Layer.Mount) continue;

                if (layer == Layer.Invalid)
                {
                    graphic = item.DisplayedGraphic;
                    Animations.AnimGroup = Animations.GetDieGroupIndex(item.GetMountAnimation(), item.UsedLayer);
                    color = GameObject.Hue;
                }
                else
                {
                    Item itemEquip = item.Equipment[(int) layer];
                    if (itemEquip == null) continue;

                    graphic = itemEquip.ItemData.AnimID;

                    if (Animations.EquipConversions.TryGetValue(itemEquip.Graphic,
                        out Dictionary<ushort, EquipConvData> map))
                    {
                        if (map.TryGetValue(itemEquip.ItemData.AnimID, out EquipConvData data))
                        {
                            convertedItem = data;
                            graphic = data.Graphic;
                        }
                    }

                    color = itemEquip.Hue;
                }

                Animations.AnimID = graphic;

                ref AnimationDirection direction = ref Animations.DataIndex[Animations.AnimID]
                    .Groups[Animations.AnimGroup].Direction[Animations.Direction];
                if (direction.FrameCount == 0 && !Animations.LoadDirectionGroup(ref direction))
                    return false;

                direction.LastAccessTime = CoreGame.Ticks;

                int fc = direction.FrameCount;

                if (fc > 0 && animIndex >= fc) animIndex = (byte) (fc - 1);

                if (animIndex < direction.FrameCount)
                {
                    TextureAnimationFrame frame = direction.Frames[animIndex];

                    if (frame == null || frame.IsDisposed) return false;

                    int drawCenterY = frame.CenterY;
                    int drawX = -22;
                    int drawY = drawCenterY + GameObject.Position.Z * 4 - 22 - 3;

                    int x = drawX + frame.CenterX;
                    int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY;

                    Texture = frame;
                    Bounds = new Rectangle(x, -y, frame.Width, frame.Height);
                    HueVector = RenderExtentions.GetHueVector(color);
                    base.Draw(spriteBatch, position, objectList);
                }
            }

            return true;
        }

        protected override void MousePick(MouseOverList objectList, SpriteVertex[] vertex)
        {
            int x = objectList.MousePosition.X - (int) vertex[0].Position.X;
            int y = objectList.MousePosition.Y - (int) vertex[0].Position.Y;

            //if (Texture.Contains(x, y))
            //{
            //    objectList.Add(GameObject, vertex[0].Position);
            //}

            if (Art.Contains(GameObject.Graphic, x, y, 0))
                objectList.Add(GameObject, vertex[0].Position);
        }
    }
}