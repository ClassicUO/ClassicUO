using ClassicUO.Game.Data;

namespace ClassicUO.Game.Managers
{
    internal interface IEffectManager
    {
        void Update();

        void CreateEffect(
            GraphicEffectType type,
            uint source,
            uint target,
            ushort graphic,
            ushort hue,
            ushort srcX,
            ushort srcY,
            sbyte srcZ,
            ushort targetX,
            ushort targetY,
            sbyte targetZ,
            byte speed,
            int duration,
            bool fixedDir,
            bool doesExplode,
            bool hasparticles,
            GraphicEffectBlendMode blendmode
        );

        void Clear();
    }
}
