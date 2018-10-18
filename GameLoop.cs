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
            Service.Register(new SpriteBatch3D(GraphicsDevice));
            Service.Register(new SpriteBatchUI(GraphicsDevice));
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

            _sceneManager.ChangeScene(ScenesType.Login);

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

        protected override void UnloadContent()
        {
            ConfigurationResolver.Save(Service.Get<Settings>(), "settings.json");

            base.UnloadContent();
        }
        
        protected override void OnInputUpdate(double totalMS, double frameMS)
        {
            _inputManager.Update(totalMS, frameMS);
        }

        protected override void OnNetworkUpdate(double totalMS, double frameMS)
        {
            if (NetClient.LoginSocket.IsDisposed && NetClient.LoginSocket.IsConnected)
            {
                NetClient.LoginSocket.Disconnect();
            }
            else if (!NetClient.Socket.IsConnected)
                NetClient.LoginSocket.Update();
            else if (!NetClient.Socket.IsDisposed)
                NetClient.Socket.Update();
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
                $"FPS: {CurrentFPS}\nObjects: {_sceneManager.CurrentScene.RenderedObjectsCount}\nCalls: {_sb3D.Calls}\nMerged: {_sb3D.Merged}\nPos: {(World.Player == null ? "" : World.Player.Position.ToString())}\nSelected: {(_sceneManager.CurrentScene is GameScene gameScene && gameScene.SelectedObject != null ? gameScene.SelectedObject.ToString() : string.Empty)}";
            _infoText.Draw(_sbUI, new Vector3(Window.ClientBounds.Width - 150, 20, 0));

            _sbUI.End();
        }
    }
}