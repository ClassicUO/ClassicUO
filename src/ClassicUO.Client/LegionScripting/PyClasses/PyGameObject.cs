using ClassicUO.Game.GameObjects;

namespace ClassicUO.LegionScripting.PyClasses
{
    public class PyGameObject
    {
        public ushort X;
        public ushort Y;
        public sbyte Z;
        public ushort Graphic;
        public ushort Hue;

        internal PyGameObject(GameObject gameObject)
        {
            if (gameObject == null) return;
            X = gameObject.X;
            Y = gameObject.Y;
            Z = gameObject.Z;
            Graphic = gameObject.Graphic;
            Hue = gameObject.Hue;
        }

        public override string ToString() => $"<{__class__} Graphic=0x{Graphic:X4} Hue=0x{Hue:X4} Pos=({X},{Y},{Z})>";
        public virtual string __class__ => "PyGameObject";
        public virtual string __repr__() => ToString();
    }
}
