using ClassicUO.Configuration;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class LoginGump : Gump
    {
        private TextBox _textboxAccount;
        private TextBox _textboxPassword;
        private Checkbox _checkboxSaveAccount;
        
        public LoginGump() 
            : base(0, 0)
        {
            var settings = Service.Get<Settings>();

            // Background
            AddChildren(new GumpPicTiled(0, 0, 640, 480, 0x0E14));
            // Border
            AddChildren(new GumpPic(0, 0, 0x157C, 0));

            if (FileManager.ClientVersion >= ClientVersions.CV_500A)
                // Full background
                AddChildren(new GumpPic(0, 0, 0x2329, 0));

            // UO Flag
            AddChildren(new GumpPic(0, 4, 0x15A0, 0));

            // Login Panel
            AddChildren(new ResizePic(0x13BE) { X = 128, Y = 288, Width = 451, Height = 157 });

            if (FileManager.ClientVersion < ClientVersions.CV_500A)
                AddChildren(new GumpPic(286, 45, 0x058A, 0));

            // Quit Button
            AddChildren(new Button(0, 0x1589, 0x158B, over: 0x158A) { X = 555, Y = 4 });

            // Arrow Button
            AddChildren(new Button((int)Buttons.NextArrow, 0x15A4, 0x15A6, over: 0x15A5) { X = 610, Y = 445, ButtonAction = ButtonAction.Activate });

            // Account Text Input Background
            AddChildren(new ResizePic(0x0BB8) { X = 328, Y = 343, Width = 210, Height = 30 });
            // Password Text Input Background
            AddChildren(new ResizePic(0x0BB8) { X = 328, Y = 383, Width = 210, Height = 30 });

            AddChildren(_checkboxSaveAccount = new Checkbox(0x00D2, 0x00D3) { X = 328, Y = 417 });
            //g_MainScreen.m_SavePassword->SetTextParameters(9, "Save Password", 0x0386, STP_RIGHT_CENTER);

            //g_MainScreen.m_AutoLogin =
            //    (CGUICheckbox*)AddChildren(new CGUICheckbox(ID_MS_AUTOLOGIN, 0x00D2, 0x00D3, 0x00D2, 183, 417));
            //g_MainScreen.m_AutoLogin->SetTextParameters(9, "Auto Login", 0x0386, STP_RIGHT_CENTER);

            AddChildren(new Label("Log in to Ultima Online", false, 0x0386) { X = 253, Y = 305 });
            
            AddChildren(new Label("Account Name", false, 0x0386) { X = 183, Y = 345 });
            AddChildren(new Label("Password", false, 0x0386) { X = 183, Y = 385 });
            
            AddChildren(new Label("UO Version " + settings.ClientVersion + ".", false, 0x034E) { X = 286, Y = 455 });
            AddChildren(new Label("ClassicUO Version xxx", false, 0x034E) { X = 286, Y = 467 });
            
            // Text Inputs
            AddChildren(_textboxAccount = new TextBox(5, 32, 190, 190, false) { X = 335, Y = 343, Width = 190, Height = 25 });
            AddChildren(_textboxPassword = new TextBox(5, 32, 190, 190, false) { X = 335, Y = 385, Width = 190, Height = 25, IsPassword = true });
            
            _textboxAccount.SetText("rdegelo");
            _textboxPassword.SetText("password");
        }

        public override void OnButtonClick(int buttonID)
        {
            switch((Buttons)buttonID)
            {
                case Buttons.NextArrow:
                    TEST(Service.Get<Settings>());
                    break;
            }
        }

        private void TEST(Settings settings)
        {
            string[] parts = settings.ClientVersion.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            byte[] clientVersionBuffer =
                {byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3])};

            NetClient.Connected += (sender, e) =>
            {
                Log.Message(LogTypes.Info, "Connected!");

                NetClient.Socket.Send(new PSeed(NetClient.Socket.ClientAddress, clientVersionBuffer));
                NetClient.Socket.Send(new PFirstLogin(settings.Username, settings.Password.ToString()));
            };

            NetClient.Disconnected += (sender, e) => Log.Message(LogTypes.Warning, "Disconnected!");

            NetClient.PacketReceived += (sender, e) =>
            {
                switch (e.ID)
                {
                    case 0xA8:
                        NetClient.Socket.Send(new PSelectServer(0));
                        break;
                    case 0x8C:
                        NetClient.Socket.EnableCompression();
                        e.Seek(0);
                        e.MoveToData();
                        e.Skip(6);
                        NetClient.Socket.Send(new PSecondLogin(settings.Username, settings.Password.ToString(), e.ReadUInt()));
                        break;
                    case 0xA9:
                        NetClient.Socket.Send(new PSelectCharacter(0, settings.LastCharacterName,
                            NetClient.Socket.ClientAddress));
                        break;
                    case 0xBD:
                        NetClient.Socket.Send(new PClientVersion(settings.ClientVersion));
                        break;
                    case 0xBE:
                        NetClient.Socket.Send(new PAssistVersion(settings.ClientVersion, e.ReadUInt()));
                        break;
                    case 0x55:
                        NetClient.Socket.Send(new PClientViewRange(24));
                        break;
                }
            };


            NetClient.Socket.Connect(settings.IP, settings.Port);
        }

        private enum Buttons
        {
            NextArrow
        }
    }
}
