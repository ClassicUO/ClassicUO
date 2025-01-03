// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class NiceButton : HitBox
    {
        private readonly ButtonAction _action;
        private readonly int _groupnumber;
        private bool _isSelected;

        public NiceButton
        (
            int x,
            int y,
            int w,
            int h,
            ButtonAction action,
            string text,
            int groupnumber = 0,
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_CENTER,
            ushort hue = 0xFFFF,
            bool unicode = true,
            byte font = 0xFF
        ) : base(x, y, w, h)
        {
            _action = action;

            Add
            (
                TextLabel = new Label
                (
                    text,
                    unicode,
                    hue,
                    w,
                    font,
                    FontStyle.BlackBorder | FontStyle.Cropped,
                    align
                )
            );

            TextLabel.Y = (h - TextLabel.Height) >> 1;
            _groupnumber = groupnumber;
        }

        internal Label TextLabel { get; }

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

                    IEnumerable<NiceButton> list = p.FindControls<NiceButton>();

                    foreach (NiceButton b in list)
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
            IEnumerable<NiceButton> list = p.FindControls<NiceButton>();

            foreach (NiceButton b in list)
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
                    ChangePage(ButtonParameter);
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

            return base.Draw(batcher, x, y);
        }
    }
}