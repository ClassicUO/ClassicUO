#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using ClassicUO.Utility;

namespace ClassicUO.Game.Data
{
    [Flags]
    internal enum CharacterListFlags
    {
        CLF_UNKNOWN = 0x01,
        CLF_OWERWRITE_CONFIGURATION_BUTTON = 0x02,
        CLF_ONE_CHARACTER_SLOT = 0x04,
        CLF_CONTEXT_MENU = 0x08,
        CLF_LIMIT_CHARACTER_SLOTS = 0x10,
        CLF_PALADIN_NECROMANCER_TOOLTIPS = 0x20,
        CLF_6_CHARACTER_SLOT = 0x40,
        CLF_SAMURAI_NINJA = 0x80,
        CLF_ELVEN_RACE = 0x100,
        CLF_UNKNOWN_1 = 0x200,
        CLF_UO3D = 0x400,
        CLF_UNKNOWN_2 = 0x800,
        CLF_7_CHARACTER_SLOT = 0x1000,
        CLF_UNKNOWN_3 = 0x2000,
        CLF_NEW_MOVEMENT_SYSTEM = 0x4000,
        CLF_UNLOCK_FELUCCA_AREAS = 0x8000
    }

    internal class ClientFeatures
    {
        public CharacterListFlags Flags { get; private set; }

        public bool TooltipsEnabled { get; private set; } = true;
        public bool PopupEnabled { get; private set; }
        public bool PaperdollBooks { get; private set; }
        public uint MaxChars { get; private set; } = 5;

        public void SetFlags(CharacterListFlags flags)
        {
            Flags = flags;

            if ((flags & CharacterListFlags.CLF_ONE_CHARACTER_SLOT) != 0)
            {
                MaxChars = 1;
            }
            else if ((flags & CharacterListFlags.CLF_7_CHARACTER_SLOT) != 0)
            {
                MaxChars = 7;
            }
            else if ((flags & CharacterListFlags.CLF_6_CHARACTER_SLOT) != 0)
            {
                MaxChars = 6;
            }

            PopupEnabled = (flags & CharacterListFlags.CLF_CONTEXT_MENU) != 0;

            TooltipsEnabled = (flags & CharacterListFlags.CLF_PALADIN_NECROMANCER_TOOLTIPS) != 0 && Client.Version >= ClientVersion.CV_308Z;

            PaperdollBooks = (flags & CharacterListFlags.CLF_PALADIN_NECROMANCER_TOOLTIPS) != 0;
        }
    }
}