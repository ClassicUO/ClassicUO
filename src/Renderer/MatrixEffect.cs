using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal class MatrixEffect : Effect
    {
        protected MatrixEffect(GraphicsDevice graphicsDevice, byte[] effectCode)
            : base(graphicsDevice, effectCode)
        {
            MatrixTransform = Parameters[nameof(MatrixTransform)];
        }

        public EffectParameter MatrixTransform { get; }

        public virtual void ApplyStates(Matrix matrix)
        {
            MatrixTransform.SetValue(matrix);

            foreach (EffectPass effectPass in CurrentTechnique.Passes)
            {
                effectPass.Apply();
            }
        }
    }
}