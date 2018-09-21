#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    public class WorldViewport : GumpControl
    {
        private readonly GameScene _scene;
        private Rectangle _rect;

        public WorldViewport(GameScene scene, int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _scene = scene;
            AcceptMouseInput = true;
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            _rect.X = (int) position.X;
            _rect.Y = (int) position.Y;
            _rect.Width = Width;
            _rect.Height = Height;

            spriteBatch.Draw2D(_scene.ViewportTexture, _rect, Vector3.Zero);

            return base.Draw(spriteBatch, position, hue);
        }


        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            UIManager.KeyboardFocusControl = Service.Get<ChatControl>().GetFirstControlAcceptKeyboardInput();
        }
    }
}