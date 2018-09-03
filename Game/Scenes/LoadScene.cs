namespace ClassicUO.Game.Scenes
{
    public sealed class LoadScene : Scene
    {
        public LoadScene()
        {
            ChainActions.Add(OnGameLoading);
            ChainActions.Add(OnGameLoaded);
        }

        private bool OnGameLoading()
        {
            return true;
        }

        private bool OnGameLoaded()
        {
            return true;
        }
    }
}