// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Chat
{
    /// <summary>
    /// Tracks the local user's chat identity. Today the per-channel user
    /// roster is a placeholder; this seam exists so future work can plug in
    /// real user lists without touching the facade.
    /// </summary>
    internal interface IChatRoster
    {
        string Username { get; set; }
        bool IsUsernameAccepted { get; set; }

        void AddUser(ushort userType, string username);
        void RemoveUser(string username);
        void ClearAllPlayers();
    }
}
