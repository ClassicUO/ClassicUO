using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    internal interface IChatManager
    {
        ChatStatus ChatIsEnabled { get; set; }
        string CurrentChannelName { get; set; }
        Dictionary<string, ChatChannel> Channels { get; }

        void AddChannel(string text, bool hasPassword);
        void RemoveChannel(string name);
        void Clear();
    }
}
