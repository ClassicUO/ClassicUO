using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;
using SDL2;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Gumps
{
    public class TextBox : GumpControl
    {
        const float CARAT_BLINK_TIME = 500f;

        private bool _caratBlink;
        private float _lastCaratBlinkTime;
        private RenderedText _text, _carat;

        public TextBox() : base()
        {
            base.AcceptKeyboardInput = true;
            base.AcceptMouseInput = true;
            IsEditable = true;
        }

        public TextBox(string[] parts, string[] lines) : this()
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            Hue = Hue.Parse(parts[5]);
            Graphic = Graphic.Parse(parts[6]);
            Text = lines[int.Parse(parts[7])];
            int maxCharCount = 0;
            if (parts[0] == "textentrylimited")
                maxCharCount = int.Parse(parts[8]);
        }

        public Hue Hue { get; set; }
        public Graphic Graphic { get; set; }
        public int MaxCharCount { get; set; }
        public bool IsPassword { get; set; }
        public bool NumericOnly { get; set; }
        public bool ReplaceDefaultTextOnFirstKeyPress { get; set; }
        public string Text
        {
            get => _text.Text;
            set => _text.Text = value;
        }

        public override bool AcceptMouseInput => base.AcceptMouseInput && IsEditable;
        public override bool AcceptKeyboardInput => base.AcceptKeyboardInput && IsEditable;


        public override void Update(double frameMS)
        {

            base.Update(frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position)
        {
            return base.Draw(spriteBatch, position);
        }

        protected override void OnTextInput(char c)
        {

        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_TAB:
                    break;
                case SDL.SDL_Keycode.SDLK_KP_ENTER:
                    break;
                case SDL.SDL_Keycode.SDLK_BACKSPACE:
                    break;
            }
        }
    }
}
