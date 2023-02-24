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

using System.Xml;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class UseAbilityButtonGump : AnchorableGump
    {
        private GumpPic _button;

        public UseAbilityButtonGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
        }

        public UseAbilityButtonGump(bool primary) : this()
        {
            IsPrimary = primary;
            BuildGump();
        }

        public override GumpType GumpType => GumpType.AbilityButton;

        public int Index { get; private set; }
        public bool IsPrimary { get; private set; }

        private void BuildGump()
        {
            Clear();
            Index = ((byte)World.Player.Abilities[IsPrimary ? 0 : 1] & 0x7F);

            ref readonly AbilityDefinition def = ref AbilityData.Abilities[Index - 1];

            _button = new GumpPic(0, 0, def.Icon, 0)
            {
                AcceptMouseInput = false
            };

            Add(_button);

            SetTooltip(ClilocLoader.Instance.GetString(1028838 + (Index - 1)), 80);

            WantUpdateSize = true;
            AcceptMouseInput = true;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
            AnchorType = ANCHOR_TYPE.SPELL;
        }


        protected override void UpdateContents()
        {
            BuildGump();
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (IsPrimary)
                {
                    GameActions.UsePrimaryAbility();
                }
                else
                {
                    GameActions.UseSecondaryAbility();
                }

                return true;
            }

            return false;
        }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            byte index = (byte) World.Player.Abilities[IsPrimary ? 0 : 1];

            if ((index & 0x80) != 0)
            {
                _button.Hue = 38;
            }
            else if (_button.Hue != 0)
            {
                _button.Hue = 0;
            }


            return base.Draw(batcher, x, y);
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("isprimary", IsPrimary.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            IsPrimary = bool.Parse(xml.GetAttribute("isprimary"));
            BuildGump();
        }
    }
}