using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ModernOptionsGump : Gump
    {
        private LeftSideMenuRightSideContent mainContent;
        private List<SettingsOption> options = new List<SettingsOption>();

        public static string SearchText { get; private set; } = String.Empty;
        public static event EventHandler SearchValueChanged;

        public ModernOptionsGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            Width = 900;
            Height = 700;

            Add(new ColorBox(Width, Height, ColorPallet.BACKGROUND) { AcceptMouseInput = true, CanMove = true, Alpha = 0.85f });

            Add(new ColorBox(Width, 40, ColorPallet.SEARCH_BACKGROUND) { AcceptMouseInput = true, CanMove = true, Alpha = 0.85f });

            Add(new TextBox("Options", TrueTypeLoader.EMBEDDED_FONT, 30, null, Color.White, strokeEffect: false) { X = 10, Y = 10 });

            Add(new TextBox("Search", TrueTypeLoader.EMBEDDED_FONT, 30, null, Color.White, strokeEffect: false) { X = (int)(Width * 0.3), Y = 10 });

            Add(mainContent = new LeftSideMenuRightSideContent(Width, Height - 40, (int)(Width * 0.23)) { Y = 40 });

            ModernButton b;
            mainContent.AddToLeft(b = CategoryButton("General", (int)PAGE.General, mainContent.LeftWidth));
            b.IsSelected = true;
            mainContent.AddToLeft(CategoryButton("Sound", (int)PAGE.Sound, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Video", (int)PAGE.Video, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Macros", (int)PAGE.Macros, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Tooltips", (int)PAGE.Tooltip, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Speech", (int)PAGE.Speech, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Combat & Spells", (int)PAGE.CombatSpells, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Counters", (int)PAGE.Counters, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Infobar", (int)PAGE.InfoBar, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Containers", (int)PAGE.Containers, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Experimental", (int)PAGE.Experimental, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Ignore List", (int)PAGE.IgnoreList, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Nameplate Options", (int)PAGE.NameplateOptions, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("Cooldown bars", (int)PAGE.TUOCooldowns, mainContent.LeftWidth));
            mainContent.AddToLeft(CategoryButton("TazUO Specific", (int)PAGE.TUOOptions, mainContent.LeftWidth));

            BuildGeneral();

            foreach (SettingsOption option in options)
            {
                mainContent.AddToRight(option.FullControl, false, (int)option.OptionsPage);
            }

            ChangePage((int)PAGE.General);
        }

        private void BuildGeneral()
        {
            LeftSideMenuRightSideContent content = new LeftSideMenuRightSideContent(mainContent.RightWidth, mainContent.Height, (int)(mainContent.RightWidth * 0.3));
            content.AddToLeft(SubCategoryButton("General", ((int)PAGE.General + 1000), content.LeftWidth));

            Checkbox cb;
            content.AddToRight(cb = new Checkbox("Highlight objects under cursor", isChecked: ProfileManager.CurrentProfile.HighlightGameObjects, valueChanged: (b) => { ProfileManager.CurrentProfile.HighlightGameObjects = b; }), true, (int)PAGE.General + 1000);

            content.AddToRight(cb = new Checkbox("Enable pathfinding", isChecked: ProfileManager.CurrentProfile.EnablePathfind, valueChanged: (b) => { ProfileManager.CurrentProfile.EnablePathfind = b; }), true, (int)PAGE.General + 1000);
            content.Indent();
            content.AddToRight(cb = new Checkbox("Use shift for pathfinding", isChecked: ProfileManager.CurrentProfile.UseShiftToPathfind, valueChanged: (b) => { ProfileManager.CurrentProfile.UseShiftToPathfind = b; }), true, (int)PAGE.General + 1000);
            content.AddToRight(cb = new Checkbox("Single click for pathfinding", isChecked: ProfileManager.CurrentProfile.PathfindSingleClick, valueChanged: (b) => { ProfileManager.CurrentProfile.PathfindSingleClick = b; }), true, (int)PAGE.General + 1000);
            content.RemoveIndent();

            content.AddToLeft(SubCategoryButton("Mobiles", ((int)PAGE.General + 1001), content.LeftWidth));
            content.AddToLeft(SubCategoryButton("Gumps & Context", ((int)PAGE.General + 1002), content.LeftWidth));
            content.AddToLeft(SubCategoryButton("Misc", ((int)PAGE.General + 1003), content.LeftWidth));
            content.AddToLeft(SubCategoryButton("Terrain & Statics", ((int)PAGE.General + 1004), content.LeftWidth));

            options.Add(new SettingsOption(
                    "",
                    content,
                    mainContent.RightWidth,
                    PAGE.General
                ));
        }

        private ModernButton CategoryButton(string text, int page, int width, int height = 40)
        {
            return new ModernButton(0, 0, width, height, ButtonAction.SwitchPage, text, ColorPallet.BUTTON_FONT_COLOR) { ButtonParameter = page, FullPageSwitch = true };
        }

        private ModernButton SubCategoryButton(string text, int page, int width, int height = 40)
        {
            return new ModernButton(0, 0, width, height, ButtonAction.SwitchPage, text, ColorPallet.BUTTON_FONT_COLOR) { ButtonParameter = page };
        }

        public override void OnPageChanged()
        {
            base.OnPageChanged();

            mainContent.ActivePage = ActivePage;
        }

        private class Checkbox : Control
        {
            private const int CHECKBOX_SIZE = 30;

            private bool _isChecked;
            private readonly TextBox _text;

            private Vector3 hueVector = ShaderHueTranslator.GetHueVector(ColorPallet.SEARCH_BACKGROUND, false, 0.9f);

            public Checkbox(
                string text = "",
                int fontSize = 18,
                int maxWidth = 0,
                bool isChecked = false,
                Action<bool> valueChanged = null
            )
            {
                _isChecked = isChecked;
                ValueChanged = valueChanged;
                _text = new TextBox(text, TrueTypeLoader.EMBEDDED_FONT, fontSize, maxWidth == 0 ? null : maxWidth, ColorPallet.TEXT_FOREGROUND, strokeEffect: false) { X = CHECKBOX_SIZE + 5 };

                Width = CHECKBOX_SIZE + 5 + _text.Width;
                Height = Math.Max(CHECKBOX_SIZE, _text.Height);

                _text.Y = (Height / 2) - (_text.Height / 2);

                Add(_text);

                CanMove = true;
                AcceptMouseInput = true;
            }

            public bool IsChecked
            {
                get => _isChecked;
                set
                {
                    if (_isChecked != value)
                    {
                        _isChecked = value;
                        OnCheckedChanged();
                    }
                }
            }

            public override ClickPriority Priority => ClickPriority.High;

            public string Text => _text.Text;

            public Action<bool> ValueChanged { get; }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (IsDisposed)
                {
                    return false;
                }

                batcher.Draw(
                    SolidColorTextureCache.GetTexture(Color.White),
                    new Rectangle(x, y, CHECKBOX_SIZE, CHECKBOX_SIZE),
                    hueVector
                );

                if (IsChecked)
                {
                    batcher.Draw(
                        SolidColorTextureCache.GetTexture(Color.Black),
                        new Rectangle(x + (CHECKBOX_SIZE / 2) / 2, y + (CHECKBOX_SIZE / 2) / 2, CHECKBOX_SIZE / 2, CHECKBOX_SIZE / 2),
                        hueVector
                    );
                }

                return base.Draw(batcher, x, y);
            }

            protected virtual void OnCheckedChanged()
            {
                ValueChanged.Invoke(IsChecked);
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left && MouseIsOver)
                {
                    IsChecked = !IsChecked;
                }
            }

            public override void Dispose()
            {
                base.Dispose();
                _text?.Dispose();
            }
        }

        private class LeftSideMenuRightSideContent : Control
        {
            private const int TOP_PADDING = 5;
            private const int INDENT_SPACE = 30;

            private ScrollArea left, right;
            private int leftY, rightY = TOP_PADDING, leftX, rightX;

            public new int ActivePage
            {
                get => base.ActivePage;
                set
                {
                    base.ActivePage = value;
                    right.ActivePage = value;
                }
            }

            public LeftSideMenuRightSideContent(int width, int height, int leftWidth, int page = 0)
            {
                Width = width;
                Height = height;
                CanMove = true;
                CanCloseWithRightClick = true;
                AcceptMouseInput = true;

                Add(new AlphaBlendControl() { Width = leftWidth, Height = Height, CanMove = true }, page);
                Add(left = new ScrollArea(0, 0, leftWidth, height) { CanMove = true, AcceptMouseInput = true }, page);
                Add(right = new ScrollArea(leftWidth, 0, Width - leftWidth, height) { CanMove = true, AcceptMouseInput = true }, page);

                LeftWidth = leftWidth - ScrollBar.SCROLL_BAR_WIDTH;
                RightWidth = Width - leftWidth;
            }

            public int LeftWidth { get; }
            public int RightWidth { get; }

            public void AddToLeft(Control c, bool autoPosition = true, int page = 0)
            {
                if (autoPosition)
                {
                    c.Y = leftY;
                    c.X = leftX;
                    leftY += c.Height;
                }

                left.Add(c, page);
            }

            public void AddToRight(Control c, bool autoPosition = true, int page = 0)
            {
                if (autoPosition)
                {
                    c.Y = rightY;
                    c.X = rightX;
                    rightY += c.Height + TOP_PADDING;
                }

                right.Add(c, page);
            }

            public void Indent()
            {
                rightX += INDENT_SPACE;
            }

            public void RemoveIndent()
            {
                rightX -= INDENT_SPACE;
                if (rightX < 0)
                {
                    rightX = 0;
                }
            }
        }

        private class SettingsOption
        {
            public SettingsOption(string optionLabel, Control control, int maxTotalWidth, PAGE optionsPage)
            {
                OptionLabel = optionLabel;
                OptionControl = control;
                OptionsPage = optionsPage;
                FullControl = new Area(false) { AcceptMouseInput = true, CanMove = true, CanCloseWithRightClick = true };

                if (!string.IsNullOrEmpty(optionLabel))
                {
                    Control labelTextBox;
                    FullControl.Add(labelTextBox = new TextBox(optionLabel, TrueTypeLoader.EMBEDDED_FONT, 20, null, ColorPallet.TEXT_FOREGROUND, strokeEffect: false));

                    if (labelTextBox.Width > maxTotalWidth)
                    {
                        labelTextBox.Width = maxTotalWidth;
                    }

                    if (labelTextBox.Width + control.Width + 5 > maxTotalWidth)
                    {
                        control.Y = labelTextBox.Height + 5;
                        control.X = 15;
                    }
                    else
                    {
                        control.X = labelTextBox.Width + 5;
                    }
                }

                FullControl.Add(OptionControl);
            }

            public string OptionLabel { get; }
            public Control OptionControl { get; }
            public PAGE OptionsPage { get; }
            public Area FullControl { get; }

            public bool CanBeDisplayed()
            {
                return true;
            }
        }

        private class ModernButton : HitBox, SearchableOption
        {
            private readonly ButtonAction _action;
            private readonly int _groupnumber;
            private bool _isSelected;

            public bool DisplayBorder;

            public bool FullPageSwitch;

            public ModernButton
            (
                int x,
                int y,
                int w,
                int h,
                ButtonAction action,
                string text,
                Color fontColor,
                int groupnumber = 0,
                FontStashSharp.RichText.TextHorizontalAlignment align = FontStashSharp.RichText.TextHorizontalAlignment.Center
            ) : base(x, y, w, h)
            {
                _action = action;

                Add
                (
                    TextLabel = new TextBox(text, TrueTypeLoader.EMBEDDED_FONT, 20, w, fontColor, align, false)
                );

                TextLabel.Y = (h - TextLabel.Height) >> 1;
                _groupnumber = groupnumber;

                ModernOptionsGump.SearchValueChanged += ModernOptionsGump_SearchValueChanged;
            }

            private void ModernOptionsGump_SearchValueChanged(object sender, EventArgs e)
            {
                if (!string.IsNullOrEmpty(ModernOptionsGump.SearchText))
                {
                    if (Search(SearchText))
                    {
                        TextLabel.Alpha = 1f;
                    }
                    else
                    {
                        TextLabel.Alpha = 0.3f;
                    }
                } else
                {
                    TextLabel.Alpha = 1f;
                }
            }

            internal TextBox TextLabel { get; }

            public int ButtonParameter { get; set; }

            public bool IsSelectable { get; set; } = true;

            public bool IsSelected
            {
                get => _isSelected && IsSelectable;
                set
                {
                    if (!IsSelectable)
                    {
                        return;
                    }

                    _isSelected = value;

                    if (value)
                    {
                        Control p = Parent;

                        if (p == null)
                        {
                            return;
                        }

                        IEnumerable<ModernButton> list = p.FindControls<ModernButton>();

                        foreach (ModernButton b in list)
                        {
                            if (b != this && b._groupnumber == _groupnumber)
                            {
                                b.IsSelected = false;
                            }
                        }
                    }
                }
            }

            internal static ModernButton GetSelected(Control p, int group)
            {
                IEnumerable<ModernButton> list = p.FindControls<ModernButton>();

                foreach (ModernButton b in list)
                {
                    if (b._groupnumber == group && b.IsSelected)
                    {
                        return b;
                    }
                }

                return null;
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    IsSelected = true;

                    if (_action == ButtonAction.SwitchPage)
                    {
                        if (!FullPageSwitch)
                        {
                            if (Parent != null)
                            { //Scroll area
                                Parent.ActivePage = ButtonParameter;
                                if (Parent.Parent != null && Parent.Parent is LeftSideMenuRightSideContent)
                                { //LeftSideMenuRightSideContent
                                    ((LeftSideMenuRightSideContent)Parent.Parent).ActivePage = ButtonParameter;
                                }
                            }
                        }
                        else
                        {
                            ChangePage(ButtonParameter);
                        }
                    }
                    else
                    {
                        OnButtonClick(ButtonParameter);
                    }
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (IsSelected)
                {
                    Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);

                    batcher.Draw
                    (
                        _texture,
                        new Vector2(x, y),
                        new Rectangle(0, 0, Width, Height),
                        hueVector
                    );
                }

                if (DisplayBorder)
                {
                    batcher.DrawRectangle(
                        SolidColorTextureCache.GetTexture(Color.LightGray),
                        x, y,
                        Width, Height,
                        ShaderHueTranslator.GetHueVector(0, false, Alpha)
                        );
                }

                return base.Draw(batcher, x, y);
            }

            public bool Search(string text)
            {
                return TextLabel.Text.ToLower().Contains(text.ToLower());
            }
        }

        private class ScrollArea : Control
        {
            private readonly ScrollBarBase _scrollBar;

            public ScrollArea
            (
                int x,
                int y,
                int w,
                int h,
                int scroll_max_height = -1
            )
            {
                X = x;
                Y = y;
                Width = w;
                Height = h;

                _scrollBar = new ScrollBar(Width - 15, 0, Height);

                ScrollMaxHeight = scroll_max_height;

                _scrollBar.MinValue = 0;
                _scrollBar.MaxValue = scroll_max_height >= 0 ? scroll_max_height : Height;
                _scrollBar.Parent = this;
                _scrollBar.IsVisible = true;

                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;
            }


            public int ScrollMaxHeight { get; set; } = -1;
            public ScrollbarBehaviour ScrollbarBehaviour { get; set; }
            public int ScrollValue => _scrollBar.Value;
            public int ScrollMinValue => _scrollBar.MinValue;
            public int ScrollMaxValue => _scrollBar.MaxValue;


            public Rectangle ScissorRectangle;


            public override void Update()
            {
                base.Update();

                CalculateScrollBarMaxValue();
            }

            public int ScrollBarWidth()
            {
                if (_scrollBar == null)
                    return 0;
                return _scrollBar.Width;
            }

            public void Scroll(bool isup)
            {
                if (isup)
                {
                    _scrollBar.Value -= _scrollBar.ScrollStep;
                }
                else
                {
                    _scrollBar.Value += _scrollBar.ScrollStep;
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ScrollBarBase scrollbar = (ScrollBarBase)Children[0];
                scrollbar.Draw(batcher, x + scrollbar.X, y + scrollbar.Y);

                if (batcher.ClipBegin(x + ScissorRectangle.X, y + ScissorRectangle.Y, Width - 14 + ScissorRectangle.Width, Height + ScissorRectangle.Height))
                {
                    for (int i = 1; i < Children.Count; i++)
                    {
                        Control child = Children[i];

                        if (!child.IsVisible || (child.Page != ActivePage && child.Page != 0))
                        {
                            continue;
                        }

                        int finalY = y + child.Y - scrollbar.Value + ScissorRectangle.Y;

                        child.Draw(batcher, x + child.X, finalY);
                    }

                    batcher.ClipEnd();
                }

                return true;
            }


            protected override void OnMouseWheel(MouseEventType delta)
            {
                switch (delta)
                {
                    case MouseEventType.WheelScrollUp:
                        _scrollBar.Value -= _scrollBar.ScrollStep;

                        break;

                    case MouseEventType.WheelScrollDown:
                        _scrollBar.Value += _scrollBar.ScrollStep;

                        break;
                }
            }

            public override void Clear()
            {
                for (int i = 1; i < Children.Count; i++)
                {
                    Children[i].Dispose();
                }
            }

            private void CalculateScrollBarMaxValue()
            {
                _scrollBar.Height = ScrollMaxHeight >= 0 ? ScrollMaxHeight : Height;
                bool maxValue = _scrollBar.Value == _scrollBar.MaxValue && _scrollBar.MaxValue != 0;

                int startX = 0, startY = 0, endX = 0, endY = 0;

                for (int i = 1; i < Children.Count; i++)
                {
                    Control c = Children[i];

                    if (c.IsVisible && !c.IsDisposed)
                    {
                        if (c.X < startX)
                        {
                            startX = c.X;
                        }

                        if (c.Y < startY)
                        {
                            startY = c.Y;
                        }

                        if (c.Bounds.Right > endX)
                        {
                            endX = c.Bounds.Right;
                        }

                        if (c.Bounds.Bottom > endY)
                        {
                            endY = c.Bounds.Bottom;
                        }
                    }
                }

                int width = Math.Abs(startX) + Math.Abs(endX);
                int height = Math.Abs(startY) + Math.Abs(endY) - _scrollBar.Height;
                height = Math.Max(0, height - (-ScissorRectangle.Y + ScissorRectangle.Height));

                if (height > 0)
                {
                    _scrollBar.MaxValue = height;

                    if (maxValue)
                    {
                        _scrollBar.Value = _scrollBar.MaxValue;
                    }
                }
                else
                {
                    _scrollBar.Value = _scrollBar.MaxValue = 0;
                }

                _scrollBar.UpdateOffset(0, Offset.Y);

                for (int i = 1; i < Children.Count; i++)
                {
                    Children[i].UpdateOffset(0, -_scrollBar.Value + ScissorRectangle.Y);
                }
            }
        }

        private class ScrollBar : ScrollBarBase
        {
            public const int SCROLL_BAR_WIDTH = 15;
            private Rectangle _rectSlider,
                _emptySpace;

            private Vector3 hueVector = ShaderHueTranslator.GetHueVector(ColorPallet.BACKGROUND, false, 0.75f);
            private Vector3 hueVectorForeground = ShaderHueTranslator.GetHueVector(ColorPallet.BLACK, false, 0.75f);
            private Texture2D whiteTexture = SolidColorTextureCache.GetTexture(Color.White);

            public ScrollBar(int x, int y, int height)
            {
                Height = height;
                Location = new Point(x, y);
                AcceptMouseInput = true;

                Width = SCROLL_BAR_WIDTH;

                _rectSlider = new Rectangle(
                    0,
                    _sliderPosition,
                    Width,
                    20
                );

                _emptySpace.X = 0;
                _emptySpace.Y = 0;
                _emptySpace.Width = Width;
                _emptySpace.Height = Height;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (Height <= 0 || !IsVisible)
                {
                    return false;
                }

                // draw scrollbar background
                batcher.Draw(
                    whiteTexture,
                    new Rectangle(x, y, Width, Height),
                    hueVector
                );

                // draw slider
                if (MaxValue > MinValue)
                {
                    batcher.Draw(
                        whiteTexture,
                        new Rectangle(x, y + _sliderPosition, Width, 20),
                        hueVectorForeground
                    );
                }

                return base.Draw(batcher, x, y);
            }

            protected override int GetScrollableArea()
            {
                return Height - _rectSlider.Height;
            }

            protected override void OnMouseDown(int x, int y, MouseButtonType button)
            {
                base.OnMouseDown(x, y, button);

                if (_btnSliderClicked && _emptySpace.Contains(x, y))
                {
                    CalculateByPosition(x, y);
                }
            }

            protected override void CalculateByPosition(int x, int y)
            {
                if (y != _clickPosition.Y)
                {
                    y -= _emptySpace.Y + (_rectSlider.Height >> 1);

                    if (y < 0)
                    {
                        y = 0;
                    }

                    int scrollableArea = GetScrollableArea();

                    if (y > scrollableArea)
                    {
                        y = scrollableArea;
                    }

                    _sliderPosition = y;
                    _clickPosition.X = x;
                    _clickPosition.Y = y;

                    if (
                        y == 0
                        && _clickPosition.Y < (_rectSlider.Height >> 1)
                    )
                    {
                        _clickPosition.Y = _rectSlider.Height >> 1;
                    }
                    else if (
                        y == scrollableArea
                        && _clickPosition.Y
                            > Height - (_rectSlider.Height >> 1)
                    )
                    {
                        _clickPosition.Y =
                            Height - (_rectSlider.Height >> 1);
                    }

                    _value = (int)
                        Math.Round(y / (float)scrollableArea * (MaxValue - MinValue) + MinValue);
                }
            }

            public override bool Contains(int x, int y)
            {
                return x >= 0 && x <= Width && y >= 0 && y <= Height;
            }
        }

        private static class ColorPallet
        {
            public const ushort BACKGROUND = 897;
            public const ushort SEARCH_BACKGROUND = 899;
            public const ushort BLACK = 0;

            public static Color BUTTON_FONT_COLOR = Color.White;
            public static Color TEXT_FOREGROUND = Color.White;
        }

        private enum PAGE
        {
            None,
            General,
            Sound,
            Video,
            Macros,
            Tooltip,
            Speech,
            CombatSpells,
            Counters,
            InfoBar,
            Containers,
            Experimental,
            IgnoreList,
            NameplateOptions,
            TUOCooldowns,
            TUOOptions
        }

        private interface SearchableOption
        {
            public bool Search(string text);
        }
    }
}
