#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class ContainerGump : Gump
    {
        private readonly ContainerData _data;
        private readonly Item _item;

        public ContainerGump(Item item, Graphic gumpid) : base(item.Serial, 0)
        {
            _item = item;
            _data = ContainerManager.Get(gumpid);

            CanMove = true;

            AddChildren(new GumpPicContainer(0, 0, _data.Graphic, 0, item));
        }


        protected override void OnInitialize()
        {
            foreach (Item item in _item.Items)
                AddChildren(new ItemGumpling(item));

            _item.SetCallbacks(OnItemUpdated, OnItemDisposed);
        }


        public override void Dispose()
        {
            _item.ClearCallBacks(OnItemUpdated, OnItemDisposed);

            base.Dispose();
        }

        private void OnItemUpdated(GameObject obj)
        {
            List<GumpControl> toremove = Children.Where(s => s is ItemGumpling ctrl && !_item.Items.Contains(ctrl.Item))
                .ToList();

            if (toremove.Count > 0)
                toremove.ForEach(RemoveChildren);

            foreach (Item item in _item.Items)
            {
                bool control = false;

                foreach (GumpControl child in Children)
                {
                    if (child is ItemGumpling ctrl && ctrl.Item == item)
                    {
                        control = true;
                        break;
                    }
                }

                if (!control)
                    AddChildren(new ItemGumpling(item));

            }
        }

        private void OnItemDisposed(GameObject obj)
        {
            Dispose();
        }
    }
}
