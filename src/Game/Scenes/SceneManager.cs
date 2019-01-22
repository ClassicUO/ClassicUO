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

using ClassicUO.Utility.Logging;
using ClassicUO.Game.UI.Gumps;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Scenes
{
    public enum ScenesType
    {
        Login,
        Game
    }

    internal sealed class SceneManager
    {
        public Scene CurrentScene { get; private set; }

        public void ChangeScene(ScenesType type)
        {         
            CurrentScene?.Dispose();
            CurrentScene = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            switch (type)
            {
                case ScenesType.Login:
                    Engine.IsFullScreen = false;
                    Engine.WindowWidth = 640;
                    Engine.WindowHeight = 480;
                    CurrentScene = new LoginScene();

                    break;

                case ScenesType.Game:
                    Engine.IsFullScreen = true;
                    CurrentScene = new GameScene();

                    if (Engine.Profile.Current.GameWindowFullSize)
                    {
                        WorldViewportGump e = Engine.UI.GetByLocalSerial<WorldViewportGump>();
                        e.ResizeWindow(new Point(Engine.WindowWidth, Engine.WindowHeight));
                    }
                    break;
            }

            CurrentScene.Load();
        }

        public void ChangeScene(Scene scene)
        {
            CurrentScene?.Dispose();
            CurrentScene = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            switch (scene)
            {
                case LoginScene login:
                    Engine.IsFullScreen = false;
                    Engine.WindowWidth = 640;
                    Engine.WindowHeight = 480;
                    CurrentScene = login;
                    break;
                case GameScene game:
                    Engine.IsFullScreen = true;
                    CurrentScene = game;
                    break;
            }
        }

        public T GetScene<T>() where T : Scene
        {
            return CurrentScene?.GetType() == typeof(T) ? (T) CurrentScene : null;
        }
    }
}