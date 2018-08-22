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


        public Button(in GumpControl parent, in int buttonID, in ushort normal, in ushort pressed, in ushort over = 0) : base(parent)
        {
            ButtonID = buttonID;
            _textures[NORMAL] = TextureManager.GetOrCreateGumpTexture(normal);
            _textures[PRESSED] = TextureManager.GetOrCreateGumpTexture(pressed);
            if (over > 0)
            {
                _textures[OVER] = TextureManager.GetOrCreateGumpTexture(over);
            }

            ref var t = ref _textures[NORMAL];

            Bounds = t.Bounds;

            _gText = new GameText()
            {
                MaxWidth = 100,
                IsPersistent = true
            };

        }

        public Button(in GumpControl parent, in string[] parts) :
            this(parent, parts.Length > 7 ? int.Parse(parts[7]) : 0, ushort.Parse(parts[3]), ushort.Parse(parts[4]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);

            ushort type = ushort.Parse(parts[5]);
            ushort param = ushort.Parse(parts[6]);
        }

        public int ButtonID { get; }

        public event EventHandler Click;

        public string Text
        {
            get => _gText.Text;
            set => _gText.Text = value;
        }



        public override void Update(in double frameMS)
        {
            base.Update(in frameMS);
        }

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (IsDisposed)
            {
                return false;
            }

            if (Text != string.Empty)
            {
                _gText.View.Draw(spriteBatch, position);
            }

            //if (Texture != _textures[_curentState])
            //    Texture = _textures[_curentState];

            spriteBatch.Draw2D(_textures[_curentState], Bounds, Vector3.Zero);

            return base.Draw(in spriteBatch, in position);
        }


        public override void OnMouseEnter(in MouseEventArgs e)
        {
            if (_textures[OVER] != null)
            {
                _curentState = OVER;
            }
        }

        public override void OnMouseLeft(in MouseEventArgs e)
        {
            _curentState = NORMAL;
        }

        public override void OnMouseButton(in MouseEventArgs e)
        {
            if (e.Button == Input.MouseButton.Left)
            {
                if (e.ButtonState == ButtonState.Pressed)
                {
                    _curentState = PRESSED;
                    Click.Raise();
                }
                else
                {
                    _curentState = NORMAL;
                }
            }
        }

        public override void Dispose()
        {
            _gText.Dispose();
            _gText = null;

            for (int i = 0; i < _textures.Length; i++)
            {
                _textures[i].Dispose();
                _textures[i] = null;
            }

            base.Dispose();
        }
    }
}
