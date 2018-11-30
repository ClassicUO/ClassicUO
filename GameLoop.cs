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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Formatting;

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Gumps;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.System;
using ClassicUO.Game.Views;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO
{
    public class GameLoop : CoreGame
    {
        private const string FORMATTED_STRING = "FPS: {0}\nObjects: {1}\nCalls: {2}\nMerged: {3}\nFlush: {7}\nPos: {4}\nSelected: {5}\nStats: {6}";
        private readonly StringBuffer _buffer = new StringBuffer();
        private RenderedText _infoText;
        private InputManager _inputManager;
        private JournalData _journalManager;
        private SpriteBatch3D _sb3D;
        private SpriteBatchUI _sbUI;
        private SceneManager _sceneManager;
        private double _statisticsTimer;
        private UIManager _uiManager;

        protected override void Initialize()
        {
            Settings settings = ConfigurationResolver.Load<Settings>(Path.Combine(Bootstrap.ExeDirectory, "settings.json"));

            if (settings == null)
            {
                Log.Message(LogTypes.Trace, "settings.json file was not found creating default");
                settings = new Settings();
                ConfigurationResolver.Save(settings, "settings.json");
                Process.Start("notepad.exe", "settings.json");
                Exit();
                return;
            }

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
            Texture2D texture0 = new Texture2D(GraphicsDevice, 32, Hues.HuesCount);
            texture0.SetData(hues, 0, 32 * Hues.HuesCount);
            Texture2D texture1 = new Texture2D(GraphicsDevice, 32, Hues.HuesCount);
            texture1.SetData(hues, 32 * Hues.HuesCount, 32 * Hues.HuesCount);
            GraphicsDevice.Textures[1] = texture0;
            GraphicsDevice.Textures[2] = texture1;
            Log.Message(LogTypes.Trace, $"Files loaded in: {stopwatch.ElapsedMilliseconds} ms!");
            stopwatch.Stop();

            //Register Service Stack
            Service.Register(this);
            Service.Register(_sb3D = new SpriteBatch3D(GraphicsDevice));
            Service.Register(_sbUI = new SpriteBatchUI(GraphicsDevice));
            Service.Register(new InputManager());
            Service.Register(_uiManager = new UIManager());
            Service.Register(_sceneManager = new SceneManager());
            Service.Register(_journalManager = new JournalData());

            //Register Command Stack
            Commands.Initialize();
            _inputManager = Service.Get<InputManager>();
            Log.Message(LogTypes.Trace, "Network calibration...");
            PacketHandlers.Load();
            PacketsTable.AdjustPacketSizeByVersion(FileManager.ClientVersion);
            Log.Message(LogTypes.Trace, "Done!");
            MaxFPS = settings.MaxFPS;

            _infoText = new RenderedText
            {
                IsUnicode = true,
                Font = 3,
                FontStyle = FontStyle.BlackBorder,
                Align = TEXT_ALIGN_TYPE.TS_LEFT,
                MaxWidth = 150
            };
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _sceneManager.ChangeScene(ScenesType.Login);
            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            Service.Get<SceneManager>().CurrentScene?.Unload();
            Service.Get<Settings>().Save();
            base.UnloadContent();
        }

        protected override void OnInputUpdate(double totalMS, double frameMS)
        {
            Mouse.Update();
        }

        protected override void OnNetworkUpdate(double totalMS, double frameMS)
        {
            if (NetClient.LoginSocket.IsDisposed && NetClient.LoginSocket.IsConnected)
                NetClient.LoginSocket.Disconnect();
            else if (!NetClient.Socket.IsConnected)
            {
                NetClient.LoginSocket.Update();
                UpdateSockeStats(NetClient.LoginSocket, totalMS);
            }
            else if (!NetClient.Socket.IsDisposed)
            {
                NetClient.Socket.Update();
                UpdateSockeStats(NetClient.Socket, totalMS);
            }
        }

        private void UpdateSockeStats(NetClient socket, double totalMS)
        {
            if (_statisticsTimer < totalMS)
            {
                socket.Statistics.Update();
                _statisticsTimer = totalMS + 500;
            }
        }

        protected override void OnUIUpdate(double totalMS, double frameMS)
        {
            _uiManager.Update(totalMS, frameMS);
        }

        protected override void OnUpdate(double totalMS, double frameMS)
        {
            _sceneManager.CurrentScene.Update(totalMS, frameMS);
        }

        protected override void OnFixedUpdate(double totalMS, double frameMS)
        {
            _sceneManager.CurrentScene.FixedUpdate(totalMS, frameMS);
        }


        protected override void OnDraw(double frameMS)
        {      
            _sceneManager.CurrentScene.Draw(_sb3D, _sbUI);
            _sbUI.GraphicsDevice.Clear(Color.Transparent);
            _sbUI.Begin();        
            _uiManager.Draw(_sbUI);
            
            _buffer.Clear();
            _buffer.AppendFormat(FORMATTED_STRING, CurrentFPS, _sceneManager.CurrentScene.RenderedObjectsCount, _sb3D.Calls, _sb3D.Merged, World.Player == null ? string.Empty : World.Player.Position.ToString(), _sceneManager.CurrentScene is GameScene gameScene && gameScene.SelectedObject != null ? gameScene.SelectedObject.ToString() : string.Empty, string.Empty, _sb3D.FlushCount + _sbUI.FlushCount);
            _infoText.Text = _buffer.ToString();
            _infoText.Draw(_sbUI, new Point(Window.ClientBounds.Width - 150, 20));
            _sbUI.End();
        }

        public void Quit()
        {
            Exit();
        }
    }
}