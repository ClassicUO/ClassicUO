using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Gumps.UIGumps.CharCreation
{
    class CreateCharTradeGump : Gump
    {
        HSliderBar[] _attributeSliders, _skillSliders;
        Combobox[] _skills;
        PlayerMobile _character;

        public CreateCharTradeGump(PlayerMobile character) : base(0, 0)
        {
            _character = character;

            AddChildren(new ResizePic(2600) { X = 100, Y = 80, Width = 470, Height = 372 });

            // center menu with fancy top
            // public GumpPic(AControl parent, int x, int y, int gumpID, int hue)
            AddChildren(new GumpPic(291, 42, 0x0589, 0));
            AddChildren(new GumpPic(214, 58, 0x058B, 0));
            AddChildren(new GumpPic(300, 51, 0x15A9, 0));

            // title text
            //TextLabelAscii(AControl parent, int x, int y, int font, int hue, string text, int width = 400)
            AddChildren(new Label(IO.Resources.Cliloc.GetString(3000326), false, 0x0386, font: 2) { X = 148, Y = 132 });

            // strength, dexterity, intelligence
            AddChildren(new Label(IO.Resources.Cliloc.GetString(3000111), false, 1, font: 1) { X = 158, Y = 170 });
            AddChildren(new Label(IO.Resources.Cliloc.GetString(3000112), false, 1, font: 1) { X = 158, Y = 250 });
            AddChildren(new Label(IO.Resources.Cliloc.GetString(3000113), false, 1, font: 1) { X = 158, Y = 330 });

            // sliders for attributes
            _attributeSliders = new HSliderBar[3];
            var values = FileManager.ClientVersion >= ClientVersions.CV_70160 ? 15 : 10;
            AddChildren(_attributeSliders[0] = new HSliderBar(164, 196, 93, 10, 60, 60, HSliderBarStyle.MetalWidgetRecessedBar, true));
            AddChildren(_attributeSliders[1] = new HSliderBar(164, 276, 93, 10, 60, values, HSliderBarStyle.MetalWidgetRecessedBar, true));
            AddChildren(_attributeSliders[2] = new HSliderBar(164, 356, 93, 10, 60, values, HSliderBarStyle.MetalWidgetRecessedBar, true));

            var skillCount = 3;
            var initialValue = 50;

            if (FileManager.ClientVersion >= ClientVersions.CV_70160)
            {
                skillCount = 4;
                initialValue = 30;
            }

            string[] skillList = IO.Resources.Skills.SkillNames;
            int y = 172;

            _skillSliders = new HSliderBar[skillCount];
            _skills = new Combobox[skillCount];
            
            for (var i = 0; i < skillCount; i++)
            {
                if (FileManager.ClientVersion < ClientVersions.CV_70160 && i == 2)
                    initialValue = 0;

                AddChildren(_skills[i] = new Combobox(344, y, 182, skillList, -1, 200, false, "Click here"));
                AddChildren(_skillSliders[i] = new HSliderBar(344, y + 32, 93, 0, 50, initialValue, HSliderBarStyle.MetalWidgetRecessedBar, true));
                
                y += 70;
            }

            //// sliders for skills
            //_skillSliders = new HSliderBar[4];
            //AddChildren(_skillSliders[0] = new HSliderBar(344, 204, 93, 0, 50, 50, HSliderBarStyle.MetalWidgetRecessedBar, true));
            //AddChildren(_skillSliders[1] = new HSliderBar(344, 284, 93, 0, 50, 50, HSliderBarStyle.MetalWidgetRecessedBar, true));
            //AddChildren(_skillSliders[2] = new HSliderBar(344, 364, 93, 0, 50, 0, HSliderBarStyle.MetalWidgetRecessedBar, true));
            
            //// drop downs for skills
            //_skills = new Combobox[3];
            
            //AddChildren(_skills[0] = new Combobox(344, 172, 182, skillList, -1, 200, false, "Click here"));
            //AddChildren(_skills[1] = new Combobox(344, 252, 182, skillList, -1, 200, false, "Click here"));
            //AddChildren(_skills[2] = new Combobox(344, 332, 182, skillList, -1, 200, false, "Click here"));

            AddChildren(new Button((int)Buttons.Prev, 0x15A1, 0x15A3, over: 0x15A2) { X = 586, Y = 445, ButtonAction = ButtonAction.Activate });
            AddChildren(new Button((int)Buttons.Next, 0x15A4, 0x15A6, over: 0x15A5) { X = 610, Y = 445, ButtonAction = ButtonAction.Activate });

            for (int i = 0; i < _attributeSliders.Length; i++)
            {
                for (int j = 0; j < _attributeSliders.Length; j++)
                {
                    if (i != j)
                        _attributeSliders[i].AddParisSlider(_attributeSliders[j]);
                }
            }

            for (int i = 0; i < _skillSliders.Length; i++)
            {
                for (int j = 0; j < _skillSliders.Length; j++)
                {
                    if (i != j)
                        _skillSliders[i].AddParisSlider(_skillSliders[j]);
                }
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch((Buttons)buttonID)
            {
                case Buttons.Prev:
                case Buttons.Next:
                    for(int i = 0; i < _skills.Length; i++)
                        if (_skills[i].SelectedIndex != -1)
                            _character.UpdateSkill(_skills[i].SelectedIndex, (ushort)_skillSliders[i].Value, 0, Data.Lock.Locked, 0);

                    _character.Strength = (ushort)_attributeSliders[0].Value;
                    _character.Dexterity = (ushort)_attributeSliders[1].Value;
                    _character.Intelligence = (ushort)_attributeSliders[2].Value;

                    Service.Get<CharCreationGump>().CreateCharacter();
                    break;
            }

            base.OnButtonClick(buttonID);
        }

        private enum Buttons
        {
            Prev,
            Next
        }
    }
}
