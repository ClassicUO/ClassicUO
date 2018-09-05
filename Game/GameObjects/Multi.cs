namespace ClassicUO.Game.GameObjects
{
    public sealed class Multi
    {
        public Multi(Item parent)
        {
            Parent = parent;
        }

        public Item Parent { get; }

        public short MinX { get; set; }
        public short MaxX { get; set; }
        public short MinY { get; set; }
        public short MaxY { get; set; }

        public MultiComponent[] Components { get; set; }
    }

    public struct MultiComponent
    {
        public MultiComponent(Graphic graphic,  ushort x,  ushort y,  sbyte z,  uint flags)
        {
            Graphic = graphic;
            Position = new Position(x, y, z);
            Flags = flags;
        }

        public Graphic Graphic { get; }
        public uint Flags { get; }
        public Position Position { get; set; }
    }
}