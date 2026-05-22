// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// Listens for <see cref="TipWindowDisplayedArgs"/> and spawns a
    /// <see cref="TipNoticeGump"/> at a flag-dependent offset.
    /// </summary>
    internal sealed class TipWindowHandler : IEventListener
    {
        private readonly World _world;

        public TipWindowHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.TipWindowDisplayed += OnTipWindowDisplayed;
        }

        public void Unsubscribe()
        {
            EventSink.TipWindowDisplayed -= OnTipWindowDisplayed;
        }

        private void OnTipWindowDisplayed(TipWindowDisplayedArgs e)
        {
            int x = e.Flag == 0 ? 200 : 20;
            int y = e.Flag == 0 ? 100 : 20;
            UIManager.Add(new TipNoticeGump(_world, e.TipId, e.Flag, e.Text) { X = x, Y = y });
        }
    }
}
