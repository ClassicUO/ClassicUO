using ClassicUO.Assets;

namespace ClassicUO.Renderer.Animations
{
    public class AnimationGroup
    {
        public AnimationDirection[] Direction = new AnimationDirection[
            AnimationsLoader.MAX_DIRECTIONS
        ];
    }

    public class AnimationGroupUop : AnimationGroup
    {
        public uint CompressedLength;
        public uint DecompressedLength;
        public int FileIndex;
        public uint Offset;
    }
}
