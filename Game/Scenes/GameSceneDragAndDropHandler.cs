using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Input;
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
                    UIManager.RemoveInputBlocker(this);
                    UIManager.GameCursor.ClearDraggedItem();
                }
                else if (value != null && _heldItem == null)
                {
                    UIManager.AddInputBlocker(this);
                    UIManager.GameCursor.SetDraggedItem(value.Graphic, value.Hue);
                }

                _heldItem = value;
            }
        }

        public bool IsHoldingItem => HeldItem != null;

        private void MergeHeldItem(Entity entity)
        {
            GameActions.DropDown(HeldItem, Position.Invalid, entity.Serial);
            ClearHolding();
            Mouse.CancelDoubleClick = true;
        }

        private void PickupItemBegin(Item item, int x, int y, int? amount = null)
        {
            // TODO: AMOUNT CHECK
            PickupItemDirectly(item, x, y, amount ?? item.Amount);
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
            UIManager.Remove<Gump>(item);

            if (item.Container.IsValid)
            {
                foreach (Item i in item.Items)
                    CloseItemGumps(i);
            }
        }

        private void DropHeldItemToWorld(Position position)
        {
            DropHeldItemToWorld(position.X, position.Y, position.Z);
        }

        private void DropHeldItemToWorld(int x, int y, sbyte z)
        {
            GameObject obj = SelectedObject;
            Serial serial;

            if (obj is Item item && TileData.IsContainer((long) item.ItemData.Flags))
            {
                serial = item;
                x = y = 0xFFFF;
                z = 0;
            }
            else
                serial = Serial.MinusOne;

            GameActions.DropDown(HeldItem.Serial, x, y, z, serial);
            ClearHolding();
            Mouse.CancelDoubleClick = true;
        }

        private void DropHeldItemToContainer(Item container)
        {
            Rectangle bounds = ContainerManager.Get(container.Graphic).Bounds;
            int x = RandomHelper.GetValue(bounds.Left, bounds.Right);
            int y = RandomHelper.GetValue(bounds.Top, bounds.Bottom);
            DropHeldItemToContainer(container, x, y);
        }

        private void DropHeldItemToContainer(Item container, int x, int y)
        {
            Rectangle bounds = ContainerManager.Get(container.Graphic).Bounds;
            ArtTexture texture = Art.GetStaticTexture(HeldItem.DisplayedGraphic);

            if (texture != null && !texture.IsDisposed)
            {
                x -= texture.Width / 2;
                y -= texture.Height / 2;

                if (x + texture.Width > bounds.Width)
                    x = bounds.Width - texture.Width;

                if (y + texture.Height > bounds.Height)
                    y = bounds.Height - texture.Height;
            }

            if (x < bounds.X)
                x = bounds.X;

            if (y < bounds.Y)
                y = bounds.Y;

            GameActions.DropDown(HeldItem.Serial, x, y, 0, container);
            ClearHolding();
            Mouse.CancelDoubleClick = true;
        }

        private void WearHeldItem(Mobile target)
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