using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.UI
{
    abstract class UIControl
    {
        private readonly List<UIControl> _children = new List<UIControl>();


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

        public List<UIControl> Children => _children;

        public bool IsVisible { get; set; }

        public bool IsEnabled { get; set; }
        public Texture2D Texture { get; set; }





        public void Add(UIControl control)
        {
            _children.Add(control);
        }

        public void Remove(UIControl control)
        {
            _children.Remove(control);
        }


        public virtual void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _children.Count; i++)
            {
                var c = _children[i];

                c.Update(totalMS, frameMS);
            }
        }

        public virtual void Draw(FNABatcher2D batcher, int x, int y)
        {
            if (!IsVisible)
                return;

            for (int i = 0; i < _children.Count; i++)
            {
                var c = _children[i];

                if (c.IsVisible)
                {
                    c.Draw(batcher, x + c.X, y + c.Y);
                }
            }
        }
    }
}
