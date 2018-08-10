using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class Label : GumpControl
    {
        private readonly GameText _gText;

        public Label(in GumpControl parent) : base(parent)
        {
            _gText = new GameText() { };
        }


        public string Text
        {
            get => _gText.Text;
            set => _gText.Text = value;
        }

        public Hue Hue
        {
            get => _gText.Hue;
            set => _gText.Hue = value;
        }

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            _gText.Draw(spriteBatch, position);
            return base.Draw(in spriteBatch, in position);
        }
    }
}
