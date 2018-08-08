namespace ClassicUO.Game.WorldObjects
{
    public sealed class Multi
    {
        public Multi(in Item parent)
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
        public MultiComponent(in Graphic graphic, in ushort x, in ushort y, in sbyte z, in uint flags)
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