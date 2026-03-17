// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
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
        private RenderedText _text;
        private ushort _inactive,
            _active;

        public Checkbox(
            GameContext context,
            ushort inactive,
            ushort active,
            string text = "",
            byte font = 0,
            ushort color = 0,
            bool isunicode = true,
            int maxWidth = 0
        ) : base(context)
        {
            _inactive = inactive;
            _active = active;

            CanMove = false;
            AcceptMouseInput = true;

            var uo = Context?.Game?.UO;
            if (uo != null)
            {
                ref readonly var gumpInfoInactive = ref uo.Gumps.GetGump(_inactive);
                ref readonly var gumpInfoActive = ref uo.Gumps.GetGump(_active);

                if (gumpInfoInactive.Texture == null || gumpInfoActive.Texture == null)
                {
                    Dispose();
                    return;
                }

                Width = gumpInfoInactive.UV.Width;

                _text = RenderedText.Create(uo, text, color, font, isunicode, maxWidth: maxWidth);

                Width += _text.Width;

                Height = Math.Max(gumpInfoInactive.UV.Width, _text.Height);
            }
        }

        public Checkbox(List<string> parts, string[] lines, GameContext context)
            : this(context, ushort.Parse(parts[3]), ushort.Parse(parts[4]))
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

        public string Text => _text?.Text ?? string.Empty;

        public event EventHandler ValueChanged;

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (IsDisposed || Context?.Game?.UO == null)
            {
                return false;
            }

            var ok = base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
            float layerDepth = layerDepthRef;

            ref readonly var gumpInfo = ref Context.Game.UO.Gumps.GetGump(
                IsChecked ? _active : _inactive
            );
            var texture = gumpInfo.Texture;
            var sourceRectangle = gumpInfo.UV;
            renderLists.AddGumpWithAtlas
            (
                (batcher) =>
                {
                    batcher.Draw(
                        texture,
                        new Vector2(x, y),
                        sourceRectangle,
                        ShaderHueTranslator.GetHueVector(0),
                        layerDepth
                     );

                    return true;
                }
            );

            if (_text != null)
            {
                renderLists.AddGumpNoAtlas
                (
                    (batcher) =>
                    {
                        _text.Draw(batcher, x + sourceRectangle.Width + 2, y, layerDepth);

                        return true;
                    }
                );
            }

            return ok;
        }

        protected virtual void OnCheckedChanged()
        {
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
