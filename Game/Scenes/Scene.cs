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
using ClassicUO.Interfaces;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.Scenes
{
    public abstract class Scene : IDrawableUI, Interfaces.IUpdateable
    {
        protected Scene()
        {
            ChainActions = new List<Func<bool>>();
        }

        public List<Func<bool>> ChainActions { get; }

        public bool AllowedToDraw { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public SpriteTexture Texture { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
       

        public virtual void Load()
        {

        }

        public virtual void Unload()
        {

        }


        public void Update(double totalMS, double frameMS)
        {
            
        }

        public bool Draw(SpriteBatchUI spriteBatch, Vector3 position)
        {
            return true;
        }
    }
}