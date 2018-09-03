using ClassicUO.Game.Renderer.Views;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Game.Renderer
{
    public static class GameTextRenderer
    {
        private static readonly List<ViewWithDrawInfo> _views = new List<ViewWithDrawInfo>();

        public static void AddView(in View view, in Vector3 position) => _views.Add(new ViewWithDrawInfo() { View = view, DrawPosition = position });

        public static void Render(in SpriteBatch3D spriteBatch)
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
    }
}
