using System;
using ClassicUO.Input;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.UI
{
    public class Button : Control
    {
        public Button(in Control parent, in int x, in int y, in int width, in int height) : base(parent, x, y, width, height)
        {
            Textures = new Texture2D[3];
        }

        public bool IsPressed { get; set; }

        /// <summary>
        ///     0: Normal,
        ///     1: Mouse over button
        ///     2: Clicked
        /// </summary>
        public Texture2D[] Textures { get; }


        public event EventHandler<EventArgs> ButtonClick;

        public override void OnMouseButton(in MouseEventArgs e)
        {
            base.OnMouseButton(e);
            ButtonClick?.Invoke(this, EventArgs.Empty);
        }
    }
}