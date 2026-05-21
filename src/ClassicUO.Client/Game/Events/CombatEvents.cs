// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events
{
    internal readonly record struct DamageReceivedArgs(uint Serial, ushort Damage);

    internal readonly record struct WarModeChangedArgs(uint Serial, bool InWarMode);

    internal readonly record struct PlayerDeathArgs(byte Action);
}
