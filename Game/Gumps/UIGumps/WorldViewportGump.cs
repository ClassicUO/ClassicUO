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
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls.InGame
{
    public class WorldViewportGump : Gump
    {
        private readonly int _worldWidth = 800;
        private readonly int _worldHeight = 600;
        private WorldViewport _viewport;
        private ChatControl _chatControl;
        private readonly GameScene _scene;

        public WorldViewportGump(GameScene scene) : base(0, 0)
        {
            AcceptMouseInput = false;
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            ControlInfo.Layer = UILayer.Under;

            X = 0;
            Y = 0;

            _scene = scene;

            OnResize();
        }


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }


        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null) =>
            base.Draw(spriteBatch, position, hue);

        protected override void OnMove()
        {
            base.OnMove();
        }

        private void OnResize()
        {
            if (Service.Has<ChatControl>())
                Service.Unregister<ChatControl>();

            Clear();

            Width = _worldWidth;
            Height = _worldHeight;

            AddChildren(_viewport = new WorldViewport(_scene, 0, 0, Width, Height));
            AddChildren(_chatControl = new ChatControl(0, 0, Width, Height));

            Service.Register(_chatControl);
        }
    }
}