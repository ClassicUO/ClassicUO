using Microsoft.Xna.Framework;

namespace ClassicUO
{
    public class FpsCounter
    {
        private double _currentFpsTime;
        private int _totalFrames;

        public int FPS { get; private set; }

        public void Update(GameTime gameTime)
        {
            _currentFpsTime += gameTime.ElapsedGameTime.TotalSeconds;

            if (_currentFpsTime >= 1.0)
            {
                FPS = _totalFrames;
                _totalFrames = 0;
                _currentFpsTime = 0;
            }
        }

        public void IncreaseFrame()
        {
            _totalFrames++;
        }
    }
}