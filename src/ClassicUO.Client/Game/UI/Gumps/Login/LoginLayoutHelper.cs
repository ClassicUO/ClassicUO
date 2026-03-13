#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.

#endregion

using ClassicUO;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps.Login
{
    internal static class LoginLayoutHelper
    {
        private const int DesignWidth = 1024;
        private const int DesignHeight = 768;
        private const int CompactWidth = 896;
        private const int CompactHeight = 672;
        private const int ThresholdWidth = 1366;
        private const int ThresholdHeight = 768;

        private static int _contentWidth = DesignWidth;
        private static int _contentHeight = DesignHeight;
        private static float _scaleX = 1f;
        private static float _scaleY = 1f;
        private static bool _initialized;

        public static int ContentWidth
        {
            get { EnsureInitialized(); return _contentWidth; }
        }
        public static int ContentHeight
        {
            get { EnsureInitialized(); return _contentHeight; }
        }

        public static int WindowWidth => ContentWidth;
        public static int WindowHeight => ContentHeight;

        public static int ContentOffsetX => 0;
        public static int ContentOffsetY => 0;

        public static int CenterX => ContentWidth >> 1;
        public static int CenterY => ContentHeight >> 1;

        public static int X(int refX) => (int)(refX * _scaleX);
        public static int Y(int refY) => (int)(refY * _scaleY);
        public static Point Pos(int refX, int refY) => new Point(X(refX), Y(refY));

        public static int W(int refW) => (int)(refW * _scaleX);
        public static int H(int refH) => (int)(refH * _scaleY);
        public static Point Size(int refW, int refH) => new Point(W(refW), H(refH));

        public static int CenterOffsetX(int controlWidth) => CenterX - (controlWidth >> 1);
        public static int CenterOffsetY(int controlHeight) => CenterY - (controlHeight >> 1);

        private const int OptionsDesignWidth = 700;
        private const int OptionsDesignHeight = 720;
        private const int OptionsCompactWidth = 720;
        private const int OptionsCompactHeight = 638;

        public static int OptionsWidth
        {
            get
            {
                EnsureInitialized();
                return _contentWidth <= CompactWidth ? OptionsCompactWidth : OptionsDesignWidth;
            }
        }
        public static int OptionsHeight
        {
            get
            {
                EnsureInitialized();
                return _contentHeight <= CompactHeight ? OptionsCompactHeight : OptionsDesignHeight;
            }
        }

        public static void Initialize(int displayWidth, int displayHeight)
        {
            bool useCompact = displayWidth <= ThresholdWidth && displayHeight <= ThresholdHeight;
            _contentWidth = useCompact ? CompactWidth : DesignWidth;
            _contentHeight = useCompact ? CompactHeight : DesignHeight;
            _scaleX = (float)_contentWidth / DesignWidth;
            _scaleY = (float)_contentHeight / DesignHeight;
            _initialized = true;
        }

        private static void EnsureInitialized()
        {
            if (!_initialized && Client.Game?.GraphicsDevice != null)
            {
                var mode = Client.Game.GraphicsDevice.Adapter.CurrentDisplayMode;
                Initialize(mode.Width, mode.Height);
            }
        }
    }
}
