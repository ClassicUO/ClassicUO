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
        private TextBox m_Title, m_Author;
        public ushort BookPageCount { get; internal set; }
        public string Title { get; internal set; }
        public string Author { get; internal set; }
        public static bool IsNewBookD4 => FileManager.ClientVersion > ClientVersions.CV_200;
        public Dictionary<int, List<string>> BookPages { get; internal set; }

        public Dictionary<int, List<TextBox>> Lines = new Dictionary<int, List<TextBox>>();
        public bool IsBookEditable { get; internal set; }

        public bool IsDirty => m_Title?.Text != Title || m_Author?.Text != Author;
        public bool IsContentsDirty => m_pages.Any( t => t.Item1 != t.Item2.Text );

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
                if ( e.Button == MouseButton.Left && sender is GumpControl ctrl ) SetActivePage(ActivePage + 1);
            };
            m_Forward.MouseDoubleClick += (sender, e) => {
                if (e.Button == MouseButton.Left && sender is GumpControl ctrl) SetActivePage(MaxPage);
            };
            m_Backward.MouseClick += ( sender, e ) => {
                if ( e.Button == MouseButton.Left && sender is GumpControl ctrl ) SetActivePage( ActivePage - 1 );
            };
            m_Backward.MouseDoubleClick += (sender, e) => {
                if (e.Button == MouseButton.Left && sender is GumpControl ctrl) SetActivePage(1);
            };
            byte font = 1;

            if (!IsNewBookD4)
                font = 4;
            //title allows only 47  dots (. + \0) so 47 is the right number
            AddChildren( m_Title = new TextBox(new TextEntry(font, 47, 150, 150, IsNewBookD4, FontStyle.None, 0), this.IsBookEditable) { X = 40, Y = 40, Height = 25, Width = 155, IsEditable = this.IsBookEditable, Text = Title ?? "", Debug = true }, 1);
            AddChildren( new Label( "by", true, 1 ) { X = 45, Y = 110 },1);
            //as the old booktitle supports only 30 characters in AUTHOR and since the new clients only allow 29 dots (. + \0 character at end), we use 29 as a limitation
            AddChildren( m_Author = new TextBox(new TextEntry(font, 29, 150, 150, IsNewBookD4, FontStyle.None, 0), this.IsBookEditable) { X = 45, Y = 130, Height = 25, Width = 155, IsEditable = this.IsBookEditable, Text = Author ?? "", Debug = true } ,1);

            for ( int k = 1; k <= BookPageCount; k++ )
            {
                List<string> p = null;
                if(k < BookPages.Count)
                    p = BookPages[k];
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
                page /= 2;
                TextBox tbox;
                AddChildren( tbox = new TextBox(new TextEntry(font, 53, 156, 156, IsNewBookD4, FontStyle.None, 2), this.IsBookEditable) { X = x, Y = y, Height = 170, Width = 155, IsEditable = this.IsBookEditable, Text = "", MultiLineInputAllowed = true , MaxLines = 8, Debug = true }, page );

                for ( int i = 0; i < 8; i++ )
                {
                    var txt = ( p != null && p.Count > i ? p[i] : "" );

                    if ( i < 7 && !string.IsNullOrEmpty(txt) && !txt.EndsWith( "\n" ) )
                        txt += "\n";
                    tbox.SetText( txt, true );
                }
                m_pages.Add( (tbox.Text, tbox) );
                AddChildren( new Label( k.ToString(), true, 1 ) { X = x + 80, Y = 200 }, page );

                
            }
            SetActivePage( 1 );
        }
        private List<(string,TextBox)> m_pages = new List<(string, TextBox)> ();
        private int MaxPage => (BookPageCount >> 1) + 1;

        public override bool Draw( SpriteBatchUI spriteBatch, Point position, Vector3? hue = null )
        {
            return base.Draw( spriteBatch, position, hue );
        }

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
            
            if ( IsDirty )
            {
                if ( IsNewBookD4 )
                {
                    NetClient.Socket.Send( new PBookHeaderNew( LocalSerial, m_Title.Text, m_Author.Text, BookPages.Count ) );
                }
                else
                {
                    NetClient.Socket.Send( new PBookHeader( LocalSerial, m_Title.Text, m_Author.Text, BookPages.Count ) );
                }

            }
            if ( IsContentsDirty )
            {
                NetClient.Socket.Send( new PBookData( LocalSerial, m_pages ) );
            }
            base.CloseWithRightClick();
        }

        public sealed class PBookHeaderNew : PacketWriter
        {
           
            public PBookHeaderNew( Serial serial, string title,string author,int pagecount ) : base( 0xD4 )
            {
                byte[] titleBuffer = Encoding.UTF8.GetBytes( title );
                byte[] authorBuffer = Encoding.UTF8.GetBytes( author );
                EnsureSize( 15 + titleBuffer.Length + authorBuffer.Length );
                WriteUInt( serial );
                WriteByte( pagecount > 0 ? (byte)1 : (byte)0 );
                WriteByte( pagecount > 0 ? (byte)1 : (byte)0 );
                WriteUShort( (ushort)pagecount );

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
            public PBookHeader( Serial serial, string title, string author, int pagecount ) : base( 0x93 )
            {
               
                EnsureSize( 15 + 60 + 30 );
                WriteUInt( serial );
                WriteByte( pagecount > 0 ? (byte)1 : (byte)0 );
                WriteByte( pagecount > 0 ? (byte)1 : (byte)0 );

                WriteUShort( (ushort)pagecount );

                WriteASCII( title, 60 );
                WriteASCII( author, 30 );
            }
        }
        public sealed class PBookData : PacketWriter
        {
            public PBookData( Serial serial, List<(string, TextBox)> data ) : base( 0x66 )
            {
                EnsureSize( 256 );

                WriteUInt( serial );
                WriteUShort( (ushort)data.Count );
                for( int i= 0; i < data.Count; i++ )
                {
                    WriteUShort( (ushort)(i+1) );
                    var splits = data[i].Item2.Text.Split( '\n' );
                    int length = splits.Length;
                    bool allowdestack = true;
                    WriteUShort( (ushort)Math.Min(length, 8) );
                    if( length > 8 & i+1 >= data.Count )
                    {
                        Log.Message( LogTypes.Error, $"Book page {i} split into too many lines: {length - 8} Additional lines will be lost" );
                        allowdestack = false;
                    }
                    for ( int j = 0; j < length; j++ )
                    {
                        // each line should BE < 53 chars long, even if 80 is admitted (the 'dot' is the least space demanding char, '.',
                        // a page full of dots is 52 chars exactly, but in multibyte things might change in byte size!)
                        if (j < 8)
                        {
                            if (IsNewBookD4)
                            {
                                byte[] buf = Encoding.UTF8.GetBytes(splits[j]);
                                if (buf.Length > 79)
                                {
                                    Log.Message(LogTypes.Error, $"Book page {i} single line too LONG, total lenght -> {buf.Length} vs MAX 79 bytes allowed, some content might get lost");
                                    splits[j] = splits[j].Substring(0, 79);
                                }
                                WriteUShort((ushort)(buf.Length + 1));
                                WriteBytes(buf, 0, buf.Length);
                                WriteByte(0);

                            }
                            else
                            {
                                if (splits[j].Length > 79)
                                {
                                    Log.Message(LogTypes.Error, $"Book page {i} single line too LONG, total lenght -> {splits[j].Length} vs MAX 79 bytes allowed, some content might get lost");
                                    splits[j] = splits[j].Substring(0, 79);
                                }
                                WriteUShort((ushort)(splits[j].Length + 1));
                                WriteASCII(splits[j]);
                                WriteByte(0);
                            }
                        }
                        else if(allowdestack)
                        {
                            data[i+1].Item2.Text.Insert(0, string.Format("{0}\n", splits[j]));
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
