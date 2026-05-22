// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0xBF/0x12 CastSpell sent to server.</summary>
    internal readonly record struct CastSpellSentArgs(int Index);

    /// <summary>0x12 CastSpellFromBook sent to server.</summary>
    internal readonly record struct CastSpellFromBookSentArgs(int Index, uint Serial);

    /// <summary>0x12 OpenSpellBook sent to server.</summary>
    internal readonly record struct OpenSpellBookSentArgs(byte Type);

    /// <summary>0xBF StunRequest sent to server.</summary>
    internal readonly record struct StunRequestSentArgs();

    /// <summary>0xBF DisarmRequest sent to server.</summary>
    internal readonly record struct DisarmRequestSentArgs();

    /// <summary>0xBF ToggleGargoyleFlying sent to server.</summary>
    internal readonly record struct ToggleGargoyleFlyingSentArgs();

    /// <summary>0x12 InvokeVirtueRequest sent to server.</summary>
    internal readonly record struct InvokeVirtueRequestSentArgs(byte Id);

    /// <summary>0xBF ChangeRaceRequest sent to server.</summary>
    internal readonly record struct ChangeRaceRequestSentArgs(
        ushort SkinHue,
        ushort HairStyle,
        ushort HairHue,
        ushort BeardStyle,
        ushort BeardHue
    );
}
