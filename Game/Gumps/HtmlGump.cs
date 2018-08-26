using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Renderer;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class HtmlGump : GumpControl
    {
        private GameText _gameText;

        public HtmlGump(in string[] parts, in string[] lines) : this()
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            int textIndex = int.Parse(parts[5]);
            HasBackground = parts[6] == "1";
            HasScrollbar = parts[7] != "0";
            
            


            _gameText.MaxWidth = (Width - (HasScrollbar ? 15 : 0) - (HasBackground ? 8 : 0));
            _gameText.Text = lines[textIndex];
        }

        public HtmlGump() : base()
        {
            _gameText = new GameText()
            {
                IsPersistent = true,
                IsHTML = true,
                IsUnicode = true,
                Align = AssetsLoader.TEXT_ALIGN_TYPE.TS_LEFT,
                Font = 1,
            };
            CanMove = true;
        }


        public bool HasScrollbar { get; }
        public bool HasBackground { get; }


        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            base.Draw(spriteBatch, position);

            _gameText.View.Draw(spriteBatch, position);

            return true;
        }

    }
}
