using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Gumps.UIGumps.CharCreation
{
    class CharCreationGump: GumpControl
    {
        public enum CharCreationStep
        {
            Appearence, ChooseTrade
        }
        
        private LoginScene loginScene;
        PlayerMobile _character;
        CharCreationStep _currentStep;

        public CharCreationGump()
        {
            loginScene = Service.Get<LoginScene>();
            SetStep(CharCreationStep.Appearence);

            CanCloseWithRightClick = false;
            Service.Register(this);
        }
        
        public void SetCharacter(PlayerMobile character)
        {
            _character = character;
            SetStep(CharCreationStep.ChooseTrade);
        }

        public void CreateCharacter()
        {
            loginScene.CreateCharacter(_character);
            // SetStep(CharCreationStep.ChooseTrade);
        }

        private void SetStep(CharCreationStep step)
        {
            _currentStep = step;
            if (Children.Count() > 0)
                RemoveChildren(Children.First());

            AddChildren(GetGumpForStep(step));
        }

        private GumpControl GetGumpForStep(CharCreationStep step)
        {
            switch (step)
            {
                case CharCreationStep.Appearence:
                    return new CreateCharAppearanceGump();
                case CharCreationStep.ChooseTrade:
                    return new CreateCharTradeGump(_character);
            }

            return null;
        }
    }
}
