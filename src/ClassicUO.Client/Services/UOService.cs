using ClassicUO.Game;
using ClassicUO.Renderer;
using ClassicUO.Renderer.Arts;
using ClassicUO.Renderer.Gumps;
using ClassicUO.Renderer.Sounds;
using ClassicUO.Renderer.Texmaps;
using ClassicUO.Renderer.Lights;
using ClassicUO.Renderer.MultiMaps;
using ClassicUO.Renderer.Animations;
using ClassicUO.IO.Audio;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Sdk;
using System;

namespace ClassicUO.Services
{
    internal class UOService : IService
    {
        private readonly GameController _game;
        private readonly UltimaOnline _uo;

        public event EventHandler? Activated;
        public event EventHandler? Deactivated;

        public UOService(GameController game, UltimaOnline uo)
        {
            _game = game;
            _uo = uo;
        }

        public bool IsActive => _game.IsActive;
        public UltimaOnline Self => _uo;
        public GameCursor GameCursor => _uo.GameCursor;
        public Animations Animations => _uo.Animations;
        public Art Arts => _uo.Arts;
        public Gump Gumps => _uo.Gumps;
        public Texmap Texmaps => _uo.Texmaps;
        public Light Lights => _uo.Lights;
        public MultiMap MultiMaps => _uo.MultiMaps;
        public Renderer.Sounds.Sound Sounds => _uo.Sounds;
        public World World => _uo.World;
        public ClientVersion Version => _uo.Version;
        public UOFileManager FileManager => _uo.FileManager;
        public ClientFlags Protocol => _uo.Protocol;

        internal void OnActivated()
        {
            Activated?.Invoke(this, EventArgs.Empty);
        }

        internal void OnDeactivated()
        {
            Deactivated?.Invoke(this, EventArgs.Empty);
        }
    }
}