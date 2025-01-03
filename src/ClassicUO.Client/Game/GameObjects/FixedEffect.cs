// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Managers;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal sealed class FixedEffect : GameEffect
    {
        public FixedEffect(World world, EffectManager manager, ushort graphic, ushort hue, int duration, byte speed)
            : base(world, manager, graphic, hue, duration, speed)
        {

        }

        public FixedEffect
        (
            World world,
            EffectManager manager,
            ushort sourceX,
            ushort sourceY,
            sbyte sourceZ,
            ushort graphic,
            ushort hue,
            int duration,
            byte speed
        ) : this(world, manager, graphic, hue, duration, speed)
        {
            SetSource(sourceX, sourceY, sourceZ);
        }

        public FixedEffect
        (
            World world,
            EffectManager manager,
            uint sourceSerial,
            ushort sourceX,
            ushort sourceY,
            sbyte sourceZ,
            ushort graphic,
            ushort hue,
            int duration,
            byte speed
        ) : this(world, manager, graphic, hue, duration, speed)
        {
            Entity source = World.Get(sourceSerial);

            if (source != null && SerialHelper.IsValid(sourceSerial))
            {
                SetSource(source);
            }
            else
            {
                SetSource(sourceX, sourceY, sourceZ);
            }
        }

        public override void Update()
        {
            base.Update();

            if (!IsDestroyed)
            {
                (var x, var y, var z) = GetSource();

                if (Source != null)
                {
                    Offset = Source.Offset;
                }

                if (X != x || Y != y || Z != z)
                {
                    SetInWorldTile(x, y, z);
                }
            }
        }
    }
}