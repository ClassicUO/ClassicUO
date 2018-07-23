namespace ClassicUO.Game
{
    public struct Property
    {
        public Property(in uint cliloc, in string args) : this()
        {
            Cliloc = cliloc;
            Args = args;
        }

        public uint Cliloc { get; }
        public string Args { get; }
    }
}