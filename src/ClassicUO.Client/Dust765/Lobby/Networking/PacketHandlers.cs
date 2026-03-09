using System;
using ClassicUO.Dust765.Managers;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Dust765.Lobby.Networking
{
    public static class PacketHandlers
    {
        public const ushort MAX_CAPACITY = 0x100;

        private static PacketHandler[] _Handlers = new PacketHandler[MAX_CAPACITY];

        public static PacketHandler GetHandler(byte packetID) => _Handlers[packetID];

        public static void Register(byte packetID, OnPacketReceive receive) =>
            _Handlers[packetID] = new PacketHandler(packetID, receive);

        public static void RemoveHandler(int packetID) => _Handlers[packetID] = null;

        static PacketHandlers()
        {
            Register(1, new OnPacketReceive(OnConnect));
            Register(2, new OnPacketReceive(OnDisconnect));
            Register(3, new OnPacketReceive(OnSpellCast));
            Register(4, new OnPacketReceive(SetTarget));
            Register(5, new OnPacketReceive(OnTarget));
            Register(6, new OnPacketReceive(HiddenPosition));
            Register(7, new OnPacketReceive(Attack));
        }

        private static void Attack(NetState ns, PacketReader pvSrc)
        {
            const ushort blueHue = 89;

            uint target = pvSrc.ReadUInt32();
            string name = pvSrc.ReadString();

            Mobile mob = World.Mobiles.Get(target);
            if (mob != null)
            {
                GameActions.Print($"[Group]: Attacking '{name}'", blueHue);

                TargetManager.LastAttack = mob;
                TargetManager.LastTargetInfo.Serial = mob.Serial;
                TargetManager.SelectedTarget = mob;
                GameActions.Attack(mob.Serial);


                if (mob.Distance < 18)
                    GameActions.Print(mob, "- attacking -", blueHue);
            }

        }
        
        private static void HiddenPosition(NetState ns, PacketReader pvSrc)
        {
            const ushort blueHue = 89;

            string[] posXY = pvSrc.ReadString().Split(',');

            GraphicEffectType type = GraphicEffectType.FixedXYZ;

            uint source = 0;
            uint target = 0;
            ushort graphic = 14138;
            ushort hue = 40;
            ushort srcX = Convert.ToUInt16(posXY[0]);
            ushort srcY = Convert.ToUInt16(posXY[1]);
            sbyte srcZ = Convert.ToSByte(posXY[2]);
            ushort targetX = Convert.ToUInt16(posXY[0]);
            ushort targetY = Convert.ToUInt16(posXY[1]);
            sbyte targetZ = Convert.ToSByte(posXY[2]);
            byte speed = 9;
            ushort duration = 32;
            bool fixedDirection = true;
            bool doesExplode = false;

            GraphicEffectBlendMode blendmode = GraphicEffectBlendMode.Normal;

            World.SpawnEffect
            (
                type, source, target, graphic, hue, srcX, srcY, srcZ, targetX, targetY, targetZ, speed, duration,
                fixedDirection, doesExplode, false, blendmode
            );

        }
        private static void OnTarget(NetState ns, PacketReader pvSrc)
        {
            uint target = pvSrc.ReadUInt32();
            Mobile mob = World.Mobiles.Get(target);
            if(( mob != null ) && TargetManager.IsTargeting)
            {
                TargetManager.Target(mob);
            }
        }

        private static void SetTarget(NetState ns, PacketReader pvSrc)
        {
            const ushort blueHue = 89;

            uint target = pvSrc.ReadUInt32();
            string name = pvSrc.ReadString();

            Mobile mob = World.Mobiles.Get(target);
            if(mob != null)
            {
                GameActions.Print($"[Group]: Target set to '{name}'", blueHue);

                TargetManager.LastTargetInfo.Serial = mob.Serial;
                TargetManager.SelectedTarget = mob;

                if(mob.Distance < 18)
                    GameActions.Print(mob, "- last target set -", blueHue);
            }
        }

        private static void OnSpellCast(NetState ns, PacketReader pvSrc)
        {
            SpellAction spell = (SpellAction)pvSrc.ReadUInt16();
            if(spell == SpellAction.Unknown)
                return;

            if(TargetManager.IsTargeting)
                TargetManager.CancelTarget();

            GameActions.CastSpell((int)spell);
        }
        
        private static void OnDisconnect(NetState ns, PacketReader pvSrc)
        {
            GameActions.Print($"{ns.Mobile.Name} disconnected from the server.");

            ns.Mobile = null;
        }

        private static void OnConnect(NetState ns, PacketReader pvSrc)
        {
            uint serial = pvSrc.ReadUInt32();
            string name = pvSrc.ReadString();
            bool rejoined = pvSrc.ReadBoolean();

            GameActions.Print($"{name} has {( rejoined ? "re" : "" )}joined the server.");

            ns.Mobile = World.Mobiles.Get(serial);
        }
    }
}