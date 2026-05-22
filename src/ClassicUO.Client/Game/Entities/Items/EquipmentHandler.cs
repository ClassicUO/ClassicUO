// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Entities.Items
{
    /// <summary>
    /// Handles <see cref="EventSink.ItemEquipped"/> — promotes the item to its
    /// equip layer, refreshes container/paperdoll gumps, and recomputes the
    /// player's ability glyphs when a primary weapon changes.
    /// Replaces the legacy <c>World.Subscribers.Equipment</c> partial.
    /// </summary>
    internal sealed class EquipmentHandler : IEventListener
    {
        private readonly World _world;

        public EquipmentHandler(World world) => _world = world;

        public void Subscribe() => EventSink.ItemEquipped += OnItemEquipped;

        public void Unsubscribe() => EventSink.ItemEquipped -= OnItemEquipped;

        private void OnItemEquipped(ItemEquippedArgs e)
        {
            if (!_world.InGame)
            {
                return;
            }

            Item item = _world.GetOrCreateItem(e.Serial);

            if (item.Graphic != 0 && item.Layer != Layer.Backpack)
            {
                //ClearContainerAndRemoveItems(item);
                _world.RemoveItemFromContainer(item);
            }

            if (SerialHelper.IsValid(item.Container))
            {
                UIManager.GetGump<ContainerGump>(item.Container)?.RequestUpdateContents();

                UIManager.GetGump<PaperDollGump>(item.Container)?.RequestUpdateContents();
            }

            item.Graphic = e.Graphic;
            item.Layer = e.Layer;
            item.Container = e.ContainerSerial;
            item.FixHue(e.Hue);
            item.Amount = 1;

            Entity entity = _world.Get(item.Container);

            entity?.PushToBack(item);

            if (item.Layer >= Layer.ShopBuyRestock && item.Layer <= Layer.ShopSell)
            {
                //item.Clear();
            }
            else if (SerialHelper.IsValid(item.Container) && item.Layer < Layer.Mount)
            {
                UIManager.GetGump<PaperDollGump>(item.Container)?.RequestUpdateContents();
            }

            if (
                entity == _world.Player
                && (item.Layer == Layer.OneHanded || item.Layer == Layer.TwoHanded)
            )
            {
                _world.Player?.UpdateAbilities();
            }
        }
    }
}
