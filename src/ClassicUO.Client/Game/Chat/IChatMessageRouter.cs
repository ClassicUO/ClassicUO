// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Chat
{
    /// <summary>
    /// Formats incoming chat text + system messages and prints them via the
    /// world's message pipeline. No state of its own.
    /// </summary>
    internal interface IChatMessageRouter
    {
        void RouteText(string username, string message);
        void RouteSystemMessage(ushort cmd, string text);
        void AnnounceConferenceJoined(string channelName);
        void AnnounceConferenceLeft(string channelName);
    }
}
