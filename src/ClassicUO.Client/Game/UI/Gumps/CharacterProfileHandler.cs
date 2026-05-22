// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// Listens for <see cref="CharacterProfileOpenedArgs"/> and (re)builds a
    /// <see cref="ProfileGump"/> with the server-provided header / body / footer.
    /// </summary>
    internal sealed class CharacterProfileHandler : IEventListener
    {
        private readonly World _world;

        public CharacterProfileHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.CharacterProfileOpened += OnCharacterProfileOpened;
        }

        public void Unsubscribe()
        {
            EventSink.CharacterProfileOpened -= OnCharacterProfileOpened;
        }

        private void OnCharacterProfileOpened(CharacterProfileOpenedArgs e)
        {
            if (_world.Player == null) return;

            UIManager.GetGump<ProfileGump>(e.Serial)?.Dispose();
            UIManager.Add(new ProfileGump(_world, e.Serial, e.Header, e.Footer, e.Body, e.Serial == _world.Player.Serial));
        }
    }
}
