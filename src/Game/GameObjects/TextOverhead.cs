using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Renderer;

namespace ClassicUO.Game.GameObjects
{
    internal class TextOverhead : BaseGameObject
    {
        public byte Alpha;
        public ushort Hue;
        public bool IsTransparent;

        public RenderedText RenderedText;
        public long Time, SecondTime;
        public MessageType Type;
        public int X, Y, OffsetY;
        public GameObject Owner;

        public TextOverhead Left, Right;

        public TextOverhead ListLeft, ListRight;
    }
}
