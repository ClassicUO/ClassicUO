#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using System;
using System.Collections.Generic;

using ClassicUO.Game.Data;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class SkillGumpAdvanced : Gump
    {
        private readonly Texture2D _line;
        private readonly ScrollArea _scrollArea;
        private readonly List<SkillListEntry> _skillListEntries;
        private double _totalReal, _totalValue;
        private bool _updateSkillsNeeded;

        public SkillGumpAdvanced() : base(0, 0)
        {
            _skillListEntries = new List<SkillListEntry>();
            _line = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _line.SetData(new[] {Color.White});
            _totalReal = 0;
            _totalValue = 0;
            X = 100;
            Y = 100;
            CanMove = true;
            AcceptMouseInput = false;
            AddChildren(new GameBorder(0, 0, 320, 350, 4));
            AddChildren(new GumpPicTiled(4, 4, 320 - 8, 350 - 8, 0x0A40) {IsTransparent = true});
            AddChildren(new GumpPicTiled(4, 4, 320 - 8, 350 - 8, 0x0A40) {IsTransparent = true});
            _scrollArea = new ScrollArea(20, 60, 295, 250, true) {AcceptMouseInput = true};
            AddChildren(_scrollArea);
            AddChildren(new Label("Skill", true, 1153) {X = 30, Y = 25});
            AddChildren(new Label("Real", true, 1153) {X = 165, Y = 25});
            AddChildren(new Label("Base", true, 1153) {X = 195, Y = 25});
            AddChildren(new Label("Cap", true, 1153) {X = 250, Y = 25});
            //======================================================================================
            AddChildren(new Label("Total", true, 1153) {X = 30, Y = 315});
            World.Player.SkillsChanged += OnSkillChanged;
        }

        protected override void OnInitialize()
        {
            _totalReal = 0;
            _totalValue = 0;
            _scrollArea.Clear();

            foreach (SkillListEntry entry in _skillListEntries)
            {
                entry.Clear();
                entry.Dispose();
            }

            _skillListEntries.Clear();

            foreach (Skill skill in World.Player.Skills)
            {
                _totalReal += skill.Base;
                _totalValue += skill.Value;
                Label skillName = new Label(skill.Name, true, 1153, font: 3); //3
                Label skillValueBase = new Label(skill.Base.ToString(), true, 1153, font: 3);
                Label skillValue = new Label(skill.Value.ToString(), true, 1153, font: 3);
                Label skillCap = new Label(skill.Cap.ToString(), true, 1153, font: 3);
                _skillListEntries.Add(new SkillListEntry(skillName, skillValueBase, skillValue, skillCap, skill));
            }

            for (int i = 0; i < _skillListEntries.Count; i++) _scrollArea.AddChildren(_skillListEntries[i]);
            AddChildren(new Label($"{_totalReal.ToString()} | {_totalValue.ToString()}", true, 1153) {X = 170, Y = 315});
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            base.Draw(spriteBatch, position, hue);
            spriteBatch.Draw2D(_line, new Rectangle((int) position.X + 30, (int) position.Y + 50, 260, 1), RenderExtentions.GetHueVector(0, false, .5f, false));
            spriteBatch.Draw2D(_line, new Rectangle((int) position.X + 30, (int) position.Y + 310, 260, 1), RenderExtentions.GetHueVector(0, false, .5f, false));

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
        }

        public override void Dispose()
        {
            _line.Dispose();
            World.Player.SkillsChanged -= OnSkillChanged;
            base.Dispose();
        }

        private void OnSkillChanged(object sender, EventArgs args)
        {
            _updateSkillsNeeded = true;
        }
    }

    public class SkillListEntry : GumpControl
    {
        private readonly SpriteTexture[] _textures = new SpriteTexture[3]
        {
            IO.Resources.Gumps.GetGumpTexture(0x983),
            IO.Resources.Gumps.GetGumpTexture(0x985),
            IO.Resources.Gumps.GetGumpTexture(0x82C)
        };
        public readonly Skill Skill;
        public readonly Label SkillCap;
        public readonly Label SkillName;
        public readonly Label SkillValue;
        public readonly Label SkillValueBase;

        public SkillListEntry(Label skillname, Label skillvaluebase, Label skillvalue, Label skillcap, Skill skill)
        {
            Height = 20;
            SkillName = skillname;
            SkillValueBase = skillvaluebase;
            SkillValue = skillvalue;
            SkillCap = skillcap;
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
            SkillCap.X = 230;
            AddChildren(SkillCap);
        }

        public override void Update(double totalMS, double frameMS)
        {
            _textures.ForEach(s => s.Ticks = (long) totalMS);
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            base.Draw(spriteBatch, position, hue);
            spriteBatch.Draw2D(_textures[(int) Skill.Lock], new Vector3(position.X + 210, position.Y + 5, position.Z), Vector3.Zero);

            return true;
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left && x >= 210 && x <= 210 + _textures[(int) Skill.Lock].Width && y >= 0 && y <= _textures[(int) Skill.Lock].Height)
                switch (Skill.Lock)
                {
                    case Lock.Up:
                        Skill.Lock = Lock.Down;
                        GameActions.ChangeSkillLockStatus((ushort) Skill.Index, (byte) Lock.Down);

                        break;
                    case Lock.Down:
                        Skill.Lock = Lock.Locked;
                        GameActions.ChangeSkillLockStatus((ushort) Skill.Index, (byte) Lock.Locked);

                        break;
                    case Lock.Locked:
                        Skill.Lock = Lock.Up;
                        GameActions.ChangeSkillLockStatus((ushort) Skill.Index, (byte) Lock.Up);

                        break;
                }
        }
    }
}