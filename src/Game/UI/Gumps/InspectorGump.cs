﻿#region license
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
using System.IO;
using System.Linq;
using System.Text;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    internal class InspectorGump : Gump
    {
        private const int WIDTH = 500;
        private const int HEIGHT = 400;
        private readonly ScrollArea _scrollArea;
        private GameObject _obj;

        public InspectorGump(GameObject obj) : base(0, 0)
        {
            X = 200;
            Y = 100;
            _obj = obj;
            CanMove = true;
            AcceptMouseInput = false;
            CanCloseWithRightClick = true;
            Add(new BorderControl(0, 0, WIDTH, HEIGHT, 4));

            Add(new GumpPicTiled(4, 4, WIDTH - 8, HEIGHT - 8, 0x0A40)
            {
                Alpha = 0.5f
            });

            Add(new GumpPicTiled(4, 4, WIDTH - 8, HEIGHT - 8, 0x0A40)
            {
                Alpha = 0.5f
            });
            Add(new Label("Object Information", true, 1153, font: 3) {X = 20, Y = 10});
            Add(new Line(20, 30, WIDTH - 50, 1, 0xFFFFFFFF));
            Add(new NiceButton(WIDTH - 115, 5, 100, 25, ButtonAction.Activate, "Dump")
            {
                ButtonParameter = 0
            });

            _scrollArea = new ScrollArea(20, 35, WIDTH - 40, HEIGHT - 45, true)
            {
                AcceptMouseInput = true
            };
            Add(_scrollArea);

            Dictionary<string, string> dict = ReflectionHolder.GetGameObjectProperties(obj);

            if (dict != null)
            {
                foreach (KeyValuePair<string, string> item in dict.OrderBy(s => s.Key))
                {
                    ScrollAreaItem areaItem = new ScrollAreaItem();

                    Label label = new Label(item.Key + ":", true, 33, font: 1, style: FontStyle.BlackBorder)
                    {
                        X = 2
                    };
                    areaItem.Add(label);

                    int height = label.Height;

                    label = new Label(item.Value, true, 1153, font: 1, style: FontStyle.BlackBorder, maxwidth: WIDTH - 65 - 200)
                    {
                        X = 200,
                        AcceptMouseInput = true
                    };
                    label.MouseUp += OnLabelClick;

                    if (label.Height > 0)
                        height = label.Height;

                    areaItem.Add(label);
                    areaItem.Add(new Line(0, height + 2, WIDTH - 65, 1, Color.Gray.PackedValue));

                    _scrollArea.Add(areaItem);
                }
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0) // dump
            {
                Dictionary<string, string> dict = ReflectionHolder.GetGameObjectProperties(_obj);

                if (dict != null)
                {
                    using (LogFile writer = new LogFile(CUOEnviroment.ExecutablePath, "dump_gameobject.txt"))
                    {
                        writer.Write("###################################################");
                        writer.Write($"CUO version: {CUOEnviroment.Version}");
                        writer.Write($"OBJECT TYPE: {_obj.GetType()}");
                        foreach (KeyValuePair<string, string> item in dict.OrderBy(s => s.Key))
                        {
                            writer.Write($"{item.Key} = {item.Value}");
                        }
                        writer.Write("###################################################");
                        writer.Write("");
                    }

                }
            }
        }

        private static void OnLabelClick(object sender, EventArgs e)
        {
            Label l = (Label) sender;

            if (l != null)
            {
                SDL.SDL_SetClipboardText(l.Text);
            }
        }
    }
}