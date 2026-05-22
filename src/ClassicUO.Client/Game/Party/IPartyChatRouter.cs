// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game.Party
{
    /// <summary>
    /// Routes <see cref="PartyChatMessageArgs"/> through
    /// <see cref="Managers.MessageManager"/> when the speaker is a known
    /// roster member.
    /// </summary>
    internal interface IPartyChatRouter
    {
        void HandlePartyChatMessage(PartyChatMessageArgs e);
    }
}
