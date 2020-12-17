#region license

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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Gumps.CharCreation;
using ClassicUO.Game.UI.Gumps.Login;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
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


        public LoginScene() : base((int) SceneType.Login, false, false, true)
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
            UIManager.Add(_currentGump = new LoginGump(this));

            // Registering Packet Events
            //NetClient.PacketReceived += NetClient_PacketReceived;
            NetClient.Socket.Disconnected += NetClient_Disconnected;
            NetClient.LoginSocket.Connected += NetClient_Connected;
            NetClient.LoginSocket.Disconnected += Login_NetClient_Disconnected;

            int music = Client.Version >= ClientVersion.CV_7000 ? 78 : Client.Version > ClientVersion.CV_308Z ? 0 : 8;

            Audio.PlayMusic(music, false, true);

            if ((Settings.GlobalSettings.AutoLogin || Reconnect) && CurrentLoginStep != LoginSteps.Main ||
                CUOEnviroment.SkipLoginScreen)
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
            Audio.StopMusic();

            UIManager.GetGump<LoginBackground>()?.Dispose();

            _currentGump?.Dispose();

            // UnRegistering Packet Events           
            // NetClient.Socket.Connected -= NetClient_Connected;
            NetClient.Socket.Disconnected -= NetClient_Disconnected;
            NetClient.LoginSocket.Connected -= NetClient_Connected;
            NetClient.LoginSocket.Disconnected -= Login_NetClient_Disconnected;
            //NetClient.PacketReceived -= NetClient_PacketReceived;

            UIManager.GameCursor.IsLoading = false;
            base.Unload();
        }

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

            if (_lastLoginStep != CurrentLoginStep)
            {
                UIManager.GameCursor.IsLoading = false;

                // this trick avoid the flickering
                Gump g = _currentGump;
                UIManager.Add(_currentGump = GetGumpForStep());
                g.Dispose();

                _lastLoginStep = CurrentLoginStep;
            }

            if (Reconnect && (CurrentLoginStep == LoginSteps.PopUpMessage || CurrentLoginStep == LoginSteps.Main) && !NetClient.Socket.IsConnected && !NetClient.LoginSocket.IsConnected)
            {
                if (_reconnectTime < totalTime)
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

                    _reconnectTime = (long) totalTime + timeT;
                    _reconnectTryCounter++;
                }
            }

            if (CurrentLoginStep == LoginSteps.CharacterCreation && Time.Ticks > _pingTime)
            {
                if (NetClient.Socket != null && NetClient.Socket.IsConnected)
                {
                    NetClient.Socket.Statistics.SendPing();
                }
                else if (NetClient.LoginSocket != null && NetClient.LoginSocket.IsConnected)
                {
                    NetClient.LoginSocket.Statistics.SendPing();
                }

                _pingTime = Time.Ticks + 60000;
            }
        }

        private Gump GetGumpForStep()
        {
            foreach (Item item in World.Items)
            {
                World.RemoveItem(item);
            }

            foreach (Mobile mobile in World.Mobiles)
            {
                World.RemoveMobile(mobile);
            }

            World.Mobiles.Clear();
            World.Items.Clear();

            switch (CurrentLoginStep)
            {
                case LoginSteps.Main:
                    PopupMessage = null;

                    return new LoginGump(this);

                case LoginSteps.Connecting:
                case LoginSteps.VerifyingAccount:
                case LoginSteps.LoginInToServer:
                case LoginSteps.EnteringBritania:
                case LoginSteps.PopUpMessage:
                case LoginSteps.CharacterCreationDone:
                    UIManager.GameCursor.IsLoading = CurrentLoginStep != LoginSteps.PopUpMessage;

                    return GetLoadingScreen();

                case LoginSteps.CharacterSelection: return new CharacterSelectionGump();

                case LoginSteps.ServerSelection:
                    _pingTime = Time.Ticks + 60000; // reset ping timer

                    return new ServerSelectionGump();

                case LoginSteps.CharacterCreation:
                    _pingTime = Time.Ticks + 60000; // reset ping timer

                    return new CharCreationGump(this);
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

                        break;

                    case LoginSteps.VerifyingAccount:
                        labelText = ClilocLoader.Instance.GetString
                            (3000003, ResGeneral.VerifyingAccount); // "Verifying Account..."

                        showButtons = LoginButtons.Cancel;

                        break;

                    case LoginSteps.LoginInToServer:
                        labelText = ClilocLoader.Instance.GetString
                            (3000053, ResGeneral.LoggingIntoShard); // logging into shard

                        break;

                    case LoginSteps.EnteringBritania:
                        labelText = ClilocLoader.Instance.GetString
                            (3000001, ResGeneral.EnteringBritannia); // Entering Britania...

                        break;

                    case LoginSteps.CharacterCreationDone:
                        labelText = ResGeneral.CreatingCharacter;

                        break;
                }
            }

            return new LoadingGump(labelText, showButtons, OnLoadingGumpButtonClick);
        }

        private void OnLoadingGumpButtonClick(int buttonId)
        {
            LoginButtons butt = (LoginButtons) buttonId;

            if (butt == LoginButtons.OK || butt == LoginButtons.Cancel)
            {
                StepBack();
            }
        }

        public async void Connect(string account, string password)
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

            EncryptionHelper.Initialize(true, NetClient.ClientAddress, (ENCRYPTION_TYPE) Settings.GlobalSettings.Encryption);

            if (!await NetClient.LoginSocket.Connect(Settings.GlobalSettings.IP, Settings.GlobalSettings.Port))
            {
                PopupMessage = ResGeneral.CheckYourConnectionAndTryAgain;
                CurrentLoginStep = LoginSteps.PopUpMessage;
                Log.Error("No Internet Access");
            }
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
                NetClient.Socket.Send(new PSelectCharacter(index, Characters[index], NetClient.ClientAddress));
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

            Settings.GlobalSettings.LastCharacterName = character.Name;

            NetClient.Socket.Send
            (
                new PCreateCharacter(character, cityIndex, NetClient.ClientAddress, ServerIndex, (uint) i, profession)
            );

            CurrentLoginStep = LoginSteps.CharacterCreationDone;
        }

        public void DeleteCharacter(uint index)
        {
            if (CurrentLoginStep == LoginSteps.CharacterSelection)
            {
                NetClient.Socket.Send(new PDeleteCharacter((byte) index, NetClient.ClientAddress));
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
                    NetClient.LoginSocket.Disconnect();

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
                    NetClient.LoginSocket.Disconnect();
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

        private void NetClient_Connected(object sender, EventArgs e)
        {
            Log.Info("Connected!");
            CurrentLoginStep = LoginSteps.VerifyingAccount;

            if (Client.Version >= ClientVersion.CV_6040)
            {
                uint clientVersion = (uint) Client.Version;

                byte major = (byte) (clientVersion >> 24);
                byte minor = (byte) (clientVersion >> 16);
                byte build = (byte) (clientVersion >> 8);
                byte extra = (byte) clientVersion;

                PSeed packet = new PSeed(NetClient.ClientAddress, major, minor, build, extra);

                NetClient.LoginSocket.Send(packet.ToArray(), packet.Length, true, true);
            }
            else
            {
                uint address = NetClient.ClientAddress;

                // TODO: stackalloc
                byte[] packet = new byte[4];
                packet[0] = (byte) (address >> 24);
                packet[1] = (byte) (address >> 16);
                packet[2] = (byte) (address >> 8);
                packet[3] = (byte) address;

                NetClient.LoginSocket.Send(packet, packet.Length, true, true);
            }

            NetClient.LoginSocket.Send(new PFirstLogin(Account, Password));
        }

        private void NetClient_Disconnected(object sender, SocketError e)
        {
            Log.Warn("Disconnected (game socket)!");

            if (CurrentLoginStep == LoginSteps.CharacterCreation)
            {
                return;
            }

            Characters = null;
            DisposeAllServerEntries();
            PopupMessage = string.Format(ResGeneral.ConnectionLost0, StringHelper.AddSpaceBeforeCapital(e.ToString()));
            CurrentLoginStep = LoginSteps.PopUpMessage;
        }

        private void Login_NetClient_Disconnected(object sender, SocketError e)
        {
            Log.Warn("Disconnected (login socket)!");

            if (e > 0)
            {
                Characters = null;
                DisposeAllServerEntries();

                if (Settings.GlobalSettings.Reconnect)
                {
                    Reconnect = true;

                    PopupMessage = string.Format
                    (
                        ResGeneral.ReconnectPleaseWait01, _reconnectTryCounter,
                        StringHelper.AddSpaceBeforeCapital(e.ToString())
                    );

                    UIManager.GetGump<LoadingGump>()?.SetText(PopupMessage);
                }
                else
                {
                    PopupMessage = string.Format
                        (ResGeneral.ConnectionLost0, StringHelper.AddSpaceBeforeCapital(e.ToString()));
                }

                CurrentLoginStep = LoginSteps.PopUpMessage;
            }
        }

        public void ServerListReceived(ref PacketBufferReader p)
        {
            byte flags = p.ReadByte();
            ushort count = p.ReadUShort();
            DisposeAllServerEntries();
            Servers = new ServerListEntry[count];

            for (ushort i = 0; i < count; i++)
            {
                Servers[i] = ServerListEntry.Create(ref p);
            }

            CurrentLoginStep = LoginSteps.ServerSelection;

            if (Settings.GlobalSettings.AutoLogin || Reconnect)
            {
                if (Servers.Length != 0)
                {
                    int index = Settings.GlobalSettings.LastServerNum;

                    if (index <= 0 || index > Servers.Length)
                    {
                        Log.Warn($"Wrong server index: {index}");
                        index = 1;
                    }

                    SelectServer((byte) Servers[index - 1].Index);
                }
            }
        }

        public void UpdateCharacterList(ref PacketBufferReader p)
        {
            ParseCharacterList(ref p);

            CurrentLoginStep = LoginSteps.CharacterSelection;
            UIManager.GetGump<CharacterSelectionGump>()?.Dispose();

            _currentGump?.Dispose();

            UIManager.Add(_currentGump = new CharacterSelectionGump());

        }

        public void ReceiveCharacterList(ref PacketBufferReader p)
        {
            ParseCharacterList(ref p);
            ParseCities(ref p);

            World.ClientFeatures.SetFlags((CharacterListFlags) p.ReadUInt());
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
            {
                SelectCharacter(charToSelect);
            }
            else if (!haveAnyCharacter)
            {
                StartCharCreation();
            }
        }

        public void HandleErrorCode(ref PacketBufferReader p)
        {
            byte code = p.ReadByte();

            PopupMessage = ServerErrorMessages.GetError(p.ID, code);
            CurrentLoginStep = LoginSteps.PopUpMessage;
        }

        public void HandleRelayServerPacket(ref PacketBufferReader p)
        {
            byte[] ip =
            {
                p.ReadByte(), p.ReadByte(), p.ReadByte(), p.ReadByte()
            };

            ushort port = p.ReadUShort();
            uint seed = p.ReadUInt();
            NetClient.LoginSocket.Disconnect();
            EncryptionHelper.Initialize(false, seed, (ENCRYPTION_TYPE) Settings.GlobalSettings.Encryption);

            NetClient.Socket
                     .Connect(new IPAddress(ip), port)
                     .ContinueWith(
                t =>
                        {
                            if (!t.IsFaulted)
                            {
                                NetClient.Socket.EnableCompression();
                                // TODO: stackalloc
                                byte[] ss = new byte[4] { (byte)(seed >> 24), (byte)(seed >> 16), (byte)(seed >> 8), (byte)seed };
                                NetClient.Socket.Send(ss, 4, true, true);
                                NetClient.Socket.Send(new PSecondLogin(Account, Password, seed));
                            }
                        }, TaskContinuationOptions.ExecuteSynchronously);
        }


        private void ParseCharacterList(ref PacketBufferReader p)
        {
            int count = p.ReadByte();
            Characters = new string[count];

            for (ushort i = 0; i < count; i++)
            {
                Characters[i] = p.ReadASCII(30).TrimEnd('\0');

                p.Skip(30);
            }
        }

        private void ParseCities(ref PacketBufferReader p)
        {
            byte count = p.ReadByte();
            Cities = new CityInfo[count];

            bool isNew = Client.Version >= ClientVersion.CV_70130;
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
                    byte cityIndex = p.ReadByte();
                    string cityName = p.ReadASCII(32);
                    string cityBuilding = p.ReadASCII(32);
                    ushort cityX = (ushort) p.ReadUInt();
                    ushort cityY = (ushort) p.ReadUInt();
                    sbyte cityZ = (sbyte) p.ReadUInt();
                    uint cityMapIndex = p.ReadUInt();
                    uint cityDescription = p.ReadUInt();
                    p.Skip(4);

                    cityInfo = new CityInfo
                    (
                        cityIndex, cityName, cityBuilding, ClilocLoader.Instance.GetString((int) cityDescription),
                        cityX, cityY, cityZ, cityMapIndex, isNew
                    );
                }
                else
                {
                    byte cityIndex = p.ReadByte();
                    string cityName = p.ReadASCII(31);
                    string cityBuilding = p.ReadASCII(31);

                    cityInfo = new CityInfo
                    (
                        cityIndex, cityName, cityBuilding, descriptions != null ? descriptions[i] : string.Empty,
                        (ushort) oldtowns[i].X, (ushort) oldtowns[i].Y, 0, 0, isNew
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
        private readonly Ping _pinger = new Ping();
        private bool _sending;
        private readonly bool[] _last10Results = new bool[10];
        private int _resultIndex;

        private ServerListEntry()
        {
          
        }

        public static ServerListEntry Create(ref PacketBufferReader p)
        {
            ServerListEntry entry = new ServerListEntry()
            {
                Index = p.ReadUShort(),
                Name = p.ReadASCII(32).MakeSafe(),
                PercentFull = p.ReadByte(),
                Timezone = p.ReadByte(),
                Address = p.ReadUInt()
            };

            // some server sends invalid ip.
            try
            {
                entry._ipAddress = new IPAddress(new byte[] {
                    (byte)((entry.Address>>24) & 0xFF) ,
                    (byte)((entry.Address>>16) & 0xFF) ,
                    (byte)((entry.Address>>8)  & 0xFF) ,
                    (byte)( entry.Address & 0xFF)});
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
            if (_ipAddress != null && !_sending)
            {
                if (_resultIndex >= _last10Results.Length)
                {
                    _resultIndex = 0;
                }

                _sending = true;
                _pinger.SendAsync(_ipAddress, 1000, _buffData, _pingOptions, _resultIndex++);
            }
        }

        private void PingerOnPingCompleted(object sender, PingCompletedEventArgs e)
        {
            int index = (int)e.UserState;

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
                    _pinger.SendAsyncCancel();
                }

                _pinger.Dispose();
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