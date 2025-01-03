#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Managers
{
    [Flags]
    internal enum NameOverheadTypeAllowed
    {
        All,
        Mobiles,
        Items,
        Corpses,
        MobilesCorpses = Mobiles | Corpses
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

        public bool IsAllowed(Entity serial)
        {
            if (serial == null)
            {
                return false;
            }

            if (TypeAllowed == NameOverheadTypeAllowed.All)
            {
                return true;
            }

            if (SerialHelper.IsItem(serial.Serial) && TypeAllowed == NameOverheadTypeAllowed.Items)
            {
                return true;
            }

            if (SerialHelper.IsMobile(serial.Serial) && TypeAllowed.HasFlag(NameOverheadTypeAllowed.Mobiles))
            {
                return true;
            }

            if (TypeAllowed.HasFlag(NameOverheadTypeAllowed.Corpses) && SerialHelper.IsItem(serial.Serial) && _world.Items.Get(serial)?.IsCorpse == true)
            {
                return true;
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

        public void Close()
        {
            if (_gump == null)
                return;

            _gump.IsEnabled = false;
            _gump.IsVisible = false;
        }

        public void ToggleOverheads()
        {
            IsToggled = !IsToggled;
        }
    }
}