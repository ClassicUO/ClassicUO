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

using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;

namespace ClassicUO.Game.UI.Gumps
{
    internal class JournalGump : Gump
    {
        private readonly ExpandableScroll _background;
        private readonly RenderedTextList _journalEntries;
        private readonly ScrollFlag _scrollBar;

        public JournalGump() : base(0, 0)
        {
            CanMove = true;
            CanBeSaved = true;
            AddChildren(_background = new ExpandableScroll(0, 0, 300)
            {
                TitleGumpID = 0x82A
            });
            _scrollBar = new ScrollFlag(this, 0, 0, Height);
            AddChildren(_journalEntries = new RenderedTextList(30, 36, 242, 200, _scrollBar));
        }

        protected override void OnMouseWheel(MouseEvent delta)
        {
            switch (delta)
            {
                case MouseEvent.WheelScrollUp:
                    _scrollBar.Value -= 5;

                    break;
                case MouseEvent.WheelScrollDown:
                    _scrollBar.Value += 5;

                    break;
            }
        }

        protected override void OnInitialize()
        {
            InitializeJournalEntries();
            Engine.SceneManager.GetScene<GameScene>().Journal.EntryAdded += AddJournalEntry;
        }

        public override void Dispose()
        {
            Engine.SceneManager.GetScene<GameScene>().Journal.EntryAdded -= AddJournalEntry;
            base.Dispose();
        }

        public override void Update(double totalMS, double frameMS)
        {
            WantUpdateSize = true;
            _journalEntries.Height = Height - 98;
            base.Update(totalMS, frameMS);
        }

        private void AddJournalEntry(object sender, JournalEntry entry)
        {
            string text = $"{(entry.Name != string.Empty ? $"{entry.Name}: " : string.Empty)}{entry.Text}";
            byte font = (byte) entry.Font;
            bool asUnicode = entry.IsUnicode;
            TransformFont(ref font, ref asUnicode);
            _journalEntries.AddEntry(text, font, entry.Hue);
        }

        private void TransformFont(ref byte font, ref bool asUnicode)
        {
            if (asUnicode)
                return;

            switch (font)
            {
                case 3:

                {
                    font = 1;
                    asUnicode = true;

                    break;
                }
            }
        }

        private void InitializeJournalEntries()
        {
            foreach (JournalEntry t in Engine.SceneManager.GetScene<GameScene>().Journal.Entries)
                AddJournalEntry(null, t);

            _scrollBar.MinValue = 0;
        }
    }
}