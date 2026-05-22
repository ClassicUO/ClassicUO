// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Input
{
    internal sealed class DenyMoveHandler : IEventListener
    {
        private readonly World _world;

        public DenyMoveHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.ItemMoveDenied += OnItemMoveDenied;
        }

        public void Unsubscribe()
        {
            EventSink.ItemMoveDenied -= OnItemMoveDenied;
        }

        private void OnItemMoveDenied(ItemMoveDeniedArgs e)
        {
            if (!_world.InGame)
            {
                return;
            }

            Item firstItem = _world.Items.Get(Client.Game.UO.GameCursor.ItemHold.Serial);

            if (
                Client.Game.UO.GameCursor.ItemHold.Enabled
                || Client.Game.UO.GameCursor.ItemHold.Dropped
                    && (firstItem == null || !firstItem.AllowedToDraw)
            )
            {
                if (_world.ObjectToRemove == Client.Game.UO.GameCursor.ItemHold.Serial)
                {
                    _world.ObjectToRemove = 0;
                }

                if (
                    SerialHelper.IsValid(Client.Game.UO.GameCursor.ItemHold.Serial)
                    && Client.Game.UO.GameCursor.ItemHold.Graphic != 0xFFFF
                )
                {
                    if (!Client.Game.UO.GameCursor.ItemHold.UpdatedInWorld)
                    {
                        if (
                            Client.Game.UO.GameCursor.ItemHold.Layer == Layer.Invalid
                            && SerialHelper.IsValid(Client.Game.UO.GameCursor.ItemHold.Container)
                        )
                        {
                            // Server should send an UpdateContainedItem after this packet.
                            Console.WriteLine("=== DENY === ADD TO CONTAINER");

                            _world.AddItemToContainer(
                                Client.Game.UO.GameCursor.ItemHold.Serial,
                                Client.Game.UO.GameCursor.ItemHold.Graphic,
                                Client.Game.UO.GameCursor.ItemHold.TotalAmount,
                                Client.Game.UO.GameCursor.ItemHold.X,
                                Client.Game.UO.GameCursor.ItemHold.Y,
                                Client.Game.UO.GameCursor.ItemHold.Hue,
                                Client.Game.UO.GameCursor.ItemHold.Container
                            );

                            UIManager
                                .GetGump<ContainerGump>(Client.Game.UO.GameCursor.ItemHold.Container)
                                ?.RequestUpdateContents();
                        }
                        else
                        {
                            Item item = _world.GetOrCreateItem(
                                Client.Game.UO.GameCursor.ItemHold.Serial
                            );

                            item.Graphic = Client.Game.UO.GameCursor.ItemHold.Graphic;
                            item.Hue = Client.Game.UO.GameCursor.ItemHold.Hue;
                            item.Amount = Client.Game.UO.GameCursor.ItemHold.TotalAmount;
                            item.Flags = Client.Game.UO.GameCursor.ItemHold.Flags;
                            item.Layer = Client.Game.UO.GameCursor.ItemHold.Layer;
                            item.X = Client.Game.UO.GameCursor.ItemHold.X;
                            item.Y = Client.Game.UO.GameCursor.ItemHold.Y;
                            item.Z = Client.Game.UO.GameCursor.ItemHold.Z;
                            item.CheckGraphicChange();

                            Entity container = _world.Get(Client.Game.UO.GameCursor.ItemHold.Container);

                            if (container != null)
                            {
                                if (SerialHelper.IsMobile(container.Serial))
                                {
                                    Console.WriteLine("=== DENY === ADD TO PAPERDOLL");

                                    _world.RemoveItemFromContainer(item);
                                    container.PushToBack(item);
                                    item.Container = container.Serial;

                                    UIManager
                                        .GetGump<PaperDollGump>(item.Container)
                                        ?.RequestUpdateContents();
                                }
                                else
                                {
                                    Console.WriteLine("=== DENY === SOMETHING WRONG");

                                    _world.RemoveItem(item, true);
                                }
                            }
                            else
                            {
                                Console.WriteLine("=== DENY === ADD TO TERRAIN");

                                _world.RemoveItemFromContainer(item);

                                item.SetInWorldTile(item.X, item.Y, item.Z);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error(
                        $"Wrong data: serial = {Client.Game.UO.GameCursor.ItemHold.Serial:X8}  -  graphic = {Client.Game.UO.GameCursor.ItemHold.Graphic:X4}"
                    );
                }

                UIManager.GetGump<SplitMenuGump>(Client.Game.UO.GameCursor.ItemHold.Serial)?.Dispose();

                Client.Game.UO.GameCursor.ItemHold.Clear();
            }
            else
            {
                Log.Warn("There was a problem with ItemHold object. It was cleared before :|");
            }

            //var result = World.Items.Get(ItemHold.Serial);

            //if (result != null && !result.IsDestroyed)
            //    result.AllowedToDraw = true;

            if (e.Code < 5)
            {
                _world.MessageManager.HandleMessage(
                    null,
                    ServerErrorMessages.GetError(0x27, e.Code),
                    string.Empty,
                    0x03b2,
                    MessageType.System,
                    3,
                    TextType.SYSTEM
                );
            }
        }
    }
}
