#region license

// Copyright (c) 2024, andreakarasho
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
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    internal class RacialAbilityButton : Gump
    {
        public RacialAbilityButton(World world, ushort graphic) : this(world)
        {
            LocalSerial = (uint) (7000 + graphic);

            UIManager.GetGump<RacialAbilityButton>(LocalSerial)?.Dispose();

            Graphic = graphic;
            BuildGump();
        }

        public RacialAbilityButton(World world) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
        }

        public override GumpType GumpType => GumpType.RacialButton;
        public ushort Graphic;


        private void BuildGump()
        {
            GumpPic pic = new GumpPic(0, 0, Graphic, 0);
            Add(pic);
            pic.SetTooltip(ClilocLoader.Instance.GetString(1112198 + (Graphic - 0x5DD0)), 200);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (Graphic == 0x5DDA && World.Player.Race == RaceType.GARGOYLE)
            {
                NetClient.Socket.Send_ToggleGargoyleFlying();

                return true;
            }

            return base.OnMouseDoubleClick(x, y, button);
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("graphic", Graphic.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            Graphic = ushort.Parse(xml.GetAttribute("graphic"));
            BuildGump();
        }
    }
}