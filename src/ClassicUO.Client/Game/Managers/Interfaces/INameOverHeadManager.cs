// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    internal interface INameOverHeadManager
    {
        NameOverheadTypeAllowed TypeAllowed { get; set; }

        bool IsToggled { get; set; }

        bool IsAllowed(Entity entity);

        void Open();

        void SetMenuVisible(bool visible);

        void Close();

        void ToggleOverheads();
    }
}
