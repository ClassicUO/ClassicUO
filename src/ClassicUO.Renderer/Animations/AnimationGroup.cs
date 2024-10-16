using ClassicUO.Assets;
using ClassicUO.IO;

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
        public CompressionType CompressionType;
        public int FileIndex;
        public uint Offset;
    }
}
