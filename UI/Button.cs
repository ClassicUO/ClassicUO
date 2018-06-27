using ClassicUO.Input;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.UI
{
    public class Button : Control
    {
        public Button(Control parent, int x, int y, int width, int height) : base(parent, x, y, width, height)
        {
            Textures = new Texture2D[3];
        }

        public bool IsPressed { get; set; }

        /// <summary>
        /// 0: Normal,
        /// 1: Mouse over button
        /// 2: Clicked
        /// </summary>
        public Texture2D[] Textures { get; }


        public event EventHandler<EventArgs> ButtonClick;

        public override void OnMouseButton(MouseEventArgs e)
        {
            base.OnMouseButton(e);
            ButtonClick?.Invoke(this, EventArgs.Empty);
        }
    }
}
