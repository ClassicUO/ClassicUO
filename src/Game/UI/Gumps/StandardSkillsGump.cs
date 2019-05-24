using ClassicUO.Game.UI.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.UI.Gumps
{
    class StandardSkillsGump : Gump
    {

        private ExpandableScroll _scrollArea;
        private GumpPic _bottomLine, _bottomComment;
        private ScrollArea _container;

        public StandardSkillsGump() : base(0, 0)
        {
            //CanBeSaved = true;
            AcceptMouseInput = false;
            CanMove = true;
            Height = 200;

            _scrollArea = new ExpandableScroll(0, 0, Height, true)
            {
                TitleGumpID = 0x0834,
                AcceptMouseInput = true,
            };

            Add(_scrollArea);


            Label text = new Label("Show:   Real    Cap", false, 0x0386, 180, 1)
            {
                X = 30,
                Y = 33
            };
            Add(text);
            Add(new GumpPic(40, 60, 0x082B, 0));
            Add(_bottomLine = new GumpPic(40, Height - 98, 0x082B, 0));
            Add(_bottomComment = new GumpPic(40, Height - 85, 0x0836, 0));

            _container = new ScrollArea(25, 60 + _bottomLine.Height + 2, _scrollArea.Width - 14,
                _scrollArea.Height - 98, false) {AcceptMouseInput = true, CanMove = true};
            Add(_container);

            foreach (KeyValuePair<string, List<int>> k in SkillsGroupManager.Groups)
            {
                MultiSelectionShrinkbox box = new MultiSelectionShrinkbox(0, 0, _container.Width - 30, k.Key, 0, 6, false, true)
                {
                    CanMove = true,
                    IsEditable = true
                };
                box.EditStateStart += (ss, e) =>
                {
                    Control p = _container;
                    var items = p.FindControls<ScrollAreaItem>().SelectMany(s => s.Children.OfType<MultiSelectionShrinkbox>());

                    foreach (var item in items)
                    {
                        foreach (EditableLabel c in item.FindControls<EditableLabel>())
                        {
                            c.SetEditable(false);
                        }
                    }
                };

                box.EditStateEnd += (ss, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.BackupText) && !string.IsNullOrWhiteSpace(e.Text))
                    {
                        SkillsGroupManager.ReplaceGroup(e.BackupText, e.Text);
                    }
                };

                _container.Add(box);

                SkillControl[] controls = new SkillControl[k.Value.Count];
                int idx = 0;

                foreach (var skill in k.Value)
                {
                    var c = new SkillControl(skill, box.Width - 25);
                    c.Width = box.Width - 25;
                    controls[idx++] = c;
                }
                box.SetItemsValue(controls);
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            WantUpdateSize = true;

            _bottomLine.Y =  Height - 98;
            _bottomComment.Y = Height - 85;
            _container.Height = Height - 170;


            _container.ForceUpdate();

            base.Update(totalMS, frameMS);
        }


        class SkillControl : Control
        {
            private GumpPic _lock;

            private Label _labelValue;

            public SkillControl(int skillIndex, int maxWidth)
            {
                AcceptMouseInput = true;
                CanMove = true;

                Skill skill = World.Player.Skills[skillIndex];

                if (skill.IsClickable)
                {
                    Button button = new Button(0, 0x0837, 0x0838, 0x0837);
                    button.MouseUp += (ss, e) => {  GameActions.UseSkill(skillIndex); };
                    Add(button);
                }
                
                Label label = new Label(skill.Name, false, 0x0288, maxwidth: maxWidth, font: 9)
                {
                    X = 12
                };
                Add(label);


                _labelValue = new Label(skill.Value.ToString("F1"), false, 0x0288, maxwidth: maxWidth - 10, font: 9, align: TEXT_ALIGN_TYPE.TS_RIGHT);
                Add(_labelValue);


                _lock = new GumpPic(maxWidth - 8, 1, GetLockValue(skill.Lock), 0) {AcceptMouseInput = true};
                _lock.MouseUp += (sender, e) =>
                {
                    switch (skill.Lock)
                    {
                        case Lock.Up:
                            skill.Lock = Lock.Down;
                            GameActions.ChangeSkillLockStatus((ushort)skill.Index, (byte)Lock.Down);
                            _lock.Graphic = 0x985;
                            _lock.Texture = FileManager.Gumps.GetTexture(0x985);

                            break;

                        case Lock.Down:
                            skill.Lock = Lock.Locked;
                            GameActions.ChangeSkillLockStatus((ushort)skill.Index, (byte)Lock.Locked);
                            _lock.Graphic = 0x82C;
                            _lock.Texture = FileManager.Gumps.GetTexture(0x82C);

                            break;

                        case Lock.Locked:
                            skill.Lock = Lock.Up;
                            GameActions.ChangeSkillLockStatus((ushort)skill.Index, (byte)Lock.Up);
                            _lock.Graphic = 0x983;
                            _lock.Texture = FileManager.Gumps.GetTexture(0x983);

                            break;
                    }

                };
                Add(_lock);

                WantUpdateSize = false;

                Width = maxWidth;
                Height = label.Height;
            }


            private ushort GetLockValue(Lock lockStatus)
            {
                switch (lockStatus)
                {
                    case Lock.Up:

                        return 0x0984;
                    case Lock.Down:

                        return 0x0986;
                    case Lock.Locked:

                        return 0x082C;
                    default:

                        return Graphic.INVALID;
                }
            }


        }
    }
}
