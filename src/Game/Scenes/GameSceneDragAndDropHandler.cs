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

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene
    {
        private Entity _dragginObject;

        public void MergeHeldItem(Entity container)
        {
            if (ItemHold.Enabled && ItemHold.Serial != container)
            {
                if (SerialHelper.IsMobile(container.Serial))
                    GameActions.DropItem(ItemHold.Serial, 0xFFFF, 0xFFFF, 0, container.Serial);
                else if (SerialHelper.IsItem(container.Serial))
                    GameActions.DropItem(ItemHold.Serial, container.X, container.Y, container.Z, container.Serial);

                ItemHold.Enabled = false;
                ItemHold.Dropped = true;
            }
        }

        private bool PickupItemBegin(Item item, int x, int y, int? amount = null, Point? offset = null)
        {
            if (World.Player.IsDead || item == null || item.IsDestroyed || item.IsMulti || item.OnGround && (item.IsLocked || item.Distance > Constants.DRAG_ITEMS_DISTANCE))
                return false;

            if (!amount.HasValue && item.Amount > 1 && item.ItemData.IsStackable)
            {
                if (ProfileManager.Current.HoldShiftToSplitStack == Keyboard.Shift)
                {
                    if (UIManager.GetGump<SplitMenuGump>(item) != null)
                        return false;

                    SplitMenuGump gump = new SplitMenuGump(item, new Point(x, y))
                    {
                        X = Mouse.Position.X - 80,
                        Y = Mouse.Position.Y - 40
                    };
                    UIManager.Add(gump);
                    UIManager.AttemptDragControl(gump, Mouse.Position, true);

                    return true;
                }
            }

            return PickupItemDirectly(item, x, y, amount ?? item.Amount, offset);
        }

        private bool PickupItemDirectly(Item item, int x, int y, int amount, Point? offset)
        {
            if (World.Player.IsDead || ItemHold.Enabled || item == null || item.IsDestroyed /*|| (!ItemHold.Enabled && ItemHold.Dropped && ItemHold.Serial.IsValid)*/)
                return false;

            ItemHold.Clear();
            ItemHold.Set(item, amount <= 0 ? item.Amount : (ushort) amount);
            UIManager.GameCursor.SetDraggedItem(offset);

            if (!item.OnGround)
            {
                Entity entity = World.Get(item.Container);
                //item.Container = Serial.INVALID;
                //entity.Items.Remove(item);

                if (entity.HasEquipment)
                    entity.Equipment[(int) item.Layer] = null;

                //entity.Items.ProcessDelta();
            }
            else
            {
                item.RemoveFromTile();
            }
            item.TextContainer?.Clear();

            item.AllowedToDraw = false;
            //World.Items.Remove(item);
            //World.Items.ProcessDelta();
            //CloseItemGumps(item);

            NetClient.Socket.Send(new PPickUpRequest(item, (ushort) amount));

            return true;
        }

        private void CloseItemGumps(Item item)
        {
            if (item != null)
            {
                var gump = UIManager.GetGump<Gump>(item);
           
                if (gump != null)
                {
                    if (gump.GumpType == GUMP_TYPE.GT_SPELLBUTTON)
                        return;

                    gump.Dispose();
                }

                if (SerialHelper.IsValid(item.Container))
                {
                    foreach (Item i in item.Items)
                        CloseItemGumps(i);
                }
            }
        }

        public void DropHeldItemToWorld(int x, int y, sbyte z)
        {
            GameObject obj = SelectedObject.Object as GameObject;
            uint serial;

            if (obj is Item item && item.ItemData.IsContainer)
            {
                serial = item;
                x = y = 0xFFFF;
                z = 0;
            }
            else
                serial = 0xFFFF_FFFF;

            if (ItemHold.Enabled && ItemHold.Serial != serial)
            {
                GameActions.DropItem(ItemHold.Serial, x, y, z, serial);
                ItemHold.Enabled = false;
                ItemHold.Dropped = true;
            }
        }

        public void DropHeldItemToContainer(Item container, int x = 0xFFFF, int y = 0xFFFF)
        {
            if (ItemHold.Enabled && container != null && ItemHold.Serial != container.Serial)
            {
                ContainerGump gump = UIManager.GetGump<ContainerGump>(container);

                if (gump != null && (x != 0xFFFF || y != 0xFFFF))
                {
                    Rectangle bounds = ContainerManager.Get(gump.Graphic).Bounds;
                    ArtTexture texture = ArtLoader.Instance.GetTexture(ItemHold.DisplayedGraphic);
                    float scale = UIManager.ContainerScale;

                    bounds.X = (int) (bounds.X * scale);
                    bounds.Y = (int) (bounds.Y * scale);
                    bounds.Width = (int) (bounds.Width * scale);
                    bounds.Height = (int) (bounds.Height * scale);

                    if (texture != null && !texture.IsDisposed)
                    {
                        int textureW, textureH;

                        if (ProfileManager.Current != null && ProfileManager.Current.ScaleItemsInsideContainers)
                        {
                            textureW = (int) (texture.Width * scale);
                            textureH = (int) (texture.Height * scale);
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

                    x = (int) (x / scale);
                    y = (int) (y / scale);
                }
                else
                {
                    x = 0xFFFF;
                    y = 0xFFFF;
                }


                GameActions.DropItem(ItemHold.Serial, x, y, 0, container);
                ItemHold.Enabled = false;
                ItemHold.Dropped = true;
            }
        }

        public void WearHeldItem(Mobile target)
        {
            if (ItemHold.Enabled && ItemHold.IsWearable)
            {
                GameActions.Equip(ItemHold.Serial, (Layer) TileDataLoader.Instance.StaticData[ItemHold.Graphic].Layer, target);
                ItemHold.Enabled = false;
                ItemHold.Dropped = true;
            }
        }
    }
}