using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    class DevConsole : GumpControl
    {
        const ushort BLACK = 0x62E;
        const ushort GRAY = 0x640;

        public DevConsole() : base()
        {

        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position)
        {
            return base.Draw(spriteBatch, position);
        }
    }
}
