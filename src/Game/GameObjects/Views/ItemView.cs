#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Item
    {
        private Graphic _originalGraphic;

       
        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || IsDisposed)
                return false;

            Engine.DebugInfo.ItemsRendered++;

            if (IsCorpse)
                return DrawCorpse(batcher, position, objectList);

            if ( _originalGraphic != DisplayedGraphic || Texture == null || Texture.IsDisposed)
            {
                _originalGraphic = DisplayedGraphic;
                Texture = FileManager.Art.GetTexture(_originalGraphic);
                Bounds = new Rectangle((Texture.Width >> 1) - 22, Texture.Height - 44, Texture.Width, Texture.Height);
            }
            
            if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
                HueVector = new Vector3(Constants.OUT_RANGE_COLOR, 1, HueVector.Z);
            else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
                HueVector = new Vector3(Constants.DEAD_RANGE_COLOR, 1, HueVector.Z);
            else
                HueVector = ShaderHuesTraslator.GetHueVector( IsSelected ? 0x0035 : IsHidden ? 0x038E : Hue, ItemData.IsPartialHue, ItemData.IsTranslucent ? .5f : 0, false);

            if (Amount > 1 && ItemData.IsStackable && DisplayedGraphic == Graphic)
            {
                Vector3 offsetDrawPosition = new Vector3(position.X - 5, position.Y - 5, 0);
                base.Draw(batcher, offsetDrawPosition, objectList);
            }

            bool ok = base.Draw(batcher, position, objectList);
            //MessageOverHead(batcher, position, Bounds.Y);
            
            return ok;
        }

        private bool DrawCorpse(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (IsDisposed)
                return false;

            byte dir = (byte) ((byte) Layer & 0x7F & 7);
            bool mirror = false;
            FileManager.Animations.GetAnimDirection(ref dir, ref mirror);
            IsFlipped = mirror;
            FileManager.Animations.Direction = dir;
            byte animIndex = (byte) AnimIndex;


            for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[dir, i];

                if (layer == Layer.Mount) continue;
                Graphic graphic;
                Hue color;

                if (layer == Layer.Invalid)
                {
                    graphic = GetGraphicForAnimation();
                    //graphic = item.DisplayedGraphic;
                    FileManager.Animations.AnimGroup = FileManager.Animations.GetDieGroupIndex(GetGraphicForAnimation(), UsedLayer);
                    color = Hue;
                }
                else if (HasEquipment)
                {
                    Item itemEquip = Equipment[(int) layer];

                    if (itemEquip == null) continue;
                    graphic = itemEquip.ItemData.AnimID;

                    if (FileManager.Animations.EquipConversions.TryGetValue(itemEquip.Graphic, out Dictionary<ushort, EquipConvData> map))
                    {
                        if (map.TryGetValue(itemEquip.ItemData.AnimID, out EquipConvData data))
                            graphic = data.Graphic;
                    }

                    color = itemEquip.Hue;
                }
                else 
                    continue;

                FileManager.Animations.AnimID = graphic;
                ref var index = ref FileManager.Animations.DataIndex[FileManager.Animations.AnimID];
                ref AnimationDirection direction = ref index.Groups[FileManager.Animations.AnimGroup].Direction[FileManager.Animations.Direction];

                if ((direction.FrameCount == 0 || direction.FramesHashes == null) && !FileManager.Animations.LoadDirectionGroup(ref direction))
                    return false;
                direction.LastAccessTime = Engine.Ticks;
                int fc = direction.FrameCount;
                if (fc > 0 && animIndex >= fc)
                    animIndex = (byte) (fc - 1);

                if (color == 0)
                    color = index.Color;

                if (animIndex < direction.FrameCount)
                {
                    AnimationFrameTexture frame = FileManager.Animations.GetTexture(direction.FramesHashes[animIndex]);

                    if (frame == null || frame.IsDisposed) return false;

                    int drawCenterY = frame.CenterY;
                    const int drawX = -22;
                    int drawY = drawCenterY - 22 - 3;
                    int x = drawX + frame.CenterX;
                    int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY;
                    Texture = frame;
                    Bounds = new Rectangle(x, -y, frame.Width, frame.Height);
                    
                    if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
                        HueVector = new Vector3(Constants.OUT_RANGE_COLOR, 1, HueVector.Z);
                    else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
                        HueVector = new Vector3(Constants.DEAD_RANGE_COLOR, 1, HueVector.Z);
                    else
                        HueVector = ShaderHuesTraslator.GetHueVector(color);

                    base.Draw(batcher, position, objectList);
                    Pick(frame, Bounds, position, objectList);
                }
            }

            //MessageOverHead(batcher, position, Bounds.Y);

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
            //if (FileManager.Animations.Contains(id, x, y))
            if (texture.Contains(x, y))
                list.Add(this, drawPosition);
        }

        protected override void MousePick(MouseOverList objectList, SpriteVertex[] vertex, bool istransparent)
        {
            int x = objectList.MousePosition.X - (int) vertex[0].Position.X;
            int y = objectList.MousePosition.Y - (int) vertex[0].Position.Y;

            //if (Texture.Contains(x, y))
            //{
            //    objectList.AddOrUpdateText(GameObject, vertex[0].Position);
            //}

            //if (FileManager.Art.Contains(((Item)GameObject).DisplayedGraphic, x, y))
            if (Texture.Contains(x, y))
                objectList.Add(this, vertex[0].Position);
        }
    }
}