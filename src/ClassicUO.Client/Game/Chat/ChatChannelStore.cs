// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Chat
{
    /// <summary>
    /// In-memory channel dictionary plus current-channel state. No events,
    /// no UI, no network.
    /// </summary>
    internal sealed class ChatChannelStore : IChatChannelStore
    {
        public Dictionary<string, ChatChannel> Channels { get; } = new Dictionary<string, ChatChannel>();
        public string CurrentChannelName { get; set; } = string.Empty;
        public ChatStatus ChatIsEnabled { get; set; }

        public void AddChannel(string text, bool hasPassword)
        {
            if (!Channels.TryGetValue(text, out ChatChannel channel))
            {
                channel = new ChatChannel(text, hasPassword);
                Channels[text] = channel;
            }
        }

        public void RemoveChannel(string name)
        {
            if (Channels.ContainsKey(name))
            {
                Channels.Remove(name);
            }
        }

        public void Clear()
        {
            Channels.Clear();
        }
    }
}
