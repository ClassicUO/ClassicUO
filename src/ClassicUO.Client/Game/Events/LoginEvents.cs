// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events
{
    internal readonly record struct LoginCompletedArgs;

    internal readonly record struct LoginRejectedArgs(byte Reason);
}
