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
using System.Diagnostics;
using System.IO;
using System.Net;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Gumps;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.System;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO
{
    public class GameLoop : CoreGame
    {
        private RenderedText _infoText;
        private InputManager _inputManager;
        private JournalData _journalManager;
        private SpriteBatch3D _sb3D;
        private SpriteBatchUI _sbUI;
        private SceneManager _sceneManager;
        private UIManager _uiManager;


        protected override void Initialize()
        {
            //uncomment it and fill it to save your first settings
            //Settings settings1 = new Settings()
            //{

            //};

            //ConfigurationResolver.Save(settings1, "settings.json");

            Settings settings =
                ConfigurationResolver.Load<Settings>(Path.Combine(Bootstrap.ExeDirectory, "settings.json"));

            Service.Register(settings);

            Log.Message(LogTypes.Trace, "Checking for Ultima Online installation...");
            try
            {
                FileManager.UoFolderPath = settings.UltimaOnlineDirectory;
            }
            catch (FileNotFoundException e)
            {
                Log.Message(LogTypes.Error, "Wrong Ultima Online installation folder.");
                throw e;
            }

            Log.Message(LogTypes.Trace, "Done!");
            Log.Message(LogTypes.Trace, $"Ultima Online installation folder: {FileManager.UoFolderPath}");


            Log.Message(LogTypes.Trace, "Loading files...");
            Stopwatch stopwatch = Stopwatch.StartNew();
            FileManager.LoadFiles();

            uint[] hues = Hues.CreateShaderColors();
            Texture2D texture0 = new Texture2D(GraphicsDevice, 32, 3000);
            texture0.SetData(hues, 0, 32 * 3000);
            Texture2D texture1 = new Texture2D(GraphicsDevice, 32, 3000);
            texture1.SetData(hues, 32 * 3000, 32 * 3000);

            GraphicsDevice.Textures[1] = texture0;
            GraphicsDevice.Textures[2] = texture1;

            Log.Message(LogTypes.Trace, $"Files loaded in: {stopwatch.ElapsedMilliseconds} ms!");
            stopwatch.Stop();

            //Register Service Stack
            Service.Register(this);
            Service.Register(new SpriteBatch3D(this));
            Service.Register(new SpriteBatchUI(this));
            Service.Register(new InputManager());
            Service.Register(_uiManager = new UIManager());
            Service.Register(_sceneManager = new SceneManager());
            Service.Register(_journalManager = new JournalData());
            //Register Command Stack
            PartySystem.RegisterCommands();
            

            _inputManager = Service.Get<InputManager>();
            _sb3D = Service.Get<SpriteBatch3D>();
            _sbUI = Service.Get<SpriteBatchUI>();

            Log.Message(LogTypes.Trace, "Network calibration...");
            PacketHandlers.Load();
            PacketsTable.AdjustPacketSizeByVersion(FileManager.ClientVersion);
            Log.Message(LogTypes.Trace, "Done!");


            MaxFPS = settings.MaxFPS;

            _sceneManager.ChangeScene(ScenesType.Loading);

            _infoText = new RenderedText
            {
                IsUnicode = true,
                Font = 3,
                FontStyle = FontStyle.BlackBorder,
                Align = TEXT_ALIGN_TYPE.TS_LEFT,
                MaxWidth = 150
            };

            // ##### START TEST #####
            TEST(settings);
            // #####  END TEST  #####

            base.Initialize();
        }

        protected override void UnloadContent()
        {
            ConfigurationResolver.Save(Service.Get<Settings>(), "settings.json");

            base.UnloadContent();
        }

        private Action _secondConnection;
        private bool _doSecondConnection;

        private void TEST(Settings settings)
        {
            string[] parts = settings.ClientVersion.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
            byte[] clientVersionBuffer =
                {byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3])};

            NetClient.Connected += (sender, e) =>
            {
                Log.Message(LogTypes.Info, "Connected!");

                NetClient.LoginSocket.Send(new PSeed(NetClient.Socket.ClientAddress, clientVersionBuffer));
                NetClient.LoginSocket.Send(new PFirstLogin(settings.Username, settings.Password));
            };

            NetClient.Disconnected += (sender, e) => Log.Message(LogTypes.Warning, "Disconnected!");

            NetClient.PacketReceived += (sender, e) =>
            {
                switch (e.ID)
                {
                    case 0xA8:
                        NetClient.LoginSocket.Send(new PSelectServer(0));
                        break;
                    case 0x8C:
                        //NetClient.LoginSocket.EnableCompression();
                        e.Seek(0);
                        e.MoveToData();

                        byte[] ipbytes = new byte[4] {e.ReadByte(), e.ReadByte(), e.ReadByte(), e.ReadByte()};
                        ushort port = e.ReadUShort();
                        uint seed = e.ReadUInt();

                        //_secondConnection = () =>
                        //{

                        //};

                        NetClient.Socket.Connect(new IPAddress(ipbytes), port);
                        NetClient.Socket.EnableCompression();

                        NetClient.Socket.Send(new PSeed(seed, clientVersionBuffer));
                        NetClient.Socket.Send(new PSecondLogin(settings.Username, settings.Password, seed));

                        NetClient.LoginSocket.Disconnect();
                        _doSecondConnection = true;

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

            _doSecondConnection = false;
            NetClient.LoginSocket.Connect(settings.IP, settings.Port);
        }


        protected override void OnInputUpdate(double totalMS, double frameMS)
        {
            _inputManager.Update(totalMS, frameMS);
        }

        protected override void OnNetworkUpdate(double totalMS, double frameMS)
        {

            //if (_doSecondConnection)
            //{
            //    _secondConnection();

            //    _doSecondConnection = false;
            //}    

            NetClient.LoginSocket.Slice();
            NetClient.Socket.Slice();
        }

        protected override void OnUIUpdate(double totalMS, double frameMS)
        {
            _uiManager.Update(totalMS, frameMS);
        }

        protected override void OnUpdate(double totalMS, double frameMS)
        {
            if (World.InGame)
                _sceneManager.CurrentScene.Update(totalMS, frameMS);
        }

        protected override void OnFixedUpdate(double totalMS, double frameMS)
        {
            if (World.InGame)
                _sceneManager.CurrentScene.FixedUpdate(totalMS, frameMS);
        }

        protected override void OnDraw(double frameMS)
        {
            if (World.InGame)
                _sceneManager.CurrentScene.Draw(_sb3D, _sbUI);

            _sbUI.GraphicsDevice.Clear(Color.Transparent);
            _sbUI.Begin();

            _uiManager.Draw(_sbUI);


            _infoText.Text =
                $"FPS: {CurrentFPS}\nObjects: {_sceneManager.CurrentScene.RenderedObjectsCount}\nCalls: {_sb3D.Calls}\nMerged: {_sb3D.Merged}\nTotals: {_sb3D.TotalCalls}\nPos: {(World.Player == null ? "" : World.Player.Position.ToString())}\nSelected: {(_sceneManager.CurrentScene is GameScene gameScene && gameScene.SelectedObject != null ? gameScene.SelectedObject.ToString() : string.Empty)}";
            _infoText.Draw(_sbUI, new Vector3(Window.ClientBounds.Width - 150, 20, 0));

            _sbUI.End();
        }
    }
}