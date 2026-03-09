using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ClassicUO.Assets;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Point = Microsoft.Xna.Framework.Point;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class SolidColorBox : Control
    {
        private readonly Color _color;
        private readonly Vector3 _hueVector;

        public SolidColorBox(int x, int y, int w, int h, Color color)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _color = color;
            _hueVector = ShaderHueTranslator.GetHueVector(0, false, MathHelper.Clamp(color.A / 255f, 0f, 1f));
            WantUpdateSize = false;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            batcher.Draw(SolidColorTextureCache.GetTexture(_color), new Rectangle(x, y, Width, Height), _hueVector);
            return base.Draw(batcher, x, y);
        }
    }

    internal sealed class ItemArtControl : Control
    {
        private readonly ushort _graphic;
        private readonly ushort _hue;
        private readonly bool _isPartial;
        private readonly Vector3 _hueVector;

        public ItemArtControl(ushort graphic, ushort hue, int displaySize = 40)
        {
            Width = Height = displaySize;
            _graphic = graphic;
            _hue = hue;
            _isPartial = graphic < TileDataLoader.Instance.StaticData.Length && TileDataLoader.Instance.StaticData[graphic].IsPartialHue;
            _hueVector = ShaderHueTranslator.GetHueVector(hue, _isPartial, 1f);
            WantUpdateSize = false;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ref readonly var artInfo = ref Client.Game.Arts.GetArt(_graphic);
            if (artInfo.Texture == null)
                return base.Draw(batcher, x, y);
            batcher.Draw(artInfo.Texture, new Rectangle(x, y, Width, Height), artInfo.UV, _hueVector);
            return base.Draw(batcher, x, y);
        }
    }
    internal class DurabilityGumpMinimized : Gump
    {
        public uint Graphic { get; set; } = 5587;

        public DurabilityGumpMinimized() : base(0, 0)
        {
            SetTooltip("Open Equipment Durability Tracker");

            WantUpdateSize = true;
            AcceptMouseInput = true;
            Width = 30;
            Height = 30;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ref readonly var texture = ref Client.Game.Gumps.GetGump(Graphic);
            if (texture.Texture != null)
            {
                Rectangle rect = new Rectangle(x, y, Width, Height);
                batcher.Draw
                (
                    texture.Texture,
                    rect,
                    texture.UV,
                    ShaderHueTranslator.GetHueVector(0)
                );
            }

            return base.Draw(batcher, x, y);
        }
        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            UIManager.GetGump<DurabilitysGump>()?.Dispose();
            UIManager.Add(new DurabilitysGump());
        }
    }

    internal class DurabilitysGumpOld : Gump
    {
        private const int WIDTH = 240;
        private const int HEIGHT = 160;
        private const int ROW_HEIGHT = 20;
        private const int PADDING = 6;

        private enum DurabilityColors
        {
            RED = 0x0805,
            BLUE = 0x0806,
            GREEN = 0x0808,
            YELLOW = 0x0809
        }

        private readonly ScrollArea _scrollArea;
        private readonly DataBox _dataBox;

        public override GumpType GumpType => GumpType.DurabilityGump;

        public DurabilitysGumpOld() : base(0, 0)
        {
            LayerOrder = UILayer.Default;
            CanCloseWithRightClick = true;
            CanMove = true;
            AcceptMouseInput = true;
            SetTooltip("Equipment Durability");
            Width = WIDTH;
            Height = HEIGHT;
            X = (Client.Game.Scene.Camera.Bounds.Width - Width) / 2;
            Y = Client.Game.Scene.Camera.Bounds.Y + 20;

            Add(new ResizePic(0x0A28) { X = 0, Y = 0, Width = Width, Height = Height });
            _scrollArea = new ScrollArea(PADDING, PADDING, Width - (PADDING * 2), Height - (PADDING * 2), true)
            {
                ScrollbarBehaviour = ScrollbarBehaviour.ShowWhenDataExceedFromView
            };
            Add(_scrollArea);
            _dataBox = new DataBox(0, 0, Width - (PADDING * 2) - 20, Height - (PADDING * 2));
            _scrollArea.Add(_dataBox);
        }

        protected override void UpdateContents()
        {
            _dataBox.Clear();
            _dataBox.WantUpdateSize = true;
            var barBounds = Client.Game.Gumps.GetGump((uint)DurabilityColors.RED).UV;
            var contentWidth = Width - (PADDING * 2) - 24;
            var nameWidth = (int)(contentWidth * 0.4f);
            var barWidth = (int)(contentWidth * 0.35f);
            var barX = nameWidth + 4;
            var barH = Math.Min(barBounds.Height, ROW_HEIGHT - 4);
            var startY = 0;

            var items = World.DurabilityManager?.Durabilities ?? new List<DurabiltyProp>();

            foreach (var durability in items.OrderBy(d => d.Percentage))
            {
                if (durability.MaxDurabilty <= 0)
                    continue;

                var item = World.Items.Get((uint)durability.Serial);
                if (item == null)
                    continue;

                var row = new Area(false);
                row.Height = ROW_HEIGHT;
                row.Width = contentWidth;
                row.Y = startY;

                var nameStr = string.IsNullOrWhiteSpace(item.Name) ? item.Layer.ToString() : item.Name;
                if (FontsLoader.Instance.GetWidthUnicode(0, nameStr) > nameWidth)
                {
                    while (nameStr.Length > 2 && FontsLoader.Instance.GetWidthUnicode(0, nameStr + "..") > nameWidth)
                        nameStr = nameStr.Substring(0, nameStr.Length - 1);
                    nameStr += "..";
                }

                row.Add(new Label(nameStr, true, 0xFFFF, maxwidth: nameWidth, ishtml: true) { Y = 2 });
                row.Add(new GumpPic(barX, (ROW_HEIGHT - barH) / 2, (ushort)DurabilityColors.RED, 0));

                DurabilityColors statusGump = DurabilityColors.GREEN;
                if (durability.Percentage < 0.7)
                    statusGump = DurabilityColors.YELLOW;
                else if (durability.Percentage < 0.95)
                    statusGump = DurabilityColors.BLUE;

                if (durability.Percentage > 0)
                {
                    var fillW = (int)(barWidth * durability.Percentage);
                    if (fillW > 0)
                        row.Add(new GumpPicTiled(barX, (ROW_HEIGHT - barH) / 2, fillW, barH, (ushort)statusGump));
                }

                row.Add(new Label($"{durability.Durabilty}/{durability.MaxDurabilty}", true, 0xFFFF)
                {
                    X = nameWidth + barWidth + 8,
                    Y = 2
                });

                _dataBox.Add(row);
                startY += ROW_HEIGHT + 2;
            }

            _dataBox.Update();
            _scrollArea.UpdateScrollbarPosition();
            _scrollArea.SlowUpdate();
        }
    }
    internal class DurabilitysGump : ResizableGump
    {
        private const int DEFAULT_WIDTH = 180;
        private const int DEFAULT_HEIGHT = 180;
        private const int MIN_WIDTH = 140;
        private const int MIN_HEIGHT = 150;
        private const int BORDER_WIDTH = 4;
        private const int ITEM_ART_SIZE = 32;
        private const int ROW_HEIGHT = 32;
        private const int PADDING = 4;
        private static int _lastX;
        private static int _lastY;
        private static int _lastWidth = DEFAULT_WIDTH;
        private static int _lastHeight = DEFAULT_HEIGHT;

        private enum DurabilityColors
        {
            RED = 0x0805,
            BLUE = 0x0806,
            GREEN = 0x0808,
            YELLOW = 0x0809
        }

        private readonly AlphaBlendControl _background;
        private readonly ScrollArea _scrollArea;
        private readonly DataBox _dataBox;

        public override GumpType GumpType => GumpType.DurabilityGump;

        public DurabilitysGump() : base(_lastWidth, _lastHeight, MIN_WIDTH, MIN_HEIGHT, 0, 0)
        {
            LayerOrder = UILayer.Default;
            CanCloseWithRightClick = true;
            CanMove = true;
            AcceptMouseInput = true;
            SetTooltip("Equipment Durability");

            X = _lastX;
            Y = _lastY;
            if (_lastX == default || _lastY == default)
            {
                X = _lastX = (Client.Game.Scene.Camera.Bounds.Width - Width) / 2;
                Y = _lastY = Client.Game.Scene.Camera.Bounds.Y + 20;
            }

            Insert(0, _background = new AlphaBlendControl(0.65f)
            {
                X = BORDER_WIDTH,
                Y = BORDER_WIDTH,
                Width = Width - (BORDER_WIDTH * 2),
                Height = Height - (BORDER_WIDTH * 2)
            });

            _scrollArea = new ScrollArea(PADDING, PADDING, Width - (PADDING * 2), Height - (PADDING * 2), true)
            {
                ScrollbarBehaviour = ScrollbarBehaviour.ShowWhenDataExceedFromView
            };
            Add(_scrollArea);

            _dataBox = new DataBox(0, 0, Width - (PADDING * 2) - 20, Height - (PADDING * 2));
            _scrollArea.Add(_dataBox);

            UpdateContents();
        }

        public override void OnResize()
        {
            base.OnResize();
            Reposition();
        }

        private void Reposition()
        {
            if (IsDisposed || _background == null || _scrollArea == null || _dataBox == null)
                return;

            _background.X = BORDER_WIDTH;
            _background.Y = BORDER_WIDTH;
            _background.Width = Width - (BORDER_WIDTH * 2);
            _background.Height = Height - (BORDER_WIDTH * 2);

            _scrollArea.X = PADDING;
            _scrollArea.Y = PADDING;
            _scrollArea.Width = Width - (PADDING * 2);
            _scrollArea.Height = Height - (PADDING * 2);

            _dataBox.Width = Width - (PADDING * 2) - 20;

            _lastWidth = Width;
            _lastHeight = Height;
            UpdateContents();
            _dataBox.Update();
            _scrollArea.UpdateScrollbarPosition();
            _scrollArea.SlowUpdate();
        }

        public override void Dispose()
        {
            base.Dispose();
            _lastX = X;
            _lastY = Y;
        }

        protected override void UpdateContents()
        {
            _dataBox.Clear();
            _dataBox.WantUpdateSize = true;
            var contentWidth = Width - (PADDING * 2) - 24;
            var barX = ITEM_ART_SIZE;
            const int BAR_VERT_WIDTH = 4;
            const int BAR_VERT_HEIGHT = 28;
            var startY = 0;

            var items = World.DurabilityManager?.Durabilities ?? new List<DurabiltyProp>();

            foreach (var durability in items.OrderBy(d => d.Percentage))
            {
                if (durability.MaxDurabilty <= 0)
                    continue;

                var item = World.Items.Get((uint)durability.Serial);
                if (item == null)
                    continue;

                var durText = $"{durability.Durabilty}/{durability.MaxDurabilty}";
                ushort durHue = 0x0040;
                if (durability.Percentage < 0.7)
                    durHue = 0x0035;
                else if (durability.Percentage < 0.95)
                    durHue = 0x0058;

                var durLabel = new Label(durText, true, durHue);
                var barY = (ROW_HEIGHT - BAR_VERT_HEIGHT) / 2;

                var row = new Area(false);
                row.AcceptMouseInput = true;
                row.WantUpdateSize = false;
                row.Height = ROW_HEIGHT;
                row.Width = contentWidth;
                row.Y = startY;

                row.Add(new ItemArtControl(item.DisplayedGraphic, item.Hue, ITEM_ART_SIZE)
                {
                    X = 0,
                    Y = (ROW_HEIGHT - ITEM_ART_SIZE) / 2
                });

                row.Add(new SolidColorBox(barX, barY, BAR_VERT_WIDTH, BAR_VERT_HEIGHT, Color.DarkGray));

                Color fillColor = Color.Green;
                if (durability.Percentage < 0.7)
                    fillColor = Color.Yellow;
                else if (durability.Percentage < 0.95)
                    fillColor = Color.Blue;

                if (durability.Percentage > 0)
                {
                    var fillH = (int)(BAR_VERT_HEIGHT * durability.Percentage);
                    if (fillH > 0)
                        row.Add(new SolidColorBox(barX, barY + BAR_VERT_HEIGHT - fillH, BAR_VERT_WIDTH, fillH, fillColor));
                }

                durLabel.X = contentWidth - durLabel.Width;
                durLabel.Y = (ROW_HEIGHT - durLabel.Height) / 2;
                row.Add(durLabel);

                row.Add(new SolidColorBox(0, ROW_HEIGHT - 1, contentWidth, 1, Color.Gray));
                _dataBox.Add(row);
                startY += ROW_HEIGHT + 2;
            }
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("lastX", X.ToString());
            writer.WriteAttributeString("lastY", Y.ToString());
            writer.WriteAttributeString("rw", Width.ToString());
            writer.WriteAttributeString("rh", Height.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            int.TryParse(xml.GetAttribute("lastX"), out int xVal);
            int.TryParse(xml.GetAttribute("lastY"), out int yVal);
            if (xVal != 0 || yVal != 0)
            {
                X = xVal;
                Y = yVal;
            }
            Point savedSize = new Point(Width, Height);
            if (int.TryParse(xml.GetAttribute("rw"), out int w) && w >= MIN_WIDTH)
                savedSize.X = w;
            if (int.TryParse(xml.GetAttribute("rh"), out int h) && h >= MIN_HEIGHT)
                savedSize.Y = h;
            ResizeWindow(savedSize);
        }
    }
}
