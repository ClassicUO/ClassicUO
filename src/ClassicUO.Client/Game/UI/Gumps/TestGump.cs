using ClassicUO.Game.Managers;

namespace ClassicUO.Game
{
    internal class Test
    {
        public static void Initialize()
        {
            EventSink.OnConnected += GameScene_OnConnected;
        }

        private static void GameScene_OnConnected(object sender, System.EventArgs e)
        {
            GameActions.Print("This script sees we connected :O");
        }
    }
}
