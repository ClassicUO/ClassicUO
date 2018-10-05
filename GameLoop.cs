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
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Gumps;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO
{
    public class GameLoop : CoreGame
    {
        private UIManager _uiManager;
        private InputManager _inputManager;
        private SceneManager _sceneManager;
        private JournalData _journalManager;
        private SpriteBatch3D _sb3D;
        private SpriteBatchUI _sbUI;

        private readonly StringBuilder _sb = new StringBuilder();
        private RenderedText _infoText;


        protected override void Initialize()
        {

            //uncomment it and fill it to save your first settings
            //Settings settings1 = new Settings()
            //{

            //};

            //ConfigurationResolver.Save(settings1, "settings.json");

            Settings settings =
                ConfigurationResolver.Load<Settings>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json"));

            Service.Register(settings);

            Log.Message(LogTypes.Trace, "Checking for Ultima Online installation...", false);
            try
            {
                FileManager.UoFolderPath = settings.UltimaOnlineDirectory;
            }
            catch (FileNotFoundException)
            {
                Log.Message(LogTypes.None, string.Empty);
                Log.Message(LogTypes.Error, "Wrong Ultima Online installation folder.");
                return;
            }

            Log.Message(LogTypes.None, "      Done!");
            Log.Message(LogTypes.Trace, $"Ultima Online installation folder: {FileManager.UoFolderPath}");


            Log.Message(LogTypes.Trace, "Loading files...", false);
            Stopwatch stopwatch = Stopwatch.StartNew();
            FileManager.LoadFiles();

            uint[] hues = Hues.CreateShaderColors();
            Texture2D texture0 = new Texture2D(GraphicsDevice, 32, 3000);
            texture0.SetData(hues, 0, 32 * 3000);
            Texture2D texture1 = new Texture2D(GraphicsDevice, 32, 3000);
            texture1.SetData(hues, 32 * 3000, 32 * 3000);

            GraphicsDevice.Textures[1] = texture0;
            GraphicsDevice.Textures[2] = texture1;

            Log.Message(LogTypes.None, $"     Done in: {stopwatch.ElapsedMilliseconds} ms!");
            stopwatch.Stop();

            Service.Register(_uiManager = new UIManager());
            Service.Register(_sceneManager = new SceneManager());
            Service.Register(_journalManager = new JournalData());

            _inputManager = Service.Get<InputManager>();
            _sb3D = Service.Get<SpriteBatch3D>();
            _sbUI = Service.Get<SpriteBatchUI>();

            Log.Message(LogTypes.Trace, "Network calibration...", false);
            PacketHandlers.Load();
            PacketsTable.AdjustPacketSizeByVersion(FileManager.ClientVersion);
            Log.Message(LogTypes.None, "      Done!");


            MaxFPS = settings.MaxFPS;

            _sceneManager.ChangeScene(ScenesType.Loading);

            _infoText = new RenderedText
            {
                IsUnicode = true,
                Font = 3,
                FontStyle = FontStyle.BlackBorder,
                Align = TEXT_ALIGN_TYPE.TS_LEFT
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

        private void TEST(Settings settings)
        {
            string[] parts = settings.ClientVersion.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
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


        protected override void OnInputUpdate(double totalMS, double frameMS)
        {
            _inputManager.Update(totalMS, frameMS);
        }

        protected override void OnNetworkUpdate(double totalMS, double frameMS)
        {
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


            _sb.Clear();
            _sb.AppendLine("");
            _sb.Append("FPS: ");
            _sb.AppendLine(CurrentFPS.ToString());
            _sb.Append("Objects: ");
            _sb.AppendLine(_sceneManager.CurrentScene.RenderedObjectsCount.ToString());
            _sb.Append("Calls: ");
            _sb.AppendLine(_sb3D.Calls.ToString());
            _sb.Append("Merged: ");
            _sb.AppendLine(_sb3D.Merged.ToString());
            _sb.Append("Totals: ");
            _sb.AppendLine(_sb3D.TotalCalls.ToString());
            _sb.Append("Pos: ");
            _sb.AppendLine(World.Player == null ? "" : World.Player.Position.ToString());
            _sb.Append("Selected: ");

            if (_sceneManager.CurrentScene is GameScene gameScene)
                _sb.AppendLine(gameScene.SelectedObject == null ? "" : gameScene.SelectedObject.ToString());

            _infoText.Text = _sb.ToString();
            _infoText.Draw(_sbUI, new Vector3( /*Window.ClientBounds.Width - 150*/ 20, 20, 0));

            _sbUI.End();
        }
    }
}