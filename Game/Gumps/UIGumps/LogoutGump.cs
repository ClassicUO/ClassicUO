using System.Reflection.Metadata.Ecma335;
using ClassicUO.Configuration;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class LogoutGump : Gump
    {
        private enum Buttons
        {
            Cancel,
            Ok

        }

        private Settings _settings;
        private Log _log;
        
        public LogoutGump()
            : base(0, 0)
        {
            AddChildren(new GumpPic(0, 0, 0x0816, 0));
            AddChildren(new Label("Quit\nUltima Online?", false, 0x0386, 165 ,align: TEXT_ALIGN_TYPE.TS_CENTER ){X = 38, Y = 30});
            AddChildren(new Button((int) Buttons.Cancel, 0x817, 0x818) { X = 40, Y = 77, ButtonAction = ButtonAction.Activate });
            AddChildren(new Button((int) Buttons.Ok, 0x81A, 0x81B) { X = 100, Y = 77, ButtonAction = ButtonAction.Activate });
            _settings = Service.Get<Settings>();
            _log = Service.Get<Log>();

            CanMove = false;
            ControlInfo.IsModal = true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
            X = 300;
            Y = 300;
//            X = (_settings.GameWindowWidth - Width) / 2;
//            Y = (_settings.GameWindowHeight - Height) / 2;
            //CenterThisControlOnScreen();
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    Dispose();
                    break;
                case 1:
//                    NetClient.Disconnected += (sender, e) => _log.Message(LogTypes.Warning, "Disconnected!");
//                    World.InGame 
                    _log.Message(LogTypes.Trace, "Disconnect Button Clicked...\n", false);
                    Dispose();
                    break;
            }
        }
    }
}