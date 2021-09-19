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

using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class TopBarGump : Gump
    {
        private TopBarGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;

            // little
            Add
            (
                new ResizePic(0x13BE)
                {
                    Width = 30, Height = 27
                },
                2
            );

            Add
            (
                new Button(0, 0x15A1, 0x15A1, 0x15A1)
                {
                    X = 5, Y = 3, ToPage = 1
                },
                2
            );


            // big
            int smallWidth = 50;

            if (GumpsLoader.Instance.GetGumpTexture(0x098B, out var bounds) != null)
            {
                smallWidth = bounds.Width;
            }

            int largeWidth = 100;

            if (GumpsLoader.Instance.GetGumpTexture(0x098D, out bounds) != null)
            {
                largeWidth = bounds.Width;
            }

            int[][] textTable =
            {
                new[] { 0, (int) Buttons.Map },
                new[] { 1, (int) Buttons.Paperdoll },
                new[] { 1, (int) Buttons.Inventory },
                new[] { 1, (int) Buttons.Journal },
                new[] { 0, (int) Buttons.Chat },
                new[] { 0, (int) Buttons.Help },
                new[] { 1, (int) Buttons.WorldMap },
                new[] { 0, (int) Buttons.Info },
                new[] { 0, (int) Buttons.Debug },
                new[] { 1, (int) Buttons.NetStats },

                new[] { 1, (int) Buttons.UOStore },
                new[] { 1, (int) Buttons.GlobalChat }
            };

            string[] texts =
            {
                ResGumps.Map, ResGumps.Paperdoll, ResGumps.Inventory, ResGumps.Journal, ResGumps.Chat, ResGumps.Help,
                ResGumps.WorldMap, ResGumps.Info, ResGumps.Debug, ResGumps.NetStats, ResGumps.UOStore,
                ResGumps.GlobalChat
            };

            bool hasUOStore = Client.Version >= ClientVersion.CV_706400;

            ResizePic background;

            Add
            (
                background = new ResizePic(0x13BE)
                {
                    Height = 27
                },
                1
            );

            Add
            (
                new Button(0, 0x15A4, 0x15A4, 0x15A4)
                {
                    X = 5, Y = 3, ToPage = 2
                },
                1
            );

            int startX = 30;

            for (int i = 0; i < textTable.Length; i++)
            {
                if (!hasUOStore && i >= (int) Buttons.UOStore)
                {
                    break;
                }

                ushort graphic = (ushort) (textTable[i][0] != 0 ? 0x098D : 0x098B);

                Add
                (
                    new RighClickableButton
                    (
                        textTable[i][1],
                        graphic,
                        graphic,
                        graphic,
                        texts[i],
                        1,
                        true,
                        0,
                        0x0036
                    )
                    {
                        ButtonAction = ButtonAction.Activate,
                        X = startX,
                        Y = 1,
                        FontCenter = true
                    },
                    1
                );

                startX += (textTable[i][0] != 0 ? largeWidth : smallWidth) + 1;
                background.Width = startX;
            }

            background.Width = startX + 1;

            //layer
            LayerOrder = UILayer.Over;
        }

        public bool IsMinimized { get; private set; }

        public static void Create()
        {
            TopBarGump gump = UIManager.GetGump<TopBarGump>();

            if (gump == null)
            {
                if (ProfileManager.CurrentProfile.TopbarGumpPosition.X < 0 || ProfileManager.CurrentProfile.TopbarGumpPosition.Y < 0)
                {
                    ProfileManager.CurrentProfile.TopbarGumpPosition = Point.Zero;
                }

                UIManager.Add
                (
                    gump = new TopBarGump
                    {
                        X = ProfileManager.CurrentProfile.TopbarGumpPosition.X,
                        Y = ProfileManager.CurrentProfile.TopbarGumpPosition.Y
                    }
                );

                if (ProfileManager.CurrentProfile.TopbarGumpIsMinimized)
                {
                    gump.ChangePage(2);
                }
            }
            else
            {
                Log.Error(ResGumps.TopBarGumpAlreadyExists);
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Right && (X != 0 || Y != 0))
            {
                X = 0;
                Y = 0;

                ProfileManager.CurrentProfile.TopbarGumpPosition = Location;
            }
        }

        public override void OnPageChanged()
        {
            ProfileManager.CurrentProfile.TopbarGumpIsMinimized = IsMinimized = ActivePage == 2;
            WantUpdateSize = true;
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            ProfileManager.CurrentProfile.TopbarGumpPosition = Location;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Map:
                    GameActions.OpenMiniMap();

                    break;

                case Buttons.Paperdoll:
                    GameActions.OpenPaperdoll(World.Player);

                    break;

                case Buttons.Inventory:
                    GameActions.OpenBackpack();

                    break;

                case Buttons.Journal:
                    GameActions.OpenJournal();

                    break;

                case Buttons.Chat:
                    GameActions.OpenChat();

                    break;

                case Buttons.GlobalChat:
                    Log.Warn(ResGumps.ChatButtonPushedNotImplementedYet);
                    GameActions.Print(ResGumps.GlobalChatNotImplementedYet, 0x23, MessageType.System);

                    break;

                case Buttons.UOStore:
                    if (Client.Version >= ClientVersion.CV_706400)
                    {
                        NetClient.Socket.Send_OpenUOStore();
                    }

                    break;

                case Buttons.Help:
                    GameActions.RequestHelp();

                    break;

                case Buttons.Debug:

                    DebugGump debugGump = UIManager.GetGump<DebugGump>();

                    if (debugGump == null)
                    {
                        debugGump = new DebugGump(100, 100);
                        UIManager.Add(debugGump);
                    }
                    else
                    {
                        debugGump.IsVisible = !debugGump.IsVisible;
                        debugGump.SetInScreen();
                    }

                    break;

                case Buttons.NetStats:
                    NetworkStatsGump netstatsgump = UIManager.GetGump<NetworkStatsGump>();

                    if (netstatsgump == null)
                    {
                        netstatsgump = new NetworkStatsGump(100, 100);
                        UIManager.Add(netstatsgump);
                    }
                    else
                    {
                        netstatsgump.IsVisible = !netstatsgump.IsVisible;
                        netstatsgump.SetInScreen();
                    }

                    break;

                case Buttons.WorldMap:
                    GameActions.OpenWorldMap();

                    break;
            }
        }

        private enum Buttons
        {
            Map,
            Paperdoll,
            Inventory,
            Journal,
            Chat,
            Help,
            WorldMap,
            Info,
            Debug,
            NetStats,
            UOStore,
            GlobalChat
        }

        private class RighClickableButton : Button
        {
            public RighClickableButton
            (
                int buttonID,
                ushort normal,
                ushort pressed,
                ushort over = 0,
                string caption = "",
                byte font = 0,
                bool isunicode = true,
                ushort normalHue = ushort.MaxValue,
                ushort hoverHue = ushort.MaxValue
            ) : base
            (
                buttonID,
                normal,
                pressed,
                over,
                caption,
                font,
                isunicode,
                normalHue,
                hoverHue
            )
            {
            }

            public RighClickableButton(List<string> parts) : base(parts)
            {
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                base.OnMouseUp(x, y, button);
                Parent?.InvokeMouseUp(new Point(x, y), button);
            }
        }
    }
}