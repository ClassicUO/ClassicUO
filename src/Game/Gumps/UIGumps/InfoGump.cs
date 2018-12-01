﻿#region license
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Renderer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class InfoGump : Gump
    {
        private const int WIDTH = 300;
        private const int HEIGHT = 600;
        private readonly ScrollArea _scrollArea;
        public InfoGump(object obj) : base(0, 0)
        {
            X = 200;
            Y = 200;
            CanMove = true;
            AcceptMouseInput = false;
            AddChildren(new GameBorder(0, 0, WIDTH, HEIGHT, 4));

            AddChildren(new GumpPicTiled(4, 4, WIDTH - 8, HEIGHT - 8, 0x0A40)
            {
                IsTransparent = true
            });

            AddChildren(new GumpPicTiled(4, 4, WIDTH - 8, HEIGHT - 8, 0x0A40)
            {
                IsTransparent = true
            });
            AddChildren(new Label("Object Information", true, 1153, font: 3) { X = 20, Y = 20 });
            AddChildren(new Line(20, 50, 250, 1, 0xFFFFFFFF));
            _scrollArea = new ScrollArea(20, 60, WIDTH - 40, 510, true)
            {
                AcceptMouseInput = true
            };
            AddChildren(_scrollArea);

            foreach (var item in ReflectionHolder.GameObjectDictionary(obj))
            {
                if (item.Value.ToString() != "")
                {
                   
                    _scrollArea.AddChildren(new Label(item.Key + " : " + item.Value, true, 1153, font: 3, maxwidth: WIDTH - 65));
                    
                }


            }

        }
    }

    public class InfoGumpEntry : GumpControl
    {
        public readonly Label Entry;

        public InfoGumpEntry(Label entry)
        {
            Entry = entry;
            Entry.X = 20;
            AddChildren(Entry);

        }


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }

    }
}
