using System;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting
{
    internal class RenameFolderModalGump : Gump
    {
        private const int MODAL_WIDTH = 360;
        private const int MODAL_HEIGHT = 140;

        public RenameFolderModalGump(string currentName, Action<string> onRename) : base(0, 0)
        {
            CanCloseWithRightClick = true;
            IsModal = true;

            Add(new AlphaBlendControl(0.6f)
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
                BaseColor = new Color(37, 37, 38, 255)
            });

            Add(new UOLabel("Rename folder", 1, 0x0481, Assets.TEXT_ALIGN_TYPE.TS_LEFT, MODAL_WIDTH - 24) { X = x + 16, Y = y + 16 });

            var input = new ClassicUO.Game.UI.Controls.StbTextBox(1, 80, MODAL_WIDTH - 50, true, FontStyle.Fixed, 0x0481)
            {
                X = x + 16,
                Y = y + 44,
                Width = MODAL_WIDTH - 32,
                Height = 24
            };
            input.SetText(currentName ?? "");
            Add(input);

            var renameBtn = new GothicStyleButton(x + MODAL_WIDTH - 180, y + MODAL_HEIGHT - 44, 80, 28, "Rename", null, 12);
            renameBtn.OnClick += () =>
            {
                var txt = input.Text?.Trim().Replace("/", "").Replace("\\", "") ?? "";
                if (!string.IsNullOrEmpty(txt))
                    onRename?.Invoke(txt);
                Dispose();
            };
            Add(renameBtn);

            var cancelBtn = new GothicStyleButton(x + MODAL_WIDTH - 88, y + MODAL_HEIGHT - 44, 72, 28, "Cancel", null, 12);
            cancelBtn.OnClick += () => Dispose();
            Add(cancelBtn);

            X = 0;
            Y = 0;
            Width = Client.Game.Window.ClientBounds.Width;
            Height = Client.Game.Window.ClientBounds.Height;
        }
    }
}
