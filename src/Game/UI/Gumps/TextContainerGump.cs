using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Mouse = ClassicUO.Input.Mouse;

namespace ClassicUO.Game.UI.Gumps
{
    abstract class TextContainerGump : Gump
    {
        protected TextContainerGump(Serial local, Serial server) : base(local, server)
        {

        }

        public TextRenderer TextRenderer { get; } = new TextRenderer();


        public void AddText(MessageInfo msg)
        {
            if (World.ClientFeatures.TooltipsEnabled || msg == null)
                return;

            msg.Time = Engine.Ticks + 4000;
           
            TextRenderer.AddMessage(msg);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
            TextRenderer.Update(totalMS, frameMS);
        }

        public override void Dispose()
        {
            TextRenderer.Clear();
            base.Dispose();
        }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            //TextRenderer.MoveToTopIfSelected();
            TextRenderer.ProcessWorldText(true);
            TextRenderer.Draw(batcher, x, y, -1, true);
            return true;
        }
    }
}
