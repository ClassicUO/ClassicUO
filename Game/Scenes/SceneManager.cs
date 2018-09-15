using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Scenes
{
    public enum ScenesType
    {
        Loading,
        Login,
        Game
    }

    public class SceneManager
    {
        public Scene CurrentScene { get; private set; }


        public void ChangeScene(ScenesType type)
        {
            CurrentScene?.Dispose();

            switch (type)
            {
                case ScenesType.Loading:                  
                    CurrentScene = new LoadScene();
                    break;
                case ScenesType.Login:
                    CurrentScene = new LoginScene();
                    break;
                case ScenesType.Game:
                    CurrentScene = new GameScene();
                    break;
            }

            CurrentScene.Load();
        }


    }
}
