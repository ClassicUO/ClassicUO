using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Managers
{
    internal class HealthLinesManager
    {
        public bool IsEnabled => Engine.Profile.Current != null && Engine.Profile.Current.ShowMobilesHP && Engine.Profile.Current.MobileHPType >= 1;



        public void Draw(Batcher2D batcher, float scale)
        {
            if (!IsEnabled)
                return;

            const int BAR_WIDTH = 27;
            const int BAR_HEIGHT = 3;

            int screenX = Engine.Profile.Current.GameWindowPosition.X;
            int screenY = Engine.Profile.Current.GameWindowPosition.Y;
            int screenW = Engine.Profile.Current.GameWindowSize.X;
            int screenH = Engine.Profile.Current.GameWindowSize.Y;


            Texture2D black = Textures.GetTexture(Color.Black);
            Texture2D red = Textures.GetTexture(Color.Red);

            foreach (Mobile mobile in World.Mobiles)
            {
                int x = screenX + mobile.RealScreenPosition.X;
                int y = screenY + mobile.RealScreenPosition.Y;


                x += (int) mobile.Offset.X + 22;
                y += (int) (mobile.Offset.Y - mobile.Offset.Z) + 22 + 5;

                x += 5;

                x -= BAR_WIDTH / 2;
                y -= BAR_HEIGHT / 2;

                x = (int) (x / scale);
                y = (int) (y / scale);
                x -= (int) (screenX / scale);
                y -= (int) (screenY / scale);
                x += screenX;
                y += screenY;


                if (x < screenX || x > screenX + screenW - BAR_WIDTH)
                    continue;

                if (y < screenY || y > screenY + screenH - BAR_HEIGHT)
                    continue;

                int current = mobile.Hits;
                int max = mobile.HitsMax;


                if (max > 0)
                {
                    max = current * 100 / max;

                    if (max > 100)
                        max = 100;

                    if (max > 1)
                        max = BAR_WIDTH * max / 100;
                }


                batcher.Draw2D(black, x - 1, y - 1, BAR_WIDTH + 2, BAR_HEIGHT + 2, Vector3.Zero);
                batcher.Draw2D(red, x, y, BAR_WIDTH, BAR_HEIGHT, Vector3.Zero);

                Color color;

                if (mobile.IsParalyzed)
                    color = Color.AliceBlue;
                else if (mobile.IsYellowHits)
                    color = Color.Orange;
                else if (mobile.IsPoisoned)
                    color = Color.LimeGreen;
                else
                    color = Color.CornflowerBlue;

                batcher.Draw2D(Textures.GetTexture(color), x, y, max, BAR_HEIGHT, Vector3.Zero);
            }
        }
    }
}