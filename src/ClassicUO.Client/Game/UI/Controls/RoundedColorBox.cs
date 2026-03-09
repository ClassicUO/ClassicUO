using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    public class RoundedColorBox : Control
    {
        private Color backgroundColor;
        private readonly int borderRadius;

        public RoundedColorBox(int width, int height, Color backgroundColor, int borderRadius = 8)
        {
            CanMove = false;
            Width = width;
            Height = height;
            this.backgroundColor = backgroundColor;
            this.borderRadius = borderRadius;
            WantUpdateSize = false;
        }

        public Color BackgroundColor
        {
            get => backgroundColor; 
            set
            {
                backgroundColor = value;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            // Draw rounded rectangle using multiple rectangles with direct RGB color
            DrawRoundedRectangle(batcher, x, y, Width, Height, borderRadius, backgroundColor);
            return true;
        }

        private void DrawRoundedRectangle(UltimaBatcher2D batcher, int x, int y, int width, int height, int radius, Color color)
        {
            Texture2D texture = SolidColorTextureCache.GetTexture(color);
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);

            // Main rectangle (center)
            if (width > radius * 2 && height > radius * 2)
            {
                batcher.Draw(
                    texture,
                    new Rectangle(x + radius, y + radius, width - radius * 2, height - radius * 2),
                    hueVector
                );
            }

            // Top and bottom rectangles
            if (width > radius * 2)
            {
                batcher.Draw(
                    texture,
                    new Rectangle(x + radius, y, width - radius * 2, radius),
                    hueVector
                );
                batcher.Draw(
                    texture,
                    new Rectangle(x + radius, y + height - radius, width - radius * 2, radius),
                    hueVector
                );
            }

            // Left and right rectangles
            if (height > radius * 2)
            {
                batcher.Draw(
                    texture,
                    new Rectangle(x, y + radius, radius, height - radius * 2),
                    hueVector
                );
                batcher.Draw(
                    texture,
                    new Rectangle(x + width - radius, y + radius, radius, height - radius * 2),
                    hueVector
                );
            }

            // Corner circles (simplified as small rectangles for performance)
            if (radius > 0)
            {
                // Top-left corner
                batcher.Draw(
                    texture,
                    new Rectangle(x, y, radius, radius),
                    hueVector
                );
                // Top-right corner
                batcher.Draw(
                    texture,
                    new Rectangle(x + width - radius, y, radius, radius),
                    hueVector
                );
                // Bottom-left corner
                batcher.Draw(
                    texture,
                    new Rectangle(x, y + height - radius, radius, radius),
                    hueVector
                );
                // Bottom-right corner
                batcher.Draw(
                    texture,
                    new Rectangle(x + width - radius, y + height - radius, radius, radius),
                    hueVector
                );
            }
        }
    }
}
