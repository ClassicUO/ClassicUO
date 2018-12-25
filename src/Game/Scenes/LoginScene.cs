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
using System.Net;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Gumps.UIGumps.CharCreation;
using ClassicUO.Game.Gumps.UIGumps.Login;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Scenes
{
    public sealed class LoginScene : Scene
    {
        public enum LoginRejectionReasons : byte
        {
            InvalidAccountPassword = 0x00,
            AccountInUse = 0x01,
            AccountBlocked = 0x02,
            BadPassword = 0x03,
            IdleExceeded = 0xFE,
            BadCommuncation = 0xFF,
        }

        public enum LoginStep
        {
            Main,
            Connecting,
            VerifyingAccount,
            ServerSelection,
            LoginInToServer,
            CharacterSelection,
            EnteringBritania,
            CharCreation,
            PopUpMessage,
        }

        private byte[] _clientVersionBuffer;
        private LoginRejectionReasons? _loginRejectionReason;
        private LoginStep _loginStep = LoginStep.Main;
        private Gump _currentGump;


        public LoginScene() : base()
        {
        }

        public LoginStep CurrentLoginStep
        {
            get => _loginStep;
            private set
            {
                _loginStep = value;

                // this trick avoid the flickering
                var g = _currentGump;
                Engine.UI.Add(_currentGump = GetGumpForStep());
                g.Dispose();

            }
        }

        public LoginRejectionReasons? LoginRejectionReason
        {
            get => _loginRejectionReason;
            private set => _loginRejectionReason = value;
        }

        public ServerListEntry[] Servers { get; private set; }

        public string[] Characters { get; private set; }

        public string PopupMessage { get; private set; }

        public byte ServerIndex { get; private set; }

        public string Account { get; private set; }

        public string Password { get; private set; }

        public override void Load()
        {
            base.Load();

            Engine.FpsLimit = 60;

            Engine.UI.Add(new LoginBackground());
            Engine.UI.Add(_currentGump = new LoginGump());

            // Registering Packet Events
            NetClient.PacketReceived += NetClient_PacketReceived;
            NetClient.Socket.Disconnected += NetClient_Disconnected;
            NetClient.LoginSocket.Connected += NetClient_Connected;
            NetClient.LoginSocket.Disconnected += NetClient_Disconnected;

            string[] parts = Engine.GlobalSettings.ClientVersion.Split(new[]
            {
                '.'
            }, StringSplitOptions.RemoveEmptyEntries);

            _clientVersionBuffer = new[]
            {
                byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3])
            };
        }


        public override void Unload()
        {
            //Engine.UI.Remove<LoginGump>();
            Engine.UI.Remove<LoginBackground>();
            _currentGump?.Dispose();

            // UnRegistering Packet Events           
            // NetClient.Socket.Connected -= NetClient_Connected;
            NetClient.Socket.Disconnected -= NetClient_Disconnected;
            NetClient.LoginSocket.Connected -= NetClient_Connected;
            NetClient.LoginSocket.Disconnected -= NetClient_Disconnected;
            NetClient.PacketReceived -= NetClient_PacketReceived;

            base.Unload();
        }


        private Gump GetGumpForStep()
        {
            switch (CurrentLoginStep)
            {
                case LoginStep.Main:

                    return new LoginGump();
                case LoginStep.Connecting:
                case LoginStep.VerifyingAccount:
                case LoginStep.LoginInToServer:
                case LoginStep.EnteringBritania:
                case LoginStep.PopUpMessage:

                    return GetLoadingScreen();
                case LoginStep.CharacterSelection:

                    return new CharacterSelectionGump();
                case LoginStep.ServerSelection:

                    return new ServerSelectionGump();
                case LoginStep.CharCreation:

                    return new CharCreationGump();
            }

            return null;
        }

        private LoadingGump GetLoadingScreen()
        {
            var labelText = "No Text";
            var showButtons = LoadingGump.Buttons.None;

            if (LoginRejectionReason.HasValue)
            {
                switch (LoginRejectionReason.Value)
                {
                    case LoginRejectionReasons.BadPassword:
                    case LoginRejectionReasons.InvalidAccountPassword:
                        labelText = FileManager.Cliloc.GetString(3000036); // Incorrect username and/or password.

                        break;
                    case LoginRejectionReasons.AccountInUse:
                        labelText = FileManager.Cliloc.GetString(3000034); // Someone is already using this account.

                        break;
                    case LoginRejectionReasons.AccountBlocked:
                        labelText = FileManager.Cliloc.GetString(3000035); // Your account has been blocked / banned

                        break;
                    case LoginRejectionReasons.IdleExceeded:
                        labelText = FileManager.Cliloc.GetString(3000004); // Login idle period exceeded (I use "Connection lost")

                        break;
                    case LoginRejectionReasons.BadCommuncation:
                        labelText = FileManager.Cliloc.GetString(3000037); // Communication problem.

                        break;
                }

                showButtons = LoadingGump.Buttons.OK;
            }
            else if (!string.IsNullOrEmpty(PopupMessage))
            {
                labelText = PopupMessage;
                showButtons = LoadingGump.Buttons.OK;
            }
            else
            {
                switch (CurrentLoginStep)
                {
                    case LoginStep.Connecting:
                        labelText = FileManager.Cliloc.GetString(3000002); // "Connecting..."

                        break;
                    case LoginStep.VerifyingAccount:
                        labelText = FileManager.Cliloc.GetString(3000003); // "Verifying Account..."

                        break;
                    case LoginStep.LoginInToServer:
                        labelText = FileManager.Cliloc.GetString(3000053); // logging into shard

                        break;
                    case LoginStep.EnteringBritania:
                        labelText = FileManager.Cliloc.GetString(3000001); // Entering Britania...

                        break;
                }
            }

            return new LoadingGump(labelText, showButtons, OnLoadingGumpButtonClick);
        }

        private void OnLoadingGumpButtonClick(int buttonId)
        {
            if ((LoadingGump.Buttons)buttonId == LoadingGump.Buttons.OK) StepBack();
        }

        public void Connect(string account, string password)
        {
            if (CurrentLoginStep == LoginStep.Connecting)
                return;
            Account = account;
            Password = password;
            Log.Message(LogTypes.Trace, $"Start login to: {Engine.GlobalSettings.IP},{Engine.GlobalSettings.Port}");
            NetClient.LoginSocket.Connect(Engine.GlobalSettings.IP, Engine.GlobalSettings.Port);
            CurrentLoginStep = LoginStep.Connecting;
        }

        public void SelectServer(byte index)
        {
            if (CurrentLoginStep == LoginStep.ServerSelection)
            {
                ServerIndex = index;
                CurrentLoginStep = LoginStep.LoginInToServer;
                World.ServerName = Servers[index].Name;
                NetClient.LoginSocket.Send(new PSelectServer(index));
            }
        }

        public void SelectCharacter(uint index)
        {
            if (CurrentLoginStep == LoginStep.CharacterSelection)
            {
                Engine.GlobalSettings.LastCharacterName = Characters[index];
                Engine.GlobalSettings.Save();
                CurrentLoginStep = LoginStep.EnteringBritania;
                NetClient.Socket.Send(new PSelectCharacter(index, Characters[index], NetClient.Socket.ClientAddress));
            }
        }

        public void StartCharCreation()
        {
            if (CurrentLoginStep == LoginStep.CharacterSelection)
                CurrentLoginStep = LoginStep.CharCreation;
        }

        public void CreateCharacter(PlayerMobile character)
        {
            int i = 0;

            for (; i < Characters.Length; i++)
            {
                if (string.IsNullOrEmpty(Characters[i]))
                    break;
            }

            NetClient.Socket.Send(new PCreateCharacter(character, NetClient.Socket.ClientAddress, ServerIndex, (uint)i));
        }

        public void DeleteCharacter(uint index)
        {
            if (CurrentLoginStep == LoginStep.CharacterSelection) NetClient.Socket.Send(new PDeleteCharacter((byte)index, NetClient.Socket.ClientAddress));
        }

        public void StepBack()
        {
            _loginRejectionReason = null;
            PopupMessage = null;

            switch (CurrentLoginStep)
            {
                case LoginStep.Connecting:
                case LoginStep.VerifyingAccount:
                case LoginStep.ServerSelection:
                    Servers = null;
                    CurrentLoginStep = LoginStep.Main;
                    NetClient.LoginSocket.Disconnect();

                    break;
                case LoginStep.LoginInToServer:
                case LoginStep.CharacterSelection:
                    NetClient.Socket.Disconnect();
                    Characters = null;
                    Servers = null;
                    Connect(Account, Password);

                    break;
                case LoginStep.CharCreation:
                    CurrentLoginStep = LoginStep.CharacterSelection;

                    break;
                case LoginStep.PopUpMessage:
                    NetClient.LoginSocket.Disconnect();
                    NetClient.Socket.Disconnect();
                    Characters = null;
                    Servers = null;
                    CurrentLoginStep = LoginStep.Main;

                    break;
            }
        }

        private void NetClient_Connected(object sender, EventArgs e)
        {
            Log.Message(LogTypes.Info, "Connected!");
            CurrentLoginStep = LoginStep.VerifyingAccount;
            NetClient.LoginSocket.Send(new PSeed(NetClient.LoginSocket.ClientAddress, _clientVersionBuffer));
            NetClient.LoginSocket.Send(new PFirstLogin(Account, Password));
        }

        private void NetClient_Disconnected(object sender, EventArgs e)
        {
            Log.Message(LogTypes.Warning, "Disconnected!");
            // TODO: Reset
        }

        private void NetClient_PacketReceived(object sender, Packet e)
        {
            switch (e.ID)
            {
                case 0xA8: // ServerListReceived
                    ParseServerList(e);

                    // Save credentials to config file
                    Engine.GlobalSettings.Username = Account;
                    Engine.GlobalSettings.Password = Password;
                    Engine.GlobalSettings.Save();
                    CurrentLoginStep = LoginStep.ServerSelection;

                    break;
                case 0x8C: // ReceiveServerRelay
                    // On OSI, upon receiving this packet, the client would disconnect and
                    // log in to the specified server. Since emulated servers use the same
                    // server for both shard selection and world, we don't need to disconnect.
                    HandleRelayServerPacket(e);

                    break;
                case 0x86: // UpdateCharacterList
                case 0xA9: // ReceiveCharacterList
                    ParseCharacterList(e);
                    CurrentLoginStep = LoginStep.CharacterSelection;

                    break;
                case 0xBD: // ReceiveVersionRequest
                    NetClient.Socket.Send(new PClientVersion(Engine.GlobalSettings.ClientVersion));

                    break;
                case 0x82: // ReceiveLoginRejection
                    HandleLoginRejection(e);

                    break;
                case 0x53: // Error Code
                    HandleErrorCode(e);

                    break;
                default:
                    Log.Message(LogTypes.Debug, e.ID.ToString());

                    break;
            }
        }

        private void HandleRelayServerPacket(Packet p)
        {
            p.Seek(0);
            p.MoveToData();

            byte[] ip =
            {
                p.ReadByte(), p.ReadByte(), p.ReadByte(), p.ReadByte()
            };
            ushort port = p.ReadUShort();
            uint seed = p.ReadUInt();
            NetClient.LoginSocket.Disconnect();
            NetClient.Socket.Connect(new IPAddress(ip), port);
            NetClient.Socket.EnableCompression();
            NetClient.Socket.Send(new PSeed(seed, _clientVersionBuffer));
            NetClient.Socket.Send(new PSecondLogin(Account, Password, seed));
        }

        private void ParseServerList(Packet reader)
        {
            byte flags = reader.ReadByte();
            ushort count = reader.ReadUShort();
            Servers = new ServerListEntry[count];
            for (ushort i = 0; i < count; i++
                 ) Servers[i] = new ServerListEntry(reader);
        }

        private void ParseCharacterList(Packet p)
        {
            p.MoveToData();
            int count = p.ReadByte();
            Characters = new string[count];

            for (ushort i = 0; i < count; i++)
            {
                Characters[i] = p.ReadASCII(30);
                p.Skip(30);
            }

            count = p.ReadByte();

            // TODO: implemnet city infos
            if (FileManager.ClientVersion >= ClientVersions.CV_70130)
            {
                for (int i = 0; i < count; i++)
                    p.Skip(1 +32 +32 + 4 + 4 + 4 + 4 + 4 + 4);
            }
            else
            {
                for (int i = 0; i < count; i++)
                    p.Skip(1 + 31 + 31);
            }

            World.ClientFlags.SetFlags((CharacterListFlag)p.ReadUInt());
        }

        private void HandleErrorCode(Packet reader)
        {
            string[] messages = {
                "Incorrect password",
                "This character does not exist any more!",
                "This character already exists.",
                "Could not attach to game server.",
                "Could not attach to game server.",
                "A character is already logged in.",
                "Synchronization Error.",
                "You have been idle for to long.",
                "Could not attach to game server.",
                "Character transfer in progress."
            };
            
            PopupMessage = messages[reader.ReadByte()];
            CurrentLoginStep = LoginStep.PopUpMessage;
        }

        private void HandleLoginRejection(Packet reader)
        {
            reader.MoveToData();
            byte reasonId = reader.ReadByte();
            LoginRejectionReason = (LoginRejectionReasons)reasonId;
        }
    }

    public class ServerListEntry
    {
        public readonly uint Address;
        public readonly ushort Index;
        public readonly string Name;
        public readonly byte PercentFull;
        public readonly byte Timezone;

        public ServerListEntry(Packet reader)
        {
            Index = reader.ReadUShort();
            Name = reader.ReadASCII(32);
            PercentFull = reader.ReadByte();
            Timezone = reader.ReadByte();
            Address = reader.ReadUInt();
        }
    }
}