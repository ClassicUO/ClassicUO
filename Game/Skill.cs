using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game
{
    public enum SkillLock : byte
    {
        Up = 0,
        Down = 1,
        Locked = 2
    }

    public sealed class Skill
    {
        public Skill(in string name, in int index, in bool click)
        {
            Name = name;
            Index = index;
            IsClickable = click;
        }

        public SkillLock Lock { get; internal set; }
        public ushort ValueFixed { get; internal set; }
        public ushort BaseFixed { get; internal set; }
        public ushort CapFixed { get; internal set; }

        public double Value { get { return ValueFixed / 10.0; } }
        public double Base { get { return BaseFixed / 10.0; } }
        public double Cap { get { return CapFixed / 10.0; } }

        public bool IsClickable { get; }
        public string Name { get; }
        public int Index { get; }

        public override string ToString() => Name;
    }
}
