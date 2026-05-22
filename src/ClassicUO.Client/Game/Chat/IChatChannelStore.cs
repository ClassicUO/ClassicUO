// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Chat
{
    /// <summary>
    /// Owns the chat channel collection plus current-channel state.
    /// Single responsibility: add / remove / lookup channels and track which
    /// one the player is currently in.
    /// </summary>
    internal interface IChatChannelStore
    {
        Dictionary<string, ChatChannel> Channels { get; }
        string CurrentChannelName { get; set; }
        ChatStatus ChatIsEnabled { get; set; }

        void AddChannel(string text, bool hasPassword);
        void RemoveChannel(string name);
        void Clear();
    }
}
