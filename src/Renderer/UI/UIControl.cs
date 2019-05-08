using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

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


        public void Add(UIControl control)
        {

        }

        public void Remove(UIControl control)
        {

        }



        public virtual void Draw(FNABatcher2D batcher, int x, int y)
        {

        }
    }
}
