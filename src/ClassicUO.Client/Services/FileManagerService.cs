using ClassicUO.Sdk.Assets;

namespace ClassicUO.Services
{
    internal class FileManagerService : IService
    {
        private readonly ClassicUO.Sdk.UOFileManager _fileManager;

        public FileManagerService(ClassicUO.Sdk.UOFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        public MapLoader Maps => _fileManager.Maps;
        public ClilocLoader Clilocs => _fileManager.Clilocs;
        public SkillsLoader Skills => _fileManager.Skills;
        public SpeechesLoader Speeches => _fileManager.Speeches;
        public TileDataLoader TileData => _fileManager.TileData;
        public AnimationsLoader Animations => _fileManager.Animations;
        public ArtLoader Arts => _fileManager.Arts;
        public GumpsLoader Gumps => _fileManager.Gumps;
        public HuesLoader Hues => _fileManager.Hues;
        public LightsLoader Lights => _fileManager.Lights;
        public MultiLoader Multis => _fileManager.Multis;
        public SoundsLoader Sounds => _fileManager.Sounds;
        public VerdataLoader Verdata => _fileManager.Verdata;

        public string GetUOFilePath(string file)
        {
            return _fileManager.GetUOFilePath(file);
        }
    }
}