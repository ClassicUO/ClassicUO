#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using System;
using System.Collections.Generic;

using ClassicUO.Game.Gumps;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility.Coroutines;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Scenes
{
    public abstract class Scene : IUpdateable, IDisposable
    {
        protected Scene()
        {
        }


        public bool IsDisposed { get; private set; }

        public int RenderedObjectsCount { get; protected set; }

        public CoroutineManager Coroutines { get; } = new CoroutineManager();

        public virtual void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            Unload();
        }

      
        public virtual void Load()
        {
            Coroutine.Start(this, CleaningResources(), "cleaning resources");
        }

        public virtual void Unload()
        {
            Coroutines.Clear();

            //Animations.Clear();
            //Art.Clear();
            //TextmapTextures.Clear();
            //IO.Resources.Gumps.Clear();
            //IO.Resources.Map.Clear();
        }

        public virtual void Update(double totalMS, double frameMS)
        {
            Coroutines.Update();
        }

        public virtual void FixedUpdate(double totalMS, double frameMS)
        {
        }

        public virtual bool Draw(Batcher2D batcher)
        {
            return true;
        }

        private IEnumerable<IWaitCondition> CleaningResources()
        {
            Log.Message(LogTypes.Trace, "Cleaning routine running...");
            while (!IsDisposed)
            {
                Art.ClearUnusedTextures();

                yield return new WaitTime(TimeSpan.FromSeconds(1));

                IO.Resources.Gumps.ClearUnusedTextures();

                yield return new WaitTime(TimeSpan.FromSeconds(1));

                TextmapTextures.ClearUnusedTextures();

                yield return new WaitTime(TimeSpan.FromSeconds(1));

                Animations.ClearUnusedTextures();

                yield return new WaitTime(TimeSpan.FromSeconds(1));

                World.Map?.ClearUnusedBlocks();

                yield return new WaitTime(TimeSpan.FromSeconds(1));
            }
            Log.Message(LogTypes.Trace, "Cleaning routine finished");
        }
    }
}