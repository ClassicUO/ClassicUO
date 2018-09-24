using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class JournalGump : Gump
    {
        private ExpandableScroll m_Background;
        private RenderedTextList m_JournalEntries;
        private readonly ScrollFlag m_ScrollBar;

        public JournalGump()
            : base(0, 0)
        {
            X = 100;
            Y = 100;
            CanMove = true;
            AcceptMouseInput = true;

            AddChildren(m_Background = new ExpandableScroll( 0, 0, 300));
            m_Background.TitleGumpID = 0x82A;

            AddChildren(m_ScrollBar = new ScrollFlag(this, 0, 0 , Height));
            AddChildren(m_JournalEntries = new RenderedTextList( 30, 36, 242, 200, m_ScrollBar));
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
            m_JournalEntries.Height = Height - 98;
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            return base.Draw(spriteBatch, position, hue);
        }

        private void AddJournalEntry(JournalEntry entry)
        {
            string text = string.Format("{0}{1}", entry.SpeakerName != string.Empty ? entry.SpeakerName + ": " : string.Empty, entry.Text);
            int font = entry.Font;
            bool asUnicode = entry.AsUnicode;
            TransformFont(ref font, ref asUnicode);

            m_JournalEntries.AddEntry(text, font, entry.Hue);
        }

        private void TransformFont(ref int font, ref bool asUnicode)
        {
            if (asUnicode)
                return;
            else
            {
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
        }

        private void InitializeJournalEntries()
        {
            for (int i = 0; i < Service.Get<JournalData>().JournalEntries.Count; i++)
            {
                AddJournalEntry(Service.Get<JournalData>().JournalEntries[i]);
            }

            m_ScrollBar.MinValue = 0;
        }
    }

    public class JournalData
    {
        private readonly List<JournalEntry> m_JournalEntries = new List<JournalEntry>();
        public List<JournalEntry> JournalEntries
        {
            get { return m_JournalEntries; }
        }

        public event Action<JournalEntry> OnJournalEntryAdded;

        public void AddEntry(string text, int font, ushort hue, string speakerName)
        {
            while (m_JournalEntries.Count > 99)
                m_JournalEntries.RemoveAt(0);
            m_JournalEntries.Add(new JournalEntry(text, font, hue, speakerName));
            OnJournalEntryAdded?.Invoke(m_JournalEntries[m_JournalEntries.Count - 1]);
        }
    }

    public class JournalEntry
    {
        public readonly string Text;
        public readonly int Font;
        public readonly ushort Hue;
        public readonly string SpeakerName;
        public readonly bool AsUnicode;

        public JournalEntry(string text, int font, ushort hue, string speakerName)
        {
            Text = text;
            Font = font;
            Hue = hue;
            SpeakerName = speakerName;
            
        }
    }
}
