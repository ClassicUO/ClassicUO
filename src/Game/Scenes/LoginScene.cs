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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;

using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Gumps.CharCreation;
using ClassicUO.Game.UI.Gumps.Login;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Scenes
{
    enum LoginSteps
    {
        Main,
        Connecting,
        VerifyingAccount,
        ServerSelection,
        LoginInToServer,
        CharacterSelection,
        EnteringBritania,
        CharCreation,
        CreatingCharacter,
        PopUpMessage
    }

    internal sealed class LoginScene : Scene
    {
        private Gump _currentGump;
        private LoginSteps _lastLoginStep;
        private long? _reconnectTime;
        private int _reconnectTryCounter = 1;


        public LoginScene() : base((int) SceneID.Login,
            false,
            false,
            true)
        {

        }


        public bool Reconnect { get; set; }

        public LoginSteps CurrentLoginStep { get; set; } = LoginSteps.Main;

        public ServerListEntry[] Servers { get; private set; }

        public CityInfo[] Cities { get; set; }

        public string[] Characters { get; private set; }

        public string PopupMessage { get; set; }

        public byte ServerIndex { get; private set; }

        public static string Account { get; private set; }

        public string Password { get; private set; }

        public override void Load()
        {
            base.Load();

            //Engine.FpsLimit = Settings.GlobalSettings.MaxLoginFPS;

            UIManager.Add(new LoginBackground());
            UIManager.Add(_currentGump = new LoginGump());

            // Registering Packet Events
            NetClient.PacketReceived += NetClient_PacketReceived;
            NetClient.Socket.Disconnected += NetClient_Disconnected;
            NetClient.LoginSocket.Connected += NetClient_Connected;
            NetClient.LoginSocket.Disconnected += Login_NetClient_Disconnected;

            int music = Client.Version >= ClientVersion.CV_7000 ? 78 : Client.Version > ClientVersion.CV_308Z ? 0 : 8;

            Audio.PlayMusic(music);

            if (((Settings.GlobalSettings.AutoLogin || Reconnect) && (CurrentLoginStep != LoginSteps.Main)) || CUOEnviroment.SkipLoginScreen)
            {
                if (!string.IsNullOrEmpty(Settings.GlobalSettings.Username))
                {
                    // disable if it's the 2nd attempt
                    CUOEnviroment.SkipLoginScreen = false;
                    Connect(Settings.GlobalSettings.Username, Crypter.Decrypt(Settings.GlobalSettings.Password));
                }
            }

            if (Client.Game.IsWindowMaximized())
                Client.Game.RestoreWindow();
            Client.Game.SetWindowSize(640, 480);
            //Client.Client.SetWindowPositionBySettings();
        }


        public override void Unload()
        {
            Audio.StopMusic();

            UIManager.GetGump<LoginBackground>()?.Dispose();
            _currentGump?.Dispose();

            // UnRegistering Packet Events           
            // NetClient.Socket.Connected -= NetClient_Connected;
            NetClient.Socket.Disconnected -= NetClient_Disconnected;
            NetClient.LoginSocket.Connected -= NetClient_Connected;
            NetClient.LoginSocket.Disconnected -= Login_NetClient_Disconnected;
            NetClient.PacketReceived -= NetClient_PacketReceived;

            UIManager.GameCursor.IsLoading = false;
            base.Unload();
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_lastLoginStep != CurrentLoginStep)
            {
                UIManager.GameCursor.IsLoading = false;

                // this trick avoid the flickering
                var g = _currentGump;
                UIManager.Add(_currentGump = GetGumpForStep());
                g.Dispose();

                _lastLoginStep = CurrentLoginStep;
            }

            if (Reconnect && (CurrentLoginStep == LoginSteps.PopUpMessage || CurrentLoginStep == LoginSteps.Main))
            {
                long rt = (long) totalMS + Settings.GlobalSettings.ReconnectTime * 1000;

                if (_reconnectTime == null)
                    _reconnectTime = rt;

                if (_reconnectTime < totalMS)
                {
                    if (!string.IsNullOrEmpty(Account))
                        Connect(Account, Crypter.Decrypt(Settings.GlobalSettings.Password));
                    else if (!string.IsNullOrEmpty(Settings.GlobalSettings.Username))
                        Connect(Settings.GlobalSettings.Username, Crypter.Decrypt(Settings.GlobalSettings.Password));

                    _reconnectTime = rt;
                    _reconnectTryCounter++;
                }
            }

            base.Update(totalMS, frameMS);
        }

        private Gump GetGumpForStep()
        {
            World.Items.Clear();
            World.Items.ProcessDelta();

            switch (CurrentLoginStep)
            {
                case LoginSteps.Main:
                    PopupMessage = null;

                    return new LoginGump();

                case LoginSteps.Connecting:
                case LoginSteps.VerifyingAccount:
                case LoginSteps.LoginInToServer:
                case LoginSteps.EnteringBritania:
                case LoginSteps.PopUpMessage:
                case LoginSteps.CreatingCharacter:
                    UIManager.GameCursor.IsLoading = CurrentLoginStep != LoginSteps.PopUpMessage;

                    return GetLoadingScreen();

                case LoginSteps.CharacterSelection:

                    return new CharacterSelectionGump();

                case LoginSteps.ServerSelection:

                    return new ServerSelectionGump();

                case LoginSteps.CharCreation:
                    return new CharCreationGump(this);
            }

            return null;
        }

        private LoadingGump GetLoadingScreen()
        {
            var labelText = "No Text";
            var showButtons = LoginButtons.None;

            if (!string.IsNullOrEmpty(PopupMessage))
            {
                labelText = PopupMessage;
                showButtons = LoginButtons.OK;
                PopupMessage = null;
            }
            else
            {
                switch (CurrentLoginStep)
                {
                    case LoginSteps.Connecting:
                        labelText = ClilocLoader.Instance.GetString(3000002); // "Connecting..."

                        break;

                    case LoginSteps.VerifyingAccount:
                        labelText = ClilocLoader.Instance.GetString(3000003); // "Verifying Account..."

                        break;

                    case LoginSteps.LoginInToServer:
                        labelText = ClilocLoader.Instance.GetString(3000053); // logging into shard

                        break;

                    case LoginSteps.EnteringBritania:
                        labelText = ClilocLoader.Instance.GetString(3000001); // Entering Britania...

                        break;
                    case LoginSteps.CreatingCharacter:
                        labelText = "Creating character...";
                        break;
                }
            }

            return new LoadingGump(labelText, showButtons, OnLoadingGumpButtonClick);
        }

        private void OnLoadingGumpButtonClick(int buttonId)
        {
            if ((LoginButtons) buttonId == LoginButtons.OK)
                StepBack();
        }

        public void Connect(string account, string password)
        {
            if (CurrentLoginStep == LoginSteps.Connecting)
                return;

            Account = account;
            Password = password;

            // Save credentials to config file
            if (Settings.GlobalSettings.SaveAccount)
            {
                Settings.GlobalSettings.Username = Account;
                Settings.GlobalSettings.Password = Crypter.Encrypt(Password);
                Settings.GlobalSettings.Save();
            }

            Log.Trace( $"Start login to: {Settings.GlobalSettings.IP},{Settings.GlobalSettings.Port}");

            if (!NetClient.LoginSocket.Connect(Settings.GlobalSettings.IP, Settings.GlobalSettings.Port))
            {
                PopupMessage = "Check your internet connection and try again";
                Log.Error( "No Internet Access");
            }
            if(!Reconnect)
                CurrentLoginStep = LoginSteps.Connecting;
        }

        public void SelectServer(byte index)
        {
            if (CurrentLoginStep == LoginSteps.ServerSelection)
            {
                for (byte i = 0; i < Servers.Length; i++)
                {
                    if (Servers[i].Index == index)
                    {
                        ServerIndex = i;

                        break;
                    }
                }

                Settings.GlobalSettings.LastServerNum = (ushort) (1 + ServerIndex);
                Settings.GlobalSettings.Save();

                CurrentLoginStep = LoginSteps.LoginInToServer;
                World.ServerName = Servers[ServerIndex].Name;
                NetClient.LoginSocket.Send(new PSelectServer(index));
            }
        }

        public void SelectCharacter(uint index)
        {
            if (CurrentLoginStep == LoginSteps.CharacterSelection)
            {
                Settings.GlobalSettings.LastCharacterName = Characters[index];
                Settings.GlobalSettings.Save();
                CurrentLoginStep = LoginSteps.EnteringBritania;
                NetClient.Socket.Send(new PSelectCharacter(index, Characters[index], NetClient.Socket.ClientAddress));
            }
        }

        public void StartCharCreation()
        {
            if (CurrentLoginStep == LoginSteps.CharacterSelection)
                CurrentLoginStep = LoginSteps.CharCreation;
        }

        public void CreateCharacter(PlayerMobile character, int cityIndex, byte profession)
        {
            int i = 0;

            for (; i < Characters.Length; i++)
            {
                if (string.IsNullOrEmpty(Characters[i]))
                    break;
            }

            Settings.GlobalSettings.LastCharacterName = character.Name;
            NetClient.Socket.Send(new PCreateCharacter(character, cityIndex, NetClient.Socket.ClientAddress, ServerIndex, (uint) i, profession));
            CurrentLoginStep = LoginSteps.CreatingCharacter;
        }

        public void DeleteCharacter(uint index)
        {
            if (CurrentLoginStep == LoginSteps.CharacterSelection) NetClient.Socket.Send(new PDeleteCharacter((byte) index, NetClient.Socket.ClientAddress));
        }

        public void StepBack()
        {
            PopupMessage = null;

            if (Characters != null && CurrentLoginStep != LoginSteps.CharCreation)
            {
                CurrentLoginStep = LoginSteps.LoginInToServer;
            }

            switch (CurrentLoginStep)
            {
                case LoginSteps.Connecting:
                case LoginSteps.VerifyingAccount:
                case LoginSteps.ServerSelection:
                    Servers = null;
                    CurrentLoginStep = LoginSteps.Main;
                    NetClient.LoginSocket.Disconnect();

                    break;

                case LoginSteps.LoginInToServer:
                    NetClient.Socket.Disconnect();
                    Characters = null;
                    Servers = null;
                    Connect(Account, Password);

                    break;

                case LoginSteps.CharCreation:
                    CurrentLoginStep = LoginSteps.CharacterSelection;

                    break;

                case LoginSteps.PopUpMessage:
                case LoginSteps.CharacterSelection:
                    NetClient.LoginSocket.Disconnect();
                    NetClient.Socket.Disconnect();
                    Characters = null;
                    Servers = null;
                    CurrentLoginStep = LoginSteps.Main;

                    break;
            }
        }

        public CityInfo GetCity(int index)
        {
            if (index < Cities.Length)
                return Cities[index];

            return null;
        }

        private void NetClient_Connected(object sender, EventArgs e)
        {
            Log.Info("Connected!");
            CurrentLoginStep = LoginSteps.VerifyingAccount;

            if (Client.Version > ClientVersion.CV_6040)
            {
                uint clientVersion = (uint) Client.Version;

                byte major = (byte) (clientVersion >> 24);
                byte minor = (byte) (clientVersion >> 16);
                byte build = (byte) (clientVersion >> 8);
                byte extra = (byte) clientVersion;

                NetClient.LoginSocket.Send(new PSeed(NetClient.LoginSocket.ClientAddress, major, minor, build, extra));
            }
            else
                NetClient.LoginSocket.Send(BitConverter.GetBytes(NetClient.LoginSocket.ClientAddress));

            NetClient.LoginSocket.Send(new PFirstLogin(Account, Password));
        }

        private void NetClient_Disconnected(object sender, SocketError e)
        {
            Log.Warn( "Disconnected (game socket)!");

            if (CurrentLoginStep == LoginSteps.CharCreation)
                return;

            Characters = null;
            Servers = null;
            PopupMessage = $"Connection lost:\n{StringHelper.AddSpaceBeforeCapital(e.ToString())}";
            CurrentLoginStep = LoginSteps.PopUpMessage;
        }

        private void Login_NetClient_Disconnected(object sender, SocketError e)
        {
            Log.Warn( "Disconnected (login socket)!");

            if (e > 0)
            {
                Characters = null;
                Servers = null;

                if (Settings.GlobalSettings.Reconnect)
                {
                    Reconnect = true;
                    PopupMessage = $"Reconnect, please wait...`{_reconnectTryCounter}`\n`{StringHelper.AddSpaceBeforeCapital(e.ToString())}`";
                    var c = UIManager.Gumps.OfType<LoadingGump>().FirstOrDefault();
                    if (c != null)
                        c._Label.Text = PopupMessage;
                }
                else
                    PopupMessage = $"Connection lost:\n`{StringHelper.AddSpaceBeforeCapital(e.ToString())}`";

                CurrentLoginStep = LoginSteps.PopUpMessage;
            }
        }

        private void NetClient_PacketReceived(object sender, Packet e)
        {
            e.MoveToData();

            switch (e.ID)
            {
                case 0xA8: // ServerListReceived
                    ParseServerList(e);

                    CurrentLoginStep = LoginSteps.ServerSelection;

                    if (Settings.GlobalSettings.AutoLogin || Reconnect)
                    {
                        if (Servers.Length != 0)
                        {
                            int index = Settings.GlobalSettings.LastServerNum;

                            if (index <= 0 || index > Servers.Length)
                            {
                                Log.Warn( $"Wrong server index: {index}");
                                index = 1;
                            }

                            SelectServer((byte) Servers[index - 1].Index);
                        }
                    }

                    break;

                case 0x8C: // ReceiveServerRelay
                    // On OSI, upon receiving this packet, the client would disconnect and
                    // log in to the specified server. Since emulated servers use the same
                    // server for both shard selection and world, we don't need to disconnect.
                    HandleRelayServerPacket(e);

                    break;

                case 0x86: // UpdateCharacterList
                    ParseCharacterList(e);

                    UIManager.GetGump<CharacterSelectionGump>()?.Dispose();

                    _currentGump?.Dispose();

                    UIManager.Add(_currentGump = new CharacterSelectionGump());

                    break;

                case 0xA9: // ReceiveCharacterList
                    ParseCharacterList(e);
                    ParseCities(e);
                    ParseFlags(e);
                    CurrentLoginStep = LoginSteps.CharacterSelection;

                    uint charToSelect = 0;

                    bool haveAnyCharacter = false;
                    bool tryAutologin = Settings.GlobalSettings.AutoLogin || Reconnect;

                    for (byte i = 0; i < Characters.Length; i++)
                    {
                        if (Characters[i].Length > 0)
                        {
                            haveAnyCharacter = true;

                            if (Characters[i] == Settings.GlobalSettings.LastCharacterName)
                            {
                                charToSelect = i;

                                break;
                            }
                        }
                    }

                    if (tryAutologin && haveAnyCharacter)
                        SelectCharacter(charToSelect);
                    else if (!haveAnyCharacter)
                        StartCharCreation();

                    break;

                case 0xBD: // ReceiveVersionRequest
                    NetClient.Socket.Send(new PClientVersion(Settings.GlobalSettings.ClientVersion));

                    break;

                case 0x82: // ReceiveLoginRejection
                case 0x85: // character list notification
                case 0x53: // Error Code
                    byte code = e.ReadByte();

                    PopupMessage = ServerErrorMessages.GetError(e.ID, code);
                    CurrentLoginStep = LoginSteps.PopUpMessage;

                    break;
                case 0xB9:
                    uint flags = 0;

                    if (Client.Version >= ClientVersion.CV_60142)
                        flags = e.ReadUInt();
                    else
                        flags = e.ReadUShort();
                    World.ClientLockedFeatures.SetFlags((LockedFeatureFlags) flags);
                    break;
                default:
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
            byte[] ss = new byte[4] {(byte) (seed >> 24), (byte) (seed >> 16), (byte) (seed >> 8), (byte) seed};
            NetClient.Socket.Send(ss);
            NetClient.Socket.Send(new PSecondLogin(Account, Password, seed));
        }

        private void ParseServerList(Packet reader)
        {
            byte flags = reader.ReadByte();
            ushort count = reader.ReadUShort();
            Servers = new ServerListEntry[count];

            for (ushort i = 0; i < count; i++)
                Servers[i] = new ServerListEntry(reader);
        }

        private void ParseCharacterList(Packet p)
        {
            int count = p.ReadByte();
            Characters = new string[count];

            for (ushort i = 0; i < count; i++)
            {
                Characters[i] = p.ReadASCII(30).TrimEnd('\0');
                p.Skip(30);
            }
        }

        private void ParseCities(Packet p)
        {
            var count = p.ReadByte();
            Cities = new CityInfo[count];

            bool isNew = Client.Version >= ClientVersion.CV_70130;
            string[] descriptions = null;

            if (!isNew)
                descriptions = ReadCityTextFile(count);

            Point[] oldtowns =
            {
                new Point(105, 130), new Point(245, 90),
                new Point(165, 200), new Point(395, 160),
                new Point(200, 305), new Point(335, 250),
                new Point(160, 395), new Point(100, 250),
                new Point(270, 130), new Point(0xFFFF, 0xFFFF)
            };

            for (int i = 0; i < count; i++)
            {
                CityInfo cityInfo;

                if (isNew)
                {
                    byte cityIndex = p.ReadByte();
                    string cityName = p.ReadASCII(32);
                    string cityBuilding = p.ReadASCII(32);
                    ushort cityX = (ushort) p.ReadUInt();
                    ushort cityY = (ushort) p.ReadUInt();
                    sbyte cityZ = (sbyte) p.ReadUInt();
                    uint cityMapIndex = p.ReadUInt();
                    uint cityDescription = p.ReadUInt();
                    p.Skip(4);

                    cityInfo = new CityInfo(cityIndex, cityName, cityBuilding, ClilocLoader.Instance.GetString((int) cityDescription), cityX, cityY, cityZ, cityMapIndex, isNew);
                }
                else
                {
                    byte cityIndex = p.ReadByte();
                    string cityName = p.ReadASCII(31);
                    string cityBuilding = p.ReadASCII(31);

                    cityInfo = new CityInfo(cityIndex, cityName, cityBuilding, descriptions != null ? descriptions[i] : string.Empty, (ushort) oldtowns[i].X, (ushort) oldtowns[i].Y, 0, 0, isNew);
                }

                Cities[i] = cityInfo;
            }
        }

        private string[] ReadCityTextFile(int count)
        {
            string path = UOFileManager.GetUOFilePath("citytext.enu");

            if (!File.Exists(path))
                return null;

            string[] descr = new string[count];

            byte[] data = new byte[4];

            StringBuilder name = new StringBuilder();
            StringBuilder text = new StringBuilder();

            using (FileStream stream = File.OpenRead(path))
            {
                int cityIndex = 0;

                while (stream.Position < stream.Length)
                {
                    int r = stream.Read(data, 0, 4);

                    if (r == -1)
                        break;

                    string dataText = Encoding.UTF8.GetString(data, 0, 4);

                    if (dataText == "END\0")
                    {
                        name.Clear();

                        while (stream.Position < stream.Length)
                        {
                            char b = (char) stream.ReadByte();

                            if (b == '<')
                            {
                                stream.Position -= 1;

                                break;
                            }

                            name.Append(b);
                        }

                        text.Clear();

                        while (stream.Position < stream.Length)
                        {
                            char b;

                            while ((b = (char) stream.ReadByte()) != '\0') text.Append(b);

                            if (text.Length != 0)
                            {
                                string t = text + "\n\n";
                                text.Clear();

                                text.Append(t);
                            }

                            long pos = stream.Position;
                            byte end = (byte) stream.ReadByte();
                            stream.Position = pos;

                            if (end == 0x2E)
                                break;

                            int r1 = stream.Read(data, 0, 4);
                            stream.Position = pos;

                            if (r1 == -1)
                                break;

                            string dataText1 = Encoding.UTF8.GetString(data, 0, 4);

                            if (dataText1 == "END\0")
                                break;
                        }

                        if (descr.Length <= cityIndex) break;

                        descr[cityIndex++] = text.ToString();
                    }
                    else
                        stream.Position -= 3;
                }
            }

            return descr;
        }

        private void ParseFlags(Packet p)
        {
            World.ClientFeatures.SetFlags((CharacterListFlags) p.ReadUInt());
        }
    }

    internal class ServerListEntry
    {
        public readonly uint Address;
        public readonly ushort Index;
        public readonly string Name;
        public readonly byte PercentFull;
        public readonly byte Timezone;

        public ServerListEntry(Packet reader)
        {
            Index = reader.ReadUShort();
            Name = reader.ReadASCII(32).MakeSafe();
            PercentFull = reader.ReadByte();
            Timezone = reader.ReadByte();
            Address = reader.ReadUInt();
        }
    }

    internal class CityInfo
    {
        public readonly string Building;
        public readonly string City;
        public readonly string Description;
        public readonly int Index;
        public readonly bool IsNewCity;
        public readonly uint Map;
        public readonly ushort X, Y;
        public readonly sbyte Z;

        public CityInfo(int index, string city, string building, string description, ushort x, ushort y, sbyte z, uint map, bool isNew)
        {
            Index = index;
            City = city;
            Building = building;
            Description = description;
            X = x;
            Y = y;
            Z = z;
            Map = map;
            IsNewCity = isNew;
        }
    }
}