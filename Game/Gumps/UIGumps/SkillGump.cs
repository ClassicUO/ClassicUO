using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class SkillGump : Gump
    {
        private ExpandableScroll m_Background;
        private List<SkillListEntry> _skillListEntries;
        private bool _updateSkillsNeeded;
        private readonly ScrollFlag _scrollBar;


        public SkillGump() : base(0, 0)
        {
            _skillListEntries = new List<SkillListEntry>();
            X = 100;
            Y = 100;
            CanMove = true;
            AcceptMouseInput = true;



            AddChildren(m_Background = new ExpandableScroll(0, 0, 200));
            m_Background.TitleGumpID = 0x834;
            AddChildren(_scrollBar = new ScrollFlag(this, 0, 0, Height));

        }

        protected override void OnInitialize()
        {
            _skillListEntries.Clear();
           foreach (Skill skill in World.Player.Skills)
            {
                Label skillName = new Label(1, 0x0288, skill.Name);
                Label skillValue = new Label(1, 0x0288, skill.Value.ToString());
                GumpPic toggleButton = new GumpPic(0, 0, 0, 0) { AcceptMouseInput = true };
                toggleButton.MouseClick += (sender, MouseEventArgs) => { OnToggleButtonClickEvent(sender, MouseEventArgs, skill); };


                bool maxScroll = (_scrollBar.Value == _scrollBar.MaxValue);

                while (_skillListEntries.Count > 99)
                {
                    _skillListEntries.RemoveAt(0);
                }
                _skillListEntries.Add(new SkillListEntry(skillName, skillValue, toggleButton, skill));
                _scrollBar.MaxValue += _skillListEntries[_skillListEntries.Count - 1].Height;
                if (maxScroll)
                {
                    _scrollBar.Value = _scrollBar.MaxValue;
                }
            }

            World.Player.SkillsChanged += OnSkillChanged;
        }


        private void OnToggleButtonClickEvent(object sender, MouseEventArgs args, Skill skill)
        {
            if (args.Button == MouseButton.Left)
            {
                switch (skill.Lock)
                {
                    case SkillLock.Up:
                        skill.Lock = SkillLock.Down;
                        break;
                    case SkillLock.Down:
                        skill.Lock = SkillLock.Locked;
                        break;
                    case SkillLock.Locked:
                        skill.Lock = SkillLock.Up;
                        break;
                }
                OnInitialize();
            }

        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            base.Draw(spriteBatch, position, hue);
            Vector3 p = new Vector3(position.X + 50, position.Y - 15, 0);
            int height = 0;
            int maxheight = _scrollBar.Value + _scrollBar.Height + 18;

            for (int i = 0; i < _skillListEntries.Count; i++)
            {
                if (height + _skillListEntries[i].Height <= _scrollBar.Value)
                {
                    height += _skillListEntries[i].Height;
                }
                else if (height + _skillListEntries[i].Height <= maxheight)
                {
                    int y = height - _scrollBar.Value;
                    if (y < 0)
                    {
                        p.Y += _skillListEntries[i].Height + y;
                    }
                    else
                    {
                        _skillListEntries[i].SkillName.Draw(spriteBatch, new Vector3(p.X, p.Y + 45, 0));
                        _skillListEntries[i].SkillValue.Draw(spriteBatch, new Vector3(p.X + 160, p.Y + 45, 0));
                        switch (_skillListEntries[i].Skill.Lock)
                        {
                            case SkillLock.Up:
                                _skillListEntries[i].ToggleButton.Texture = IO.Resources.Gumps.GetGumpTexture(0x983);
                                break;
                            case SkillLock.Down:
                                _skillListEntries[i].ToggleButton.Texture = IO.Resources.Gumps.GetGumpTexture(0x985);
                                break;
                            case SkillLock.Locked:
                                _skillListEntries[i].ToggleButton.Texture = IO.Resources.Gumps.GetGumpTexture(0x82C);
                                break;
                        }
                        _skillListEntries[i].ToggleButton.Draw(spriteBatch, new Vector3(p.X + 190, p.Y + 45, 0));
                        p.Y += _skillListEntries[i].Height;
                    }
                    height += _skillListEntries[i].Height;
                }
                else
                {
                    int y = maxheight - height;
                    break;
                }

            }

            return true;


        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
            if (_updateSkillsNeeded)
            {
                OnInitialize();
                _updateSkillsNeeded = false;
            }
            Point p = new Point(m_Background.X + m_Background.Width - 42, m_Background.Y + 38);
            _scrollBar.X = p.X;
            _scrollBar.Y = p.Y;
            _scrollBar.Height = m_Background.Height - 100;
            CalculateScrollBarMaxValue();

        }

        private void CalculateScrollBarMaxValue()
        {
            bool maxValue = _scrollBar.Value == _scrollBar.MaxValue;

            int height = 0;
            for (int i = 0; i < _skillListEntries.Count; i++)
            {
                height += _skillListEntries[i].Height;
            }

            height -= _scrollBar.Height;

            if (height > 0)
            {
                _scrollBar.MaxValue = height;
                if (maxValue)
                    _scrollBar.Value = _scrollBar.MaxValue;
            }
            else
            {
                _scrollBar.MaxValue = 0;
                _scrollBar.Value = 0;
            }
        }



        public override void Dispose()
        {
            World.Player.SkillsChanged -= OnSkillChanged;
            base.Dispose();
        }

        private void OnSkillChanged(object sender, EventArgs args)
        {
            _updateSkillsNeeded = true;
        }


    }


    public class SkillListEntry
    {

        public readonly Label SkillName;
        public readonly Label SkillValue;
        public readonly GumpPic ToggleButton;
        public readonly Skill Skill;

        public SkillListEntry(Label skillname, Label skillvalue, GumpPic togglebutton, Skill skill)
        {
            SkillName = skillname;
            SkillValue = skillvalue;
            ToggleButton = togglebutton;
            Skill = skill;


        }

        public int Height
        {
            get => SkillName.Height + 18;
            set => SkillName.Height = value;
        }


    }

}
