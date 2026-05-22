// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Utility.Platforms;

namespace ClassicUO.Game.Input
{
    /// <summary>
    /// Listens for <see cref="OpenUrlRequestedArgs"/> and forwards the URL to
    /// the platform browser launcher.
    /// </summary>
    internal sealed class OpenUrlHandler : IEventListener
    {
        private readonly World _world;

        public OpenUrlHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.OpenUrlRequested += OnOpenUrlRequested;
        }

        public void Unsubscribe()
        {
            EventSink.OpenUrlRequested -= OnOpenUrlRequested;
        }

        private void OnOpenUrlRequested(OpenUrlRequestedArgs e)
        {
            if (!string.IsNullOrEmpty(e.Url))
            {
                PlatformHelper.LaunchBrowser(e.Url);
            }
        }
    }
}
