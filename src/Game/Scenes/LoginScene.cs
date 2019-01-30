#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Gumps.CharCreation;
using ClassicUO.Game.UI.Gumps.Login;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Scenes
{
    internal sealed class LoginScene : Scene
    {
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
        private Gump _currentGump;
        private LoginStep _lastLoginStep;
        private static bool _isFirstLogin = true;

        public LoginScene() : base()
        {
        }

        public LoginStep CurrentLoginStep { get; private set; } = LoginStep.Main;


        public ServerListEntry[] Servers { get; private set; }

	    public CityInfo[] Cities { get; set; }

		public string[] Characters { get; private set; }

        public string PopupMessage { get; private set; }

        public byte ServerIndex { get; private set; }

        public string Account { get; private set; }

        public string Password { get; private set; }

        public override void Load()
        {
            base.Load();

            Engine.FpsLimit = Engine.GlobalSettings.MaxLoginFPS;

            Engine.UI.Add(new LoginBackground());
            Engine.UI.Add(_currentGump = new LoginGump());

            // Registering Packet Events
            NetClient.PacketReceived += NetClient_PacketReceived;
            NetClient.Socket.Disconnected += NetClient_Disconnected;
            NetClient.LoginSocket.Connected += NetClient_Connected;
            NetClient.LoginSocket.Disconnected += Login_NetClient_Disconnected;

            string[] parts = Engine.GlobalSettings.ClientVersion.Split(new[]
            {
                '.'
            }, StringSplitOptions.RemoveEmptyEntries);

            _clientVersionBuffer = new[]
            {
                byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3])
            };

            int music = FileManager.ClientVersion >= ClientVersions.CV_7000 ? 78 : FileManager.ClientVersion > ClientVersions.CV_308Z ? 0 : 8;

            Audio.PlayMusic(music);

            if (Engine.GlobalSettings.AutoLogin && _isFirstLogin && CurrentLoginStep != LoginStep.Main)
            {
                if (!string.IsNullOrEmpty(Engine.GlobalSettings.Username))
                {
                    Connect(Engine.GlobalSettings.Username, Crypter.Decrypt(Engine.GlobalSettings.Password));
                }
            }
        }


        public override void Unload()
        {
            Audio.StopMusic();

            Engine.UI.Remove<LoginBackground>();
            _currentGump?.Dispose();

            // UnRegistering Packet Events           
            // NetClient.Socket.Connected -= NetClient_Connected;
            NetClient.Socket.Disconnected -= NetClient_Disconnected;
            NetClient.LoginSocket.Connected -= NetClient_Connected;
            NetClient.LoginSocket.Disconnected -= NetClient_Disconnected;
            NetClient.PacketReceived -= NetClient_PacketReceived;

            Engine.UI.GameCursor.IsLoading = false;

            base.Unload();
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_lastLoginStep != CurrentLoginStep)
            {
                Engine.UI.GameCursor.IsLoading = false;

                // this trick avoid the flickering
                var g = _currentGump;
                Engine.UI.Add(_currentGump = GetGumpForStep());
                g.Dispose();

                _lastLoginStep = CurrentLoginStep;
            }

            base.Update(totalMS, frameMS);
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
                    Engine.UI.GameCursor.IsLoading = true;
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

            if (!string.IsNullOrEmpty(PopupMessage))
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
                for (byte i = 0; i < Servers.Length; i++)
                {
                    if (Servers[i].Index == index)
                    {
                        ServerIndex = i;
                        break;
                    }
                }

                Engine.GlobalSettings.LastServerNum = (ushort) ( 1 + ServerIndex) ;
                Engine.GlobalSettings.Save();

                CurrentLoginStep = LoginStep.LoginInToServer;
                World.ServerName = Servers[ServerIndex].Name;
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

        public void CreateCharacter(PlayerMobile character, CityInfo startingCity)
        {
            int i = 0;

            for (; i < Characters.Length; i++)
            {
                if (string.IsNullOrEmpty(Characters[i]))
                    break;
            }

            NetClient.Socket.Send(new PCreateCharacter(character, startingCity, NetClient.Socket.ClientAddress, ServerIndex, (uint)i));
        }

        public void DeleteCharacter(uint index)
        {
            if (CurrentLoginStep == LoginStep.CharacterSelection) NetClient.Socket.Send(new PDeleteCharacter((byte)index, NetClient.Socket.ClientAddress));
        }

        public void StepBack()
        {
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
            Log.Message(LogTypes.Warning, "Disconnected (game socket)!");

            if (CurrentLoginStep == LoginStep.CharCreation)
                return;

            Characters = null;
            Servers = null;
            PopupMessage = "Connection lost";
            CurrentLoginStep = LoginStep.PopUpMessage;
        }

        private void Login_NetClient_Disconnected(object sender, EventArgs e)
        {
            Log.Message(LogTypes.Warning, "Disconnected (login socket)!");
        }

        private void NetClient_PacketReceived(object sender, Packet e)
        {
            e.MoveToData();
            switch (e.ID)
            {
                case 0xA8: // ServerListReceived
                    ParseServerList(e);

                    // Save credentials to config file
                    if (Engine.GlobalSettings.SaveAccount)
                    {
                        Engine.GlobalSettings.Username = Account;
                        Engine.GlobalSettings.Password = Crypter.Encrypt(Password);
                        Engine.GlobalSettings.Save();
                    }

                    CurrentLoginStep = LoginStep.ServerSelection;

                    if (Engine.GlobalSettings.AutoLogin && _isFirstLogin)
                    {
                        if (Servers.Length != 0)
                            SelectServer( (byte) Servers[(Engine.GlobalSettings.LastServerNum-1)].Index);
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

					Engine.UI.Remove<CharacterSelectionGump>();

                    _currentGump?.Dispose();

                    Engine.UI.Add(_currentGump = new CharacterSelectionGump());

					break;

				case 0xA9: // ReceiveCharacterList
                    ParseCharacterList(e);
					ParseCities(e);
					ParseFlags(e);
                    CurrentLoginStep = LoginStep.CharacterSelection;

				    if (Engine.GlobalSettings.AutoLogin && _isFirstLogin)
				    {
				        _isFirstLogin = false;
                        bool haveAnyCharacter = false;

                        for (byte i = 0; i < Characters.Length; i++)
                        {
                            if (Characters[i].Length > 0)
				            {
                                haveAnyCharacter = true;
                                if (Characters[i] == Engine.GlobalSettings.LastCharacterName)
                                {
                                    SelectCharacter(i);
                                    return;
                                }
				            }
				        }

                        if (haveAnyCharacter)
                            SelectCharacter(0);
				    }

                    break;
                case 0xBD: // ReceiveVersionRequest
                    NetClient.Socket.Send(new PClientVersion(Engine.GlobalSettings.ClientVersion));

                    break;
                case 0x82: // ReceiveLoginRejection
                case 0x85: // character list notification
                case 0x53: // Error Code
                    byte code = e.ReadByte();

                    PopupMessage = ServerErrorMessages.GetError(e.ID, code);
                    CurrentLoginStep = LoginStep.PopUpMessage;

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
            byte[] ss = new byte[4]{(byte)(seed>>24), (byte)(seed>>16), (byte)(seed>>8), (byte)(seed)};
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
                Characters[i] = p.ReadASCII(30);
                p.Skip(30);
            }
        }

	    private void ParseCities(Packet p)
	    {
		    var count = p.ReadByte();
		    var cities = new CityInfo[count];

	        bool isNew = FileManager.ClientVersion >= ClientVersions.CV_70130;
	        string[] descriptions = null;

	        if (!isNew)
	            descriptions = ReadCityTextFile(count);

	        Position[] oldtowns =
	        {
	            new Position(105, 130), new Position(245, 90),
                new Position(165, 200), new Position(395, 160),
                new Position(200, 305), new Position(335, 250),
                new Position(160, 395), new Position(100, 250),
                new Position(270, 130), new Position(0xFFFF, 0xFFFF), 
	        };

            for (int i = 0; i < count; i++)
		    {
			    var cityInfo = default(CityInfo);

			    if (isNew)
			    {
				    var cityIndex = p.ReadByte();
				    var cityName = p.ReadASCII(32);
				    var cityBuilding = p.ReadASCII(32);
				    var cityPosition = new Position((ushort)p.ReadUInt(), (ushort)p.ReadUInt(), (sbyte)p.ReadUInt());
				    var cityMapIndex = p.ReadUInt();
				    var cityDescription = p.ReadUInt();
				    p.ReadUInt();

				    cityInfo = new CityInfo(cityIndex, cityName, cityBuilding, FileManager.Cliloc.GetString((int)cityDescription), cityPosition, cityMapIndex, isNew);
			    }
			    else
			    {
				    var cityIndex = p.ReadByte();
				    var cityName = p.ReadASCII(31);
				    var cityBuilding = p.ReadASCII(31);

				    cityInfo = new CityInfo(cityIndex, cityName, cityBuilding, descriptions != null ? descriptions[i] : string.Empty, oldtowns[i], 0, isNew);
			    }

			    cities[i] = cityInfo;
		    }

		    Cities = cities;
		}

        private string[] ReadCityTextFile(int count)
        {
            string path = Path.Combine(FileManager.UoFolderPath, "citytext.enu");
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

                            while ((b = (char) stream.ReadByte()) != '\0')
                            {
                                text.Append(b);
                            }

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
		    World.ClientFlags.SetFlags((CharacterListFlag)p.ReadUInt());
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
		public readonly int Index;
		public readonly string City;
		public readonly string Building;
		public readonly string Description;
		public readonly Position Position;
		public readonly uint Map;
	    public readonly bool IsNewCity;

		public CityInfo(int index, string city, string building, string description, Position position, uint map, bool isNew)
		{
			Index = index;
			City = city;
			Building = building;
			Description = description;
			Position = position;
			Map = map;
		    IsNewCity = isNew;
		}
	}
}