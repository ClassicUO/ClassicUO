#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    class RacialAbilityButton : Gump
    {
        public RacialAbilityButton(ushort graphic) : this()
        {
            Graphic = graphic;
            BuildGump();
        }

        public RacialAbilityButton() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
        }

        public ushort Graphic;

        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_RACIALBUTTON;


        private void BuildGump()
        {
            var pic = new GumpPic(0, 0, Graphic, 0);
            Add(pic);
            pic.SetTooltip(ClilocLoader.Instance.GetString(1112198 + (Graphic - 0x5DD0)), 200);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (Graphic == 0x5DDA && World.Player.Race == RaceType.GARGOYLE)
            {
                NetClient.Socket.Send(new PToggleGargoyleFlying());

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
