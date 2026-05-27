// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration
{
    internal sealed class Profile
    {
        public Point GameWindowPosition { get; set; } = new Point(10, 10);
        public Point GameWindowSize { get; set; } = new Point(600, 480);
        public bool GameWindowFullSize { get; set; }
    }
}
