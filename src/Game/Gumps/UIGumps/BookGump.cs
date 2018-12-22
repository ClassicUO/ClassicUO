using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.IO;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace ClassicUO.Game.Gumps.UIGumps
{
    public class BookGump : Gump
    {
        public TextBox BookTitle, BookAuthor;
        public ushort BookPageCount { get; internal set; }
        public static bool IsNewBookD4 => FileManager.ClientVersion > ClientVersions.CV_200;
        public static byte DefaultFont => (byte)(IsNewBookD4 ? 1 : 4);
        public string[] BookPages { get; internal set; }
        public bool IsBookEditable { get; internal set; }
        public bool IntroChanges => PageChanged[0];
        public bool[] PageChanged;

        public BookGump( Item book ) : base( book.Serial, 0 )
        {
            CanMove = true;
            AcceptMouseInput = true;
        }

        private Button m_Forward, m_Backward;
        private void BuildGump()
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
                if ( k % 2 == 1 )
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
                    Text = BookPages[k - 1],
                    MultiLineInputAllowed = true,
                    MaxLines = 8,
                    Debug = true
                };
                AddChildren( tbox, page );
                m_Pages.Add( tbox );
                AddChildren( new Label( k.ToString(), true, 1 ) { X = x + 80, Y = 200 }, page );
            }
            SetActivePage( 1 );
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

        protected override void OnInitialize()
        {
            base.OnInitialize();
            BuildGump();
            Debug = true;
        }
        protected override void CloseWithRightClick()
        {
            if ( PageChanged[0] )
            {
                if ( IsNewBookD4 )
                {
                    NetClient.Socket.Send( new PBookHeaderNew( this ) );
                }
                else
                {
                    NetClient.Socket.Send( new PBookHeader( this ) );
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
            if (!IsDisposed && BookPages == null)
            {
                if (BookAuthor.IsChanged || BookTitle.IsChanged)
                    PageChanged[0] = true;
                for (int i = m_Pages.Count - 1; i >= 0; --i)
                {
                    if (m_Pages[i].IsChanged)
                        PageChanged[i + 1] = true;
                }
            }
            else
                BookPages = null;
            base.Update(totalMS, frameMS);
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            int curpage = ActiveInternalPage;
            switch ((TextBox.PageCommand)textID)
            {
                case TextBox.PageCommand.GoBackward when curpage > 0:
                    if((curpage+1)%2 == 0)
                        SetActivePage(ActivePage - 1);
                    RefreshShowCaretPos(m_Pages[curpage - 1].Text.Length, m_Pages[curpage - 1]);
                    break;
                case TextBox.PageCommand.GoForward when curpage >= 0 && curpage+1 < BookPageCount:
                    if((curpage+1)%2 == 1)
                        SetActivePage(ActivePage + 1);
                    RefreshShowCaretPos(0, m_Pages[curpage + 1]);
                    break;
                case TextBox.PageCommand.PasteText when text!=null && curpage >= 0:
                    curpage++;
                    if (curpage % 2 == 1)
                        SetActivePage(ActivePage + 1);
                    while (text != null && curpage < BookPageCount)
                    {
                        RefreshShowCaretPos(0, m_Pages[curpage]);
                        text = m_Pages[curpage]._entry.InsertString(text);
                        if (!string.IsNullOrEmpty(text))
                        {
                            curpage++;
                            if (curpage < BookPageCount)
                            {
                                if(curpage % 2 == 1)
                                    SetActivePage(ActivePage + 1);
                            }
                            else
                            {
                                --curpage;
                                text = null;
                            }
                        }
                    }
                    RefreshShowCaretPos(m_Pages[curpage].Text.Length, m_Pages[curpage]);
                    break;
                case TextBox.PageCommand.RemoveText:
                    //TODO: remove text from other pages, making it roll over on the previous pages, line by line
                    break;
            }
        }

        private void RefreshShowCaretPos(int pos, TextBox box)
        {
            box.SetKeyboardFocus();
            box._entry.SetCaretPosition(pos);
            box._entry.UpdateCaretPosition();
        }

        public sealed class PBookHeaderNew : PacketWriter
        {
            public PBookHeaderNew( BookGump gump ) : base( 0xD4 )//Serial serial, string title,string author,int pagecount ) : base( 0xD4 )
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
        public sealed class PBookHeader : PacketWriter
        {
            public PBookHeader( BookGump gump ) : base( 0x93 )//Serial serial, string title, string author, int pagecount ) : base( 0x93 )
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
        public sealed class PBookData : PacketWriter
        {
            public PBookData( BookGump gump ) : base(0x66)//Serial serial, List<TextBox> data
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
