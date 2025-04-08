using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ClassicUO.Services
{
    internal class PluginHostService : IService
    {
        private readonly IPluginHost? _pluginHost;

        public PluginHostService(IPluginHost? pluginHost)
        {
            _pluginHost = pluginHost;
        }

        public Dictionary<IntPtr, GraphicsResource> GfxResources => _pluginHost?.GfxResources;


        public void LoadPlugin(string pluginPath)
        {
            _pluginHost?.LoadPlugin(pluginPath);
        }

        public void Tick()
        {
            _pluginHost?.Tick();
        }

        public void Closing()
        {
            _pluginHost?.Closing();
        }

        public void FocusGained()
        {
            _pluginHost?.FocusGained();
        }

        public void FocusLost()
        {
            _pluginHost?.FocusLost();
        }

        public void Connected()
        {
            _pluginHost?.Connected();
        }

        public void Disconnected()
        {
            _pluginHost?.Disconnected();
        }

        public bool Hotkey(int key, int mod, bool pressed)
        {
            return _pluginHost?.Hotkey(key, mod, pressed) ?? false;
        }

        public void Mouse(int button, int wheel)
        {
            _pluginHost?.Mouse(button, wheel);
        }

        public void GetCommandList(out IntPtr listPtr, out int listCount)
        {
            listPtr = 0;
            listCount = 0;
            _pluginHost?.GetCommandList(out listPtr, out listCount);
        }

        public unsafe int SdlEvent(SDL2.SDL.SDL_Event* ev)
        {
            return _pluginHost?.SdlEvent(ev) ?? 0;
        }

        public void UpdatePlayerPosition(int x, int y, int z)
        {
            _pluginHost?.UpdatePlayerPosition(x, y, z);
        }

        public bool PacketOut(Span<byte> buffer)
        {
            return _pluginHost?.PacketOut(buffer) ?? true;
        }

        public bool PacketIn(ArraySegment<byte> buffer)
        {
            return _pluginHost?.PacketIn(buffer) ?? true;
        }
    }
}