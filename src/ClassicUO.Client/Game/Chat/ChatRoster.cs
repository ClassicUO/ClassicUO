// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Chat
{
    /// <summary>
    /// Holds the local user's chat username + accepted flag. The per-channel
    /// user list hooks are present but no-op today: the original
    /// <c>ChatManager</c> handlers were also empty placeholders so we preserve
    /// that behaviour while giving future work a real seam.
    /// </summary>
    internal sealed class ChatRoster : IChatRoster
    {
        public string Username { get; set; } = string.Empty;
        public bool IsUsernameAccepted { get; set; }

        public void AddUser(ushort userType, string username)
        {
            // currently nothing tracked; placeholder for future per-channel user list
        }

        public void RemoveUser(string username)
        {
            // currently nothing tracked; placeholder for future per-channel user list
        }

        public void ClearAllPlayers()
        {
            // currently nothing tracked; placeholder for future per-channel user list
        }
    }
}
