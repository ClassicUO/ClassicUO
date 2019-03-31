using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    class NiceButton : HitBox
    {
        private bool _isSelected;
        private readonly ButtonAction _action;
        private readonly int _groupnumber;

        public NiceButton(int x, int y, int w, int h, ButtonAction action, string text, int groupnumber = 0) : base(x, y, w, h)
        {
            _action = action;
            Label label;
            Add(label = new Label(text, true, 999, w, 0xFF, FontStyle.BlackBorder | FontStyle.Cropped, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = -2,
            });
            label.Y = (h - label.Height) / 2;
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
                    {
                        list = p.FindControls<NiceButton>();
                    }

                    foreach (var b in list)
                    {
                        if (b != this && b._groupnumber == _groupnumber)
                        {
                            b.IsSelected = false;
                        }
                    }
                }

            }
        }

        internal static NiceButton GetSelected(Control p, int group)
        {
            IEnumerable<NiceButton> list;

            if (p is ScrollArea)
            {
                list = p.FindControls<ScrollAreaItem>().SelectMany(s => s.Children.OfType<NiceButton>());
            }
            else
            {
                list = p.FindControls<NiceButton>();
            }

            foreach (var b in list)
            {
                if (b._groupnumber == group && b.IsSelected)
                {
                    return b;
                }
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

        public override bool Draw(Batcher2D batcher, int x, int y)
        {
            if (IsSelected)
                batcher.Draw2D(_texture, x, y, 0, 0, Width, Height, ShaderHuesTraslator.GetHueVector(0, false, IsTransparent ? Alpha : 0, false));
            return base.Draw(batcher, x, y);
        }
    }

}
