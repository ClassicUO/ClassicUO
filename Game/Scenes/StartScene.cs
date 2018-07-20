using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Scenes
{
    public sealed class StartScene : Scene
    {
        public StartScene()
        {
            ChainActions.Add(OnGameLoading);
            ChainActions.Add(OnGameLoaded);
        }


        private bool OnGameLoading()
        {
            return true;
        }

        private bool OnGameLoaded()
        {
            return true;
        }
    }
}
