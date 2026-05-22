// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0xB8 ProfileRequest sent to server.</summary>
    internal readonly record struct ProfileRequestSentArgs(uint Serial);

    /// <summary>0xB8 ProfileUpdate sent to server.</summary>
    internal readonly record struct ProfileUpdateSentArgs(uint Serial, string Text);

    /// <summary>0xA7 TipRequest sent to server.</summary>
    internal readonly record struct TipRequestSentArgs(ushort Id, byte Flag);
}
