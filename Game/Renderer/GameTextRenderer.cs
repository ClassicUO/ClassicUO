using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Renderer.Views;
using ClassicUO.IO.Resources;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Game.Renderer
{
    public static class GameTextRenderer
    {
        private static readonly List<ViewWithDrawInfo> _views = new List<ViewWithDrawInfo>();

        public static void AddView(View view,  Vector3 position) => _views.Add(new ViewWithDrawInfo() { View = view, DrawPosition = position });

        public static void Render(SpriteBatch3D spriteBatch)
        {
            if (_views.Count > 0)
            {
                for (int i = 0; i < _views.Count; i++)
                {
                    _views[i].View.Draw(spriteBatch, _views[i].DrawPosition);
                }

                _views.Clear();
            }
        }

        struct ViewWithDrawInfo
        {
            public View View;
            public Vector3 DrawPosition;
        }


        public static SpriteTexture CreateTexture(in GameText gt)
        {
            uint[] data;
            int linesCount;

            if (gt.IsHTML)
                Fonts.SetUseHTML(true);

            if (gt.IsUnicode)
            {
                (data, gt.Width, gt.Height, linesCount, gt.Links) = Fonts.GenerateUnicode(gt.Font, gt.Text, gt.Hue, gt.Cell, gt.MaxWidth, gt.Align, (ushort)gt.FontStyle);
            }
            else
            {
                (data, gt.Width, gt.Height, linesCount, gt.IsPartialHue) = Fonts.GenerateASCII(gt.Font, gt.Text, gt.Hue, gt.MaxWidth, gt.Align, (ushort)gt.FontStyle);
            }

            var texture = new SpriteTexture(gt.Width, gt.Height);
            texture.SetData(data);


            if (gt.IsHTML)
                Fonts.SetUseHTML(false);

            return texture;
        }
    }
}
