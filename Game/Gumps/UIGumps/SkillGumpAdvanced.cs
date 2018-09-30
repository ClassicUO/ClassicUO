using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.Controls.InGame;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class SkillGumpAdvanced : Gump
    {
        private ScrollArea _scrollArea;
        private Texture2D _blackTexture;
        private Texture2D _line;
        private List<SkillListEntry> _skillListEntries;
        private bool _updateSkillsNeeded;

        public SkillGumpAdvanced() : base(0, 0)
        {
            _skillListEntries = new List<SkillListEntry>();
            _blackTexture = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _blackTexture.SetData(new[] { Color.Black });
            _line = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _line.SetData(new[] { Color.White });


            X = 100;
            Y = 100;
            CanMove = true;
            AcceptMouseInput = true;

            AddChildren(new GameBorder(0, 0, 320, 330));
            _scrollArea = new ScrollArea(20, 60, 295, 250, true) { AcceptMouseInput = true };
            AddChildren(_scrollArea);
            AddChildren(new Label("Skill", true, 1153) { X = 30, Y = 25 });
            AddChildren(new Label("Real", true, 1153) { X = 165, Y = 25 });
            AddChildren(new Label("Base", true, 1153) { X = 195, Y = 25 });
            AddChildren(new Label("Cap", true, 1153) { X = 250, Y = 25 });



        }

        protected override void OnInitialize()
        {
            foreach (var entry in _skillListEntries)
            {
                entry.Dispose();
            }
            _skillListEntries.Clear();
            GumpPic toggleButton;
            //double skillValueAggregation = 0;
            //double skillBaseValueAggregation = 0;

            foreach (Skill skill in World.Player.Skills)
            {
                Label skillName = new Label(skill.Name, true, 1153) { Font = 3 }; //3
                Label skillValueBase = new Label(skill.Base.ToString(), true, 1153) { Font = 3 };
                Label skillValue = new Label(skill.Value.ToString(), true, 1153) { Font = 3 };
                Label skillCap = new Label(skill.Cap.ToString(), true, 1153) { Font = 3 };
                //=================================================================================
                switch (skill.Lock)
                {
                    case SkillLock.Up:
                        toggleButton = new GumpPic(0, 0, 0x983, 0) { AcceptMouseInput = true };
                        break;
                    case SkillLock.Down:
                        toggleButton = new GumpPic(0, 0, 0x985, 0) { AcceptMouseInput = true };
                        break;
                    case SkillLock.Locked:
                        toggleButton = new GumpPic(0, 0, 0x82C, 0) { AcceptMouseInput = true };
                        break;
                    default:
                        toggleButton = new GumpPic(0, 0, 0, 0) { AcceptMouseInput = true };
                        break;
                }
                //=================================================================================
                //Calculation of real and showed values
                //skillValueAggregation += skill.Value;
                //skillBaseValueAggregation += skill.Base;


                toggleButton.MouseClick += (sender, MouseEventArgs) => { OnToggleButtonClickEvent(sender, MouseEventArgs, skill); };
                _skillListEntries.Add(new SkillListEntry(skillName, skillValueBase, skillValue, skillCap, toggleButton, skill));
            }

            for (int i = 0; i < _skillListEntries.Count; i++)
            {
                _scrollArea.AddChildren(_skillListEntries[i]);
            }

            World.Player.SkillsChanged += OnSkillChanged;
            //AddChildren(new Label(skillBaseValueAggregation.ToString(), true, 1153) { X = 200, Y = 300, Font = 3 });
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            spriteBatch.Draw2D(_blackTexture, new Rectangle((int)position.X + 4, (int)position.Y + 4, 312, 322), RenderExtentions.GetHueVector(0, false, true, false));
            spriteBatch.Draw2D(_line, new Rectangle((int)position.X + 30, (int)position.Y + 50, 260, 1), RenderExtentions.GetHueVector(0, false, false, false));
            //spriteBatch.Draw2D(_line, new Rectangle((int)position.X + 30, (int)position.Y + 50, 260, 1), RenderExtentions.GetHueVector(0, false, false, false));
            return base.Draw(spriteBatch, position, hue);

        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
            if (_updateSkillsNeeded)
            {
                OnInitialize();
                _updateSkillsNeeded = false;
            }

        }

        public override void Dispose()
        {
            World.Player.SkillsChanged -= OnSkillChanged;
            base.Dispose();
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

        private void OnSkillChanged(object sender, EventArgs args)
        {
            _updateSkillsNeeded = true;
        }
    }

    public class SkillListEntry : GumpControl
    {

        public readonly Label SkillName;
        public readonly Label SkillValueBase;
        public readonly Label SkillValue;
        public readonly Label SkillCap;
        public readonly GumpPic ToggleButton;
        public readonly Skill Skill;

        public SkillListEntry(Label skillname, Label skillvaluebase, Label skillvalue, Label skillcap, GumpPic togglebutton, Skill skill)
        {
            Height = 20;

            SkillName = skillname;
            SkillValueBase = skillvaluebase;
            SkillValue = skillvalue;
            SkillCap = skillcap;
            ToggleButton = togglebutton;
            Skill = skill;


            SkillName.X = 10;
            AddChildren(SkillName);
            //======================
            SkillValueBase.X = 150;
            AddChildren(SkillValueBase);
            //======================
            SkillValue.X = 180;
            AddChildren(SkillValue);
            //======================
            ToggleButton.X = 210;
            AddChildren(ToggleButton);
            //======================
            SkillCap.X = 230;
            AddChildren(SkillCap);



        }

    }

}


