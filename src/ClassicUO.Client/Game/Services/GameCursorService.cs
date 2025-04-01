using ClassicUO.Game;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Services
{
    internal class GameCursorService : IService
    {
        private readonly GameCursor _gameCursor;

        public GameCursorService(GameCursor gameCursor)
        {
            _gameCursor = gameCursor;
        }

        public ushort Graphic
        {
            get => _gameCursor.Graphic;
            set => _gameCursor.Graphic = value;
        }

        public bool IsLoading
        {
            get => _gameCursor.IsLoading;
            set => _gameCursor.IsLoading = value;
        }
    }
}