using ClassicUO.Configuration;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class LogoutGump : Gump
    {
        private Settings _settings;

        public LogoutGump()
            : base(0, 0)
        {
            AddChildren(new GumpPic(0, 0, 0x0816, 0));
            AddChildren(new Label("Quit\nUltima Online?", false, 0x0386, 165)
                {X = 33, Y = 30});
            AddChildren(new Button((int) Buttons.Cancel, 0x817, 0x818)
                {X = 37, Y = 75, ButtonAction = ButtonAction.Activate});
            AddChildren(new Button((int) Buttons.Ok, 0x81A, 0x81B)
                {X = 100, Y = 75, ButtonAction = ButtonAction.Activate});
            _settings = Service.Get<Settings>();

            CanMove = false;
            ControlInfo.IsModal = true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            X = (UIManager.Width - Width) / 2;
            Y = (UIManager.Height - Height) / 2;
            base.Update(totalMS, frameMS);
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    Dispose();
                    break;
                case 1:
                    Service.Get<SceneManager>().ChangeScene(ScenesType.Login);
                    Dispose();
                    break;
            }
        }

        private enum Buttons
        {
            Cancel,
            Ok
        }
    }
}