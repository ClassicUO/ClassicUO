using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Scenes
{
    public sealed class LoginScene : Scene
    {
        public LoginScene()
        {

        }

        private bool OnBeforeLogin()
        {
            return true;
        }

        private bool OnLogin()
        {
            return true;
        }

        private bool OnLoginFailed()
        {
            return true;
        }

        private bool OnLoginAccepted()
        {
            return true;
        }

        private bool OnServerList()
        {
            return true;
        }

        private bool OnServerListSelected()
        {
            return true;
        }

        private bool OnCharacterList()
        {
            return true;
        }

        private bool OnCharacterListSelected()
        {
            return true;
        }

        private bool OnCharacterDeleted()
        {
            return true;
        }

        private bool OnCharacterCreated()
        {
            return true;
        }

        private bool OnWorldEntered()
        {
            return true;
        }
    }
}
