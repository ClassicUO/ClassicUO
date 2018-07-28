using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassicUO
{
    public class FpsCounter 
    {
        private double _currentFpsTime;
        private int _fps;
        private int _totalFrames;

        public int FPS => _fps;

        public void Update(GameTime gameTime)
        {
            _currentFpsTime += gameTime.ElapsedGameTime.TotalSeconds;

            if (_currentFpsTime >= 1.0)
            {
                _fps = _totalFrames;
                _totalFrames = 0;
                _currentFpsTime = 0;
            }
        }

        public void IncreaseFrame() => _totalFrames++;
    }
}
