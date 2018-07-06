using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Stages
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
