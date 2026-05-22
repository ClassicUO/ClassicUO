// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// Listens for <see cref="TextEntryDialogArgs"/> and spawns the
    /// corresponding <see cref="TextEntryDialogGump"/>.
    /// </summary>
    internal sealed class TextEntryDialogHandler : IEventListener
    {
        private readonly World _world;

        public TextEntryDialogHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.TextEntryDialogOpened += OnTextEntryDialogOpened;
        }

        public void Unsubscribe()
        {
            EventSink.TextEntryDialogOpened -= OnTextEntryDialogOpened;
        }

        private void OnTextEntryDialogOpened(TextEntryDialogArgs e)
        {
            UIManager.Add(new TextEntryDialogGump(
                _world,
                e.Serial,
                143,
                172,
                0,
                (int)e.MaxLength,
                e.Text,
                e.Description,
                e.ButtonId,
                e.ParentId
            )
            {
                CanCloseWithRightClick = true
            });
        }
    }
}
