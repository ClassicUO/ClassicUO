#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using SDL2;

namespace ClassicUO.Game.Scenes
{
    internal abstract class Scene : IDisposable
    {
        public bool IsDestroyed { get; private set; }
        public bool IsLoaded { get; private set; }
        public int RenderedObjectsCount { get; protected set; }
        public Camera Camera { get; } = new Camera(0.5f, 2.5f, 0.1f);



        public virtual void Dispose()
        {
            if (IsDestroyed)
            {
                return;
            }

            Unload();
            IsDestroyed = true;
        }

        public virtual void Update()
        {
            Camera.Update(true, Time.Delta, Mouse.Position);
        }

        public virtual bool Draw(UltimaBatcher2D batcher)
        {
            return true;
        }


        public virtual void Load()
        {
            IsLoaded = true;
        }

        public virtual void Unload()
        {
            IsLoaded = false;
        }


        internal virtual bool OnMouseUp(MouseButtonType button) => false;
        internal virtual bool OnMouseDown(MouseButtonType button) => false;
        internal virtual bool OnMouseDoubleClick(MouseButtonType button) => false;
        internal virtual bool OnMouseWheel(bool up) => false;
        internal virtual bool OnMouseDragging() => false;

        internal virtual void OnControllerButtonDown(SDL.SDL_ControllerButtonEvent e) { }
        internal virtual void OnControllerButtonUp(SDL.SDL_ControllerButtonEvent e) { }

        internal virtual void OnTextInput(string text)
        {
        }

        internal virtual void OnKeyDown(SDL.SDL_KeyboardEvent e)
        {
        }

        internal virtual void OnKeyUp(SDL.SDL_KeyboardEvent e)
        {
        }
    }
}