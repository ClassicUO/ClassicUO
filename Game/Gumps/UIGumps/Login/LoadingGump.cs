using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Scenes;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class LoadingGump : Gump
    {
        public LoadingGump()
            : base(0, 0)
        {
            //if (g_ConnectionScreen.GetType() != CST_CONLOST)
            AddChildren(new ResizePic(0x0A28) { X = 142, Y = 134, Width = 356, Height = 212 });
            // else
            // AddChildren(new ResizePic(0x0A28) { X = 210, Y = 178, Width = 203, Height = 121 });

            AddChildren(new Label(GetText(), false, 0x0386, font: 2, align: IO.Resources.TEXT_ALIGN_TYPE.TS_CENTER) { X = 162, Y = 178, Width = 326 });
        }
        
        private string GetText()
        {
            var loginScene = Service.Get<LoginScene>();

            switch (loginScene.CurrentLoginStep)
            {
                case LoginScene.LoginStep.Connecting:
                    return IO.Resources.Cliloc.GetString(3000002); // "Connecting..."
                case LoginScene.LoginStep.VerifyingAccount:
                    return IO.Resources.Cliloc.GetString(3000003); // "Verifying Account..."
                case LoginScene.LoginStep.LoginInToServer:
                    return IO.Resources.Cliloc.GetString(3000053); // logging into shard
                case LoginScene.LoginStep.EnteringBritania:
                    return IO.Resources.Cliloc.GetString(3000001); // Entering Britania...
                default:
                    return "No Text";
            }
        }
    }
}
