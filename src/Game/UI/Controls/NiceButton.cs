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

        public NiceButton(int x, int y, int w, int h, ButtonAction action, string text) : base(x, y, w, h)
        {
            _action = action;
            Label label;
            Add(label = new Label(text, true, 999, w, 0xFF, FontStyle.BlackBorder | FontStyle.Cropped, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = -2,
            });
            label.Y = (h - label.Height) / 2;
        }

        public int ToPage { get; set; }

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
                        if (b != this)
                        {
                            b.IsSelected = false;
                        }
                    }
                }

            }
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                IsSelected = true;

                if (_action == ButtonAction.SwitchPage)
                    ChangePage(ToPage);
                else
                    OnButtonClick(ToPage);
            }
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            if (IsSelected)
                batcher.Draw2D(_texture, position, new Rectangle(0, 0, Width, Height), ShaderHuesTraslator.GetHueVector(0, false, IsTransparent ? Alpha : 0, false));
            return base.Draw(batcher, position, hue);
        }
    }

}
