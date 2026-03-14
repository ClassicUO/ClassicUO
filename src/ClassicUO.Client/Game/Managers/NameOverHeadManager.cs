// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Managers
{
    [Flags]
    internal enum NameOverheadTypeAllowed
    {
        None = 0,
        Items = 1 << 0,
        Corpses = 1 << 1,
        Innocent = 1 << 2,
        Ally = 1 << 3,
        Gray = 1 << 4,
        Criminal = 1 << 5,
        Enemy = 1 << 6,
        Murderer = 1 << 7,
        Invulnerable = 1 << 8,
        AllMobiles = Innocent | Ally | Gray | Criminal | Enemy | Murderer | Invulnerable,
        All = Items | Corpses | AllMobiles
    }

    internal sealed class NameOverHeadManager
    {
        private NameOverHeadHandlerGump _gump;
        private readonly World _world;

        public NameOverHeadManager(World world) { _world = world; }

        public NameOverheadTypeAllowed TypeAllowed
        {
            get => ProfileManager.CurrentProfile.NameOverheadTypeAllowed;
            set => ProfileManager.CurrentProfile.NameOverheadTypeAllowed = value;
        }

        public bool IsToggled
        {
            get => ProfileManager.CurrentProfile.NameOverheadToggled;
            set => ProfileManager.CurrentProfile.NameOverheadToggled = value;
        }

        public bool IsAllowed(Entity entity)
        {
            if (entity == null)
            {
                return false;
            }

            if (SerialHelper.IsItem(entity.Serial))
            {
                if (entity is Item item && item.IsCorpse)
                    return TypeAllowed.HasFlag(NameOverheadTypeAllowed.Corpses);

                return TypeAllowed.HasFlag(NameOverheadTypeAllowed.Items);
            }

            if (SerialHelper.IsMobile(entity.Serial) && entity is Mobile mobile)
            {
                return mobile.NotorietyFlag switch
                {
                    NotorietyFlag.Innocent => TypeAllowed.HasFlag(NameOverheadTypeAllowed.Innocent),
                    NotorietyFlag.Ally => TypeAllowed.HasFlag(NameOverheadTypeAllowed.Ally),
                    NotorietyFlag.Gray => TypeAllowed.HasFlag(NameOverheadTypeAllowed.Gray),
                    NotorietyFlag.Criminal => TypeAllowed.HasFlag(NameOverheadTypeAllowed.Criminal),
                    NotorietyFlag.Enemy => TypeAllowed.HasFlag(NameOverheadTypeAllowed.Enemy),
                    NotorietyFlag.Murderer => TypeAllowed.HasFlag(NameOverheadTypeAllowed.Murderer),
                    NotorietyFlag.Invulnerable => TypeAllowed.HasFlag(NameOverheadTypeAllowed.Invulnerable),
                    _ => true
                };
            }

            return false;
        }

        public void Open()
        {
            if (_gump == null || _gump.IsDisposed)
            {
                _gump = new NameOverHeadHandlerGump(_world);
                UIManager.Add(_gump);
            }

            _gump.IsEnabled = true;
            _gump.IsVisible = true;
        }

        public void SetMenuVisible(bool visible)
        {
            if (_gump != null && !_gump.IsDisposed)
            {
                _gump.IsVisible = visible;
            }
        }

        public void Close()
        {
            if (_gump == null)
            { //Required in case nameplates are active when closing and reopening the client
                _gump = new NameOverHeadHandlerGump(_world);
                UIManager.Add(_gump);
            }


            _gump.IsEnabled = false;
            _gump.IsVisible = false;
        }

        public void ToggleOverheads()
        {
            IsToggled = !IsToggled;
        }
    }
}
