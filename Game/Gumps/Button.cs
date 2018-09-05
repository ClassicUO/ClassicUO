using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Renderer;
using ClassicUO.Input;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace ClassicUO.Game.Gumps
{
    public class Button : GumpControl
    {

        private const int NORMAL = 0;
        private const int PRESSED = 1;
        private const int OVER = 2;

        private readonly SpriteTexture[] _textures = new SpriteTexture[3];
        private int _curentState = NORMAL;
        private GameText _gText;


        public Button(int buttonID,  ushort normal,  ushort pressed,  ushort over = 0) : base()
        {
            ButtonID = buttonID;
            _textures[NORMAL] = TextureManager.GetOrCreateGumpTexture(normal);
            _textures[PRESSED] = TextureManager.GetOrCreateGumpTexture(pressed);
            if (over > 0)
            {
                _textures[OVER] = TextureManager.GetOrCreateGumpTexture(over);
            }

            ref var t = ref _textures[NORMAL];

            Width = t.Width;
            Height = t.Height;

            _gText = new GameText()
            {
                MaxWidth = 100,
                IsPersistent = true
            };

            CanMove = false;
        }

        public Button(string[] parts) :
            this(parts.Length > 7 ? int.Parse(parts[7]) : 0, ushort.Parse(parts[3]), ushort.Parse(parts[4]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);

            ushort type = ushort.Parse(parts[5]);
            ushort param = ushort.Parse(parts[6]);
        }

        public int ButtonID { get; }

        public event EventHandler<MouseEventArgs> Click;

        public string Text
        {
            get => _gText.Text;
            set => _gText.Text = value;
        }



        public override void Update(double frameMS)
        {
            base.Update(frameMS);
        }

        public override bool Draw(SpriteBatch3D spriteBatch,  Vector3 position)
        {
            for (int i = 0; i < _textures.Length; i++)
            {
                if (_textures[i] != null)
                    _textures[i].Ticks = World.Ticks;
            }

            spriteBatch.Draw2D(_textures[_curentState], new Rectangle((int)position.X, (int)position.Y, Width, Height), Vector3.Zero);

            if (Text != string.Empty)
            {
                _gText.GetView().Draw(spriteBatch, position);
            }

            return base.Draw(spriteBatch,  position);
        }


        public override void OnMouseEnter(MouseEventArgs e)
        {
            if (_textures[OVER] != null)
            {
                _curentState = OVER;
            }
            else
                _curentState = NORMAL;
        }

        public override void OnMouseLeft(MouseEventArgs e)
        {
            _curentState = NORMAL;
        }

        public override void OnMouseButton(MouseEventArgs e)
        {
            if (e.Button == Input.MouseButton.Left)
            {
                if (e.ButtonState == ButtonState.Pressed)
                {
                    _curentState = PRESSED;
                    Click.Raise(e);
                }
                else
                {
                    _curentState = NORMAL;
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            _gText.Dispose();
            _gText = null;

            for (int i = 0; i < _textures.Length; i++)
            {
                if (_textures[i] != null)
                    _textures[i].Dispose();
                _textures[i] = null;
            }
        }
    }
}
