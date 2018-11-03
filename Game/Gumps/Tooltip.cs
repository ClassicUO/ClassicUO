using System.Text;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;


namespace ClassicUO.Game.Gumps
{
    public class Tooltip : IDrawableUI
    {
        private RenderedText _renderedText;
        private Entity _gameObject;
        private uint _hash;

        private string _textHTML;


        public string Text { get; protected set; }

        public bool AllowedToDraw { get; set; } = true;

        public SpriteTexture Texture { get; set; }


        public bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            if (_gameObject != null && _hash != _gameObject.PropertiesHash)
            {
                _hash = _gameObject.PropertiesHash;
                Text = ReadProperties(_gameObject, out _textHTML);
            }

            if (string.IsNullOrEmpty(Text))
                return false;

            if (_renderedText == null)
            {
                _renderedText = new RenderedText()
                {
                    Align = TEXT_ALIGN_TYPE.TS_CENTER, Font = 1, IsUnicode = true, IsHTML = true, Cell = 5, FontStyle = FontStyle.BlackBorder,
                };
            }
            else if (_renderedText.Text != Text)
            {
                Fonts.RecalculateWidthByInfo = true;
                int width = Fonts.GetWidthUnicode(1, Text);

                if (width > 600)
                    width = 600;

                _renderedText.MaxWidth = width;
                _renderedText.Text = _textHTML;
                Fonts.RecalculateWidthByInfo = false;
            }

            spriteBatch.Draw2D(CheckerTrans.TransparentTexture, new Rectangle((int) position.X - 4, (int) position.Y - 4, _renderedText.Width + 8, _renderedText.Height + 8), RenderExtentions.GetHueVector(0, false, 0.5f, false));

            _renderedText.Draw(spriteBatch, position);
            return true;
        }

        public void Clear() => _textHTML = Text = null;

        public void SetGameObject(Entity obj)
        {
            if (_gameObject == null || obj != _gameObject || obj.PropertiesHash != _gameObject.PropertiesHash)
            {
                _gameObject = obj;
                _hash = obj.PropertiesHash;
                Text = ReadProperties(obj, out _textHTML);
            }
        }

        private string ReadProperties(Entity obj, out string htmltext)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder sbHTML = new StringBuilder();

            bool hasStartColor = false;

            for (int i = 0; i < obj.Properties.Count; i++)
            {
                Property property = obj.Properties[i];

                if (property.Cliloc <= 0)
                    continue;

                if (i == 0 /*&& !string.IsNullOrEmpty(obj.Name)*/)
                {
                    if (obj.Serial.IsMobile)
                    {
                        Mobile mobile = (Mobile)obj;
                        //ushort hue = Notoriety.GetHue(mobile.NotorietyFlag);

                        sbHTML.Append(Notoriety.GetHTMLHue(mobile.NotorietyFlag));
                    }
                    else
                    {
                        sbHTML.Append("<basefont color=\"yellow\">");
                    }

                    hasStartColor = true;
                }

                

                string text = Cliloc.Translate((int)property.Cliloc, property.Args, true);

                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                sb.Append(text);
                sbHTML.Append(text);

                if (hasStartColor)
                {
                    sbHTML.Append("<basefont color=\"#FFFFFFFF\">");
                    hasStartColor = false;
                }


                if (i < obj.Properties.Count - 1)
                {
                    sb.Append("\n");
                    sbHTML.Append("\n");
                }
            }

            htmltext = sbHTML.ToString();
            string result= sb.ToString();

            return string.IsNullOrEmpty(result) ? null : sb.ToString();
        }

        public void SetText(string text)
        {
            _gameObject = null;
            Text = _textHTML = text;
        }

    }
}
