namespace ClassicUO.Game.Data
{
    public struct BuffIcon
    {
        public BuffIcon(Graphic graphic, long timer, string text)
        {
            Graphic = graphic;
            Timer = timer;
            Text = text;
        }

        public Graphic Graphic { get; }

        public long Timer { get; }

        public string Text { get; }
    }
}