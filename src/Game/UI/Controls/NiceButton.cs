using System.Collections.Generic;
using System.Linq;

using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class NiceButton : HitBox
    {
        private readonly ButtonAction _action;
        private readonly int _groupnumber;
        private bool _isSelected;

        public NiceButton(int x, int y, int w, int h, ButtonAction action, string text, int groupnumber = 0, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_CENTER) : base(x, y, w, h)
        {
            _action = action;
            Label label;

            Add(label = new Label(text, true, 999, w, 0xFF, FontStyle.BlackBorder | FontStyle.Cropped, align)
            {
                X = align == TEXT_ALIGN_TYPE.TS_CENTER ? -2 : 0
            });
            label.Y = (h - label.Height) >> 1;
            _groupnumber = groupnumber;
        }

        public int ButtonParameter { get; set; }

        public bool IsSelectable { get; set; } = true;

        public bool IsSelected
        {
            get => _isSelected && IsSelectable;
            set
            {
                if (!IsSelectable)
                    return;

                _isSelected = value;

                if (value)
                {
                    Control p = Parent;

                    if (p == null)
                        return;

                    IEnumerable<NiceButton> list;

                    if (p is ScrollAreaItem)
                    {
                        p = p.Parent;
                        list = p.FindControls<ScrollAreaItem>().SelectMany(s => s.Children.OfType<NiceButton>());
                    }
                    else
                        list = p.FindControls<NiceButton>();

                    foreach (var b in list)
                    {
                        if (b != this && b._groupnumber == _groupnumber) b.IsSelected = false;
                    }
                }
            }
        }

        internal static NiceButton GetSelected(Control p, int group)
        {
            IEnumerable<NiceButton> list = p is ScrollArea ? p.FindControls<ScrollAreaItem>().SelectMany(s => s.Children.OfType<NiceButton>()) : p.FindControls<NiceButton>();

            foreach (var b in list)
            {
                if (b._groupnumber == group && b.IsSelected) return b;
            }

            return null;
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                IsSelected = true;

                if (_action == ButtonAction.SwitchPage)
                    ChangePage(ButtonParameter);
                else
                    OnButtonClick(ButtonParameter);
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsSelected)
            {
                Vector3 hue = Vector3.Zero;
                ShaderHuesTraslator.GetHueVector(ref hue, 0, false, IsTransparent ? Alpha : 0);
                batcher.Draw2D(_texture, x, y, 0, 0, Width, Height, hue);
            }

            return base.Draw(batcher, x, y);
        }
    }
}