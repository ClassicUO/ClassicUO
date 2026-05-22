// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// Listens for <see cref="DyeDataReceivedArgs"/> and opens / refreshes a
    /// <see cref="ColorPickerGump"/> centered on the client viewport.
    /// </summary>
    internal sealed class DyeDataHandler : IEventListener
    {
        private readonly World _world;

        public DyeDataHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.DyeDataReceived += OnDyeDataReceived;
        }

        public void Unsubscribe()
        {
            EventSink.DyeDataReceived -= OnDyeDataReceived;
        }

        private void OnDyeDataReceived(DyeDataReceivedArgs e)
        {
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(0x0906);
            int x = (Client.Game.ClientBounds.Width >> 1) - (gumpInfo.UV.Width >> 1);
            int y = (Client.Game.ClientBounds.Height >> 1) - (gumpInfo.UV.Height >> 1);

            ColorPickerGump gump = UIManager.GetGump<ColorPickerGump>(e.Serial);
            if (gump == null || gump.IsDisposed || gump.Graphic != e.Graphic)
            {
                gump?.Dispose();
                UIManager.Add(new ColorPickerGump(_world, e.Serial, e.Graphic, x, y, null));
            }
        }
    }
}
