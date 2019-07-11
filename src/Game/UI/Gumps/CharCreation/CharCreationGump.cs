#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps.Login;
using ClassicUO.IO;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CharCreationGump : Gump
    {
        public enum CharCreationStep
        {
            Appearence = 0,
            ChooseProfession = 1,
            ChooseTrade = 2,
            ChooseCity = 3
        }

        private readonly LoginScene loginScene;

        private PlayerMobile _character;
        private CharCreationStep _currentStep;
        private LoadingGump _loadingGump;
        private ProfessionInfo _selectedProfession;

        private CityInfo _startingCity;

        public CharCreationGump() : base(0, 0)
        {
            loginScene = Engine.SceneManager.GetScene<LoginScene>();
            Add(new CreateCharAppearanceGump(), 1);
            SetStep(CharCreationStep.Appearence);
            CanCloseWithRightClick = false;
        }

        internal static int _skillsCount => FileManager.ClientVersion >= ClientVersions.CV_70160 ? 4 : 3;

        public void SetCharacter(PlayerMobile character)
        {
            _character = character;
            SetStep(CharCreationStep.ChooseProfession);
        }

        public void SetAttributes(bool force = false)
        {
            SetStep(_selectedProfession.DescriptionIndex >= 0 || force ? CharCreationStep.ChooseCity : CharCreationStep.ChooseTrade);
        }

        public void SetCity(CityInfo city)
        {
            _startingCity = city;
        }

        public void SetProfession(ProfessionInfo info)
        {
            _selectedProfession = info;

            for (int i = 0; i < _skillsCount; i++) _character.UpdateSkill(_selectedProfession.SkillDefVal[i, 0], (ushort) _selectedProfession.SkillDefVal[i, 1], 0, Lock.Locked, 0);

            _character.Strength = (ushort) _selectedProfession.StatsVal[0];
            _character.Intelligence = (ushort) _selectedProfession.StatsVal[1];
            _character.Dexterity = (ushort) _selectedProfession.StatsVal[2];

            SetAttributes();

            SetStep(_selectedProfession.DescriptionIndex > 0 ? CharCreationStep.ChooseCity : CharCreationStep.ChooseTrade);
        }

        public void CreateCharacter(byte profession)
        {
            loginScene.CreateCharacter(_character, _startingCity, profession);
        }

        public void StepBack(int steps = 1)
        {
            if (_currentStep == CharCreationStep.Appearence)
                loginScene.StepBack();
            else
                SetStep(_currentStep - steps);
        }

        public void ShowMessage(string message)
        {
            var currentPage = ActivePage;

            if (_loadingGump != null)
                Remove(_loadingGump);
            Add(_loadingGump = new LoadingGump(message, LoadingGump.Buttons.OK, a => ChangePage(currentPage)), 4);
            ChangePage(4);
        }

        private void SetStep(CharCreationStep step)
        {
            _currentStep = step;

            switch (step)
            {
                default:
                case CharCreationStep.Appearence:
                    ChangePage(1);

                    break;

                case CharCreationStep.ChooseProfession:
                    var existing = Children.FirstOrDefault(page => page.Page == 2);

                    if (existing != null)
                        Remove(existing);

                    Add(new CreateCharProfessionGump(), 2);

                    ChangePage(2);

                    break;

                case CharCreationStep.ChooseTrade:
                    existing = Children.FirstOrDefault(page => page.Page == 3);

                    if (existing != null)
                        Remove(existing);

                    Add(new CreateCharTradeGump(_character, _selectedProfession), 3);
                    ChangePage(3);

                    break;

                case CharCreationStep.ChooseCity:
                    existing = Children.FirstOrDefault(page => page.Page == 4);

                    if (existing != null)
                        Remove(existing);

                    Add(new CreateCharCityGump((byte) _selectedProfession.DescriptionIndex), 4);

                    ChangePage(4);

                    break;
            }
        }
    }
}