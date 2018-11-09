using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.UIGumps;
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

        private void DropHeldItemToWorld(ushort x, ushort y, sbyte z)
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
        }

        private void DropHeldItemToContainer(Item container)
        {
            Rectangle bounds = ContainerManager.Get(container.Graphic).Bounds;
            ushort x = (ushort) RandomHelper.GetValue(bounds.Left, bounds.Right);
            ushort y = (ushort) RandomHelper.GetValue(bounds.Top, bounds.Bottom);
            DropHeldItemToContainer(container, x, y);
        }

        private void DropHeldItemToContainer(Item container, ushort x, ushort y)
        {
            Rectangle bounds = ContainerManager.Get(container.Graphic).Bounds;
            SpriteTexture texture = Art.GetStaticTexture(HeldItem.DisplayedGraphic);

            if (x < bounds.X)
                x = (ushort) bounds.X;

            if (x > bounds.Width - texture.Width)
                x = (ushort) (bounds.Width - texture.Width);

            if (y < bounds.Y)
                y = (ushort) bounds.Y;

            if (y > bounds.Height - texture.Height)
                y = (ushort) (bounds.Height - texture.Height);
            GameActions.DropDown(HeldItem.Serial, x, y, 0, container);
            ClearHolding();
        }

        private void WearHeldItem(Mobile target)
        {
            GameActions.Equip(HeldItem, Layer.Invalid, target);
            ClearHolding();
        }

        public void ClearHolding()
        {
            HeldItem = null;
        }
    }
}