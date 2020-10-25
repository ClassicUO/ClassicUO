namespace ClassicUO.Renderer
{
    internal class AnimationFrameTexture : UOTexture
    {
        public AnimationFrameTexture(int width, int height)
            : base(width, height)
        {
        }

        public short CenterX { get; set; }

        public short CenterY { get; set; }
    }
}