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
using System.IO;
using System.Linq;
using ClassicUO.Configuration;
//using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.RegularExpressions;


namespace ClassicUO.Game.UI.Gumps
{
    internal class TabGridGump : Gump
    {
        private const byte FONT = 0xFF;//
        private const ushort HUE_FONT = 0xFFFF;//
        private const int WIDTH = 700;//
        private const int HEIGHT = 500;//
        private const int TEXTBOX_HEIGHT = 25;//

        private const int ButonsperTab = 25;//
        private AlphaBlendControl _background;

        private int NRows, NTabs;
        private int Buttonwidth = 70;
        private int Buttonheight = 20;
        List<Macro> listaMacros = Client.Game.GetScene<GameScene>().Macros.GetAllMacros();
        private string[] TabNames;


        public TabGridGump() : base(0, 0)
        {
            string tablist = ProfileManager.CurrentProfile.TabList.ToString();
            TabNames = tablist.Split(':');
            string str1 = ProfileManager.CurrentProfile.GridTabs.ToString();
            string str2 = ProfileManager.CurrentProfile.GridRows.ToString();
            int Tabs = int.Parse(str1);
            int Rows = int.Parse(str2);

            if (Rows < 1)
            {
                Rows = 1;
            }

            if (Tabs < 1)
            {
                Tabs = 1;
            }

            NRows = Rows;
            NTabs = Tabs;


            //BuildGeneral(Tablist, FunctionsList);
            BuildGrid();
        }
        public override GumpType GumpType => GumpType.TabGridGump;

        private void BuildGrid()
        {

            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            CanCloseWithRightClick = false;
            WantUpdateSize = true;
            //int ButtonWidth = 70;
            //int ButtonHeight = 20;
            Width = ((Buttonwidth + 10) * NTabs);
            Height = ((Buttonheight + 10) * (NRows));

            Add(new AlphaBlendControl(0.7f) { X = 1, Y = 1, Width = Width, Height = Buttonheight + 10, Hue = 41 });
            int color = 2114 + (100);
            Add(new AlphaBlendControl(0.7f) { X = 1, Y = Buttonheight + 10, Width = Width, Height = Height, Hue = (ushort)color });


            //we add the button to each page
            for (int tab = 0; tab < NTabs; tab++)
            {
                Add(new Line(tab * (Buttonwidth + 10), 0, Buttonwidth + 10, 1, Color.Gray.PackedValue));//superior
                Add(new Line(tab * (Buttonwidth + 10), 0, 1, Buttonheight + 10, Color.Gray.PackedValue));//izquierda
                Add(new Line((tab * (Buttonwidth + 10)) + Buttonwidth + 10, 0, 1, Buttonheight + 10, Color.Gray.PackedValue));//derecha
                Add(new Line(tab * (Buttonwidth + 10), Buttonheight + 10, Buttonwidth + 10, 1, Color.Gray.PackedValue));//inferior
                if (tab < TabNames.Length)
                {
                    Add(new NiceButton((tab * (Buttonwidth + 10)) + 5, 5, Buttonwidth, Buttonheight, ButtonAction.SwitchPage, TabNames[tab]) { IsSelected = false, ButtonParameter = tab + 1 });
                }
                else
                {
                    Add(new NiceButton((tab * (Buttonwidth + 10)) + 5, 5, Buttonwidth, Buttonheight, ButtonAction.SwitchPage, "No Name") { IsSelected = false, ButtonParameter = tab + 1 });
                }
            }


            int contador = 0;
            int offsety = Buttonheight + 10;
            for (int pagina = 0; pagina < NTabs; pagina++)
            {
                //We create a page per tab starting at page 1 to avoid overdraw
                int PAGE = pagina;

                for (int tab = 0; tab < NTabs; tab++)
                {
                    for (int row = 0; row < NRows; row++)
                    {
                        Add(new Line(tab * (Buttonwidth + 10), ((row + 1) * (Buttonheight + 10)), Buttonwidth + 10, 1, Color.Gray.PackedValue));//superior
                        Add(new Line(tab * (Buttonwidth + 10), ((row + 1) * (Buttonheight + 10)), 1, Buttonheight + 10, Color.Gray.PackedValue));//izquierda
                        Add(new Line((tab * (Buttonwidth + 10)) + Buttonwidth + 10, ((row + 1) * (Buttonheight + 10)), 1, Buttonheight + 10, Color.Gray.PackedValue));//derecha
                        Add(new Line(tab * (Buttonwidth + 10), ((row + 1) * (Buttonheight + 10)) + Buttonheight + 10, Buttonwidth + 10, 1, Color.Gray.PackedValue));//inferior
                        if (contador < listaMacros.Count)
                        {
                            Add(new NiceButton((tab * (Buttonwidth + 10)) + 5, ((row + 1) * (Buttonheight + 10)) + 5, Buttonwidth, Buttonheight, ButtonAction.Activate, listaMacros[contador].Name) { IsSelected = false, ButtonParameter = contador }, PAGE + 1);
                        }
                        else
                        {
                            Add(new NiceButton((tab * (Buttonwidth + 10)) + 5, ((row + 1) * (Buttonheight + 10)) + 5, Buttonwidth, Buttonheight, ButtonAction.Activate, "unused") { IsSelected = false, ButtonParameter = contador }, PAGE + 1);
                        }
                        contador = contador + 1;
                        //Add(new GridButton(col * _rectSize + 2, row * _rectSize + 2, _rectSize - 4, _rectSize - 4));
                    }
                }
            }


        }


        public override void OnButtonClick(int buttonID)
        {
            GameActions.Print(buttonID.ToString());
            if (buttonID < listaMacros.Count)
            {
                GameScene gs = Client.Game.GetScene<GameScene>();
                gs.Macros.SetMacroToExecute(listaMacros[buttonID].Items as MacroObject);
            }

        }

        protected override void OnDragBegin(int x, int y)
        {
            if (UIManager.MouseOverControl?.RootParent == this)
            {
                UIManager.MouseOverControl.InvokeDragBegin(new Point(x, y));
            }

            base.OnDragBegin(x, y);
        }

        protected override void OnDragEnd(int x, int y)
        {
            if (UIManager.MouseOverControl?.RootParent == this)
            {
                UIManager.MouseOverControl.InvokeDragEnd(new Point(x, y));
            }

            base.OnDragEnd(x, y);
        }



    }
}
