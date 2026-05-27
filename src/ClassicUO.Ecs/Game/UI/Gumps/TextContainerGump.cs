// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal abstract class TextContainerGump : Gump
    {
        protected TextContainerGump(World world, uint local, uint server) : base(world, local, server)
        {
            TextRenderer = new TextRenderer(World);
        }

        public TextRenderer TextRenderer { get; }

        public void AddText(TextObject msg)
        {
            if (msg == null)
            {
                return;
            }

            msg.Time = Time.Ticks + 4000;

            TextRenderer.AddMessage(msg);
        }

        public override void Update()
        {
            base.Update();
            TextRenderer.Update();
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

            TextRenderer.Draw
            (
                batcher,
                x,
                y,
                true
            );

            return true;
        }
    }
}