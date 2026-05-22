// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Entities.Players
{
    /// <summary>
    /// Handles <see cref="EventSink.BuffApplied"/> and
    /// <see cref="EventSink.BuffRemoved"/> by updating the player's buff list
    /// and refreshing the <see cref="BuffGump"/>.
    /// Replaces the legacy <c>OnBuffApplied</c> / <c>OnBuffRemoved</c> handlers
    /// from <c>World.Subscribers</c>.
    /// </summary>
    internal sealed class BuffHandler : IEventListener
    {
        private readonly World _world;

        public BuffHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.BuffApplied += OnBuffApplied;
            EventSink.BuffRemoved += OnBuffRemoved;
        }

        public void Unsubscribe()
        {
            EventSink.BuffApplied -= OnBuffApplied;
            EventSink.BuffRemoved -= OnBuffRemoved;
        }

        private void OnBuffApplied(BuffAppliedArgs e)
        {
            if (_world.Player == null) return;
            if (e.IconId >= BuffTable.Table.Length) return;

            bool alreadyExists = _world.Player.IsBuffIconExists((BuffIconType)e.IconType);
            _world.Player.AddBuff((BuffIconType)e.IconType, BuffTable.Table[e.IconId], e.Timer, e.Text);

            if (!alreadyExists)
            {
                UIManager.GetGump<BuffGump>()?.RequestUpdateContents();
            }
        }

        private void OnBuffRemoved(BuffRemovedArgs e)
        {
            if (_world.Player == null) return;

            _world.Player.RemoveBuff((BuffIconType)e.IconType);
            UIManager.GetGump<BuffGump>()?.RequestUpdateContents();
        }
    }
}
