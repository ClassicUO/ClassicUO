// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Gumps.CharCreation;
using ClassicUO.Game.UI.Gumps.Login;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using SDL3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

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
        public (int min, int max) LoginDelay { get; private set; }


        public override void Load()
        {
            base.Load();

            SubscribeLoginEvents();

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

            int width = Client.Game.ScaleWithDpi(640);
            int height = Client.Game.ScaleWithDpi(480);
            SDL.SDL_SetWindowMinimumSize(Client.Game.Window.Handle, width, height);
            Client.Game.SetWindowSize(width, height);
        }


        public override void Unload()
        {
            if (IsDestroyed)
            {
                return;
            }

            UnsubscribeLoginEvents();

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

        private void SubscribeLoginEvents()
        {
            EventSink.ServerListReceived += OnServerListReceived;
            EventSink.ServerRelayReceived += OnServerRelayReceived;
            EventSink.CharacterListUpdated += OnCharacterListUpdated;
            EventSink.CharacterListReceived += OnCharacterListReceived;
            EventSink.LoginDelayReceived += OnLoginDelayReceived;
            EventSink.LoginRejected += OnLoginRejected;
            EventSink.LoginCompleted += OnLoginCompleted;
        }

        private void UnsubscribeLoginEvents()
        {
            EventSink.ServerListReceived -= OnServerListReceived;
            EventSink.ServerRelayReceived -= OnServerRelayReceived;
            EventSink.CharacterListUpdated -= OnCharacterListUpdated;
            EventSink.CharacterListReceived -= OnCharacterListReceived;
            EventSink.LoginDelayReceived -= OnLoginDelayReceived;
            EventSink.LoginRejected -= OnLoginRejected;
            EventSink.LoginCompleted -= OnLoginCompleted;
        }

        private void OnServerListReceived(ServerListReceivedArgs e)
        {
            DisposeAllServerEntries();
            Servers = new ServerListEntry[e.Servers.Count];

            for (int i = 0; i < e.Servers.Count; i++)
            {
                Servers[i] = e.Servers[i];
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

        private void OnServerRelayReceived(ServerRelayReceivedArgs e)
        {
            NetClient.Socket.Disconnect();
            NetClient.Socket.Connected -= OnNetClientConnected;

            try
            {
                // Ignore the packet, connect with the original IP regardless (i.e. websocket proxying)
                if (Settings.GlobalSettings.IgnoreRelayIp || e.Ip == 0)
                {
                    Log.Trace("Ignoring relay server packet IP address");
                    NetClient.Socket.Connect(Settings.GlobalSettings.IP, Settings.GlobalSettings.Port);
                }
                else
                    NetClient.Socket.Connect(new IPAddress(e.Ip).ToString(), e.Port);

                if (NetClient.Socket.IsConnected)
                {
                    uint seed = e.Seed;
                    NetClient.Socket.Encryption?.Initialize(false, seed);
                    NetClient.Socket.EnableCompression();
                    unsafe
                    {
                        Span<byte> b = stackalloc byte[4] { (byte)(seed >> 24), (byte)(seed >> 16), (byte)(seed >> 8), (byte)seed };
                        NetClient.Socket.Send(b, true, true);
                    }

                    NetClient.Socket.Send_SecondLogin(Account, Password, seed);
                }
            }
            finally
            {
                NetClient.Socket.Connected += OnNetClientConnected;
            }
        }

        private void OnCharacterListUpdated(CharacterListUpdatedArgs e)
        {
            Characters = new string[e.Characters.Count];
            for (int i = 0; i < e.Characters.Count; i++)
            {
                Characters[i] = e.Characters[i];
            }

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

        private void OnCharacterListReceived(CharacterListReceivedArgs e)
        {
            Characters = new string[e.Characters.Count];
            for (int i = 0; i < e.Characters.Count; i++)
            {
                Characters[i] = e.Characters[i];
            }

            Cities = new CityInfo[e.Cities.Count];
            for (int i = 0; i < e.Cities.Count; i++)
            {
                Cities[i] = e.Cities[i];
            }

            _world.ClientFeatures.SetFlags((CharacterListFlags) e.ClientFlags);
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

        private void OnLoginDelayReceived(LoginDelayReceivedArgs e)
        {
            LoginDelay = ((e.Delay - 1) * 10, e.Delay * 10);
        }

        private void OnLoginRejected(LoginRejectedArgs e)
        {
            PopupMessage = ServerErrorMessages.GetError(e.PacketId, e.Reason, LoginDelay);
            CurrentLoginStep = LoginSteps.PopUpMessage;
            LoginDelay = default;
        }

        private void OnLoginCompleted(LoginCompletedArgs e)
        {
            // Mirrors the original 0x55 LoginComplete handler tail: now owned by
            // the LoginScene subscriber so the packet handler stays parse-and-emit-only.
            if (_world.Player == null || Client.Game.Scene is not LoginScene)
            {
                return;
            }

            var scene = new GameScene(_world);
            Client.Game.SetScene(scene);

            //GameActions.OpenPaperdoll(_world.Player);
            GameActions.RequestMobileStatus(_world, _world.Player);
            NetClient.Socket.Send_OpenChat("");

            NetClient.Socket.Send_SkillsRequest(_world.Player);
            scene.DoubleClickDelayed(_world.Player);

            if (Client.Game.UO.Version >= ClientVersion.CV_306E)
            {
                NetClient.Socket.Send_ClientType();
            }

            if (Client.Game.UO.Version >= ClientVersion.CV_305D)
            {
                NetClient.Socket.Send_ClientViewRange(_world.ClientViewRange);
            }

            List<Gump> gumps = ProfileManager.CurrentProfile.ReadGumps(
                _world,
                ProfileManager.ProfilePath
            );

            if (gumps != null)
            {
                foreach (Gump gump in gumps)
                {
                    UIManager.Add(gump);
                }
            }
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
                        labelText = Client.Game.UO.FileManager.Clilocs.GetString(3000002, ResGeneral.Connecting); // "Connecting..."

                        showButtons = LoginButtons.Cancel;

                        break;

                    case LoginSteps.VerifyingAccount:
                        labelText = Client.Game.UO.FileManager.Clilocs.GetString(3000003, ResGeneral.VerifyingAccount); // "Verifying Account..."

                        showButtons = LoginButtons.Cancel;

                        break;

                    case LoginSteps.LoginInToServer:
                        labelText = Client.Game.UO.FileManager.Clilocs.GetString(3000053, ResGeneral.LoggingIntoShard); // logging into shard

                        break;

                    case LoginSteps.EnteringBritania:
                        labelText = Client.Game.UO.FileManager.Clilocs.GetString(3000001, ResGeneral.EnteringBritannia); // Entering Britania...

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

            NetClient.Socket.Encryption?.Initialize(true, address);

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
