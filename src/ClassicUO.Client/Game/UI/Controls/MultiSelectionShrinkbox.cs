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

namespace ClassicUO.Game.UI.Controls
{
    //internal class MultiSelectionShrinkbox : Control
    //{
    //    private readonly GumpPic _arrow;
    //    private readonly int _buttongroup;
    //    private readonly ushort _buttonimg, _pressedbuttonimg;
    //    private NiceButton[] _buttons;
    //    private string[] _items;
    //    private readonly Label _label;
    //    //this particular list will be used when inside a scroll area or similar situations where you want to nest a multi selection shrinkbox inside another one,
    //    //so that when the parent is deactivated, all the child will be made non visible
    //    private readonly List<MultiSelectionShrinkbox> _nestedBoxes = new List<MultiSelectionShrinkbox>();
    //    private bool _opened;
    //    private Button[] _pics;
    //    private readonly bool _useArrow2;

    //    public MultiSelectionShrinkbox(int x, int y, int width, string indextext, string[] items, ushort hue = 0x0453, bool unicode = false, byte font = 9, int group = 0, ushort button = 0, ushort pressedbutton = 0, bool useArrow2 = false) : this(x, y, width, indextext, hue, unicode, font, group, button, pressedbutton, useArrow2)
    //    {
    //        SetItemsValue(items);
    //    }

    //    private MultiSelectionShrinkbox(int x, int y, int width, string indextext, ushort hue, bool unicode, byte font, int group, ushort button, ushort pressedbutton, bool userArrow2 = false)
    //    {
    //        WantUpdateSize = false;
    //        X = x;
    //        Y = y;

    //        if (button > 0)
    //        {
    //            _buttonimg = button;

    //            if (pressedbutton > 0)
    //            {
    //                _pressedbuttonimg = pressedbutton;
    //            }
    //            else
    //            {
    //                _pressedbuttonimg = button;
    //            }
    //        }

    //        _buttongroup = group;
    //        Width = width;
    //        _useArrow2 = userArrow2;

    //        Add
    //        (
    //            _label = new Label(indextext, unicode, hue, font: font, align: TEXT_ALIGN_TYPE.TS_LEFT)
    //            {
    //                X = 18
    //            }
    //        );

    //        Height = _label.Height;

    //        Add(_arrow = new GumpPic(1, 1, (ushort) (userArrow2 ? 0x0827 : 0x15E1), 0));

    //        _arrow.MouseUp += (sender, state) =>
    //        {
    //            if (state.Button == MouseButtonType.Left)
    //            {
    //                Opened = !_opened;
    //            }
    //        };
    //    }

    //    internal bool Opened
    //    {
    //        get => _opened;
    //        set
    //        {
    //            if (_opened != value)
    //            {
    //                _opened = value;

    //                if (_opened)
    //                {
    //                    _arrow.Graphic = (ushort) (_useArrow2 ? 0x0826 : 0x15E2);
    //                    OnBeforeContextMenu?.Invoke(this, null);
    //                    GenerateButtons();

    //                    foreach (MultiSelectionShrinkbox msb in _nestedBoxes)
    //                    {
    //                        msb.IsVisible = true;
    //                        msb.OnPageChanged();
    //                    }
    //                }
    //                else
    //                {
    //                    _arrow.Graphic = (ushort) (_useArrow2 ? 0x0827 : 0x15E1);
    //                    ClearButtons();
    //                    Height = _label.Height;
    //                    OnAfterContextMenu?.Invoke(this, null);

    //                    foreach (MultiSelectionShrinkbox msb in _nestedBoxes)
    //                    {
    //                        msb.IsVisible = false;
    //                        msb.OnPageChanged();
    //                    }
    //                }

    //                Parent?.OnPageChanged();
    //            }
    //        }
    //    }

    //    public int SelectedIndex { get; private set; }

    //    public string SelectedName
    //    {
    //        get
    //        {
    //            if (_items != null && SelectedIndex >= 0 && SelectedIndex < _items.Length)
    //            {
    //                return _items[SelectedIndex];
    //            }

    //            return null;
    //        }
    //    }

    //    internal uint GetItemsLength => (uint) _items.Length;

    //    public string Name => _label == null ? null : _label.Text;

    //    public MultiSelectionShrinkbox ParentBox { get; private set; }

    //    internal bool NestBox(MultiSelectionShrinkbox box)
    //    {
    //        if (_nestedBoxes.Contains(box))
    //        {
    //            return false;
    //        }

    //        Control c = Parent;

    //        while (c != null)
    //        {
    //            if (c is ScrollArea area)
    //            {
    //                _arrow.IsVisible = true;
    //                _nestedBoxes.Add(box);
    //                box.Width = Width - box.X;
    //                area.Add(box);

    //                if (!_opened)
    //                {
    //                    box.IsVisible = false;
    //                }

    //                box.OnPageChanged();
    //                box.ParentBox = this;

    //                return true;
    //            }

    //            c = c.Parent;
    //        }

    //        return false;
    //    }

    //    internal void SetItemsValue(string[] items)
    //    {
    //        _items = items;

    //        if (_opened)
    //        {
    //            GenerateButtons();
    //        }

