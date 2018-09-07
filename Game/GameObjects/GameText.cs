using ClassicUO.Game.Renderer.Views;
using ClassicUO.IO.Resources;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Game.GameObjects
{
    public enum FontStyle : int
    {
        None = 0x00,

        Solid = 0x01,
        Italic = 0x02,
        Indention = 0x04,
        BlackBorder = 0x08,
        Underline = 0x10,
        Cropped = 0x40,
        BQ = 0x80
    }

    public class GameText : GameObject
    {
        private Rectangle _bounds;

        public GameText(GameObject parent = null,  string text = "") : base(World.Map)
        {
            Parent = parent;
            Text = text;

            Timeout = 2500 + (text.Substring(text.IndexOf('>') + 1).Length * 100);
            if (Timeout > 10000)
            {
                Timeout = 10000;
            }

            Hue = 0xFFFF;
        }

        public bool IsUnicode { get; set; }
        public byte Font { get; set; }
        public TEXT_ALIGN_TYPE Align { get; set; }
        public int MaxWidth { get; set; }
        public FontStyle FontStyle { get; set; }
        public byte Cell { get; set; } = 30;
        public string Text { get; set; }
        public MessageType MessageType { get; set; }
        public GameObject Parent { get; private set; }
        public long Timeout { get; set; }
        public bool IsPersistent { get; set; }
        public bool IsHTML { get; set; }
        public List<WebLinkRect> Links { get; set; } = new List<WebLinkRect>();
        //public new GameTextView View => (GameTextView)base.View;

        public bool IsPartialHue { get; set; }


        public Rectangle Bounds
        {
            get => _bounds;
            set => _bounds = value;
        }

        public int X
        {
            get => _bounds.X;
            set => _bounds.X = value;
        }

        public int Y
        {
            get => _bounds.Y;
            set => _bounds.Y = value;
        }

        public int Width
        {
            get => _bounds.Width;
            set => _bounds.Width = value;
        }

        public int Height
        {
            get => _bounds.Height;
            set => _bounds.Height = value;
        }


        protected override View CreateView() => new GameTextView(this);

        //public override int GetHashCode()
        //{
        //    return Text.GetHashCode() + base.GetHashCode();
        //}

        public override void Update(double frameMS)
        {
            base.Update(frameMS);

            if (IsPersistent)
                return;

            Timeout -= (int)frameMS;
            if (Timeout <= 0)
            {
                Dispose();
            }
        }

        public override void Dispose()
        {
            Parent = null;
            Links.Clear();
            base.Dispose();
        }

    }
}