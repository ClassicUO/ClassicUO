#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using ClassicUO.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;

namespace ClassicUO.Game.UI.Gumps
{
    internal class BulletinBoardGump : Gump
    {
        private readonly DataBox _databox;

        public BulletinBoardGump(uint serial, int x, int y, string name) : base(serial, 0)
        {
            X = x;
            Y = y;
            CanMove = true;
            CanCloseWithRightClick = true;

            Add(new GumpPic(0, 0, 0x087A, 0));

            Label label = new Label
            (
                name,
                true,
                1,
                170,
                1,
                align: TEXT_ALIGN_TYPE.TS_CENTER
            )
            {
                X = 159,
                Y = 36
            };

            Add(label);

            HitBox hitbox = new HitBox(15, 170, 80, 80)
            {
                Alpha = 1
            };

            hitbox.MouseUp += (sender, e) =>
            {
                UIManager.GetGump<BulletinBoardItem>(LocalSerial)?.Dispose();

                UIManager.Add
                (
                    new BulletinBoardItem
                    (
                        LocalSerial,
                        0,
                        World.Player.Name,
                        string.Empty,
                        ResGumps.DateTime,
                        string.Empty,
                        0
                    ) { X = 400, Y = 335 }
                );
            };

            Add(hitbox);

            ScrollArea area = new ScrollArea
            (
                127,
                159,
                241,
                195,
                false
            );

            Add(area);

            _databox = new DataBox(0, 0, 1, 1);
            _databox.WantUpdateSize = true;

            area.Add(_databox);


            // TODO: buuttons
        }


        public override void Dispose()
        {
            for (LinkedListNode<Gump> g = UIManager.Gumps.Last; g != null; g = g.Previous)
            {
                if (g.Value is BulletinBoardItem)
                {
                    g.Value.Dispose();
                }
            }

            base.Dispose();
        }

        public void RemoveBulletinObject(uint serial)
        {
            foreach (Control child in _databox.Children)
            {
                if (child.LocalSerial == serial)
                {
                    child.Dispose();
                    _databox.WantUpdateSize = true;
                    _databox.ReArrangeChildren();

                    return;
                }
            }
        }


        public void AddBulletinObject(uint serial, string msg)
        {
            foreach (Control c in _databox.Children)
            {
                if (c.LocalSerial == serial)
                {
                    c.Dispose();

                    break;
                }
            }

            BulletinBoardObject obj = new BulletinBoardObject(serial, msg);
            _databox.Add(obj);

            _databox.WantUpdateSize = true;
            _databox.ReArrangeChildren();
        }
    }

    internal class BulletinBoardItem : Gump
    {
        private readonly ExpandableScroll _articleContainer;
        private readonly Button _buttonPost;
        private readonly Button _buttonRemove;
        private readonly Button _buttonReply;
        private readonly DataBox _databox;
        private readonly string _datatime;
        private readonly uint _msgSerial;
        private readonly StbTextBox _subjectTextbox;
        private readonly StbTextBox _textBox;

        public BulletinBoardItem
        (
            uint serial,
            uint msgSerial,
            string poster,
            string subject,
            string datatime,
            string data,
            byte variant
        ) : base(serial, 0)
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

            ScrollArea area = new ScrollArea
            (
                0,
                120,
                272,
                224,
                false
            );

            Add(area);

            _databox = new DataBox(0, 0, 1, 1);
            area.Add(_databox);

            bool useUnicode = Client.Version >= ClientVersion.CV_305D;
            byte unicodeFontIndex = 1;
            int unicodeFontHeightOffset = 0;

            ushort textColor = 0x0386;

            if (useUnicode)
            {
                unicodeFontHeightOffset = -6;
                textColor = 0;
            }

            Label text = new Label(ResGumps.Author, useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte) 6)
            {
                X = 30,
                Y = 40
            };

            Add(text);

