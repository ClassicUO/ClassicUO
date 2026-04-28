// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    internal class Checkbox : Control
    {
        private bool _isChecked;
        private readonly RenderedText _text;
        private ushort _inactive,
            _active;

        public Checkbox(
            ushort inactive,
            ushort active,
            string text = "",
            byte font = 0,
            ushort color = 0,
            bool isunicode = true,
            int maxWidth = 0
        )
        {
            _inactive = inactive;
            _active = active;

            ref readonly var gumpInfoInactive = ref Client.Game.UO.Gumps.GetGump(inactive);
            ref readonly var gumpInfoActive = ref Client.Game.UO.Gumps.GetGump(active);

            if (gumpInfoInactive.Texture == null || gumpInfoActive.Texture == null)
            {
                Dispose();

                return;
            }

            Width = gumpInfoInactive.UV.Width;

            _text = RenderedText.Create(text, color, font, isunicode, maxWidth: maxWidth);

            Width += _text.Width;

            Height = Math.Max(gumpInfoInactive.UV.Width, _text.Height);
            CanMove = false;
            AcceptMouseInput = true;
        }

        public Checkbox(List<string> parts, string[] lines)
            : this(ushort.Parse(parts[3]), ushort.Parse(parts[4]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            IsChecked = parts[5] == "1";
            LocalSerial = SerialHelper.Parse(parts[6]);
            IsFromServer = true;
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

        public event EventHandler ValueChanged;

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (IsDisposed)
            {
                return false;
            }

            var ok = base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(
                IsChecked ? _active : _inactive
            );

            if (gumpInfo.Texture != null)
            {
                renderLists.AddGumpSprite(
                    gumpInfo.Texture,
                    gumpInfo.UV,
                    new Rectangle(x, y, gumpInfo.UV.Width, gumpInfo.UV.Height),
                    ShaderHueTranslator.GetHueVector(0),
                    layerDepthRef
                );
            }

            renderLists.AddGumpNoAtlas(_text, x + gumpInfo.UV.Width + 2, y, layerDepthRef);

            return ok;
        }

        protected virtual void OnCheckedChanged()
        {
            // The check state selects between _active and _inactive graphics in
            // AddToRenderLists; without this notify, the owning gump's retained
            // render cache keeps replaying the old sprite until some other path
            // (drag, resize, etc.) bumps the version.
            NotifyRenderDirty();
            ValueChanged.Raise(this);
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
            _text?.Destroy();
        }
    }
}
