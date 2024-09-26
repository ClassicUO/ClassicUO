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

namespace ClassicUO.Game.UI.Gumps
{
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

            return base.Draw(batcher, x, y); ;
        }
        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            UIManager.GetGump<DurabilitysGump>()?.Dispose();
            UIManager.Add(new DurabilitysGump());
        }
    }
    internal class DurabilitysGump : Gump
    {
        private const int WIDTH = 300, HEIGHT = 400;


        private enum DurabilityColors
        {
            RED = 0x0805,
            BLUE = 0x0806,
            GREEN = 0x0808,
            YELLOW = 0x0809
        }

        private readonly Dictionary<string, ContextMenuItemEntry> _menuItems = new Dictionary<string, ContextMenuItemEntry>();
        private DataBox _dataBox;
        public override GumpType GumpType => GumpType.DurabilityGump;

        public DurabilitysGump() : base(0, 0)
        {

            LayerOrder = UILayer.Default;
            CanCloseWithRightClick = true;
            CanMove = true;

            Width = WIDTH;
            Height = HEIGHT;
            X = Client.Game.Scene.Camera.Bounds.Width - Width - 10;
            Y = Client.Game.Scene.Camera.Bounds.Y + 10;


            var _borderControl = new BorderControl
               (
                   0,
                   0,
                   Width,
                   Height,
                   4
               );

            Add(_borderControl);
            Add(new AlphaBlendControl(0.9f) { Width = Width, Height = Height });
            BuildHeader();
            ScrollArea area = new ScrollArea
              (
                  10,
                  30,
                  Width - 20,
                  Height - 50,
                  true
              )
            {
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
            };

            Add(area);


            _dataBox = new DataBox(0, 0, Width - 40, Height - 20);
            area.Add(_dataBox);
            UpdateContents();
        }


        private void BuildHeader()
        {
            var a = new Area();
            a.Width = Width;
            a.WantUpdateSize = false;
            a.CanMove = true;

            Label l;
            a.Add(l = new Label("Equipment Durability", true, 0xFF));
            l.X = (Width >> 1) - (l.Width >> 1);
            l.Y = (l.Height >> 1) >> 1;
            a.Height = l.Y + l.Height;

            Add(a);
        }

        protected override void UpdateContents()
        {
            _dataBox.Clear();
            _dataBox.WantUpdateSize = true;
            Rectangle barBounds = Client.Game.Gumps.GetGump((uint)DurabilityColors.RED).UV;
            var startY = 0;

            var items = World.DurabilityManager?.Durabilities ?? new List<DurabiltyProp>();
            
            foreach (var durability in items.OrderBy(d => d.Percentage))
            {
                if (durability.MaxDurabilty <= 0)
                {
                    continue;
                }
                var item = World.Items.Get((uint)durability.Serial);
                if (item == null)
                {
                    continue;
                }

                var a = new Area();
                a.AcceptMouseInput = true;
                a.WantUpdateSize = false;
                a.CanMove = true;
                a.Height = 44;
                a.Width = Width - (a.X * 2) - 40;
                a.Y = startY;

                Label name;
                a.Add(name = new Label($"{(string.IsNullOrWhiteSpace(item.Name) ? item.Layer : item.Name)}", true, 0xFFFF));
                GumpPic red;
                a.Add(red = new GumpPic(0, name.Y + name.Height + 5, (ushort)DurabilityColors.RED, 0));

                DurabilityColors statusGump = DurabilityColors.GREEN;

                if (durability.Percentage < 0.7)
                {
                    statusGump = DurabilityColors.YELLOW;
                }
                else if (durability.Percentage < 0.95)
                {
                    statusGump = DurabilityColors.BLUE;
                }

                if (durability.Percentage > 0)
                {
                    a.Add(new GumpPicTiled(0, red.Y, (int)Math.Floor(barBounds.Width * durability.Percentage), barBounds.Height, (ushort)statusGump));
                }

                var durWidth = FontsLoader.Instance.GetWidthUnicode(0, $"{durability.Durabilty} / {durability.MaxDurabilty}");

                a.Add(new Label($"{durability.Durabilty} / {durability.MaxDurabilty}", true, 0xFFFF)
                {
                    Y = red.Y - 2,
                    X = Width - 38 - durWidth
                });
                _dataBox.Add(a);

                startY += a.Height + 4;
            }
        }

        public override void Update()
        {
            base.Update();
        }
        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("lastX", X.ToString());
            writer.WriteAttributeString("lastY", Y.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            int.TryParse(xml.GetAttribute("lastX"), out X);
            int.TryParse(xml.GetAttribute("lastY"), out Y);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }
    }
}
