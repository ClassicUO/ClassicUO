using System;

namespace ClassicUO.Game.Managers
{
    public class EventSink
    {
        public static event EventHandler<EventArgs> OnConnected;
        public static void InvokeOnConnected(object sender) => OnConnected?.Invoke(sender, EventArgs.Empty);

        public static event EventHandler<MessageEventArgs> MessageReceived;
        public static void InvokeMessageReceived(object sender, MessageEventArgs e) => MessageReceived?.Invoke(sender, e);

        public static event EventHandler<MessageEventArgs> RawMessageReceived;
        public static void InvokeRawMessageReceived(object sender, MessageEventArgs e) => RawMessageReceived?.Invoke(sender, e);

        public static event EventHandler<MessageEventArgs> LocalizedMessageReceived;
        public static void InvokeLocalizedMessageReceived(object sender, MessageEventArgs e) => LocalizedMessageReceived?.Invoke(sender, e);

        public static event EventHandler<JournalEntry> JournalEntryAdded;
        public static void InvokeJournalEntryAdded(object sender, JournalEntry e) => JournalEntryAdded?.Invoke(sender, e);

        public static event EventHandler<OPLEventArgs> OPLOnReceive;
        public static void InvokeOPLOnReceive(object sender, OPLEventArgs e) => OPLOnReceive?.Invoke(sender, e);
    }

    public class OPLEventArgs : EventArgs
    {
        public readonly uint Serial;
        public readonly string Name;
        public readonly string Data;

        public OPLEventArgs(uint serial, string name, string data)
        {
            Serial = serial;
            Name = name;
            Data = data;
        }
    }
}
