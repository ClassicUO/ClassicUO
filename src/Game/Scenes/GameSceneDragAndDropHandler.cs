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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene
    {
        private GameObject _dragginObject;
        private Point _dragOffset;


        public ItemHold HeldItem { get; private set; }

        public bool IsHoldingItem => HeldItem != null && HeldItem.Enabled;


        public void MergeHeldItem(Entity container)
        {
            if (HeldItem.Enabled && HeldItem.Serial != container)
            {
                if (container.Serial.IsMobile)
                    GameActions.DropItem(HeldItem.Serial, 0xFFFF, 0xFFFF, 0, container.Serial);
                else if (container.Serial.IsItem)
                    GameActions.DropItem(HeldItem.Serial, container.Position.X, container.Position.Y, container.Position.Z, container.Serial);

                HeldItem.Enabled = false;
                HeldItem.Dropped = true;
            }
        }

        private bool PickupItemBegin(Item item, int x, int y, int? amount = null)
        {
            if (World.Player.IsDead || item == null || item.IsDestroyed || item.IsMulti || item.OnGround && (item.IsLocked || item.Distance > Constants.DRAG_ITEMS_DISTANCE))
                return false;

            if (!amount.HasValue && item.Amount > 1 && item.ItemData.IsStackable)
            {
                if (Engine.Profile.Current.HoldShiftToSplitStack == _isShiftDown)
                {
                    if (Engine.UI.GetGump<SplitMenuGump>(item) != null)
                        return false;

                    SplitMenuGump gump = new SplitMenuGump(item, new Point(x, y))
                    {
                        X = Mouse.Position.X - 80,
                        Y = Mouse.Position.Y - 40
                    };
                    Engine.UI.Add(gump);
                    Engine.UI.AttemptDragControl(gump, Mouse.Position, true);

                    return true;
                }
            }

            return PickupItemDirectly(item, x, y, amount ?? item.Amount);
        }

        private bool PickupItemDirectly(Item item, int x, int y, int amount)
        {
            if (World.Player.IsDead || HeldItem.Enabled || item == null || item.IsDestroyed /*|| (!HeldItem.Enabled && HeldItem.Dropped && HeldItem.Serial.IsValid)*/) return false;

            HeldItem.Clear();
            HeldItem.Set(item, amount <= 0 ? item.Amount : (ushort) amount);

            if (!item.OnGround)
            {
                Entity entity = World.Get(item.Container);
                item.Container = Serial.INVALID;
                entity.Items.Remove(item);

                if (entity.HasEquipment) entity.Equipment[(int) item.Layer] = null;

                entity.Items.ProcessDelta();
            }
            else
            {
                item.RemoveFromTile();
            }
            item.TextContainer?.Clear();

            World.Items.Remove(item);
            World.Items.ProcessDelta();
            CloseItemGumps(item);

            NetClient.Socket.Send(new PPickUpRequest(item, (ushort) amount));

            return true;
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
            GameObject obj = SelectedObject.Object as GameObject;
            Serial serial;

            if (obj is Item item && item.ItemData.IsContainer)
            {
                serial = item;
                x = y = 0xFFFF;
                z = 0;
            }
            else
                serial = Serial.MINUS_ONE;

            if (HeldItem.Enabled && HeldItem.Serial != serial)
            {
                GameActions.DropItem(HeldItem.Serial, x, y, z, serial);
                HeldItem.Enabled = false;
                HeldItem.Dropped = true;
            }
        }

        public void DropHeldItemToContainer(Item container, int x = 0xFFFF, int y = 0xFFFF)
        {
            if (HeldItem.Enabled && container != null && HeldItem.Serial != container.Serial)
            {
                ContainerGump gump = Engine.UI.GetGump<ContainerGump>(container);

                if (gump != null && (x != 0xFFFF || y != 0xFFFF))
                {
                    Rectangle bounds = ContainerManager.Get(gump.Graphic).Bounds;
                    ArtTexture texture = FileManager.Art.GetTexture(HeldItem.DisplayedGraphic);
                    float scale = Engine.UI.ContainerScale;

                    bounds.X = (int)(bounds.X * scale);
                    bounds.Y = (int)(bounds.Y * scale);
                    bounds.Width = (int) (bounds.Width * scale);
                    bounds.Height = (int)(bounds.Height * scale);

                    if (texture != null && !texture.IsDisposed)
                    {
                        int textureW, textureH;

                        if (Engine.Profile.Current != null && Engine.Profile.Current.ScaleItemsInsideContainers)
                        {
                            textureW = (int)(texture.Width * scale);
                            textureH = (int)(texture.Height * scale);
                        }
                        else
                        {
                            textureW = texture.Width;
                            textureH = texture.Height;
                        }

                        x -= textureW >> 1;
                        y -= textureH >> 1;

                        if (x + textureW > bounds.Width)
                            x = bounds.Width - textureW;

                        if (y + textureH > bounds.Height)
                            y = bounds.Height - textureH;
                    }

                    if (x < bounds.X)
                        x = bounds.X;

                    if (y < bounds.Y)
                        y = bounds.Y;

                    x = (int)(x / scale);
                    y = (int)(y / scale);
                }
                else
                {
                    x = 0xFFFF;
                    y = 0xFFFF;
                }


                GameActions.DropItem(HeldItem.Serial, x, y, 0, container);
                HeldItem.Enabled = false;
                HeldItem.Dropped = true;
            }
        }

        public void WearHeldItem(Mobile target)
        {
            if (HeldItem.Enabled && HeldItem.IsWearable)
            {
                GameActions.Equip(HeldItem.Serial, (Layer) FileManager.TileData.StaticData[HeldItem.Graphic].Layer, target);
                HeldItem.Enabled = false;
                HeldItem.Dropped = true;
            }
        }
    }
}