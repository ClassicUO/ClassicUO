﻿using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Gumps.UIGumps.Login
{
    class ServerSelectionGump : Gump
    {
        public ServerSelectionGump()
            : base(0, 0)
        {
            AddChildren(new Button((int)Buttons.Prev, 0x15A1, 0x15A3, over: 0x15A2) { X = 586, Y = 445, ButtonAction = ButtonAction.Activate });
            AddChildren(new Button((int)Buttons.Next, 0x15A4, 0x15A6, over: 0x15A5) { X = 610, Y = 445, ButtonAction = ButtonAction.Activate });

            if (FileManager.ClientVersion >= ClientVersions.CV_500A)
            {
                ushort textColor = 0xFFFF;
                AddChildren(new Label(IO.Resources.Cliloc.GetString(1044579), true, textColor, font: 1) { X = 155, Y = 70 }); // "Select which shard to play on:"
                AddChildren(new Label(IO.Resources.Cliloc.GetString(1044577), true, textColor, font: 1) { X = 400, Y = 70 }); // "Latency:"
                AddChildren(new Label(IO.Resources.Cliloc.GetString(1044578), true, textColor, font: 1) { X = 470, Y = 70 }); // "Packet Loss:"
                AddChildren(new Label(IO.Resources.Cliloc.GetString(1044580), true, textColor, font: 1) { X = 153, Y = 368 }); // "Sort by:"
            }
            else
            {
                ushort textColor = 0x0481;
                AddChildren(new Label("Select which shard to play on:", true, textColor, font: 9) { X = 155, Y = 70 });
                AddChildren(new Label("Latency:", true, textColor, font: 9) { X = 400, Y = 70 });
                AddChildren(new Label("Packet Loss:", true, textColor, font: 9) { X = 470, Y = 70 });
                AddChildren(new Label("Sort by:", true, textColor, font: 9) { X = 153, Y = 368 });
            }
            
            AddChildren(new Button((int)Buttons.SortTimeZone, 0x093B, 0x093C, over: 0x093D) { X = 230, Y = 366 });
            AddChildren(new Button((int)Buttons.SortFull, 0x093E, 0x093F, over: 0x0940) { X = 338, Y = 366 });
            AddChildren(new Button((int)Buttons.SortConnection, 0x0941, 0x0942, over: 0x0943) { X = 446, Y = 366 });

            // World Pic Bg
            AddChildren(new GumpPic(150, 390, 0x0589, 0));
            // Earth
            AddChildren(new Button((int)Buttons.Earth, 0x15E8, 0x15EA, over: 0x15E9) { X = 160, Y = 400 });

            // Sever Scroll Area Bg
            AddChildren(new ResizePic(0x0DAC) { X = 150, Y = 90, Width = 393 - 14, Height = 271 });
            // Sever Scroll Area
            ScrollArea scrollArea = new ScrollArea(150, 90, 393, 271, true);
            
            var loginScene = Service.Get<LoginScene>();
            foreach(var server in loginScene.Servers)
            {
                scrollArea.AddChildren(new ServerEntryGump(server));
            }

            AddChildren(scrollArea);

            if (loginScene.Servers.Count() > 0)
            {
                if (loginScene.Servers.Last().Index < loginScene.Servers.Count())
                    AddChildren(new Label(loginScene.Servers.Last().Name, false, 0x0481, font: 9) { X = 243, Y = 420 });
                else
                    AddChildren(new Label(loginScene.Servers.First().Name, false, 0x0481, font: 9) { X = 243, Y = 420 });
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            var loginScene = Service.Get<LoginScene>();

            if (buttonID >= (int)Buttons.Server)
            {
                var index = buttonID - (int)Buttons.Server;
                loginScene.SelectServer((byte)index);
            }
            else
            {
                switch((Buttons)buttonID)
                {
                    case Buttons.Next:
                        if (loginScene.Servers.Count() > 0)
                            loginScene.SelectServer(0);
                        break;
                }
            }
        }

        private enum Buttons
        {
            Prev, Next, SortTimeZone, SortFull, SortConnection, Earth, Server = 99
        }

        private class ServerEntryGump: GumpControl
        {
            RenderedText _labelName;
            RenderedText _labelPing;
            RenderedText _labelPacketLoss;

            ushort _normalColor = 0x034F;
            ushort _hoverColor = 0x0021;
            int _buttonId;

            public ServerEntryGump(ServerListEntry entry)
            {
                _buttonId = entry.Index;
                _labelName = CreateRenderedText(entry.Name);
                _labelPing = CreateRenderedText("-");
                _labelPacketLoss = CreateRenderedText("-");

                _labelName.CreateTexture();
                _labelPing.CreateTexture();
                _labelPacketLoss.CreateTexture();

                AcceptMouseInput = true;

                Width = 393;
                Height = new int[] { _labelName.Height, _labelPing.Height, _labelPacketLoss.Height }.Max() + 10;
                X = 0;
                Y = 0;
            }

            private RenderedText CreateRenderedText(string text)
            {
                return new RenderedText()
                {
                    Text = text,
                    Font = 5,
                    IsUnicode = false,
                    Hue = _normalColor,
                    Align = IO.Resources.TEXT_ALIGN_TYPE.TS_LEFT,
                    MaxWidth = 0
                };
            }

            public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
            {
                _labelName.Draw(spriteBatch, position + new Vector3(74, 10, 0));
                _labelPing.Draw(spriteBatch, position + new Vector3(250, 10, 0));
                _labelPacketLoss.Draw(spriteBatch, position + new Vector3(310, 10, 0));

                return base.Draw(spriteBatch, position, hue);
            }

            protected override void OnMouseEnter(int x, int y)
            {
                _labelName.Hue = _hoverColor;
                _labelPing.Hue = _hoverColor;
                _labelPacketLoss.Hue = _hoverColor;

                _labelName.CreateTexture();
                _labelPing.CreateTexture();
                _labelPacketLoss.CreateTexture();
                base.OnMouseEnter(x, y);
            }

            protected override void OnMouseLeft(int x, int y)
            {
                _labelName.Hue = _normalColor;
                _labelPing.Hue = _normalColor;
                _labelPacketLoss.Hue = _normalColor;

                _labelName.CreateTexture();
                _labelPing.CreateTexture();
                _labelPacketLoss.CreateTexture();
                base.OnMouseLeft(x, y);
            }

            protected override void OnMouseClick(int x, int y, MouseButton button)
            {
                if (button == MouseButton.Left)
                {
                    OnButtonClick((int)Buttons.Server + _buttonId);
                }
            }
        }
    }
}