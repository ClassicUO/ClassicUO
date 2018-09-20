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
using ClassicUO.Renderer;

namespace ClassicUO.Game.Scenes
{
    public sealed class LoadScene : Scene
    {
        public LoadScene() : base(ScenesType.Loading)
        {
            //ChainActions.Add(OnGameLoading);
            //ChainActions.Add(OnGameLoaded);
        }

        private bool OnGameLoading()
        {
            return true;
        }

        private bool OnGameLoaded()
        {
            return true;
        }


        public override void FixedUpdate(double totalMS, double frameMS)
        {
            base.FixedUpdate(totalMS, frameMS);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatch3D sb3D, SpriteBatchUI sbUI)
        {
            return base.Draw(sb3D, sbUI);
        }
    }
}