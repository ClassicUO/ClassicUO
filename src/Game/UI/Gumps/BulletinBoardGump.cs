#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using System.Collections.Generic;
using System.Linq;
using ClassicUO.Data;
using System.Text;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    internal class BulletinBoardGump : Gump
    {
        private readonly ScrollArea _area;
        private readonly Item _item;
        private readonly List<BulletinBoardObject> _elements = new List<BulletinBoardObject>();
        private int _indexMax;


        public BulletinBoardGump(Item item, int x, int y, string name) : base(item, 0)
        {
            _item = item;
            _item.Items.Added += ItemsOnAdded;
            _item.Items.Removed += ItemsOnRemoved;

            X = x;
            Y = y;
            CanMove = true;
            CanCloseWithRightClick = true;

            Add(new GumpPic(10, 0, 0x087A, 0));

            Label label = new Label(name, false, 0x0386, 170, 2, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 160,
                Y = 25
            };

            Add(label);

            HitBox hitbox = new HitBox(15, 170, 80, 80)
            {
                Alpha = 1
            };
            hitbox.MouseUp += (sender, e) =>
            {
                UIManager.GetGump<BulletinBoardItem>(LocalSerial)?.Dispose();

                UIManager.Add(new BulletinBoardItem(LocalSerial, 0, World.Player.Name, string.Empty, "Date/Time",
                    string.Empty, 0)
                { X = 400, Y = 335 });
            };
            Add(hitbox);

            _area = new ScrollArea(127, 162, 241, 155, false);
            Add(_area);
        }

        private void ItemsOnRemoved(object sender, CollectionChangedEventArgs<uint> e)
        {
            foreach (BulletinBoardObject v in Children.OfType<BulletinBoardObject>().Where(s => e.Contains(s.Item)))
                v.Dispose();
        }

        private void
            ItemsOnAdded(object sender,
                CollectionChangedEventArgs<uint> e) 
        {
            foreach (BulletinBoardObject v in Children.OfType<BulletinBoardObject>().Where(s => e.Contains(s.Item)))
                v.Dispose();
            foreach (uint item in e)
            {
                NetClient.Socket.Send(new PBulletinBoardRequestMessageSummary(LocalSerial, item));
                if (e.Count != _item.Items.Count)
                    InvokeMouseCloseGumpWithRClick(); 
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_item == null || _item.IsDestroyed)
                Dispose();
        }

        public void Add(BulletinBoardObject pdo)
        {
            for (int mm = 0;
                mm < _elements.Count;
                mm++) 
            {
                BulletinBoardObject pdm = _elements[mm] as BulletinBoardObject;

                if (pdm != null && pdo._msgser != pdm._msgser && pdo._parentSerial == pdm._msgser && pdm._msgser != 0 &&
                    pdo._parentSerial != 0 && pdo.index == 0)
                {
                    pdo.index = pdm.index;
                    _elements.Add(pdo);
                }
                else if (pdm != null && pdo._msgser != pdm._msgser && pdo._parentSerial == 0 && pdo.index == 0
                ) 
                {
                    _indexMax++;
                    pdo.index = _indexMax;
                    _elements.Add(pdo);
                }
            } 

            if (_elements.Count == 0)
            {
                pdo.index = 1;
                _indexMax = 1;
                _elements.Add(pdo);
            }

            if (_elements.Count == _item.Items.Count)
            {
                int conta = 1;
                int tolti = 0;
                while (tolti < _elements.Count)
                {
                    bool insert = false;
                    for (int m = 0; m < _elements.Count; m++)
                    {
                        BulletinBoardObject bob = _elements[m] as BulletinBoardObject;
                        if (bob != null && bob.index == conta)
                        {
                            _area.Add(bob);
                            insert = true;
                            bob.index = 0;
                            tolti++;
                        }
                    }

                    if (insert) conta++;
                }
                _elements.Clear();
                _indexMax = 0;
            }
        }

        public override void Dispose()
        {
            if (_item != null)
            {
                _item.Items.Added -= ItemsOnAdded;
                _item.Items.Removed -= ItemsOnRemoved;
            }

            base.Dispose();
        }
    }
   
    internal class BulletinBoardItem : Gump
    {
        private readonly ExpandableScroll _articleContainer;
        private readonly Button _buttonPost;
        private readonly Button _buttonRemove;
        private readonly Button _buttonReply;
        private readonly string _datatime;
        private readonly uint _msgSerial;
        private readonly TextBox _subjectTextbox;
        private readonly MultiLineBox _textBox;
        private readonly ScrollArea _scrollArea;

        public BulletinBoardItem(uint serial, uint msgSerial, string poster, string subject, string datatime,
            string data, byte variant) : base(serial, 0)
        {
            _msgSerial = msgSerial;
            AcceptKeyboardInput = true;
            CanMove = true;
            CanCloseWithRightClick = true;
            _datatime = datatime;
            _articleContainer = new ExpandableScroll(0, 0, 408, 0x0820)
            {
                TitleGumpID = 0x0820,
                AcceptMouseInput = true
            };

            Add(_articleContainer);
            _scrollArea = new ScrollArea(0, 120, 272, 224, false);

            AddHorizontalBar(_scrollArea, 92, 35, 220);

            bool useUnicode = Client.Version >= ClientVersion.CV_305D;
            byte unicodeFontIndex = 1;
            int unicodeFontHeightOffset = 0;

            ushort textColor = 0x0386;

            if (useUnicode)
            {
                unicodeFontHeightOffset = -6;
                textColor = 0;
            }

            Label text = new Label("Author:", useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte)6)
            {
                X = 30,
                Y = 40
            };
            Add(text);

            text = new Label(poster, useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte)9)
            {
                X = 30 + text.Width,
                Y = 46 + unicodeFontHeightOffset
            };
            Add(text);


            text = new Label("Date:", useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte)6)
            {
                X = 30,
                Y = 58
            };
            Add(text);

            text = new Label(datatime, useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte)9)
            {
                X = 32 + text.Width,
                Y = 64 + unicodeFontHeightOffset
            };
            Add(text);

            text = new Label("Title:", useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte)6)
            {
                X = 30,
                Y = 77
            };
            Add(text);

            ushort subjectColor = textColor;

            if (variant == 0)
                subjectColor = 0x0008;

            Add(_subjectTextbox = new TextBox(useUnicode ? unicodeFontIndex : (byte)9, maxWidth: 150, width: 150,
                isunicode: useUnicode, hue: subjectColor)
            {
                X = 30 + text.Width,
                Y = 83 + unicodeFontHeightOffset,
                Width = 150,
                IsEditable = variant == 0 ? true : false
            });
            _subjectTextbox.SetText(subject);

            Add(new GumpPicTiled(30, 106, 235, 4, 0x0835)); 

            _scrollArea.Add(_textBox =
                new MultiLineBox(
                    new MultiLineEntry(useUnicode ? unicodeFontIndex : (byte)9, -1, 0, 220, hue: textColor,
                        unicode: useUnicode), true)
                {
                    X = 40,
                    Y = 0,
                    Width = 220,
                    ScissorsEnabled = true,
                    Text = data,
                    IsEditable = variant == 0 ? true : false
                });
            Add(_scrollArea);
            switch (variant)
            {
                case 0:
                    Add(new GumpPic(97, 12, 0x0883, 0));

                    Add(_buttonPost = new Button((int)ButtonType.Post, 0x0886, 0x0886)
                    {
                        X = 37,
                        Y = Height - 50,
                        ButtonAction = ButtonAction.Activate,
                        ContainsByBounds = true
                    });

                    break;

                case 1:

                    Add(_buttonReply = new Button((int)ButtonType.Reply, 0x0884, 0x0884)
                    {
                        X = 37,
                        Y = Height - 50,
                        ButtonAction = ButtonAction.Activate,
                        ContainsByBounds = true
                    });

                    break;

                case 2:

                    Add(_buttonRemove = new Button((int)ButtonType.Remove, 0x0885, 0x0885)//DISABLED
                    {
                        X = 235,
                        Y = Height - 50,
                        ButtonAction = ButtonAction.Activate,
                        ContainsByBounds = true
                    });

                    break;
            }

        }
        public override void Update(double totalMS, double frameMS)
        {
           
            if (_buttonPost != null)
                _buttonPost.Y = Height - 50;

            if (_buttonReply != null)
                _buttonReply.Y = Height - 50;

            if (_buttonRemove != null)
                _buttonRemove.Y = Height - 50;

            if (!_textBox.IsDisposed && _textBox.IsChanged)
            {
                _textBox.Height = System.Math.Max(FontsLoader.Instance.GetHeightUnicode(1, _textBox.TxEntry.Text, 220, TEXT_ALIGN_TYPE.TS_LEFT, 0x0) + 20, 40);

                foreach (Control c in _scrollArea.Children)
                {
                    if (c is ScrollAreaItem)
                        c.OnPageChanged();
                }
            }

            base.Update(totalMS, frameMS);
        }

        class PrivateContainer : Control
        {

        }
        private void AddHorizontalBar(ScrollArea area, ushort start, int x, int width)
        {
            PrivateContainer container = new PrivateContainer();
            area.Add(container);
        }

        public override void OnButtonClick(int buttonID)
        {
            // necessary to avoid closing
            if (_subjectTextbox == null)
                return;

            switch ((ButtonType)buttonID)
            {
                case ButtonType.Post:
                    NetClient.Socket.Send(new PBulletinBoardPostMessage(LocalSerial, _msgSerial, _subjectTextbox.Text,_textBox.Text));
                    Dispose();

                    break;

                case ButtonType.Reply:
                    UIManager.Add(new BulletinBoardItem(LocalSerial, _msgSerial, World.Player.Name,
                        "RE: " + _subjectTextbox.Text, _datatime, string.Empty, 0)
                    { X = 400, Y = 335 });
                    Dispose();

                    break;

                case ButtonType.Remove:
                    NetClient.Socket.Send(new PBulletinBoardRemoveMessage(LocalSerial, _msgSerial));
                    Dispose();

                    break;
            }
        }
        public override void OnPageChanged()
        {
            Height = _articleContainer.SpecialHeight;
            _scrollArea.Height = _articleContainer.SpecialHeight - (184);

            foreach (Control c in _scrollArea.Children)
            {
                if (c is ScrollAreaItem)
                    c.OnPageChanged();
            }
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if ((MultiLineBox.PasteRetnCmdID & textID) != 0 && !string.IsNullOrEmpty(text))
                _textBox.TxEntry.InsertString(text.Replace("\r", string.Empty));
        }




        private enum ButtonType
        {
            Post,
            Remove,
            Reply
        }
    }

    internal class BulletinBoardObject : ScrollAreaItem
    {
        public uint _msgser;
        public uint _parentSerial;
        public int index; 

        public BulletinBoardObject(uint parent, uint parentID, Item msgserial, string text)
        {
            LocalSerial = parent; //board
            _parentSerial = parentID; //if 0 is not reply
            Item = msgserial;
            _msgser = msgserial;
            CanMove = false;
            bool unicode = Client.Version >= ClientVersion.CV_305D;

            if (_parentSerial == 0)
            {
                Add(new GumpPic(0, 0, 0x1523, 0));
                Add(new Label(text, unicode, (ushort)(unicode ? 0 : 0x0386), font: (byte)(unicode ? 1 : 9))
                { X = Children[Children.Count - 1].Texture.Width + 2, Y = -1 });
            }
            else
            {
                Add(new GumpPic(20, 1, 0x2332, 0));
                Add(new Label("  " + text, unicode, (ushort)(unicode ? 0 : 0x0386), font: (byte)(unicode ? 2 : 9))
                { X = Children[Children.Count - 1].Texture.Width + 10, Y = -3 });
            }
        }

        public Item Item { get; }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
                return false;

            NetClient.Socket.Send(new PBulletinBoardRequestMessage(LocalSerial, Item));

            return true;
        }
    }
}