            text = new Label(poster, useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte) 9)
            {
                X = 30 + text.Width,
                Y = 46 + unicodeFontHeightOffset
            };

            Add(text);


            text = new Label(ResGumps.Date, useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte) 6)
            {
                X = 30,
                Y = 58
            };

            Add(text);

            text = new Label(datatime, useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte) 9)
            {
                X = 32 + text.Width,
                Y = 64 + unicodeFontHeightOffset
            };

            Add(text);

            text = new Label(ResGumps.Title, useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte) 6)
            {
                X = 30,
                Y = 77
            };

            Add(text);

            ushort subjectColor = textColor;

            if (variant == 0)
            {
                subjectColor = 0x0008;
            }

            Add
            (
                _subjectTextbox = new StbTextBox(useUnicode ? unicodeFontIndex : (byte) 9, maxWidth: 150, isunicode: useUnicode, hue: subjectColor)
                {
                    X = 30 + text.Width,
                    Y = 83 + unicodeFontHeightOffset,
                    Width = 150,
                    IsEditable = variant == 0
                }
            );

            _subjectTextbox.SetText(subject);

            Add
            (
                new GumpPicTiled
                (
                    30,
                    106,
                    235,
                    4,
                    0x0835
                )
            );

            _databox.Add
            (
                _textBox = new StbTextBox
                (
                    useUnicode ? unicodeFontIndex : (byte) 9,
                    -1,
                    220,
                    hue: textColor,
                    isunicode: useUnicode
                )
                {
                    X = 40,
                    Y = 0,
                    Width = 220,
                    Height = 300,
                    IsEditable = variant == 0,
                    Multiline = true
                }
            );

            _textBox.SetText(data);
            _textBox.TextChanged += _textBox_TextChanged;

            switch (variant)
            {
                case 0:
                    Add(new GumpPic(97, 12, 0x0883, 0));

                    Add
                    (
                        _buttonPost = new Button((int) ButtonType.Post, 0x0886, 0x0886)
                        {
                            X = 37,
                            Y = Height - 50,
                            ButtonAction = ButtonAction.Activate,
                            ContainsByBounds = true
                        }
                    );

                    break;

                case 1:

                    Add
                    (
                        _buttonReply = new Button((int) ButtonType.Reply, 0x0884, 0x0884)
                        {
                            X = 37,
                            Y = Height - 50,
                            ButtonAction = ButtonAction.Activate,
                            ContainsByBounds = true
                        }
                    );

                    break;

                case 2:

                    Add
                    (
                        _buttonRemove = new Button((int) ButtonType.Remove, 0x0885, 0x0885) //DISABLED
                        {
                            X = 235,
                            Y = Height - 50,
                            ButtonAction = ButtonAction.Activate,
                            ContainsByBounds = true
                        }
                    );

                    break;
            }

            _databox.WantUpdateSize = true;
            _databox.ReArrangeChildren();
        }

        private void _textBox_TextChanged(object sender, EventArgs e)
        {
            _textBox.Height = Math.Max
            (
                FontsLoader.Instance.GetHeightUnicode
                (
                    1,
                    _textBox.Text,
                    220,
                    TEXT_ALIGN_TYPE.TS_LEFT,
                    0x0
                ) + 5,
                20
            );

            foreach (Control c in _databox.Children)
            {
                if (c is BulletinBoardItem)
                {
                    c.OnPageChanged();
                }
            }
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (_buttonPost != null)
            {
                _buttonPost.Y = Height - 50;
            }

            if (_buttonReply != null)
            {
                _buttonReply.Y = Height - 50;
            }

            if (_buttonRemove != null)
            {
                _buttonRemove.Y = Height - 50;
            }

            //if (!_textBox.IsDisposed && _textBox.IsChanged)
            //{
            //    _textBox.Height = System.Math.Max(FontsLoader.Instance.GetHeightUnicode(1, _textBox.TxEntry.Text, 220, TEXT_ALIGN_TYPE.TS_LEFT, 0x0) + 20, 40);

            //    foreach (Control c in _scrollArea.Children)
            //    {
            //        if (c is ScrollAreaItem)
            //            c.OnPageChanged();
            //    }
            //}

            base.Update(totalTime, frameTime);
        }


        public override void OnButtonClick(int buttonID)
        {
            // necessary to avoid closing
            if (_subjectTextbox == null)
            {
                return;
            }

            switch ((ButtonType) buttonID)
            {
                case ButtonType.Post:
                    NetClient.Socket.Send_BulletinBoardPostMessage(LocalSerial, _msgSerial, _subjectTextbox.Text, _textBox.Text);

                    Dispose();

                    break;

                case ButtonType.Reply:
                    UIManager.Add
                    (
                        new BulletinBoardItem
                        (
                            LocalSerial,
                            _msgSerial,
                            World.Player.Name,
                            ResGumps.RE + _subjectTextbox.Text,
                            _datatime,
                            string.Empty,
                            0
                        ) { X = 400, Y = 335 }
                    );

                    Dispose();

                    break;

                case ButtonType.Remove:
                    NetClient.Socket.Send_BulletinBoardRemoveMessage(LocalSerial, _msgSerial);
                    Dispose();

                    break;
            }
        }

        public override void OnPageChanged()
        {
            Height = _articleContainer.SpecialHeight;
            _databox.Parent.Height = _databox.Height = _articleContainer.SpecialHeight - 184;

            foreach (Control c in _databox.Children)
            {
                if (c is BulletinBoardItem)
                {
                    c.OnPageChanged();
                }
            }
        }

        //public override void OnKeyboardReturn(int textID, string text)
        //{
        //    if ((MultiLineBox.PasteRetnCmdID & textID) != 0 && !string.IsNullOrEmpty(text))
        //        _textBox.TxEntry.InsertString(text.Replace("\r", string.Empty));
        //}


        private enum ButtonType
        {
            Post,
            Remove,
            Reply
        }
    }

    internal class BulletinBoardObject : Control
    {
        public BulletinBoardObject(uint serial, string text)
        {
            LocalSerial = serial; //board
            CanMove = true;
            Width = 230;
            Height = 18;

            Add(new GumpPic(0, 0, 0x1523, 0));

            if (Client.Version >= ClientVersion.CV_305D)
            {
                Add
                (
                    new Label
                    (
                        text,
                        true,
                        0,
                        Width - 23,
                        1,
                        FontStyle.Fixed
                    )
                    {
                        X = 23, Y = 1
                    }
                );
            }
            else
            {
                Add
                (
                    new Label
                    (
                        text,
                        false,
                        0x0386,
                        Width - 23,
                        9,
                        FontStyle.Fixed
                    )
                    {
                        X = 23,
                        Y = 1
                    }
                );
            }

            WantUpdateSize = false;
        }


        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return false;
            }

            Control root = RootParent;

            if (root != null)
            {
                NetClient.Socket.Send_BulletinBoardRequestMessage(root.LocalSerial, LocalSerial);
            }

            return true;
        }
    }
}