using ClassicUO.Game;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ClassicUO.Game.Services
{
    internal class PluginHostService : IService
    {
        private readonly ClassicUO.IPluginHost _pluginHost;

        public PluginHostService(ClassicUO.IPluginHost pluginHost)
        {
            _pluginHost = pluginHost;
        }

        public void LoadPlugin(string pluginPath)
        {
            _pluginHost.LoadPlugin(pluginPath);
        }

        public void Tick()
        {
            _pluginHost.Tick();
        }

        public void Closing()
        {
            _pluginHost.Closing();
        }

        public void FocusGained()
        {
            _pluginHost.FocusGained();
        }

        public void FocusLost()
        {
            _pluginHost.FocusLost();
        }

        public void Connected()
        {
            _pluginHost.Connected();
        }

        public void Disconnected()
        {
            _pluginHost.Disconnected();
        }

        public bool Hotkey(int key, int mod, bool pressed)
        {
            return _pluginHost.Hotkey(key, mod, pressed);
        }

        public void Mouse(int button, int wheel)
        {
            _pluginHost.Mouse(button, wheel);
        }

        public void GetCommandList(out IntPtr listPtr, out int listCount)
        {
            _pluginHost.GetCommandList(out listPtr, out listCount);
        }

        public unsafe int SdlEvent(SDL2.SDL.SDL_Event* ev)
        {
            return _pluginHost.SdlEvent(ev);
        }

        public void UpdatePlayerPosition(int x, int y, int z)
        {
            _pluginHost.UpdatePlayerPosition(x, y, z);
        }

        public Dictionary<IntPtr, GraphicsResource> GfxResources => _pluginHost.GfxResources;
    }
}