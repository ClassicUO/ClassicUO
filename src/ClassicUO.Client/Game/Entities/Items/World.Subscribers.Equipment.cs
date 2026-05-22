// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeEquipment()
        {
            EventSink.ItemEquipped += OnItemEquipped;
        }

        private void UnsubscribeEquipment()
        {
            EventSink.ItemEquipped -= OnItemEquipped;
        }

        private void OnItemEquipped(ItemEquippedArgs e)
        {
            if (!InGame)
            {
                return;
            }

            Item item = GetOrCreateItem(e.Serial);

            if (item.Graphic != 0 && item.Layer != Layer.Backpack)
            {
                //ClearContainerAndRemoveItems(item);
                RemoveItemFromContainer(item);
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

            Entity entity = Get(item.Container);

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
                entity == Player
                && (item.Layer == Layer.OneHanded || item.Layer == Layer.TwoHanded)
            )
            {
                Player?.UpdateAbilities();
            }
        }
    }
}
