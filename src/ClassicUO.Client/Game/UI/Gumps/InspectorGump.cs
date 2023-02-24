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
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Resources;
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
        private readonly GameObject _obj;

        public InspectorGump(GameObject obj) : base(0, 0)
        {
            X = 200;
            Y = 100;
            _obj = obj;
            CanMove = true;
            AcceptMouseInput = false;
            CanCloseWithRightClick = true;

            Add
            (
                new BorderControl
                (
                    0,
                    0,
                    WIDTH,
                    HEIGHT,
                    4
                )
            );

            Add
            (
                new GumpPicTiled
                (
                    4,
                    4,
                    WIDTH - 8,
                    HEIGHT - 8,
                    0x0A40
                )
                {
                    Alpha = 0.5f
                }
            );

            Add
            (
                new GumpPicTiled
                (
                    4,
                    4,
                    WIDTH - 8,
                    HEIGHT - 8,
                    0x0A40
                )
                {
                    Alpha = 0.5f
                }
            );

            Add(new Label(ResGumps.ObjectInformation, true, 1153, font: 3) { X = 20, Y = 10 });

            Add
            (
                new Line
                (
                    20,
                    30,
                    WIDTH - 50,
                    1,
                    0xFFFFFFFF
                )
            );

            Add
            (
                new NiceButton
                (
                    WIDTH - 115,
                    5,
                    100,
                    25,
                    ButtonAction.Activate,
                    ResGumps.Dump
                )
                {
                    ButtonParameter = 0
                }
            );

            ScrollArea scrollArea = new ScrollArea
            (
                20,
                35,
                WIDTH - 40,
                HEIGHT - 45,
                true
            )
            {
                AcceptMouseInput = true
            };

            Add(scrollArea);

            DataBox databox = new DataBox(0, 0, 1, 1);
            databox.WantUpdateSize = true;
            scrollArea.Add(databox);

            Dictionary<string, string> dict = GetGameObjectProperties(obj);

            if (dict != null)
            {
                int startX = 5;
                int startY = 5;

                foreach (KeyValuePair<string, string> item in dict.OrderBy(s => s.Key))
                {
                    Label label = new Label
                    (
                        item.Key + ":",
                        true,
                        33,
                        font: 1,
                        style: FontStyle.BlackBorder
                    )
                    {
                        X = startX,
                        Y = startY
                    };

                    databox.Add(label);

                    int height = label.Height;

                    label = new Label
                    (
                        item.Value,
                        true,
                        1153,
                        font: 1,
                        style: FontStyle.BlackBorder,
                        maxwidth: WIDTH - 65 - 200
                    )
                    {
                        X = startX + 200,
                        Y = startY,
                        AcceptMouseInput = true
                    };

                    label.MouseUp += OnLabelClick;

                    if (label.Height > 0)
                    {
                        height = label.Height;
                    }

                    databox.Add(label);

                    databox.Add
                    (
                        new Line
                        (
                            startX,
                            startY + height + 2,
                            WIDTH - 65,
                            1,
                            Color.Gray.PackedValue
                        )
                    );

                    startY += height + 4;
                }
            }

            //databox.ReArrangeChildren();
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0) // dump
            {
                Dictionary<string, string> dict = GetGameObjectProperties(_obj);

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

        private void OnLabelClick(object sender, EventArgs e)
        {
            Label l = (Label) sender;

            if (l != null)
            {
                SDL.SDL_SetClipboardText(l.Text);
            }
        }

        private Dictionary<string, string> GetGameObjectProperties(GameObject obj)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            dict["Graphics"] = $"0x{obj.Graphic:X4}";
            dict["Hue"] = $"0x{obj.Hue:X4}";
            dict["Position"] = $"X={obj.X}, Y={obj.Y}, Z={obj.Z}";
            dict["PriorityZ"] = obj.PriorityZ.ToString();
            dict["Distance"] = obj.Distance.ToString();
            dict["AllowedToDraw"] = obj.AllowedToDraw.ToString();
            dict["AlphaHue"] = obj.AlphaHue.ToString();

            switch (obj)
            {
                case Mobile mob:

                    dict["Serial"] = $"0x{mob.Serial:X8}";
                    dict["Flags"] = mob.Flags.ToString();
                    dict["Notoriety"] = mob.NotorietyFlag.ToString();
                    dict["Title"] = mob.Title ?? string.Empty;
                    dict["Name"] = mob.Name ?? string.Empty;
                    dict["HP"] = $"{mob.Hits}/{mob.HitsMax}";
                    dict["Mana"] = $"{mob.Mana}/{mob.ManaMax}";
                    dict["Stamina"] = $"{mob.Stamina}/{mob.StaminaMax}";
                    dict["SpeedMode"] = mob.SpeedMode.ToString();
                    dict["Race"] = mob.Race.ToString();
                    dict["IsRenamable"] = mob.IsRenamable.ToString();
                    dict["Direction"] = mob.Direction.ToString();
                    dict["IsDead"] = mob.IsDead.ToString();
                    dict["IsDrivingABoat"] = mob.IsDrivingBoat.ToString();
                    dict["IsMounted"] = mob.IsMounted.ToString();

                    break;

                case Item it:

                    dict["Serial"] = $"0x{it.Serial:X8}";
                    dict["Flags"] = it.Flags.ToString();
                    dict["HP"] = $"{it.Hits}/{it.HitsMax}";
                    dict["IsCoins"] = it.IsCoin.ToString();
                    dict["Amount"] = it.Amount.ToString();
                    dict["Container"] = it.Container.ToString();
                    dict["Layer"] = it.Layer.ToString();
                    dict["Price"] = it.Price.ToString();
                    dict["Direction"] = it.Direction.ToString();
                    dict["IsMulti"] = it.IsMulti.ToString();
                    dict["MultiGraphic"] = $"0x{it.MultiGraphic:X4}";

                    break;

                case Static st:

                    dict["IsVegetation"] = st.IsVegetation.ToString();

                    break;

                case Multi multi:

                    dict["State"] = multi.State.ToString();
                    dict["IsMovable"] = multi.IsMovable.ToString();

                    break;

                case Land land:

                    dict["IsFlat"] = (!land.IsStretched).ToString();
                    dict["NormalLeft"] = land.NormalLeft.ToString();
                    dict["NormalRight"] = land.NormalRight.ToString();
                    dict["NormalTop"] = land.NormalTop.ToString();
                    dict["NormalBottom"] = land.NormalBottom.ToString();
                    dict["MinZ"] = land.MinZ.ToString();
                    dict["AvgZ"] = land.AverageZ.ToString();
                    dict["YOffsets"] = land.YOffsets.ToString();

                    break;
            }

            return dict;
        }
    }
}