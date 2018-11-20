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

using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class JournalGump : Gump
    {
        private readonly ExpandableScroll _background;
        private readonly RenderedTextList _journalEntries;
        private readonly ScrollFlag _scrollBar;

        public JournalGump() : base(0, 0)
        {
            X = 100;
            Y = 100;
            CanMove = true;
            AcceptMouseInput = true;

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
            Service.Get<JournalData>().OnJournalEntryAdded += AddJournalEntry;
        }

        public override void Dispose()
        {
            Service.Get<JournalData>().OnJournalEntryAdded -= AddJournalEntry;
            base.Dispose();
        }

        public override void Update(double totalMS, double frameMS)
        {
            WantUpdateSize = true;
            _journalEntries.Height = Height - 98;
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            return base.Draw(spriteBatch, position, hue);
        }

        private void AddJournalEntry(JournalEntry entry)
        {
            string text = string.Format("{0}{1}", entry.SpeakerName != string.Empty ? entry.SpeakerName + ": " : string.Empty, entry.Text);
            int font = entry.Font;
            bool asUnicode = entry.AsUnicode;
            TransformFont(ref font, ref asUnicode);
            _journalEntries.AddEntry(text, font, entry.Hue);
        }

        private void TransformFont(ref int font, ref bool asUnicode)
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
            for (int i = 0; i < Service.Get<JournalData>().JournalEntries.Count; i++) AddJournalEntry(Service.Get<JournalData>().JournalEntries[i]);
            _scrollBar.MinValue = 0;
        }
    }

    public class JournalData
    {
        public List<JournalEntry> JournalEntries { get; } = new List<JournalEntry>();

        public event Action<JournalEntry> OnJournalEntryAdded;

        public void AddEntry(string text, int font, ushort hue, string speakerName)
        {
            while (JournalEntries.Count > 99)
                JournalEntries.RemoveAt(0);
            JournalEntries.Add(new JournalEntry(text, font, hue, speakerName, false));
            OnJournalEntryAdded?.Invoke(JournalEntries[JournalEntries.Count - 1]);
        }
    }

    public struct JournalEntry
    {
        public readonly bool AsUnicode;
        public readonly int Font;
        public readonly ushort Hue;
        public readonly string SpeakerName;
        public readonly string Text;

        public JournalEntry(string text, int font, ushort hue, string speakerName, bool unicode)
        {
            Text = text;
            Font = font;
            Hue = hue;
            SpeakerName = speakerName;
            AsUnicode = unicode;
        }
    }
}