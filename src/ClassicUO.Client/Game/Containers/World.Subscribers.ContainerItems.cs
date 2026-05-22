// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeContainerItems()
        {
            EventSink.ContainerItemAdded += OnContainerItemAdded;
            EventSink.ContainerItemsReceived += OnContainerItemsReceived;
        }

        private void UnsubscribeContainerItems()
        {
            EventSink.ContainerItemAdded -= OnContainerItemAdded;
            EventSink.ContainerItemsReceived -= OnContainerItemsReceived;
        }

        private void OnContainerItemAdded(ContainerItemAddedArgs e)
        {
            AddItemToContainer(e.Serial, e.Graphic, e.Amount, e.X, e.Y, e.Hue, e.ContainerSerial);
        }

        private void OnContainerItemsReceived(ContainerItemsReceivedArgs e)
        {
            Entity container = Get(e.ContainerSerial);
            if (container == null) return;

            ClearContainerAndRemoveItems(container, container.Graphic == 0x2006);
        }
    }
}
