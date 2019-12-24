using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    sealed class UOChatChannel
    {
        public UOChatChannel(string name, bool haspassword)
        {
            Name = name;
            HasPassword = haspassword;
        }

        public readonly string Name;
        public readonly bool HasPassword;
    }

    static class UOChatManager
    {
        public static readonly Dictionary<string, UOChatChannel> Channels = new Dictionary<string, UOChatChannel>();
        public static bool ChatIsEnabled;
        public static string CurrentChannelName = string.Empty;

        public static void AddChannel(string text, bool haspassword)
        {
            if (!Channels.TryGetValue(text, out var channel))
            {
                channel = new UOChatChannel(text, haspassword);
                Channels[text] = channel;
            }
        }

        public static void RemoveChannel(string name)
        {
            if (Channels.ContainsKey(name))
            {
                Channels.Remove(name);
            }
        }

        public static void Clear()
        {
            Channels.Clear();
        }
    }
}
