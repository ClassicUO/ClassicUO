using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.LegionScripting.PyClasses
{
    public class PyMobile : PyEntity
    {
        public int HitsDiff { get { var m = GetMobile(); return m != null ? m.HitsMax - m.Hits : 0; } }
        public int ManaDiff { get { var m = GetMobile(); return m != null ? m.ManaMax - m.Mana : 0; } }
        public int StamDiff { get { var m = GetMobile(); return m != null ? m.StaminaMax - m.Stamina : 0; } }
        public bool IsDead => GetMobile()?.IsDead ?? false;
        public bool IsPoisoned => GetMobile()?.IsPoisoned ?? false;
        public int HitsMax => GetMobile()?.HitsMax ?? 0;
        public int Hits => GetMobile()?.Hits ?? 0;
        public int StaminaMax => GetMobile()?.StaminaMax ?? 0;
        public int Stamina => GetMobile()?.Stamina ?? 0;
        public int ManaMax => GetMobile()?.ManaMax ?? 0;
        public int Mana => GetMobile()?.Mana ?? 0;
        public bool IsRenamable => GetMobile()?.IsRenamable ?? false;
        public bool IsHuman => GetMobile()?.IsHuman ?? false;

        internal PyMobile(Mobile mobile) : base(mobile)
        {
            if (mobile != null)
                _mobile = mobile;
        }

        public override string __class__ => "PyMobile";

        private Mobile _mobile;
        private Mobile GetMobile()
        {
            if (_mobile != null && _mobile.Serial == Serial) return _mobile;
            var m = MainThreadQueue.InvokeOnMainThread(() =>
            {
                if (World.Mobiles.TryGetValue(Serial, out var mob))
                    return mob;
                return null;
            });
            _mobile = m;
            return m;
        }
    }
}
