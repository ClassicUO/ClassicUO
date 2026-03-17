// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    internal enum ButtonAction
    {
        Default = 0,
        SwitchPage = 0,
        Activate = 1
    }

    internal class Button : Control
    {
        private readonly string _caption;
        private readonly byte _font;
        private readonly bool _isunicode;
        private bool _entered;
        private RenderedText[] _fontTexture;
        private ushort _normal,
            _pressed,
            _over;

        public Button(
            GameContext context,
            int buttonID,
            ushort normal,
            ushort pressed,
            ushort over = 0,
            string caption = "",
            byte font = 0,
            bool isunicode = true,
            ushort normalHue = ushort.MaxValue,
            ushort hoverHue = ushort.MaxValue
        ) : base(context)
        {
            ButtonID = buttonID;
            _normal = normal;
            _pressed = pressed;
            _over = over;
            _font = font;
            _isunicode = isunicode;

            FontHue = normalHue == ushort.MaxValue ? (ushort)0 : normalHue;
            HueHover = hoverHue == ushort.MaxValue ? normalHue : hoverHue;

            _caption = !string.IsNullOrEmpty(caption) && normalHue != ushort.MaxValue ? caption : null;

            CanMove = false;
            AcceptMouseInput = true;
            //CanCloseWithRightClick = false;
            CanCloseWithEsc = false;

            InitializeSize();
            InitializeRenderedText();
        }

        public Button(List<string> parts, GameContext context)
            : this(
                context,
                parts.Count >= 8 ? int.Parse(parts[7]) : 0,
                UInt16Converter.Parse(parts[3]),
                UInt16Converter.Parse(parts[4])
            )
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);

            if (parts.Count >= 6)
            {
                int action = int.Parse(parts[5]);

                ButtonAction = action == 0 ? ButtonAction.SwitchPage : ButtonAction.Activate;
            }

            ToPage = parts.Count >= 7 ? int.Parse(parts[6]) : 0;
            WantUpdateSize = false;
            ContainsByBounds = true;
            IsFromServer = true;
        }

        private void InitializeSize()
        {
            var uo = Context?.Game?.UO;
            if (uo == null)
                return;

            ref readonly var gumpInfo = ref uo.Gumps.GetGump(_normal);
            if (gumpInfo.Texture == null)
            {
                Dispose();
                return;
            }

            Width = gumpInfo.UV.Width;
            Height = gumpInfo.UV.Height;
        }

        private void InitializeRenderedText()
        {
            if (_caption == null)
                return;

            var uo = Context?.Game?.UO;
            if (uo == null)
                return;

            _fontTexture = new RenderedText[2];
            _fontTexture[0] = RenderedText.Create(uo, _caption, FontHue, _font, _isunicode);

            if (HueHover != ushort.MaxValue)
            {
                _fontTexture[1] = RenderedText.Create(uo, _caption, HueHover, _font, _isunicode);
            }
        }

        public bool IsClicked { get; set; }

        public int ButtonID { get; }

        public ButtonAction ButtonAction { get; set; }

        public int ToPage { get; set; }

        public override ClickPriority Priority => ClickPriority.High;

        public ushort ButtonGraphicNormal
        {
            get => _normal;
            set
            {
                _normal = value;
                InitializeSize();
            }
        }

        public ushort ButtonGraphicPressed
        {
            get => _pressed;
            set
            {
                _pressed = value;
            }
        }

        public ushort ButtonGraphicOver
        {
            get => _over;
            set
            {
                _over = value;
            }
        }

        public int Hue { get; set; }
        public ushort FontHue { get; }

        public ushort HueHover { get; }

        public bool FontCenter { get; set; }

        public bool ContainsByBounds { get; set; }

        protected override void OnMouseEnter(int x, int y)
        {
            _entered = true;
        }

        protected override void OnMouseExit(int x, int y)
        {
            _entered = false;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (IsDisposed || Context?.Game?.UO == null)
                return false;

            float layerDepth = layerDepthRef;
            Texture2D texture = null;
            Rectangle bounds = Rectangle.Empty;

            if (_entered || IsClicked)
            {
                if (IsClicked && _pressed > 0)
                {
                    ref readonly var gumpInfo = ref Context.Game.UO.Gumps.GetGump(_pressed);
                    texture = gumpInfo.Texture;
                    bounds = gumpInfo.UV;
                }

                if (texture == null && _over > 0)
                {
                    ref readonly var gumpInfo = ref Context.Game.UO.Gumps.GetGump(_over);
                    texture = gumpInfo.Texture;
                    bounds = gumpInfo.UV;
                }
            }

            if (texture == null)
            {
                ref readonly var gumpInfo = ref Context.Game.UO.Gumps.GetGump(_normal);
                texture = gumpInfo.Texture;
                bounds = gumpInfo.UV;
            }

            if (texture == null)
            {
                return false;
            }

            var hue = ShaderHueTranslator.GetHueVector(Hue, false, Alpha, true);

            renderLists.AddGumpWithAtlas(
                batcher =>
                {
                    batcher.Draw(texture, new Rectangle(x, y, Width, Height), bounds, hue, layerDepth);
                    return true;
                });


            if (_caption != null && _fontTexture != null)
            {
                RenderedText textTexture = _fontTexture[_entered ? 1 : 0];

                if (textTexture != null)
                {
                    if (FontCenter)
                    {
                        int yoffset = IsClicked ? 1 : 0;

                        renderLists.AddGumpNoAtlas(
                            batcher => textTexture.Draw(
                            batcher,
                            x + ((Width - textTexture.Width) >> 1),
                            y + yoffset + ((Height - textTexture.Height) >> 1),
                            depth: layerDepth
                        ));
                    }
                    else
                    {
                        renderLists.AddGumpNoAtlas(
                            batcher => textTexture.Draw(batcher, x, y, depth: layerDepth)
                        );
                    }
                }
            }

            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsClicked = true;
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsClicked = false;

                if (!MouseIsOver)
                {
                    return;
                }

                if (_entered || Context.Game.Scene is GameScene)
                {
                    switch (ButtonAction)
                    {
                        case ButtonAction.SwitchPage:
                            ChangePage(ToPage);

                            break;

                        case ButtonAction.Activate:
                            OnButtonClick(ButtonID);

                            break;
                    }

                    Mouse.LastLeftButtonClickTime = 0;
                    Mouse.CancelDoubleClick = true;
                }
            }
        }

        public override bool Contains(int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            return ContainsByBounds
                ? base.Contains(x, y)
                : Context.Game.UO.Gumps.PixelCheck(_normal, x - Offset.X, y - Offset.Y);
        }

        public sealed override void Dispose()
        {
            if (_fontTexture != null)
            {
                foreach (RenderedText t in _fontTexture)
                {
                    t?.Destroy();
                }
            }

            base.Dispose();
        }
    }
}
