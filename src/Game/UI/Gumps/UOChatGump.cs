using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.UI.Controls;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    class UOChatGump : Gump
    {
        public UOChatGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;

            WantUpdateSize = false;
            Width = 345;
            Height = 390;

            Add(new ResizePic(0x0A28)
            {
                Width = Width,
                Height = Height
            });

            int startY = 25;

            Label text = new Label("Channels", false, 0x0386, 345, 2, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                Y = startY
            };
            Add(text);

            startY += 40;

            HtmlControl html = new HtmlControl(64, startY, 220, 200, true, true, false, isunicode: false);

            Add(html);

            startY = 275;

            text = new Label("Your current channel:", false, 0x0386, 345, 2, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                Y = startY
            };
            Add(text);

            startY += 25;

            text = new Label("{CHANNEL_NAME}", false, 0x0386, 345, 2, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                Y = startY
            };
            Add(text);


            startY = 337;

            Button button = new Button(0, 0x0845, 0x0846, 0x0845)
            {
                X = 48,
                Y = startY + 5,
                ButtonAction = ButtonAction.Activate
            };
            Add(button);

            button = new Button(1, 0x0845, 0x0846, 0x0845)
            {
                X = 123,
                Y = startY + 5,
                ButtonAction = ButtonAction.Activate
            };
            Add(button);

            button = new Button(2, 0x0845, 0x0846, 0x0845)
            {
                X = 216,
                Y = startY + 5,
                ButtonAction = ButtonAction.Activate
            };
            Add(button);

            text = new Label("Join", false, 0x0386, 0, 2, FontStyle.None, TEXT_ALIGN_TYPE.TS_LEFT)
            {
                X = 65,
                Y = startY
            };
            Add(text);

            text = new Label("Leave", false, 0x0386, 0, 2, FontStyle.None, TEXT_ALIGN_TYPE.TS_LEFT)
            {
                X = 140,
                Y = startY
            };
            Add(text);

            text = new Label("Create", false, 0x0386, 0, 2, FontStyle.None, TEXT_ALIGN_TYPE.TS_LEFT)
            {
                X = 233,
                Y = startY
            };
            Add(text);
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0: // join
                    break;
                case 1: // leave
                    break;
                case 2: // create
                    break;
            }
        }
    }
}
