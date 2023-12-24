using System;

namespace ClassicUO.Game.Managers
{
    public class EventSink
    {
        public static event EventHandler<EventArgs> OnConnected;
        public static void InvokeOnConnected(object sender) => OnConnected?.Invoke(sender, EventArgs.Empty);

    }
}
