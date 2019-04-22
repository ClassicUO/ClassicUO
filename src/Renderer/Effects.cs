using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal class IsometricEffect : Effect
    {
        public IsometricEffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, Resources.IsometricEffect)
        {
            Parameters["HuesPerTexture"].SetValue(3000.0f);
            CanDrawLight = Parameters["DrawLighting"];
            ProjectionMatrix = Parameters["ProjectionMatrix"];
            WorldMatrix = Parameters["WorldMatrix"];
            Viewport = Parameters["Viewport"];
            CurrentTechnique = Techniques["HueTechnique"];
        }

        public EffectParameter CanDrawLight { get; }
        public EffectParameter ProjectionMatrix { get; }
        public EffectParameter WorldMatrix { get; }
        public EffectParameter Viewport { get; }
        public EffectPass Pass => CurrentTechnique.Passes[0];

        protected IsometricEffect(Effect cloneSource) : base(cloneSource)
        {
        }
    }

    internal class LightEffect : Effect
    {
        public LightEffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, Resources.LightEffect)
        {
        }


        protected LightEffect(Effect cloneSource) : base(cloneSource)
        {
        }
    }
}
