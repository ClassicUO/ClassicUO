using System;
using ClassicUO.Assets;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting
{
    internal class DocumentationModalGump : Gump
    {
        private const int MODAL_WIDTH = 500;
        private const int MODAL_HEIGHT = 400;

        public DocumentationModalGump(string title, string content) : base(0, 0)
        {
            CanCloseWithRightClick = true;
            IsModal = true;

            Add(new AlphaBlendControl(0.5f)
            {
                Width = Client.Game.Window.ClientBounds.Width,
                Height = Client.Game.Window.ClientBounds.Height,
                BaseColor = Color.Black
            });

            int x = (Client.Game.Window.ClientBounds.Width - MODAL_WIDTH) / 2;
            int y = (Client.Game.Window.ClientBounds.Height - MODAL_HEIGHT) / 2;

            Add(new AlphaBlendControl(1f)
            {
                X = x,
                Y = y,
                Width = MODAL_WIDTH,
                Height = MODAL_HEIGHT,
                BaseColor = new Color(30, 30, 30, 255)
            });

            Add(new UOLabel(title ?? "Documentation", 1, 0x0481, Assets.TEXT_ALIGN_TYPE.TS_CENTER, MODAL_WIDTH - 20) { X = x, Y = y + 12 });

            var scrollArea = new ScrollArea(x + 8, y + 40, MODAL_WIDTH - 24, MODAL_HEIGHT - 52, true)
            {
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
            };
            var docLabel = new UOLabel(content ?? "", 1, 0x0481, Assets.TEXT_ALIGN_TYPE.TS_LEFT, MODAL_WIDTH - 40) { X = 4, Y = 4 };
            scrollArea.Add(docLabel);
            Add(scrollArea);

            var closeBtn = new GothicStyleButton(x + (MODAL_WIDTH - 80) / 2, y + MODAL_HEIGHT - 40, 80, 28, "Close", null, 14);
            closeBtn.OnClick += () => Dispose();
            Add(closeBtn);

            X = 0;
            Y = 0;
            Width = Client.Game.Window.ClientBounds.Width;
            Height = Client.Game.Window.ClientBounds.Height;
        }
    }
}
