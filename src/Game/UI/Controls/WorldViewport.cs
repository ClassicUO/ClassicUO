#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    internal class WorldViewport : Control
    {
        private readonly BlendState _blend = new BlendState
        {
            ColorSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.SourceColor,

            ColorBlendFunction = BlendFunction.Add
        };

        private readonly GameScene _scene;

        private readonly XBREffect _xBR;

        public WorldViewport(GameScene scene, int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _scene = scene;
            AcceptMouseInput = true;

            _xBR = new XBREffect(Engine.Instance.GraphicsDevice);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Rectangle rectangle = ScissorStack.CalculateScissors(Matrix.Identity, x, y, Width, Height);

            if (ScissorStack.PushScissors(rectangle))
            {
                batcher.EnableScissorTest(true);

                ResetHueVector();

                if (Engine.Profile.Current != null && Engine.Profile.Current.UseXBR)
                {
                    // draw regular world
                    _xBR.SetSize(_scene.ViewportTexture.Width, _scene.ViewportTexture.Height);

                    batcher.End();

                    batcher.Begin(_xBR);
                    batcher.Draw2D(_scene.ViewportTexture, x, y, Width, Height, ref _hueVector);
                    batcher.End();

                    batcher.Begin();
                }
                else
                    batcher.Draw2D(_scene.ViewportTexture, x, y, Width, Height, ref _hueVector);


                // draw lights
                if (_scene.UseLights)
                {
                    batcher.SetBlendState(_blend);
                    batcher.Draw2D(_scene.Darkness, x, y, Width, Height, ref _hueVector);
                    batcher.SetBlendState(null);
                }

                // draw overheads
                _scene.DrawSelection(batcher, x, y);
                _scene.DrawOverheads(batcher, x, y);

                base.Draw(batcher, x, y);

                batcher.EnableScissorTest(false);
                ScissorStack.PopScissors();
            }

            return true;
        }


        public override void Dispose()
        {
            _xBR?.Dispose();
            _blend?.Dispose();
            base.Dispose();
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (!Engine.UI.IsMouseOverWorld && Engine.UI.MouseOverControl != null)
            {
                var p = Engine.UI.MouseOverControl.GetFirstControlAcceptKeyboardInput();
                p?.SetKeyboardFocus();
            }
            else
            {
                if (!(Engine.UI.KeyboardFocusControl is TextBox tb && tb.Parent is WorldViewportGump))
                    Parent.GetFirstControlAcceptKeyboardInput()?.SetKeyboardFocus();
            }

            base.OnMouseUp(x, y, button);
        }

        class XBREffect : MatrixEffect
        {
            private readonly EffectParameter _textureSizeParam;
            private Vector2 _vectorSize;

            public XBREffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, Resources.xBREffect)
            {
                _textureSizeParam = Parameters["textureSize"];
            }
            
            public void SetSize(int w, int h)
            {
                _vectorSize.X = w;
                _vectorSize.Y = h;

                _textureSizeParam.SetValue(_vectorSize);
            }
        }
    }
}