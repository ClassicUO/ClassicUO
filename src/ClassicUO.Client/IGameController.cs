// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO
{
    /// <summary>
    /// Interface for game controller, enabling testability by decoupling from
    /// the concrete GameController (which inherits XNA Game and requires GPU).
    /// </summary>
    internal interface IGameController
    {
        Scene Scene { get; }
        AudioManager Audio { get; }
        UOAssets UO { get; }
        IPluginHost PluginHost { get; }
        Rectangle ClientBounds { get; }
        float ScreenScale { get; set; }
        float DpiScale { get; }
        int ScaleWithDpi(int value, float previousDpi = 1);
        uint[] FrameDelay { get; }
        bool IsActive { get; }
        GameWindow Window { get; }
        GraphicsDevice GraphicsDevice { get; }
        bool IsMouseVisible { get; set; }

        event EventHandler<EventArgs> Activated;
        event EventHandler<EventArgs> Deactivated;

        T GetScene<T>() where T : Scene;
        void SetScene(Scene scene);
        void SetWindowTitle(string title);
        void SetWindowSize(int width, int height);
        void SetWindowBorderless(bool borderless);
        void MaximizeWindow();
        bool IsWindowMaximized();
        void RestoreWindow();
        void SetRefreshRate(int rate);
        void EnqueueAction(uint time, Action action);
        void Exit();
    }
}
