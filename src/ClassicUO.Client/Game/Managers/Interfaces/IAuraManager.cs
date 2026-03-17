// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Managers
{
    internal interface IAuraManager
    {
        bool IsEnabled { get; }

        void ToggleVisibility();
    }
}
