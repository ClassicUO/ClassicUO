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

using ClassicUO.Configuration;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Gumps.UIGumps.Login;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using System;
using System.ComponentModel;
using System.Net;

namespace ClassicUO.Game.Scenes
{
    public sealed class LoginScene : Scene
    {
        public enum LoginStep
        {
            Main,
            Connecting,
            VerifyingAccount,
            ServerSelection,
            LoginInToServer,
            CharacterSelection,
            EnteringBritania
        }

        public enum LoginRejectionReasons : byte
        {
            InvalidAccountPassword = 0x00,
            AccountInUse = 0x01,
            AccountBlocked = 0x02,
            BadPassword = 0x03,
            IdleExceeded = 0xFE,
            BadCommuncation = 0xFF
        }

        private LoginStep _loginStep = LoginStep.Main;
        private LoginRejectionReasons? _loginRejectionReason = null;
        private byte[] _clientVersionBuffer;
        
        public bool UpdateScreen { get; set; }
        public LoginStep CurrentLoginStep
        {
            get => _loginStep;
            private set => SetProperty(ref _loginStep, value);
        }
        public LoginRejectionReasons? LoginRejectionReason
        {
            get => _loginRejectionReason;
            private set => SetProperty(ref _loginRejectionReason, value);
        }
        public ServerListEntry[] Servers { get; private set; }
        public CharacterListEntry[] Characters { get; private set; }
        public string Account { get; private set; }
        public string Password { get; private set; }

        public LoginScene() : base(ScenesType.Login)
        {
        }
        
        public override void Load()
        {
            base.Load();

            Service.Register(this);
            UIManager.Add(new LoginGump());

            // Registering Packet Events
            NetClient.PacketReceived += NetClient_PacketReceived;
            // NetClient.Socket.Connected += NetClient_Connected;
            NetClient.Socket.Disconnected += NetClient_Disconnected;
            NetClient.LoginSocket.Connected += NetClient_Connected;
            NetClient.LoginSocket.Disconnected += NetClient_Disconnected;

            var settings = Service.Get<Settings>();
            string[] parts = settings.ClientVersion.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            _clientVersionBuffer = new []
            {
                byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3])
            };
        }
        
        public override void Unload()
        {
            UIManager.Remove<LoginGump>();
            Service.Unregister<LoginScene>();

            // UnRegistering Packet Events           
            // NetClient.Socket.Connected -= NetClient_Connected;
            NetClient.Socket.Disconnected -= NetClient_Disconnected;
            NetClient.LoginSocket.Connected -= NetClient_Connected;
            NetClient.LoginSocket.Disconnected -= NetClient_Disconnected;
            NetClient.PacketReceived -= NetClient_PacketReceived;
        }

        public void Connect(string account, string password)
        {
            Account = account;
            Password = password;

            var settings = Service.Get<Settings>();
            NetClient.LoginSocket.Connect(settings.IP, settings.Port);

            CurrentLoginStep = LoginStep.Connecting;
        }
        
        public void SelectServer(byte index)
        {
            if (CurrentLoginStep == LoginStep.ServerSelection)
            {
                CurrentLoginStep = LoginStep.LoginInToServer;
                NetClient.LoginSocket.Send(new PSelectServer(index));
            }
        }

        public void SelectCharacter(byte index, string name)
        {
            if (CurrentLoginStep == LoginStep.CharacterSelection)
            {
                CurrentLoginStep = LoginStep.EnteringBritania;
                NetClient.Socket.Send(new PSelectCharacter(index, name, NetClient.Socket.ClientAddress));
            }
        }

        public void StepBack()
        {
            _loginRejectionReason = null;

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
            }
        }
        
        private void NetClient_Connected(object sender, global::System.EventArgs e)
        {
            Log.Message(LogTypes.Info, "Connected!");
            CurrentLoginStep = LoginStep.VerifyingAccount;
            
            NetClient.LoginSocket.Send(new PSeed(NetClient.LoginSocket.ClientAddress, _clientVersionBuffer));
            NetClient.LoginSocket.Send(new PFirstLogin(Account, Password));
        }

        private void NetClient_Disconnected(object sender, global::System.EventArgs e)
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
                    CurrentLoginStep = LoginStep.ServerSelection;
                    break;
                case 0x8C: // ReceiveServerRelay
                    // On OSI, upon receiving this packet, the client would disconnect and
                    // log in to the specified server. Since emulated servers use the same
                    // server for both shard selection and world, we don't need to disconnect.
                    HandleRelayServerPacket(e);
                    break;
                case 0xA9: // ReceiveCharacterList
                    ParseCharacterList(e);
                    CurrentLoginStep = LoginStep.CharacterSelection;
                    break;
                case 0xBD: // ReceiveVersionRequest
                    var settings = Service.Get<Settings>();
                    NetClient.Socket.Send(new PClientVersion(settings.ClientVersion));
                    break;
                case 0x82: // ReceiveLoginRejection
                    HandleLoginRejection(e);
                    break;
            }
        }

        private void HandleRelayServerPacket(Packet p)
        {
            p.Seek(0);
            p.MoveToData();

            byte[] ip =  {p.ReadByte(), p.ReadByte(), p.ReadByte(), p.ReadByte()};
            ushort port = p.ReadUShort();
            uint seed = p.ReadUInt();
            
            NetClient.Socket.Connect(new IPAddress(ip), port);
            NetClient.Socket.EnableCompression();

            NetClient.Socket.Send(new PSeed(seed, _clientVersionBuffer));
            NetClient.Socket.Send(new PSecondLogin(Account, Password, seed));

            NetClient.LoginSocket.Disconnect();
        }

        private void ParseServerList(Packet reader)
        {
            var flags = reader.ReadByte();
            ushort count = reader.ReadUShort();

            Servers = new ServerListEntry[count];

            for (ushort i = 0; i < count; i++)
            {
                Servers[i] = new ServerListEntry(reader);
            }
        }

        private void ParseCharacterList(Packet reader)
        {
            int count = reader.ReadByte();

            Characters = new CharacterListEntry[count];

            for (ushort i = 0; i < count; i++)
            {
                Characters[i] = new CharacterListEntry(reader);
            }
        }
        
        private void HandleLoginRejection(Packet reader)
        {
            reader.MoveToData();
            var reasonId = reader.ReadByte();
            LoginRejectionReason = (LoginRejectionReasons)reasonId;
        }

        private void SetProperty<T>(ref T storage, T value)
        {
            storage = value;
            UpdateScreen = true;
        }
    }

    public class ServerListEntry
    {
        public readonly ushort Index;
        public readonly string Name;
        public readonly byte PercentFull;
        public readonly byte Timezone;
        public readonly uint Address;

        public ServerListEntry(Packet reader)
        {
            Index = reader.ReadUShort();
            Name = reader.ReadASCII(32);
            PercentFull = reader.ReadByte();
            Timezone = reader.ReadByte();
            Address = reader.ReadUInt();
        }
    }

    public class CharacterListEntry
    {
        public readonly string Name;
        public readonly string Password;

        public CharacterListEntry(Packet reader)
        {
            Name = reader.ReadASCII(30);
            Password = reader.ReadASCII(30);
        }
    }
}