using System;
using System.Collections.Generic;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.GameObjects.Interfaces;
using ClassicUO.Game.Renderer;
using ClassicUO.Game.Renderer.Views;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        public GameText(in GameObject parent = null, in string text = "") : base(World.Map)
        {
            Parent = parent;
            Text = text;
        }

        public bool IsUnicode { get; set; }
        public bool IsPartialHue { get; set; }
        public byte Font { get; set; }
        public TEXT_ALIGN_TYPE Align { get; set; }
        public byte MaxWidth { get; set; } 
        public FontStyle FontStyle { get; set; }
        public byte Cell { get; set; }
        public string Text { get; set; }
        public MessageType MessageType { get; set; }
        public GameObject Parent { get; }

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



        protected override View CreateView()
        {
            return new GameTextView(this);
        }



        public override int GetHashCode()
        {
            return Text.GetHashCode() + base.GetHashCode();
        }
    }
}