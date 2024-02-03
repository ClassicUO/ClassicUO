#region license

// Copyright (c) 2024, andreakarasho
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
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Gumps.CharCreation;
using ClassicUO.Game.UI.Gumps.Login;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Network.Encryption;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Scenes
{
    internal enum LoginSteps
    {
        Main,
        Connecting,
        VerifyingAccount,
        ServerSelection,
        LoginInToServer,
        CharacterSelection,
        EnteringBritania,
        CharacterCreation,
        CharacterCreationDone,
        PopUpMessage
    }

    internal sealed class LoginScene : Scene
    {
        private Gump _currentGump;
        private LoginSteps _lastLoginStep;
        private uint _pingTime;
        private long _reconnectTime;
        private int _reconnectTryCounter = 1;
        private bool _autoLogin;
        private readonly World _world;

        public LoginScene(World world) => _world = world;


        public bool Reconnect { get; set; }
        public LoginSteps CurrentLoginStep { get; set; } = LoginSteps.Main;
        public ServerListEntry[] Servers { get; private set; }
        public CityInfo[] Cities { get; set; }
        public string[] Characters { get; private set; }
        public string PopupMessage { get; set; }
        public byte ServerIndex { get; private set; }
        public static string Account { get; internal set; }
        public string Password { get; private set; }
        public bool CanAutologin => _autoLogin || Reconnect;


        public override void Load()
        {
            base.Load();

            Client.Game.Window.AllowUserResizing = false;

            _autoLogin = Settings.GlobalSettings.AutoLogin;

            UIManager.Add(new LoginBackground(_world));
            UIManager.Add(_currentGump = new LoginGump(_world, this));

            Client.Game.Audio.PlayMusic(Client.Game.Audio.LoginMusicIndex, false, true);

            if (CanAutologin && CurrentLoginStep != LoginSteps.Main || CUOEnviroment.SkipLoginScreen)
            {
                if (!string.IsNullOrEmpty(Settings.GlobalSettings.Username))
                {
                    // disable if it's the 2nd attempt
                    CUOEnviroment.SkipLoginScreen = false;
                    Connect(Settings.GlobalSettings.Username, Crypter.Decrypt(Settings.GlobalSettings.Password));
                }
            }

            if (Client.Game.IsWindowMaximized())
            {
                Client.Game.RestoreWindow();
            }

            Client.Game.SetWindowSize(640, 480);
        }


        public override void Unload()
        {
            if (IsDestroyed)
            {
                return;
            }

            Client.Game.Audio?.StopMusic();
            Client.Game.Audio?.StopSounds();

            UIManager.GetGump<LoginBackground>()?.Dispose();

            _currentGump?.Dispose();

            // UnRegistering Packet Events
            NetClient.Socket.Connected -= OnNetClientConnected;
            NetClient.Socket.Disconnected -= OnNetClientDisconnected;

            Client.Game.UO.GameCursor.IsLoading = false;
            base.Unload();
        }

        public override void Update()
        {
            base.Update();

            if (_lastLoginStep != CurrentLoginStep)
            {
                Client.Game.UO.GameCursor.IsLoading = false;

                // this trick avoid the flickering
                Gump g = _currentGump;
                UIManager.Add(_currentGump = GetGumpForStep());
                g.Dispose();

                _lastLoginStep = CurrentLoginStep;
            }

            if (Reconnect && (CurrentLoginStep == LoginSteps.PopUpMessage || CurrentLoginStep == LoginSteps.Main) && !NetClient.Socket.IsConnected)
            {
                if (_reconnectTime < Time.Ticks)
                {
                    if (!string.IsNullOrEmpty(Account))
                    {
                        Connect(Account, Crypter.Decrypt(Settings.GlobalSettings.Password));
                    }
                    else if (!string.IsNullOrEmpty(Settings.GlobalSettings.Username))
                    {
                        Connect(Settings.GlobalSettings.Username, Crypter.Decrypt(Settings.GlobalSettings.Password));
                    }

                    int timeT = Settings.GlobalSettings.ReconnectTime * 1000;

                    if (timeT < 1000)
                    {
                        timeT = 1000;
                    }

                    _reconnectTime = (long)Time.Ticks + timeT;
                    _reconnectTryCounter++;
                }
            }

            if ((CurrentLoginStep == LoginSteps.CharacterCreation || CurrentLoginStep == LoginSteps.CharacterSelection) && Time.Ticks > _pingTime)
            {
                // Note that this will not be an ICMP ping, so it's better that this *not* be affected by -no_server_ping.

                if (NetClient.Socket.IsConnected)
                {
                    NetClient.Socket.Statistics.SendPing();
                }

                _pingTime = Time.Ticks + 60000;
            }
        }

        private Gump GetGumpForStep()
        {
            foreach (Item item in _world.Items.Values)
            {
                _world.RemoveItem(item);
            }

            foreach (Mobile mobile in _world.Mobiles.Values)
            {
                _world.RemoveMobile(mobile);
            }

            _world.Mobiles.Clear();
            _world.Items.Clear();

            switch (CurrentLoginStep)
            {
                case LoginSteps.Main:
                    PopupMessage = null;

                    return new LoginGump(_world,this);

                case LoginSteps.Connecting:
                case LoginSteps.VerifyingAccount:
                case LoginSteps.LoginInToServer:
                case LoginSteps.EnteringBritania:
                case LoginSteps.PopUpMessage:
                case LoginSteps.CharacterCreationDone:
                    Client.Game.UO.GameCursor.IsLoading = CurrentLoginStep != LoginSteps.PopUpMessage;

                    return GetLoadingScreen();

                case LoginSteps.CharacterSelection: return new CharacterSelectionGump(_world);

                case LoginSteps.ServerSelection:
                    _pingTime = Time.Ticks + 60000; // reset ping timer

                    return new ServerSelectionGump(_world);

                case LoginSteps.CharacterCreation:
                    _pingTime = Time.Ticks + 60000; // reset ping timer

                    return new CharCreationGump(_world,this);
            }

            return null;
        }

        private LoadingGump GetLoadingScreen()
        {
            string labelText = "No Text";
            LoginButtons showButtons = LoginButtons.None;

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
                        labelText = ClilocLoader.Instance.GetString(3000002, ResGeneral.Connecting); // "Connecting..."

                        showButtons = LoginButtons.Cancel;

                        break;

                    case LoginSteps.VerifyingAccount:
                        labelText = ClilocLoader.Instance.GetString(3000003, ResGeneral.VerifyingAccount); // "Verifying Account..."

                        showButtons = LoginButtons.Cancel;

                        break;

                    case LoginSteps.LoginInToServer:
                        labelText = ClilocLoader.Instance.GetString(3000053, ResGeneral.LoggingIntoShard); // logging into shard

                        break;

                    case LoginSteps.EnteringBritania:
                        labelText = ClilocLoader.Instance.GetString(3000001, ResGeneral.EnteringBritannia); // Entering Britania...

                        break;

                    case LoginSteps.CharacterCreationDone:
                        labelText = ResGeneral.CreatingCharacter;

                        break;
                }
            }

            return new LoadingGump(_world, labelText, showButtons, OnLoadingGumpButtonClick);
        }

        private void OnLoadingGumpButtonClick(int buttonId)
        {
            LoginButtons butt = (LoginButtons) buttonId;

            if (butt == LoginButtons.OK || butt == LoginButtons.Cancel)
            {
                StepBack();
            }
        }

        public void Connect(string account, string password)
        {
            if (CurrentLoginStep == LoginSteps.Connecting)
            {
                return;
            }

            Account = account;
            Password = password;

            // Save credentials to config file
            if (Settings.GlobalSettings.SaveAccount)
            {
                Settings.GlobalSettings.Username = Account;
                Settings.GlobalSettings.Password = Crypter.Encrypt(Password);
                Settings.GlobalSettings.Save();
            }

            Log.Trace($"Start login to: {Settings.GlobalSettings.IP},{Settings.GlobalSettings.Port}");


            if (!Reconnect)
            {
                CurrentLoginStep = LoginSteps.Connecting;
            }

            //NetClient.LoginSocket.Disconnected += (o, e) => {
            //    PopupMessage = ResGeneral.CheckYourConnectionAndTryAgain;
            //    CurrentLoginStep = LoginSteps.PopUpMessage;
            //    Log.Error("No Internet Access");
            //};

            NetClient.Socket.Connected -= OnNetClientConnected;
            NetClient.Socket.Disconnected -= OnNetClientDisconnected;
            NetClient.Socket.Connected += OnNetClientConnected;
            NetClient.Socket.Disconnected += OnNetClientDisconnected;
            NetClient.Socket.Connect(Settings.GlobalSettings.IP, Settings.GlobalSettings.Port);
        }



        public int GetServerIndexByName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                for (int i = 0; i < Servers.Length; i++)
                {
                    if (Servers[i].Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public int GetServerIndexFromSettings()
        {
            string name = Settings.GlobalSettings.LastServerName;
            int index = GetServerIndexByName(name);

            if (index == -1)
            {
                index = Settings.GlobalSettings.LastServerNum;
            }

            if (index < 0 || index >= Servers.Length)
            {
                index = 0;
            }

            return index;
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
                Settings.GlobalSettings.LastServerName = Servers[ServerIndex].Name;
                Settings.GlobalSettings.Save();

                CurrentLoginStep = LoginSteps.LoginInToServer;

                _world.ServerName = Servers[ServerIndex].Name;

                NetClient.Socket.Send_SelectServer(index);
            }
        }

        public void SelectCharacter(uint index)
        {
            if (CurrentLoginStep == LoginSteps.CharacterSelection)
            {
                LastCharacterManager.Save(Account, _world.ServerName, Characters[index]);

                CurrentLoginStep = LoginSteps.EnteringBritania;
                NetClient.Socket.Send_SelectCharacter(index, Characters[index], NetClient.Socket.LocalIP);
            }
        }

        public void StartCharCreation()
        {
            if (CurrentLoginStep == LoginSteps.CharacterSelection)
            {
                CurrentLoginStep = LoginSteps.CharacterCreation;
            }
        }

        public void CreateCharacter(PlayerMobile character, int cityIndex, byte profession)
        {
            int i = 0;

            for (; i < Characters.Length; i++)
            {
                if (string.IsNullOrEmpty(Characters[i]))
                {
                    break;
                }
            }

            LastCharacterManager.Save(Account, _world.ServerName, character.Name);

            NetClient.Socket.Send_CreateCharacter(character,
                                                  cityIndex,
                                                  NetClient.Socket.LocalIP,
                                                  ServerIndex,
                                                  (uint)i,
                                                  profession);

            CurrentLoginStep = LoginSteps.CharacterCreationDone;
        }

        public void DeleteCharacter(uint index)
        {
            if (CurrentLoginStep == LoginSteps.CharacterSelection)
            {
                NetClient.Socket.Send_DeleteCharacter((byte)index, NetClient.Socket.LocalIP);
            }
        }

        public void StepBack()
        {
            PopupMessage = null;

            if (Characters != null && CurrentLoginStep != LoginSteps.CharacterCreation)
            {
                CurrentLoginStep = LoginSteps.LoginInToServer;
            }

            switch (CurrentLoginStep)
            {
                case LoginSteps.Connecting:
                case LoginSteps.VerifyingAccount:
                case LoginSteps.ServerSelection:
                    DisposeAllServerEntries();
                    CurrentLoginStep = LoginSteps.Main;
                    NetClient.Socket.Disconnect();

                    break;

                case LoginSteps.LoginInToServer:
                    NetClient.Socket.Disconnect();
                    Characters = null;
                    DisposeAllServerEntries();
                    Connect(Account, Password);

                    break;

                case LoginSteps.CharacterCreation:
                    CurrentLoginStep = LoginSteps.CharacterSelection;

                    break;

                case LoginSteps.PopUpMessage:
                case LoginSteps.CharacterSelection:
                    NetClient.Socket.Disconnect();
                    Characters = null;
                    DisposeAllServerEntries();
                    CurrentLoginStep = LoginSteps.Main;

                    break;
            }
        }

        public CityInfo GetCity(int index)
        {
            if (index < Cities.Length)
            {
                return Cities[index];
            }

            return null;
        }

        private void OnNetClientConnected(object sender, EventArgs e)
        {
            Log.Info("Connected!");
            CurrentLoginStep = LoginSteps.VerifyingAccount;

            uint address = NetClient.Socket.LocalIP;

            EncryptionHelper.Initialize(true, address, (ENCRYPTION_TYPE)Settings.GlobalSettings.Encryption);

            if (Client.Game.UO.Version >= ClientVersion.CV_6040)
            {
                uint clientVersion = (uint) Client.Game.UO.Version;

                byte major = (byte) (clientVersion >> 24);
                byte minor = (byte) (clientVersion >> 16);
                byte build = (byte) (clientVersion >> 8);
                byte extra = (byte) clientVersion;


                NetClient.Socket.Send_Seed(address, major, minor, build, extra);
            }
            else
            {
                NetClient.Socket.Send_Seed_Old(address);
            }

            NetClient.Socket.Send_FirstLogin(Account, Password);
        }

        private void OnNetClientDisconnected(object sender, SocketError e)
        {
            Log.Warn("Disconnected");

            if (CurrentLoginStep == LoginSteps.CharacterCreation)
            {
                return;
            }

            if (e != 0)
            {
                Characters = null;
                DisposeAllServerEntries();

                if (Settings.GlobalSettings.Reconnect)
                {
                    Reconnect = true;

                    PopupMessage = string.Format(ResGeneral.ReconnectPleaseWait01, _reconnectTryCounter, StringHelper.AddSpaceBeforeCapital(e.ToString()));

                    UIManager.GetGump<LoadingGump>()?.SetText(PopupMessage);
                }
                else
                {
                    PopupMessage = string.Format(ResGeneral.ConnectionLost0, StringHelper.AddSpaceBeforeCapital(e.ToString()));
                }

                CurrentLoginStep = LoginSteps.PopUpMessage;
            }
        }

        public void ServerListReceived(ref StackDataReader p)
        {
            byte flags = p.ReadUInt8();
            ushort count = p.ReadUInt16BE();
            DisposeAllServerEntries();
            Servers = new ServerListEntry[count];

            for (ushort i = 0; i < count; i++)
            {
                Servers[i] = ServerListEntry.Create(ref p);
            }

            CurrentLoginStep = LoginSteps.ServerSelection;

            if (CanAutologin)
            {
                if (Servers.Length != 0)
                {
                    int index = GetServerIndexFromSettings();

                    SelectServer((byte)Servers[index].Index);
                }
            }
        }

        public void UpdateCharacterList(ref StackDataReader p)
        {
            ParseCharacterList(ref p);

            if (CurrentLoginStep != LoginSteps.PopUpMessage)
            {
                PopupMessage = null;
            }
            CurrentLoginStep = LoginSteps.CharacterSelection;
            UIManager.GetGump<CharacterSelectionGump>()?.Dispose();

            _currentGump?.Dispose();

            UIManager.Add(_currentGump = new CharacterSelectionGump(_world));
            if (!string.IsNullOrWhiteSpace(PopupMessage))
            {
                Gump g = null;
                g = new LoadingGump(_world,PopupMessage, LoginButtons.OK, (but) => g.Dispose()) { IsModal = true };
                UIManager.Add(g);
                PopupMessage = null;
            }
        }

        public void ReceiveCharacterList(ref StackDataReader p)
        {
            ParseCharacterList(ref p);
            ParseCities(ref p);

            _world.ClientFeatures.SetFlags((CharacterListFlags) p.ReadUInt32BE());
            CurrentLoginStep = LoginSteps.CharacterSelection;

            uint charToSelect = 0;

            bool haveAnyCharacter = false;
            bool canLogin = CanAutologin;

            if (_autoLogin)
            {
                _autoLogin = false;
            }

            string lastCharName = LastCharacterManager.GetLastCharacter(Account, _world.ServerName);

            for (byte i = 0; i < Characters.Length; i++)
            {
                if (Characters[i].Length > 0)
                {
                    haveAnyCharacter = true;

                    if (Characters[i] == lastCharName)
                    {
                        charToSelect = i;

                        break;
                    }
                }
            }

            if (canLogin && haveAnyCharacter)
            {
                SelectCharacter(charToSelect);
            }
            else if (!haveAnyCharacter)
            {
                StartCharCreation();
            }
        }

        public void HandleErrorCode(ref StackDataReader p)
        {
            byte code = p.ReadUInt8();

            PopupMessage = ServerErrorMessages.GetError(p[0], code);
            CurrentLoginStep = LoginSteps.PopUpMessage;
        }

        public void HandleRelayServerPacket(ref StackDataReader p)
        {
            long ip = p.ReadUInt32LE(); // use LittleEndian here
            ushort port = p.ReadUInt16BE();
            uint seed = p.ReadUInt32BE();

            NetClient.Socket.Disconnect();
            // NOTE: i don't think i need to create a new socket wrapper anymore
            //NetClient.Socket = new NetClient();
            EncryptionHelper.Initialize(false, seed, (ENCRYPTION_TYPE) Settings.GlobalSettings.Encryption);

            NetClient.Socket.Connect(new IPAddress(ip).ToString(), port);

            if (NetClient.Socket.IsConnected)
            {
                NetClient.Socket.EnableCompression();
                unsafe
                {
                    Span<byte> b = stackalloc byte[4] { (byte)(seed >> 24), (byte)(seed >> 16), (byte)(seed >> 8), (byte)seed };
                    NetClient.Socket.Send(b, true, true);
                }

                NetClient.Socket.Send_SecondLogin(Account, Password, seed);
            }
        }


        private void ParseCharacterList(ref StackDataReader p)
        {
            int count = p.ReadUInt8();
            Characters = new string[count];

            for (ushort i = 0; i < count; i++)
            {
                Characters[i] = p.ReadASCII(30).TrimEnd('\0');

                p.Skip(30);
            }
        }

        private void ParseCities(ref StackDataReader p)
        {
            byte count = p.ReadUInt8();
            Cities = new CityInfo[count];

            bool isNew = Client.Game.UO.Version >= ClientVersion.CV_70130;
            string[] descriptions = null;

            if (!isNew)
            {
                descriptions = ReadCityTextFile(count);
            }

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
                    byte cityIndex = p.ReadUInt8();
                    string cityName = p.ReadASCII(32);
                    string cityBuilding = p.ReadASCII(32);
                    ushort cityX = (ushort) p.ReadUInt32BE();
                    ushort cityY = (ushort) p.ReadUInt32BE();
                    sbyte cityZ = (sbyte) p.ReadUInt32BE();
                    uint cityMapIndex = p.ReadUInt32BE();
                    uint cityDescription = p.ReadUInt32BE();
                    p.Skip(4);

                    cityInfo = new CityInfo
                    (
                        cityIndex,
                        cityName,
                        cityBuilding,
                        ClilocLoader.Instance.GetString((int) cityDescription),
                        cityX,
                        cityY,
                        cityZ,
                        cityMapIndex,
                        isNew
                    );
                }
                else
                {
                    byte cityIndex = p.ReadUInt8();
                    string cityName = p.ReadASCII(31);
                    string cityBuilding = p.ReadASCII(31);

                    cityInfo = new CityInfo
                    (
                        cityIndex,
                        cityName,
                        cityBuilding,
                        descriptions != null ? descriptions[i] : string.Empty,
                        (ushort) oldtowns[i % oldtowns.Length].X,
                        (ushort) oldtowns[i % oldtowns.Length].Y,
                        0,
                        0,
                        isNew
                    );
                }

                Cities[i] = cityInfo;
            }
        }

        private string[] ReadCityTextFile(int count)
        {
            string path = UOFileManager.GetUOFilePath("citytext.enu");

            if (!File.Exists(path))
            {
                return null;
            }

            string[] descr = new string[count];

            // TODO: stackalloc ?
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
                    {
                        break;
                    }

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
                            {
                                break;
                            }

                            int r1 = stream.Read(data, 0, 4);
                            stream.Position = pos;

                            if (r1 == -1)
                            {
                                break;
                            }

                            string dataText1 = Encoding.UTF8.GetString(data, 0, 4);

                            if (dataText1 == "END\0")
                            {
                                break;
                            }
                        }

                        if (descr.Length <= cityIndex)
                        {
                            break;
                        }

                        descr[cityIndex++] = text.ToString();
                    }
                    else
                    {
                        stream.Position -= 3;
                    }
                }
            }

            return descr;
        }

        private void DisposeAllServerEntries()
        {
            if (Servers != null)
            {
                for (int i = 0; i < Servers.Length; i++)
                {
                    if (Servers[i] != null)
                    {
                        Servers[i].Dispose();
                        Servers[i] = null;
                    }
                }

                Servers = null;
            }
        }
    }

    internal class ServerListEntry
    {
        private IPAddress _ipAddress;
        private IPAddress _ipAddressLittleEndian;
        private Ping _pinger = new Ping();
        private bool _sending;
        private readonly bool[] _last10Results = new bool[10];
        private int _resultIndex;

        private ServerListEntry()
        {
        }

        public static ServerListEntry Create(ref StackDataReader p)
        {
            ServerListEntry entry = new ServerListEntry()
            {
                Index = p.ReadUInt16BE(),
                Name = p.ReadASCII(32, true),
                PercentFull = p.ReadUInt8(),
                Timezone = p.ReadUInt8(),
                Address = p.ReadUInt32BE()
            };

            // some server sends invalid ip.
            try
            {
                entry._ipAddress = new IPAddress
                (
                    new byte[]
                    {
                        (byte) ((entry.Address >> 24) & 0xFF),
                        (byte) ((entry.Address >> 16) & 0xFF),
                        (byte) ((entry.Address >> 8) & 0xFF),
                        (byte) (entry.Address & 0xFF)
                    }
                );

                // IP address in little-endian format, required for server ping
                entry._ipAddressLittleEndian = new IPAddress
                (
                    new byte[]
                    {
                        (byte) (entry.Address & 0xFF),
                        (byte) ((entry.Address >> 8) & 0xFF),
                        (byte) ((entry.Address >> 16) & 0xFF),
                        (byte) ((entry.Address >> 24) & 0xFF)
                    }
                );
                
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }

            entry._pinger.PingCompleted += entry.PingerOnPingCompleted;

            return entry;
        }


        public uint Address;
        public ushort Index;
        public string Name;
        public byte PercentFull;
        public byte Timezone;
        public int Ping = -1;
        public int PacketLoss;
        public IPStatus PingStatus;

        private static byte[] _buffData = new byte[32];
        private static PingOptions _pingOptions = new PingOptions(64, true);

        public void DoPing()
        {
            if (_ipAddress != null && !_sending && _pinger != null)
            {
                if (_resultIndex >= _last10Results.Length)
                {
                    _resultIndex = 0;
                }

                try
                {
                    _pinger.SendAsync
                    (
                        _ipAddressLittleEndian,
                        1000,
                        _buffData,
                        _pingOptions,
                        _resultIndex++
                    );

                    _sending = true;
                }
                catch
                {
                    _ipAddress = null;
                    Dispose();
                }
            }
        }

        private void PingerOnPingCompleted(object sender, PingCompletedEventArgs e)
        {
            int index = (int) e.UserState;

            if (e.Reply != null)
            {
                Ping = (int) e.Reply.RoundtripTime;
                PingStatus = e.Reply.Status;

                _last10Results[index] = e.Reply.Status == IPStatus.Success;
            }

            //if (index >= _last10Results.Length - 1)
            {
                PacketLoss = 0;

                for (int i = 0; i < _resultIndex; i++)
                {
                    if (!_last10Results[i])
                    {
                        ++PacketLoss;
                    }
                }

                PacketLoss = (Math.Max(1, PacketLoss) / Math.Max(1, _resultIndex)) * 100;

                //_resultIndex = 0;
            }

            _sending = false;
        }

        public void Dispose()
        {
            if (_pinger != null)
            {
                _pinger.PingCompleted -= PingerOnPingCompleted;

                if (_sending)
                {
                    try
                    {
                        _pinger.SendAsyncCancel();
                    }
                    catch { }

                }

                _pinger.Dispose();
                _pinger = null;
            }
        }
    }

    internal class CityInfo
    {
        public CityInfo
        (
            int index,
            string city,
            string building,
            string description,
            ushort x,
            ushort y,
            sbyte z,
            uint map,
            bool isNew
        )
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

        public readonly string Building;
        public readonly string City;
        public readonly string Description;
        public readonly int Index;
        public readonly bool IsNewCity;
        public readonly uint Map;
        public readonly ushort X, Y;
        public readonly sbyte Z;
    }
}
