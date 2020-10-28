using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal abstract class TextContainerGump : Gump
    {
        protected TextContainerGump(uint local, uint server) : base(local, server)
        {
        }

        public TextRenderer TextRenderer { get; } = new TextRenderer();


        public void AddText(TextObject msg)
        {
            if (msg == null)
            {
                return;
            }

            msg.Time = Time.Ticks + 4000;

            TextRenderer.AddMessage(msg);
        }

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);
            TextRenderer.Update(totalTime, frameTime);
        }

        public override void Dispose()
        {
            TextRenderer.UnlinkD();

            //TextRenderer.Clear();
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