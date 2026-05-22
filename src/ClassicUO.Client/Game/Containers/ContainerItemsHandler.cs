// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Containers
{
    internal sealed class ContainerItemsHandler : IEventListener
    {
        private readonly World _world;

        public ContainerItemsHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.ContainerItemAdded += OnContainerItemAdded;
            EventSink.ContainerItemsReceived += OnContainerItemsReceived;
        }

        public void Unsubscribe()
        {
            EventSink.ContainerItemAdded -= OnContainerItemAdded;
            EventSink.ContainerItemsReceived -= OnContainerItemsReceived;
        }

        private void OnContainerItemAdded(ContainerItemAddedArgs e)
        {
            _world.AddItemToContainer(e.Serial, e.Graphic, e.Amount, e.X, e.Y, e.Hue, e.ContainerSerial);
        }

        private void OnContainerItemsReceived(ContainerItemsReceivedArgs e)
        {
            Entity container = _world.Get(e.ContainerSerial);
            if (container == null) return;

            _world.ClearContainerAndRemoveItems(container, container.Graphic == 0x2006);
        }
    }
}
