// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private readonly System.Collections.Generic.List<IEventListener> _listeners = new();

        /// <summary>
        /// Register an <see cref="IEventListener"/> so its
        /// <c>Subscribe()</c> / <c>Unsubscribe()</c> are driven centrally
        /// from <see cref="SubscribeEvents"/> / <see cref="UnsubscribeEvents"/>.
        /// Returns the listener for fluent assignment.
        /// </summary>
        internal T RegisterListener<T>(T listener) where T : IEventListener
        {
            _listeners.Add(listener);
            return listener;
        }

        private void SubscribeEvents()
        {
            EventSink.TipWindowDisplayed += OnTipWindowDisplayed;
            EventSink.OpenUrlRequested += OnOpenUrlRequested;
            EventSink.VendorWindowClosed += OnVendorWindowClosed;
            EventSink.CharacterProfileOpened += OnCharacterProfileOpened;
            EventSink.TextEntryDialogOpened += OnTextEntryDialogOpened;
            EventSink.DyeDataReceived += OnDyeDataReceived;

            foreach (var listener in _listeners) listener.Subscribe();
        }

        public void UnsubscribeEvents()
        {
            foreach (var listener in _listeners) listener.Unsubscribe();

            EventSink.TipWindowDisplayed -= OnTipWindowDisplayed;
            EventSink.OpenUrlRequested -= OnOpenUrlRequested;
            EventSink.VendorWindowClosed -= OnVendorWindowClosed;
            EventSink.CharacterProfileOpened -= OnCharacterProfileOpened;
            EventSink.TextEntryDialogOpened -= OnTextEntryDialogOpened;
            EventSink.DyeDataReceived -= OnDyeDataReceived;
        }

        private void OnTipWindowDisplayed(TipWindowDisplayedArgs e)
        {
            int x = e.Flag == 0 ? 200 : 20;
            int y = e.Flag == 0 ? 100 : 20;
            UIManager.Add(new TipNoticeGump(this, e.TipId, e.Flag, e.Text) { X = x, Y = y });
        }

        private void OnOpenUrlRequested(OpenUrlRequestedArgs e)
        {
            if (!string.IsNullOrEmpty(e.Url))
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser(e.Url);
            }
        }

        private void OnVendorWindowClosed(VendorWindowClosedArgs e)
        {
            UIManager.GetGump<ShopGump>(e.VendorSerial)?.Dispose();
        }

        private void OnCharacterProfileOpened(CharacterProfileOpenedArgs e)
        {
            if (Player == null) return;

            UIManager.GetGump<ProfileGump>(e.Serial)?.Dispose();
            UIManager.Add(new ProfileGump(this, e.Serial, e.Header, e.Footer, e.Body, e.Serial == Player.Serial));
        }

        private void OnTextEntryDialogOpened(TextEntryDialogArgs e)
        {
            UIManager.Add(new TextEntryDialogGump(
                this,
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

        private void OnDyeDataReceived(DyeDataReceivedArgs e)
        {
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(0x0906);
            int x = (Client.Game.ClientBounds.Width >> 1) - (gumpInfo.UV.Width >> 1);
            int y = (Client.Game.ClientBounds.Height >> 1) - (gumpInfo.UV.Height >> 1);

            ColorPickerGump gump = UIManager.GetGump<ColorPickerGump>(e.Serial);
            if (gump == null || gump.IsDisposed || gump.Graphic != e.Graphic)
            {
                gump?.Dispose();
                UIManager.Add(new ColorPickerGump(this, e.Serial, e.Graphic, x, y, null));
            }
        }
    }
}
