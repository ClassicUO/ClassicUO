using ClassicUO.Game.Data;
using ClassicUO.Sdk;


namespace ClassicUO.Game.Services
{
    internal class UOService : IService
    {
        private readonly UltimaOnline _uo;

        public UOService(UltimaOnline uo)
        {
            _uo = uo;
        }

        public UltimaOnline Self => _uo;
        public ClientVersion Version => _uo.Version;
        public ClientFlags Protocol => _uo.Protocol;
        public World World => _uo.World;
        public GameCursor GameCursor => _uo.GameCursor;
        public UOFileManager FileManager => _uo.FileManager;
    }
}