    //        _arrow.IsVisible = items.Length > 0 || _nestedBoxes.Count > 0;
    //    }

    //    internal void SetItemsValue(Dictionary<int, string> items)
    //    {
    //        _items = items.Select(o => o.Value)
    //                      .ToArray();

    //        if (_opened)
    //        {
    //            GenerateButtons();
    //        }

    //        _arrow.IsVisible = items.Count > 0 || _nestedBoxes.Count > 0;
    //    }

    //    private void GenerateButtons()
    //    {
    //        ClearButtons();
    //        _buttons = new NiceButton[_items.Length];

    //        if (_buttonimg > 0)
    //        {
    //            _pics = new Button[_items.Length];
    //        }

    //        int index = 0;
    //        int width = 0;
    //        int height = 0;
    //        int lh = _label.Height + 2;

    //        foreach (string item in _items)
    //        {
    //            int w, h;

    //            if (_label.Unicode)
    //            {
    //                w = FontsLoader.Instance.GetWidthUnicode(_label.Font, item);
    //            }
    //            else
    //            {
    //                w = FontsLoader.Instance.GetWidthASCII(_label.Font, item);
    //            }

    //            if (w > width)
    //            {
    //                if (_label.Unicode)
    //                {
    //                    h = FontsLoader.Instance.GetHeightUnicode(_label.Font, item, w, TEXT_ALIGN_TYPE.TS_LEFT, 0x0);
    //                }
    //                else
    //                {
    //                    h = FontsLoader.Instance.GetHeightASCII(_label.Font, item, w, TEXT_ALIGN_TYPE.TS_LEFT, 0x0);
    //                }

    //                width = w;
    //                height = h + 2;
    //            }
    //        }

    //        foreach (string item in _items)
    //        {
    //            NiceButton but = new NiceButton(20, index * height + lh, width, height, ButtonAction.Activate, item, _buttongroup, TEXT_ALIGN_TYPE.TS_LEFT) {Tag = index};

    //            if (_buttonimg > 0)
    //            {
    //                Add(_pics[index] = new Button(index, _buttonimg, _pressedbuttonimg) {X = 6, Y = index * height + lh + 2, ButtonAction = (ButtonAction) 0xBEEF, Tag = index});

    //                _pics[index]
    //                    .MouseUp += Selection_MouseClick;
    //            }

    //            but.MouseUp += Selection_MouseClick;
    //            _buttons[index] = but;
    //            Add(but);
    //            index++;
    //        }

    //        int totalHeight = _buttons.Length > 0 ? _buttons.Sum(o => o.Height) + lh : lh;

    //        Height = totalHeight;

    //        Parent.WantUpdateSize = true;
    //    }

    //    private void ClearButtons()
    //    {
    //        if (_buttons != null)
    //        {
    //            for (int i = _buttons.Length - 1; i >= 0; --i)
    //            {
    //                _buttons[i]
    //                    ?.Dispose();

    //                _buttons[i] = null;
    //            }
    //        }

    //        if (_pics != null)
    //        {
    //            for (int i = _pics.Length - 1; i >= 0; --i)
    //            {
    //                _pics[i]
    //                    ?.Dispose();

    //                _pics[i] = null;
    //            }
    //        }
    //    }

    //    private void Selection_MouseClick(object sender, MouseEventArgs e)
    //    {
    //        if (sender is Control c)
    //        {
    //            SelectedIndex = (int) c.Tag;

    //            if (sender is Button)
    //            {
    //                _buttons[SelectedIndex]
    //                    .IsSelected = true;
    //            }

    //            if (_buttongroup > 0)
    //            {
    //                OnGroupSelection();
    //            }

    //            if (_items != null && SelectedIndex >= 0 && SelectedIndex < _items.Length)
    //            {
    //                OnOptionSelected?.Invoke(this, c);
    //            }
    //        }
    //    }

    //    private void OnGroupSelection()
    //    {
    //        if (Parent != null && Parent.Parent is ScrollArea area)
    //        {
    //            foreach (Control sai in area.Children)
    //            {
    //                if (sai is ScrollAreaItem)
    //                {
    //                    foreach (Control c in sai.Children)
    //                    {
    //                        if (c is MultiSelectionShrinkbox msb && msb._buttongroup == _buttongroup && msb != this && msb._buttons != null)
    //                        {
    //                            foreach (NiceButton button in msb._buttons)
    //                            {
    //                                if (button != null)
    //                                {
    //                                    button.IsSelected = false;
    //                                }
    //                            }
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    public event EventHandler<Control> OnOptionSelected;
    //    public event EventHandler OnBeforeContextMenu;
    //    public event EventHandler OnAfterContextMenu;

    //    protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
    //    {
    //        if (_label.Bounds.Contains(Mouse.Position.X - ScreenCoordinateX, Mouse.Position.Y - ScreenCoordinateY) && button == MouseButtonType.Left)
    //        {
    //            Opened = !_opened;
    //        }

    //        return base.OnMouseDoubleClick(x, y, button);
    //    }

    //    public override void OnPageChanged()
    //    {
    //        Parent?.OnPageChanged();
    //    }
    //}
}