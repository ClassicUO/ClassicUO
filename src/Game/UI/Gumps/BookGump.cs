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
        public MultiLineBox BookTitle, BookAuthor;
        public ushort BookPageCount { get; internal set; }
        public static bool IsNewBookD4 => FileManager.ClientVersion > ClientVersions.CV_200;
        public static byte DefaultFont => (byte)(IsNewBookD4 ? 1 : 4);
        private byte _activated = 0;

        public string[] BookPages
        {
            get => null;
            set
            {
                if (value != null)
                {
                    if (_activated > 0)
                    {
                        for (int i = 0; i < m_Pages.Count; i++)
                        {
                            m_Pages[i].IsEditable = this.IsEditable;
                            m_Pages[i].Text = value[i];
                        }
                        SetActivePage(ActivePage);
                    }
                    else
                    {
                        BuildGump(value);
                        SetActivePage(1);
                    }
                }
            }
        }

        public bool IntroChanges => PageChanged[0];
        public bool[] PageChanged;

        public BookGump( UInt32 serial ) : base( serial, 0 )
        {
            CanMove = true;
            AcceptMouseInput = true;
        }

        private Button m_Forward, m_Backward;
        private void BuildGump(string[] pages)
        {
            Add( new GumpPic( 0, 0, 0x1FE, 0 )
            {
                CanMove = true
            } );

            Add(m_Backward = new Button( (int)Buttons.Backwards, 0x1FF, 0x1FF, 0x1FF )
            {
                ButtonAction = ButtonAction.Activate,
            } );

            Add(m_Forward =  new Button( (int)Buttons.Forward, 0x200, 0x200, 0x200 )
            {
                X = 356,
                ButtonAction = ButtonAction.Activate
            } );
            m_Forward.MouseClick += ( sender,e ) => {
                if (e.Button == MouseButton.Left && sender is Control ctrl)
                {
                    SetActivePage(ActivePage + 1);
                }
            };
            m_Forward.MouseDoubleClick += (sender, e) => {
                if (e.Button == MouseButton.Left && sender is Control ctrl) SetActivePage(MaxPage);
            };
            m_Backward.MouseClick += ( sender, e ) => {
                if (e.Button == MouseButton.Left && sender is Control ctrl)
                {
                    SetActivePage( ActivePage - 1 );
                }
            };
            m_Backward.MouseDoubleClick += (sender, e) => {
                if (e.Button == MouseButton.Left && sender is Control ctrl) SetActivePage(1);
            };

            PageChanged = new bool[BookPageCount + 1];
            Add( BookTitle, 1);
            Add( new Label( "by", true, 1 ) { X = BookAuthor.X, Y = BookAuthor.Y - 30 },1);
            Add( BookAuthor, 1);
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
                MultiLineBox tbox = new MultiLineBox(new MultiLineEntry(DefaultFont, 53 * 8, 0, 155, IsNewBookD4, FontStyle.ExtraHeight, 2), this.IsEditable)
                {
                    X = x,
                    Y = y,
                    Height = 170,
                    Width = 155,
                    IsEditable = this.IsEditable,
                    Text = pages[k - 1],
                    MaxLines = 8,
                };
                Add(tbox, page);
                m_Pages.Add(tbox);
                tbox.MouseClick += (sender, e) => {
                    if (e.Button == MouseButton.Left && sender is Control ctrl) OnLeftClick();
                };
                tbox.MouseDoubleClick += (sender, e) => {
                    if (e.Button == MouseButton.Left && sender is Control ctrl) OnLeftClick();
                };
                Add( new Label( k.ToString(), true, 1 ) { X = x + 80, Y = 200 }, page );
            }
            _activated = 1;

            Engine.SceneManager.CurrentScene.Audio.PlaySound(0x0055);
        }

        private List<MultiLineBox> m_Pages = new List<MultiLineBox> ();
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
            Engine.SceneManager.CurrentScene.Audio.PlaySound(0x0055);

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
            var box = curpage >= 0 ? m_Pages[curpage] : null;
            var entry = box?.TxEntry;

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
                                box = m_Pages[curpage];
                                entry = box.TxEntry;
                                RefreshShowCaretPos(entry.Text.Length, box);
                                _AtEnd = 1;
                            }
                            else if (entry.CaretIndex == 0)
                            {
                                _AtEnd = -1;
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
                            entry = m_Pages[curpage].TxEntry;
                            box = m_Pages[curpage];
                            int curlen = entry.Text.Length, prevlen = m_Pages[curpage - 1].Text.Length, chonline = box.GetCharsOnLine(0), prevpage = curpage - 1;
                            m_Pages[prevpage].TxEntry.SetCaretPosition(prevlen);

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

                            m_Pages[prevpage].TxEntry.InsertString(sb.ToString());
                            curpage++;
                            sb.Clear();
                        } while (curpage < BookPageCount);

                        m_Pages[active].TxEntry.SetCaretPosition(caretpos);
                    }
                }
            }
            else if (key == SDL.SDL_Keycode.SDLK_RIGHT)
            {
                if (curpage >= 0 && curpage + 1 < BookPageCount)
                {
                    if (entry.CaretIndex + 1 >= box.Text.Length)
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
                    if (entry.CaretIndex + 1 >= box.Text.Length)
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
        public void ScaleOnBackspace(MultiLineEntry entry)
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
            _scale = _scale && (ActiveInternalPage > 0 || entry.CaretIndex > 0);
        }

        public void ScaleOnDelete(MultiLineEntry entry)
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

        public void OnHomeOrEnd(MultiLineEntry entry, bool home)
        {
            var linech = entry.GetLinesCharsCount();
            // sepos = 1 if at end of whole text, -1 if at begin, else it will always be 0
            sbyte sepos = (sbyte)(entry.CaretIndex == 0 ? -1 : (entry.CaretIndex == entry.Text.Length ? 1 : 0));
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
                    int txtlen = entry.Text.Length;
                    if ((caretpos == -1 && sepos <= 0) || sepos > 0 || txtlen == 0)
                    {
                        if (entry.CaretIndex == txtlen && ActiveInternalPage+1 < BookPageCount)
                            _AtEnd = 1;
                        entry.SetCaretPosition(txtlen);
                        break;
                    }
                    else if (caretpos < 0 || sepos < 0)
                    {
                        entry.SetCaretPosition(entry.CaretIndex - caretpos - (l+1 != linech.Length ? 1 : 0));
                        break;
                    }
                }
                else
                {
                    if((caretpos == 0 && (sepos == 0 || (l+2 == linech.Length && linech[l+1] == 0))) || sepos < 0)
                    {
                        if(entry.CaretIndex == 0 && ActiveInternalPage > 0)
                            _AtEnd = -1;
                        entry.SetCaretPosition(0);
                        break;
                    }
                    else if(caretpos < 0 || (sepos > 0 && caretpos == 0))
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
            var entry = m_Pages[curpage].TxEntry;
            var caretpos = m_Pages[curpage].TxEntry.CaretIndex;
            _AtEnd = (sbyte)(caretpos == 0 && curpage > 0 ? -1 : caretpos + 1 >= entry.Text.Length && curpage >= 0 && curpage < BookPageCount ? 1 : 0);
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if((MultiLineBox.PasteRetnCmdID & textID) != 0 && !string.IsNullOrEmpty(text))
            {
                text = text.Replace("\r", string.Empty);
                int curpage = ActiveInternalPage, oldcaretpos = m_Pages[curpage].TxEntry.CaretIndex, oldpage = curpage;
                string original = textID == MultiLineBox.PasteCommandID ? text : m_Pages[curpage].Text;
                text = m_Pages[curpage].TxEntry.InsertString(text);
                if (curpage >= 0)
                {
                    curpage++;
                    if (curpage % 2 == 1)
                        SetActivePage(ActivePage + 1);
                    while (text != null && curpage < BookPageCount)
                    {
                        var entry = m_Pages[curpage].TxEntry;
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
                    if (MultiLineBox.RetrnCommandID == textID)
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
                        int[] linechr = m_Pages[oldpage].TxEntry.GetLinesCharsCount(original);
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

        private void RefreshShowCaretPos(int pos, MultiLineBox box)
        {
            box.SetKeyboardFocus();
            box.TxEntry.SetCaretPosition(pos);
            box.TxEntry.UpdateCaretPosition();
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
                            gump.m_Pages[changed[i]].Text = gump.m_Pages[changed[i]].Text.Insert(0, string.Format("{0}\n", splits[j]));
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
