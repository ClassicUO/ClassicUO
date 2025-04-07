using ClassicUO.Renderer.Arts;
using ClassicUO.Renderer.Gumps;
using ClassicUO.Renderer.Texmaps;
using ClassicUO.Renderer.Lights;
using ClassicUO.Renderer.MultiMaps;
using ClassicUO.Renderer.Animations;
using ClassicUO.Game.Data;
using ClassicUO.Sdk;

namespace ClassicUO.Services
{
    internal class UOService : IService
    {
        private readonly UltimaOnline _uo;


        public UOService(UltimaOnline uo)
        {
            _uo = uo;
        }

        public UltimaOnline Self => _uo;
        public Animations Animations => _uo.Animations;
        public Art Arts => _uo.Arts;
        public Gump Gumps => _uo.Gumps;
        public Texmap Texmaps => _uo.Texmaps;
        public Light Lights => _uo.Lights;
        public MultiMap MultiMaps => _uo.MultiMaps;
        public Renderer.Sounds.Sound Sounds => _uo.Sounds;
        public ClientVersion Version => _uo.Version;
        public ClientFlags Protocol => _uo.Protocol;
    }
}