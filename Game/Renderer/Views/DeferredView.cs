using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class DeferredView : View
    {
        private readonly View _baseView;
        private readonly Vector3 _position;

        public DeferredView(DeferredEntity deferred, View baseView, Vector3 position) : base(deferred)
        {
            _baseView = baseView;
            _position = position;
        }

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position)
        {
            return _baseView.DrawInternal(spriteBatch, _position);
        }
    }
}