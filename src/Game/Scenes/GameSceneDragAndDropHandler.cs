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
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Scenes
{
    partial class GameScene
    {
        private GameObject _dragginObject;
        private Point _dragOffset;
        private Item _heldItem;

        public Item HeldItem
        {
            get => _heldItem;
            set
            {
                if (value == null && _heldItem != null)
                {
                    //Engine.UI.RemoveInputBlocker(this);
                    Engine.UI.GameCursor.ClearDraggedItem();
                }
                else if (value != null && _heldItem == null)
                {
                    //Engine.UI.AddInputBlocker(this);
                    Engine.UI.GameCursor.SetDraggedItem(value.DisplayedGraphic, value.Hue, value.Amount > 1 && value.DisplayedGraphic == value.Graphic && value.ItemData.IsStackable, value.ItemData.IsPartialHue, value.ItemData.IsTransparent || value.ItemData.IsTranslucent);
                }

                _heldItem = value;
            }
        }

        public bool IsHoldingItem => HeldItem != null;

        public void MergeHeldItem(Entity entity)
        {
            GameActions.DropItem(HeldItem, Position.Invalid, entity.Serial);
            ClearHolding();
            Mouse.CancelDoubleClick = true;
        }

        private void PickupItemBegin(Item item, int x, int y, int? amount = null)
        {
            if (!_isShiftDown && !amount.HasValue && !item.IsCorpse && item.Amount > 1 && item.ItemData.IsStackable)
            {
                if (Engine.UI.GetByLocalSerial<SplitMenuGump>(item) != null)
                    return;

                SplitMenuGump gump = new SplitMenuGump(item, new Point(x, y))
                {
                    X = Mouse.Position.X - 80,
                    Y = Mouse.Position.Y - 40,
                };
                Engine.UI.Add(gump);
                Engine.UI.AttemptDragControl(gump, Mouse.Position, true);
            }
            else
            {
                PickupItemDirectly(item, x, y, amount ?? item.Amount);
            }
        }

        private void PickupItemDirectly(Item item, int x, int y, int amount)
        {
            if (!item.IsPickable)
                return;

            if (item.Container.IsValid)
            {
                Entity entity = World.Get(item.Container);
                item.Position = entity.Position;
                entity.Items.Remove(item);

                if (item.Container.IsMobile)
                {
                    ((Mobile)entity).Equipment[item.ItemData.Layer] = null;
                }

                //item.Container = Serial.Invalid;
                entity.Items.ProcessDelta();
            }

            CloseItemGumps(item);
            item.Amount = (ushort) amount;
            HeldItem = item;
            NetClient.Socket.Send(new PPickUpRequest(item, (ushort) amount));
        }

        private void CloseItemGumps(Item item)
        {
            Engine.UI.Remove<Gump>(item);

            if (item.Container.IsValid)
            {
                foreach (Item i in item.Items)
                    CloseItemGumps(i);
            }
        }

        public void DropHeldItemToWorld(Position position)
        {
            DropHeldItemToWorld(position.X, position.Y, position.Z);
        }

        public void DropHeldItemToWorld(int x, int y, sbyte z)
        {
            GameObject obj = SelectedObject;
            Serial serial;

            if (obj is Item item && item.ItemData.IsContainer)
            {
                serial = item;
                x = y = 0xFFFF;
                z = 0;
            }
            else
                serial = Serial.MINUS_ONE;

            GameActions.DropItem(HeldItem.Serial, x, y, z, serial);
            ClearHolding();
            Mouse.CancelDoubleClick = true;
        }

        public void DropHeldItemToContainer(Item container, int x = 0xFFFF, int y = 0xFFFF)
        {
            ContainerGump gump = Engine.UI.GetByLocalSerial<ContainerGump>(container);

            if (gump != null)
            {
                Rectangle bounds = ContainerManager.Get(gump.Graphic).Bounds;
                ArtTexture texture = FileManager.Art.GetTexture(HeldItem.DisplayedGraphic);

                if (texture != null && !texture.IsDisposed)
                {
                    x -= texture.Width >> 1;
                    y -= texture.Height >> 1;

                    if (x + texture.Width > bounds.Width)
                        x = bounds.Width - texture.Width;

                    if (y + texture.Height > bounds.Height)
                        y = bounds.Height - texture.Height;
                }

                if (x < bounds.X)
                    x = bounds.X;

                if (y < bounds.Y)
                    y = bounds.Y;
            }
            else
            {
                x = 0xFFFF;
                y = 0xFFFF;
            }
            

            GameActions.DropItem(HeldItem.Serial, x, y, 0, container);
            ClearHolding();
            Mouse.CancelDoubleClick = true;
        }

        public void WearHeldItem(Mobile target)
        {
            GameActions.Equip(HeldItem, Layer.Invalid, target);
            ClearHolding();
            Mouse.CancelDoubleClick = true;
        }

        public void ClearHolding()
        {
            HeldItem = null;
        }
    }
}