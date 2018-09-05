using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class Panel : GumpControl
    {
        private readonly SpriteTexture[] _frame = new SpriteTexture[9];

        public Panel(ushort background) : base()
        {
            for (int i = 0; i < _frame.Length; i++)
            {
                _frame[i] = TextureManager.GetOrCreateGumpTexture((ushort)(background + i));
            }
        }


        public override bool Draw(SpriteBatch3D spriteBatch,  Vector3 position)
        {
            int centerWidth = Width - _frame[0].Width - _frame[2].Width;
            int centerHeight = Height - _frame[0].Height - _frame[6].Height;
            int line2Y = (int)position.Y + _frame[0].Height;
            int line3Y = (int)position.Y + Height - _frame[6].Height;
            // top row
            spriteBatch.Draw2D(_frame[0], new Vector3(position.X, position.Y, 0), Vector3.Zero);
            spriteBatch.Draw2DTiled(_frame[1], new Rectangle((int)position.X + _frame[0].Width, (int)position.Y, centerWidth, _frame[0].Height), Vector3.Zero);
            spriteBatch.Draw2D(_frame[2], new Vector3(position.X + Width - _frame[2].Width, position.Y, 0), Vector3.Zero);
            // middle
            spriteBatch.Draw2DTiled(_frame[3], new Rectangle((int)position.X, line2Y, _frame[3].Width, centerHeight), Vector3.Zero);
            spriteBatch.Draw2DTiled(_frame[4], new Rectangle((int)position.X + _frame[3].Width, line2Y, centerWidth, centerHeight), Vector3.Zero);
            spriteBatch.Draw2DTiled(_frame[5], new Rectangle((int)position.X + Width - _frame[5].Width, line2Y, _frame[5].Width, centerHeight), Vector3.Zero);
            // bottom
            spriteBatch.Draw2D(_frame[6], new Vector3(position.X, line3Y, 0), Vector3.Zero);
            spriteBatch.Draw2DTiled(_frame[7], new Rectangle((int)position.X + _frame[6].Width, line3Y, centerWidth, _frame[6].Height), Vector3.Zero);
            spriteBatch.Draw2D(_frame[8], new Vector3(position.X + Width - _frame[8].Width, line3Y, 0), Vector3.Zero);


            return base.Draw(spriteBatch,  position);
        }
    }
}
