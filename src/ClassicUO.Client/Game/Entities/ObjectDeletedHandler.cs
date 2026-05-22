// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Entities
{
    /// <summary>
    /// Handles <see cref="EventSink.ObjectDeleted"/> by removing the entity
    /// (item or mobile) from the world and cleaning up the associated gumps
    /// (container, paperdoll, trade, grid loot, bulletin board, minimap).
    /// Replaces the legacy <c>OnObjectDeleted</c> handler from
    /// <c>World.Subscribers</c>.
    /// </summary>
    internal sealed class ObjectDeletedHandler : IEventListener
    {
        private readonly World _world;

        public ObjectDeletedHandler(World world) => _world = world;

        public void Subscribe() => EventSink.ObjectDeleted += OnObjectDeleted;

        public void Unsubscribe() => EventSink.ObjectDeleted -= OnObjectDeleted;

        private void OnObjectDeleted(ObjectDeletedArgs e)
        {
            if (_world.Player == null || _world.Player.Serial == e.Serial) return;

            uint serial = e.Serial;
            Entity entity = _world.Get(serial);
            if (entity == null) return;

            bool updateAbilities = false;

            if (entity is Item it)
            {
                uint cont = it.Container & 0x7FFFFFFF;

                if (SerialHelper.IsValid(it.Container))
                {
                    Entity top = _world.Get(it.RootContainer);

                    if (top != null && top == _world.Player)
                    {
                        updateAbilities = it.Layer == Layer.OneHanded || it.Layer == Layer.TwoHanded;
                        Item tradeBoxItem = _world.Player.GetSecureTradeBox();
                        if (tradeBoxItem != null)
                        {
                            UIManager.GetTradingGump(tradeBoxItem)?.RequestUpdateContents();
                        }
                    }

                    if (cont == _world.Player.Serial && it.Layer == Layer.Invalid)
                    {
                        Client.Game.UO.GameCursor.ItemHold.Enabled = false;
                    }

                    if (it.Layer != Layer.Invalid)
                    {
                        UIManager.GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                    }

                    UIManager.GetGump<ContainerGump>(cont)?.RequestUpdateContents();

                    var profile = ProfileManager.CurrentProfile;
                    if (top != null && top.Graphic == 0x2006 && profile != null
                        && (profile.GridLootType == 1 || profile.GridLootType == 2))
                    {
                        UIManager.GetGump<GridLootGump>(cont)?.RequestUpdateContents();
                    }

                    if (it.Graphic == 0x0EB0)
                    {
                        UIManager.GetGump<BulletinBoardItem>(serial)?.Dispose();
                        UIManager.GetGump<BulletinBoardGump>()?.RemoveBulletinObject(serial);
                    }
                }
            }

            if (_world.CorpseManager.Exists(0, serial)) return;

            if (entity is Mobile)
            {
                _world.RemoveMobile(serial, true);
            }
            else
            {
                Item item = (Item)entity;

                if (item.IsMulti)
                {
                    _world.HouseManager.Remove(serial);
                }

                Entity cont = _world.Get(item.Container);

                if (cont != null)
                {
                    cont.Remove(item);

                    if (item.Layer != Layer.Invalid)
                    {
                        UIManager.GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                    }
                }
                else if (item.IsMulti)
                {
                    UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();
                }

                _world.RemoveItem(serial, true);

                if (updateAbilities)
                {
                    _world.Player.UpdateAbilities();
                }
            }
        }
    }
}
