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

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Gumps
{
    internal class TopBarGump : Gump
    {
        private RighClickableButton XmlGumps;

        private TopBarGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;

            // little
            Add(new ResizePic(0x13BE) { Width = 30, Height = 27 }, 2);

            Add(
                new Button(0, 0x15A1, 0x15A1, 0x15A1)
                {
                    X = 5,
                    Y = 3,
                    ToPage = 1
                },
                2
            );

            // big
            int smallWidth = 50;
            ref readonly var gumpInfo = ref Client.Game.Gumps.GetGump(0x098B);
            if (gumpInfo.Texture != null)
            {
                smallWidth = gumpInfo.UV.Width;
            }

            int largeWidth = 100;

            gumpInfo = ref Client.Game.Gumps.GetGump(0x098D);
            if (gumpInfo.Texture != null)
            {
                largeWidth = gumpInfo.UV.Width;
            }

            int[][] textTable =
            {
                new[] { 1, (int) Buttons.Paperdoll },
                new[] { 1, (int) Buttons.Inventory },
                new[] { 1, (int) Buttons.Journal },
                new[] { 0, (int) Buttons.Chat },
                new[] { 1, (int) Buttons.WorldMap },
                new[] { 1, (int) Buttons.UOStore },
            };

            var cliloc = ClilocLoader.Instance;

            string[] texts =
            {
                cliloc.GetString(3000133, ResGumps.Paperdoll),
                cliloc.GetString(3000431, ResGumps.Inventory),
                cliloc.GetString(3000129, ResGumps.Journal),
                cliloc.GetString(3000131, ResGumps.Chat),
                StringHelper.CapitalizeAllWords(cliloc.GetString(1015233, ResGumps.WorldMap)),
                cliloc.GetString(1158008, ResGumps.UOStore),
            };

            bool hasUOStore = Client.Version >= ClientVersion.CV_706400;

            ResizePic background;

            Add(background = new ResizePic(0x13BE) { Height = 27 }, 1);

            Add(
                new Button(0, 0x15A4, 0x15A4, 0x15A4)
                {
                    X = 5,
                    Y = 3,
                    ToPage = 2
                },
                1
            );

            int startX = 30;

            for (int i = 0; i < textTable.Length; i++)
            {
                if (!hasUOStore && i >= (int)Buttons.UOStore)
                {
                    break;
                }

                ushort graphic = (ushort)(textTable[i][0] != 0 ? 0x098D : 0x098B);

                Add(
                    new RighClickableButton(
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

            RighClickableButton supporters;
            Add
            (supporters =
                new RighClickableButton
                (
                    998877,
                    0x098D,
                    0x098D,
                    0x098D,
                    "Supporters",
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
            supporters.MouseUp += (s, e) => { UIManager.Add(new Supporters()); };

            RighClickableButton moreMenu;
            Add
            (moreMenu =
                new RighClickableButton
                (
                    998877,
                    0x098D,
                    0x098D,
                    0x098D,
                    "More +",
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
            moreMenu.ContextMenu = new ContextMenuControl();
            moreMenu.MouseUp += (s, e) => { moreMenu.ContextMenu?.Show(); };
            moreMenu.ContextMenu.Add(new ContextMenuItemEntry(Language.Instance.TopBarGump.CommandsEntry, () =>
            {
                UIManager.Add(new CommandsGump());
            }));
            moreMenu.ContextMenu.Add(new ContextMenuItemEntry(cliloc.GetString(1079449, ResGumps.Info), () =>
            {
                if (TargetManager.IsTargeting)
                {
                    TargetManager.CancelTarget();
                }

                TargetManager.SetTargeting(CursorTarget.SetTargetClientSide, CursorType.Target, TargetType.Neutral);
            }));
            moreMenu.ContextMenu.Add(new ContextMenuItemEntry(cliloc.GetString(1042237, ResGumps.Debug), () =>
            {
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
            }));
            moreMenu.ContextMenu.Add(new ContextMenuItemEntry(cliloc.GetString(3000169, ResGumps.NetStats), () =>
            {
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
            }));
            moreMenu.ContextMenu.Add(new ContextMenuItemEntry(cliloc.GetString(3000134, ResGumps.Help), () => { GameActions.RequestHelp(); }));
            moreMenu.ContextMenu.Add(new ContextMenuItemEntry("Open boat control", () => { UIManager.Add(new BoatControl() { X = 200, Y = 200 }); }));

            startX += largeWidth + 1;

            string[] xmls = XmlGumpHandler.GetAllXmlGumps();
            if (xmls.Length > 0)
            {
                Add
                (XmlGumps =
                    new RighClickableButton
                    (
                        998877,
                        0x098D,
                        0x098D,
                        0x098D,
                        "Xml Gumps",
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

                XmlGumps.MouseUp += (s, e) => { XmlGumps.ContextMenu?.Show(); };

                RefreshXmlGumps();

                startX += largeWidth + 1;
            }

            background.Width = startX + 1;

            //layer
            LayerOrder = UILayer.Over;
        }

        public bool IsMinimized { get; private set; }

        public void RefreshXmlGumps()
        {
            XmlGumps.ContextMenu?.Dispose();
            if (XmlGumps.ContextMenu == null)
            {
                XmlGumps.ContextMenu = new ContextMenuControl();
            }

            string[] xmls = XmlGumpHandler.GetAllXmlGumps();

            ContextMenuItemEntry ci = null;
            foreach (var xml in xmls)
            {
                XmlGumps.ContextMenu.Add(ci = new ContextMenuItemEntry(xml, () =>
                {
                    if (Keyboard.Ctrl)
                    {
                        if (ProfileManager.CurrentProfile.AutoOpenXmlGumps.Contains(xml))
                        {
                            ProfileManager.CurrentProfile.AutoOpenXmlGumps.Remove(xml);
                        }
                        else
                        {
                            ProfileManager.CurrentProfile.AutoOpenXmlGumps.Add(xml);
                        }
                    }
                    else
                    {
                        UIManager.Add(XmlGumpHandler.CreateGumpFromFile(System.IO.Path.Combine(XmlGumpHandler.XmlGumpPath, xml + ".xml")));
                    }
                    RefreshXmlGumps();
                }, false, ProfileManager.CurrentProfile.AutoOpenXmlGumps.Contains(xml)));
            }

            ContextMenuItemEntry reload = new ContextMenuItemEntry("Reload", RefreshXmlGumps);
            XmlGumps.ContextMenu.Add(reload);
        }

        public static void Create()
        {
            TopBarGump gump = UIManager.GetGump<TopBarGump>();

            if (gump == null)
            {
                if (
                    ProfileManager.CurrentProfile.TopbarGumpPosition.X < 0
                    || ProfileManager.CurrentProfile.TopbarGumpPosition.Y < 0
                )
                {
                    ProfileManager.CurrentProfile.TopbarGumpPosition = Point.Zero;
                }

                UIManager.Add(
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
            switch ((Buttons)buttonID)
            {
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

                case Buttons.UOStore:
                    if (Client.Version >= ClientVersion.CV_706400)
                    {
                        NetClient.Socket.Send_OpenUOStore();
                    }

                    break;

                case Buttons.WorldMap:
                    GameActions.OpenWorldMap();

                    break;
            }
        }

        private enum Buttons
        {
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
        }

        private class RighClickableButton : Button
        {
            public RighClickableButton(
                int buttonID,
                ushort normal,
                ushort pressed,
                ushort over = 0,
                string caption = "",
                byte font = 0,
                bool isunicode = true,
                ushort normalHue = ushort.MaxValue,
                ushort hoverHue = ushort.MaxValue
            ) : base(buttonID, normal, pressed, over, caption, font, isunicode, normalHue, hoverHue)
            { }

            public RighClickableButton(List<string> parts) : base(parts) { }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                base.OnMouseUp(x, y, button);
                Parent?.InvokeMouseUp(new Point(x, y), button);
            }
        }
    }
}
