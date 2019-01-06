using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    internal class BookGump : Gump
    {
        public TextBox BookTitle, BookAuthor;
        public ushort BookPageCount { get; internal set; }
        public static bool IsNewBookD4 => FileManager.ClientVersion > ClientVersions.CV_200;
        public static byte DefaultFont => (byte)(IsNewBookD4 ? 1 : 4);
        private byte _activated = 0;

        public string[] BookPages
        {
            get => null;
            set
            {
                if(_activated > 0)
                {
                    for (int i = 0; i < m_Pages.Count; i++)
                    {
                        m_Pages[i].Text = value[i];
                    }
                    SetActivePage(ActivePage);
                }
                else if(value!=null)
                {
                    BuildGump(value);
                    SetActivePage(1);
                }
            }
        }
        public bool IsBookEditable { get; internal set; }
        public bool IntroChanges => PageChanged[0];
        public bool[] PageChanged;

        public BookGump( Item book ) : base( book.Serial, 0 )
        {
            CanMove = true;
            AcceptMouseInput = true;
        }

        private Button m_Forward, m_Backward;
        private void BuildGump(string[] pages)
        {
            AddChildren( new GumpPic( 0, 0, 0x1FE, 0 )
            {
                CanMove = true
            } );

            AddChildren(m_Backward = new Button( (int)Buttons.Backwards, 0x1FF, 0x1FF, 0x1FF )
            {
                ButtonAction = ButtonAction.Activate,
            } );

            AddChildren(m_Forward =  new Button( (int)Buttons.Forward, 0x200, 0x200, 0x200 )
            {
                X = 356,
                ButtonAction = ButtonAction.Activate
            } );
            m_Forward.MouseClick += ( sender,e ) => {
                if ( e.Button == MouseButton.Left && sender is Control ctrl ) SetActivePage(ActivePage + 1);
            };
            m_Forward.MouseDoubleClick += (sender, e) => {
                if (e.Button == MouseButton.Left && sender is Control ctrl) SetActivePage(MaxPage);
            };
            m_Backward.MouseClick += ( sender, e ) => {
                if ( e.Button == MouseButton.Left && sender is Control ctrl ) SetActivePage( ActivePage - 1 );
            };
            m_Backward.MouseDoubleClick += (sender, e) => {
                if (e.Button == MouseButton.Left && sender is Control ctrl) SetActivePage(1);
            };

            PageChanged = new bool[BookPageCount + 1];
            //title allows only 47  dots (. + \0) so 47 is the right number
            AddChildren( BookTitle, 1);
            AddChildren( new Label( "by", true, 1 ) { X = BookAuthor.X, Y = BookAuthor.Y - 30 },1);
            //as the old booktitle supports only 30 characters in AUTHOR and since the new clients only allow 29 dots (. + \0 character at end), we use 29 as a limitation
            AddChildren( BookAuthor, 1);
            for ( int k = 1; k <= BookPageCount; k++ )
            {
                int x = 38;
                int y = 30;
                if (k % 2 == 1)
                {
                    x = 223;
                    //right hand page
                }
                int page = k + 1;
                if ( page % 2 == 1 )
                    page += 1;
                page = page >> 1;
                TextBox tbox = new TextBox(new TextEntry(DefaultFont, 53 * 8, 0, 155, IsNewBookD4, FontStyle.ExtraHeight, 2), this.IsBookEditable)
                {
                    X = x,
                    Y = y,
                    Height = 170,
                    Width = 155,
                    IsEditable = this.IsBookEditable,
                    Text = pages[k - 1],
                    MultiLineInputAllowed = true,
                    MaxLines = 8,
                };
                AddChildren(tbox, page);
                m_Pages.Add(tbox);
                tbox.MouseClick += (sender, e) => {
                    if (e.Button == MouseButton.Left && sender is Control ctrl) OnLeftClick();
                };
                tbox.MouseDoubleClick += (sender, e) => {
                    if (e.Button == MouseButton.Left && sender is Control ctrl) OnLeftClick();
                };
                AddChildren( new Label( k.ToString(), true, 1 ) { X = x + 80, Y = 200 }, page );
            }
            _activated = 1;
        }
        private List<TextBox> m_Pages = new List<TextBox> ();
        private int MaxPage => (BookPageCount >> 1) + 1;
        private int ActiveInternalPage => m_Pages.FindIndex(t => t.HasKeyboardFocus);

        private void SetActivePage( int page )
        {
            if (page <= 1)
            {
                m_Backward.IsVisible = false;
                m_Forward.IsVisible = true;
                page = 1;
            }
            else if (page >= MaxPage)
            {
                m_Forward.IsVisible = false;
                m_Backward.IsVisible = true;
                page = MaxPage;
            }
            else
            {
                m_Backward.IsVisible = true;
                m_Forward.IsVisible = true;
            }

            ActivePage = page;
        }
        public override void OnButtonClick( int buttonID )
        {
            switch ( (Buttons)buttonID )
            {
                case Buttons.Backwards:
                    return;
                case Buttons.Forward:
                    return;
            }
            base.OnButtonClick( buttonID );
        }

        protected override void CloseWithRightClick()
        {
            if ( PageChanged[0] )
            {
                if ( IsNewBookD4 )
                {
                    NetClient.Socket.Send( new PBookHeader( this ) );
                }
                else
                {
                    NetClient.Socket.Send( new PBookHeaderOld( this ) );
                }
                PageChanged[0] = false;
            }
            if ( PageChanged.Any(t => t == true) )
            {
                NetClient.Socket.Send( new PBookData( this ) );
            }
            base.CloseWithRightClick();
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_activated > 1)
            {
                if (!IsDisposed)
                {
                    if (BookAuthor.IsChanged || BookTitle.IsChanged)
                        PageChanged[0] = true;
                    for (int i = m_Pages.Count - 1; i >= 0; --i)
                    {
                        if (m_Pages[i].IsChanged)
                            PageChanged[i + 1] = true;
                    }
                }
            }
            else if(_activated > 0)
                _activated++;
            base.Update(totalMS, frameMS);
        }

        // < 0 == backward
        // > 0 == forward
        // 0 == invariant
        // this is our only place to check for page movement from key
        // or displacement of caretindex from mouse
        private sbyte _AtEnd = 0;
        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            int curpage = ActiveInternalPage;
            var textbox = m_Pages[curpage];
            var entry = textbox._entry;

            if (key == SDL.SDL_Keycode.SDLK_BACKSPACE || key == SDL.SDL_Keycode.SDLK_DELETE)
            {
                if (curpage >= 0)
                {
                    if (curpage > 0)
                    {
                        if (key == SDL.SDL_Keycode.SDLK_BACKSPACE)
                        {
                            if (_AtEnd < 0)
                            {
                                if ((curpage + 1) % 2 == 0)
                                    SetActivePage(ActivePage - 1);
                                curpage--;
                                textbox = m_Pages[curpage];
                                entry = textbox._entry;
                                RefreshShowCaretPos(entry.Text.Length, textbox);
                                _AtEnd = 1;
                            }
                            else if (entry.CaretIndex == 0)
                            {
                                _AtEnd = -1;
                                _scale = false;
                            }
                        }
                        else
                            _AtEnd = (sbyte)(entry.CaretIndex == 0 ? -1 : (entry.CaretIndex + 1 >=  entry.Text.Length && curpage < BookPageCount ? 1 : 0));
                    }
                    else
                        _AtEnd = 0;
                    
                    if (!_scale)
                    {
                        _AtEnd = 0;
                        return;
                    }
                    _scale = false;
                    int caretpos = entry.CaretIndex, active = curpage;
                    curpage++;

                    if (curpage < BookPageCount) //if we are on the last page it doesn't need the front text backscaling
                    {
                        StringBuilder sb = new StringBuilder();

                        do
                        {
                            entry = m_Pages[curpage]._entry;
                            textbox = m_Pages[curpage];
                            int curlen = entry.Text.Length, prevlen = m_Pages[curpage - 1].Text.Length, chonline = textbox.GetCharsOnLine(0), prevpage = curpage - 1;
                            m_Pages[prevpage]._entry.SetCaretPosition(prevlen);

                            for (int i = MaxBookLines - m_Pages[prevpage].LinesCount; i > 0 && prevlen > 0; --i)
                            {
                                sb.Append('\n');
                            }

                            sb.Append(entry.Text.Substring(0, chonline));

                            if (curlen > 0)
                            {
                                sb.Append('\n');

                                if (entry.Text[Math.Min(Math.Max(curlen - 1, 0), chonline)] == '\n')
                                    chonline++;

                                entry.Text = entry.Text.Substring(chonline);
                            }

                            m_Pages[prevpage]._entry.InsertString(sb.ToString());
                            curpage++;
                            sb.Clear();
                        } while (curpage < BookPageCount);

                        m_Pages[active]._entry.SetCaretPosition(caretpos);
                    }
                }
            }
            else if (key == SDL.SDL_Keycode.SDLK_RIGHT)
            {
                if (curpage >= 0 && curpage + 1 < BookPageCount)
                {
                    if (entry.CaretIndex + 1 >= textbox.Text.Length)
                    {
                        if (_AtEnd > 0)
                        {
                            if ((curpage + 1) % 2 == 1)
                                SetActivePage(ActivePage + 1);
                            RefreshShowCaretPos(0, m_Pages[curpage + 1]);
                            _AtEnd = -1;
                        }
                        else
                            _AtEnd = 1;
                        return;
                    }
                }
                _AtEnd = 0;
            }
            else if (key == SDL.SDL_Keycode.SDLK_LEFT)
            {
                if (curpage > 0)
                {
                    if (entry.CaretIndex == 0)
                    {
                        if (_AtEnd < 0)
                        {
                            if ((curpage + 1) % 2 == 0)
                                SetActivePage(ActivePage - 1);
                            RefreshShowCaretPos(m_Pages[curpage - 1].Text.Length, m_Pages[curpage - 1]);
                            _AtEnd = 1;
                        }
                        else
                            _AtEnd = -1;
                        return;
                    }
                }
                _AtEnd = 0;
            }
            else if (key == SDL.SDL_Keycode.SDLK_UP)
            {
                if (curpage > 0)
                {
                    if (entry.CaretIndex == 0)
                    {
                        if (_AtEnd < 0)
                        {
                            if ((curpage + 1) % 2 == 0)
                                SetActivePage(ActivePage - 1);
                            RefreshShowCaretPos(m_Pages[curpage - 1].Text.Length, m_Pages[curpage - 1]);
                            _AtEnd = 1;
                        }
                        else
                            _AtEnd = -1;
                        return;
                    }
                }
                _AtEnd = 0;
            }
            else if (key == SDL.SDL_Keycode.SDLK_DOWN)
            {
                if (curpage + 1 < BookPageCount && curpage >= 0)
                {
                    if (entry.CaretIndex + 1 >= textbox.Text.Length)
                    {
                        if (_AtEnd > 0)
                        {
                            if ((curpage + 1) % 2 == 1)
                                SetActivePage(ActivePage + 1);
                            RefreshShowCaretPos(0, m_Pages[curpage + 1]);
                            _AtEnd = -1;
                        }
                        else
                            _AtEnd = 1;
                        return;
                    }
                }
                _AtEnd = 0;
            }
            else if (key == SDL.SDL_Keycode.SDLK_HOME)
            {
                if (curpage > 0)
                {
                    if (_AtEnd < 0)
                    {
                        if ((curpage + 1) % 2 == 0)
                            SetActivePage(ActivePage - 1);
                        RefreshShowCaretPos(m_Pages[curpage - 1].Text.Length, m_Pages[curpage - 1]);
                        _AtEnd = 1;
                        return;
                    }
                }
                _AtEnd = 0;
            }
            else if (key == SDL.SDL_Keycode.SDLK_END)
            {
                if (curpage >= 0 && curpage + 1 < BookPageCount)
                {
                    if (_AtEnd > 0)
                    {
                        if ((curpage + 1) % 2 == 1)
                            SetActivePage(ActivePage + 1);
                        RefreshShowCaretPos(0, m_Pages[curpage + 1]);
                        _AtEnd = -1;
                        return;
                    }
                }
                _AtEnd = 0;
            }
            else
                _AtEnd = 0;
        }

        private bool _scale = false;
        public void ScaleOnBackspace(TextEntry entry)
        {
            var linech = entry.GetLinesCharsCount();
            for (int l = 0; l + 1 < linech.Length; l++)
            {
                linech[l]++;
            }
            int caretpos = entry.CaretIndex;
            for (int l = 0; l < linech.Length && !_scale; l++)
            {
                caretpos -= linech[l];
                _scale = caretpos == -linech[l];
            }
        }

        public void ScaleOnDelete(TextEntry entry)
        {
            var linech = entry.GetLinesCharsCount();
            for (int l = 0; l + 1 < linech.Length; l++)
            {
                linech[l]++;
            }
            int caretpos = entry.CaretIndex;
            for (int l = 0; l < linech.Length && !_scale; l++)
            {
                caretpos -= linech[l];
                _scale = caretpos == 0;
            }
        }

        public void OnHomeOrEnd(TextEntry entry, bool home)
        {
            var linech = entry.GetLinesCharsCount();
            for (int l = 0; l + 1 < linech.Length; l++)
            {
                linech[l]++;
            }
            int caretpos = entry.CaretIndex;

            for (int l = 0; l < linech.Length; l++)
            {
                caretpos -= linech[l];
                if (!home)
                {
                    if (caretpos == -1 || entry.CaretIndex == entry.Text.Length)
                    {
                        if (entry.CaretIndex == entry.Text.Length && ActiveInternalPage+1 < BookPageCount)
                            _AtEnd = 1;
                        entry.SetCaretPosition(entry.Text.Length);
                        break;
                    }
                    else if (caretpos < 0)
                    {
                        entry.SetCaretPosition(entry.CaretIndex - caretpos - 1);
                        break;
                    }
                }
                else
                {
                    if(caretpos == 0 || entry.CaretIndex == 0)
                    {
                        if(entry.CaretIndex == 0 && ActiveInternalPage > 0)
                            _AtEnd = -1;
                        entry.SetCaretPosition(0);
                        break;
                    }
                    else if(caretpos < 0)
                    {
                        entry.SetCaretPosition(entry.CaretIndex - (linech[l] + caretpos));
                        break;
                    }
                }
            }
        }

        private void OnLeftClick()
        {
            var curpage = ActiveInternalPage;
            var entry = m_Pages[curpage]._entry;
            var caretpos = m_Pages[curpage]._entry.CaretIndex;
            _AtEnd = (sbyte)(caretpos == 0 && curpage > 0 ? -1 : caretpos + 1 >= entry.Text.Length && curpage >= 0 && curpage < BookPageCount ? 1 : 0);
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if((TextBox.PasteRetnCmdID & textID) != 0 && !string.IsNullOrEmpty(text))
            {
                text = text.Replace("\r", string.Empty);
                int curpage = ActiveInternalPage, oldcaretpos = m_Pages[curpage]._entry.CaretIndex, oldpage = curpage;
                string original = textID == TextBox.PasteCommandID ? text : m_Pages[curpage].Text;
                text = m_Pages[curpage]._entry.InsertString(text);
                if (curpage >= 0)
                {
                    curpage++;
                    if (curpage % 2 == 1)
                        SetActivePage(ActivePage + 1);
                    while (text != null && curpage < BookPageCount)
                    {
                        var entry = m_Pages[curpage]._entry;
                        RefreshShowCaretPos(0, m_Pages[curpage]);
                        if(text.Length==0 || text[text.Length - 1] != '\n')
                            text = entry.InsertString(text + "\n");
                        else
                            text = entry.InsertString(text);
                        if (!string.IsNullOrEmpty(text))
                        {
                            curpage++;
                            if (curpage < BookPageCount)
                            {
                                if (curpage % 2 == 1)
                                    SetActivePage(ActivePage + 1);
                            }
                            else
                            {
                                --curpage;
                                text = null;
                            }
                        }
                    }
                    if (TextBox.RetrnCommandID == textID)
                    {
                        if (oldcaretpos >= m_Pages[oldpage].Text.Length && original == m_Pages[oldpage].Text && oldpage + 1 < BookPageCount)
                        {
                            oldcaretpos = 0;
                            oldpage++;
                        }
                        else
                            oldcaretpos++;
                        RefreshShowCaretPos(oldcaretpos, m_Pages[oldpage]);
                    }
                    else
                    {
                        int[] linechr = m_Pages[oldpage]._entry.GetLinesCharsCount(original);
                        for (int l = 0; l+1 < linechr.Length; l++)
                        {
                            if(l+1 % MaxBookLines != 0)
                                linechr[l]++;
                        }
                        for(int l = 0; l < linechr.Length; l++)
                        {
                            oldcaretpos += linechr[l];
                            if (oldcaretpos > m_Pages[oldpage].Text.Length)
                            {
                                oldcaretpos = linechr[l];
                                if (oldpage+1 < BookPageCount)
                                    oldpage++;
                                else
                                    break;
                            }
                        }
                        RefreshShowCaretPos(oldcaretpos, m_Pages[oldpage]);
                    }
                    SetActivePage((oldpage >> 1) + (oldpage % 2) + 1);
                }
            }
        }

        private void RefreshShowCaretPos(int pos, TextBox box)
        {
            box.SetKeyboardFocus();
            box._entry.SetCaretPosition(pos);
            box._entry.UpdateCaretPosition();
        }

        internal sealed class PBookHeader : PacketWriter
        {
            public PBookHeader( BookGump gump ) : base( 0xD4 )
            {
                byte[] titleBuffer = Encoding.UTF8.GetBytes(gump.BookTitle.Text);
                byte[] authorBuffer = Encoding.UTF8.GetBytes(gump.BookAuthor.Text);
                EnsureSize( 15 + titleBuffer.Length + authorBuffer.Length );
                WriteUInt( gump.LocalSerial );
                WriteByte( gump.BookPageCount > 0 ? (byte)1 : (byte)0 );
                WriteByte( gump.BookPageCount > 0 ? (byte)1 : (byte)0 );
                WriteUShort( gump.BookPageCount );

                WriteUShort( (ushort) (titleBuffer.Length + 1) );
                WriteBytes( titleBuffer, 0, titleBuffer.Length );
                WriteByte( 0 );
                WriteUShort( (ushort)(authorBuffer.Length + 1) );
                WriteBytes( authorBuffer, 0, authorBuffer.Length );
                WriteByte( 0 );
            }

           
        }
        internal sealed class PBookHeaderOld : PacketWriter
        {
            public PBookHeaderOld( BookGump gump ) : base( 0x93 )
            {
                EnsureSize( 15 + 60 + 30 );
                WriteUInt( gump.LocalSerial );
                WriteByte( gump.BookPageCount > 0 ? (byte)1 : (byte)0 );
                WriteByte( gump.BookPageCount > 0 ? (byte)1 : (byte)0 );

                WriteUShort( gump.BookPageCount );

                WriteASCII( gump.BookTitle.Text, 60 );
                WriteASCII( gump.BookAuthor.Text, 30 );
            }
        }

        private const int MaxBookLines = 8;
        internal sealed class PBookData : PacketWriter
        {
            public PBookData( BookGump gump ) : base(0x66)
            {
                EnsureSize( 256 );

                WriteUInt( gump.LocalSerial );
                List<int> changed = new List<int>();
                for(int i = 1; i < gump.PageChanged.Length; i++)
                {
                    if(gump.PageChanged[i])
                    {
                        changed.Add(i);
                    }
                }
                WriteUShort( (ushort)changed.Count );
                for( int i = changed.Count - 1; i >= 0; --i )
                {
                    WriteUShort( (ushort)(changed[i]) );
                    var splits = gump.m_Pages[changed[i]-1].Text.Split( '\n' );
                    int length = splits.Length;
                    bool allowdestack = true;
                    WriteUShort( (ushort)Math.Min(length, MaxBookLines) );
                    if( length > MaxBookLines && changed[i] >= gump.BookPageCount )
                    {
                        Log.Message( LogTypes.Error, $"Book page {changed[i]} split into too many lines: {length - MaxBookLines} Additional lines will be lost" );
                        allowdestack = false;
                    }
                    for ( int j = 0; j < length; j++ )
                    {
                        // each line should BE < 53 chars long, even if 80 is admitted (the 'dot' is the least space demanding char, '.',
                        // a page full of dots is 52 chars exactly, but in multibyte things might change in byte size!)
                        if (j < MaxBookLines)
                        {
                            if (IsNewBookD4)
                            {
                                byte[] buf = Encoding.UTF8.GetBytes(splits[j]);
                                if (buf.Length > 79)
                                {
                                    Log.Message(LogTypes.Error, $"Book page {changed[i]} single line too LONG, total lenght -> {buf.Length} vs MAX 79 bytes allowed, some content might get lost");
                                    splits[j] = splits[j].Substring(0, 79);
                                }
                                WriteBytes(buf, 0, buf.Length);
                                WriteByte(0);
                            }
                            else
                            {
                                if (splits[j].Length > 79)
                                {
                                    Log.Message(LogTypes.Error, $"Book page {changed[i]} single line too LONG, total lenght -> {splits[j].Length} vs MAX 79 bytes allowed, some content might get lost");
                                    splits[j] = splits[j].Substring(0, 79);
                                }
                                WriteASCII(splits[j]);
                            }
                        }
                        else if(allowdestack)
                        {
                            int idx = Position;
                            Seek(7);
                            gump.m_Pages[changed[i]].Text.Insert(0, string.Format("{0}\n", splits[j]));
                            changed.Insert(0, changed[i]);
                            WriteUShort((ushort)changed.Count);
                            Seek(idx);
                            i++;
                        }
                    }
                }
            }
        }

        private enum Buttons
        {
            Closing = 0,
            Forward = 1,
            Backwards = 2
        }
    }
}
