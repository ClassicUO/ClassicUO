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

using System.Collections.Generic;
using System.Diagnostics;

using ClassicUO.Input;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SDL2;

namespace ClassicUO.Renderer.UI
{
    internal abstract class UIControl
    {
        public Rectangle Bounds;

        public int X
        {
            get => Bounds.X;
            set => Bounds.X = value;
        }

        public int Y
        {
            get => Bounds.Y;
            set => Bounds.Y = value;
        }

        public int Width
        {
            get => Bounds.Width;
            set => Bounds.Width = value;
        }

        public int Height
        {
            get => Bounds.Height;
            set => Bounds.Height = value;
        }

        public List<UIControl> Children { get; } = new List<UIControl>();

        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public Texture2D Texture { get; set; }

        public UIControl Parent { get; private set; }

        public int GetParentX()
        {
            UIControl p = Parent;

            if (p == null)
                return 0;

            return p.X + X;
        }

        public int GetParentY()
        {
            UIControl p = Parent;

            if (p == null)
                return 0;

            return p.Y + Y;
        }

        public (int, int) RelativePosition()
        {
            int x = X + GetParentX() + Parent?.GetParentX() ?? 0;
            int y = Y + GetParentY() + Parent?.GetParentY() ?? 0;

            return (x, y);
        }



        public void Add(UIControl control)
        {
            Debug.Assert(control.Parent == null);
            control.Parent = this;
            Children.Add(control);
        }

        public void Remove(UIControl control)
        {
            control.Parent = null;
            Children.Remove(control);
        }


        public UIControl[] HitTest(int x, int y)
        {
            if (!IsEnabled || !IsVisible)
                return null;

            Stack<UIControl> results = new Stack<UIControl>();

            int parentX = GetParentX();
            int parentY = GetParentY();

            if (Bounds.Contains(x - parentX, y - parentY))
            {
                if (MouseInControl(x - X - parentX, y - Y - parentY))
                {
                    results.Push(this);

                    for (int i = 0; i < Children.Count; i++)
                    {
                        var list = Children[i].HitTest(x, y);

                        if (list != null)
                        {
                            for (int j = list.Length - 1; j >= 0; j--)
                                results.Push(list[j]);
                        }
                    }
                }
            }


            return results.Count == 0 ? null : results.ToArray();
        }


        public virtual void Update(GameTime gameTime)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var c = Children[i];

                c.Update(gameTime);
            }
        }

        public virtual void Draw(SpriteBatch batcher, int x, int y)
        {
            if (!IsVisible)
                return;

            for (int i = 0; i < Children.Count; i++)
            {
                var c = Children[i];

                if (c.IsVisible) c.Draw(batcher, x + c.X, y + c.Y);
            }
        }


        protected bool MouseInControl(int x, int y)
        {
            return IsEnabled;
        }



        public virtual void OnDragBegin(int x, int y, MouseButton button)
        {
        }

        public virtual void OnDragEnd(int x, int y, MouseButton button)
        {
        }

        public virtual void OnMouseUp(int x, int y, MouseButton button)
        {
        }

        public virtual void OnMouseDown(int x, int y, MouseButton button)
        {
        }

        public virtual void OnMouseEnter(int x, int y)
        {
        }

        public virtual void OnMouseExit(int x, int y)
        {
        }

        public virtual void OnMouseWheel(MouseEvent delta)
        {
        }

        public virtual void OnKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
        }

        public virtual void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
        }
    }
}