using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Renderer;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
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
                Align = IO.Resources.TEXT_ALIGN_TYPE.TS_LEFT,
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

        public override void OnMouseLeft(in MouseEventArgs e)
        {
            if (e.ButtonState == Microsoft.Xna.Framework.Input.ButtonState.Released)
            {
                for (int i = 0; i < _gameText.Links.Count; i++)
                {
                    var link = _gameText.Links[i];
                    Rectangle rect = new Rectangle(X + link.StartX, Y + link.StartY, link.EndX, link.EndY);
                    bool inbounds = rect.Contains(e.Location.X - ParentX, e.Location.Y - ParentY);
                    if (inbounds && Fonts.GetWebLink(link.LinkID, out var result))
                    {
                        Utility.Log.Message(Utility.LogTypes.Info, "LINK CLICKED: " + result.Link);
                        Utility.BrowserHelper.OpenBrowser(result.Link);
                        break;
                    }
                }
            }
        }

    }
}
