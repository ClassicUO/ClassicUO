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

using System;
using System.Collections.Generic;

using ClassicUO.Game.Managers;
using ClassicUO.Interfaces;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Utility.Coroutines;
using ClassicUO.Utility.Logging;

using SDL2;

namespace ClassicUO.Game.Scenes
{
    internal abstract class Scene : IUpdateable
    {
        public bool IsDestroyed { get; private set; }

        public bool IsLoaded { get; private set; }

        public int RenderedObjectsCount { get; protected set; }

        public CoroutineManager Coroutines { get; } = new CoroutineManager();

        public AudioManager Audio { get; private set; }

        public virtual void Update(double totalMS, double frameMS)
        {
            Audio?.Update();
            Coroutines.Update();
        }

        public virtual void Destroy()
        {
            if (IsDestroyed)
                return;

            IsDestroyed = true;
            Unload();
        }


        public virtual void Load()
        {
            if (this is GameScene || this is LoginScene)
            {
                Audio = new AudioManager();
                Coroutine.Start(this, CleaningResources(), "cleaning resources");
            }

            IsLoaded = true;
        }

        public virtual void Unload()
        {
            Audio?.StopMusic();
            Coroutines.Clear();
        }
        
        public virtual bool Draw(UltimaBatcher2D batcher)
        {
            return true;
        }


        internal virtual void OnLeftMouseUp() { }
        internal virtual void OnLeftMouseDown() { }

        internal virtual void OnRightMouseUp() { }
        internal virtual void OnRightMouseDown() { }

        internal virtual void OnMiddleMouseUp() { }
        internal virtual void OnMiddleMouseDown() { }


        internal virtual bool OnLeftMouseDoubleClick() => false;
        internal virtual bool OnRightMouseDoubleClick() => false;
        internal virtual bool OnMiddleMouseDoubleClick() => false;
        internal virtual void OnMouseWheel(bool up) { }
        internal virtual void OnMouseDragging() { }
        internal virtual void OnTextInput(string text) { }
        internal virtual void OnKeyDown(SDL.SDL_KeyboardEvent e) { }
        internal virtual void OnKeyUp(SDL.SDL_KeyboardEvent e) { }


        private IEnumerable<IWaitCondition> CleaningResources()
        {
            Log.Message(LogTypes.Trace, "Cleaning routine running...");

            yield return new WaitTime(TimeSpan.FromMilliseconds(10000));

            while (!IsDestroyed)
            {
                FileManager.Art.CleaUnusedResources();

                yield return new WaitTime(TimeSpan.FromMilliseconds(500));

                FileManager.Gumps.CleaUnusedResources();

                yield return new WaitTime(TimeSpan.FromMilliseconds(500));

                FileManager.Textmaps.CleaUnusedResources();

                yield return new WaitTime(TimeSpan.FromMilliseconds(500));

                FileManager.Animations.CleaUnusedResources();

                yield return new WaitTime(TimeSpan.FromMilliseconds(500));

                World.Map?.ClearUnusedBlocks();

                yield return new WaitTime(TimeSpan.FromMilliseconds(500));

                FileManager.Lights.CleaUnusedResources();

                yield return new WaitTime(TimeSpan.FromMilliseconds(500));
            }

            Log.Message(LogTypes.Trace, "Cleaning routine finished");
        }
    }
}