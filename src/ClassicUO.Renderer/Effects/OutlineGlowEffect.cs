using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ClassicUO.Renderer;

namespace ClassicUO.Renderer.Effects
{
    public class OutlineGlowEffect : Effect
    {
        public OutlineGlowEffect(GraphicsDevice graphicsDevice, byte[] bytecode)
            : base(graphicsDevice, bytecode ?? new byte[0])
        {
            if (bytecode == null || bytecode.Length == 0)
                return;
            SpriteTexture = Parameters["SpriteTexture"];
            OutlineColor = Parameters["OutlineColor"];
            OutlineThickness = Parameters["OutlineThickness"];
            TextureSize = Parameters["TextureSize"];
            MatrixTransform = Parameters["MatrixTransform"];
            WorldMatrix = Parameters["WorldMatrix"];
            Viewport = Parameters["Viewport"];
            CurrentTechnique = Techniques["Outline"];
        }

        public static OutlineGlowEffect Create(GraphicsDevice graphicsDevice)
        {
            byte[] bytes = null;
            try
            {
                var span = Resources.GetOutlineGlowShader();
                if (span.Length > 0)
                    bytes = span.ToArray();
            }
            catch { }

            if (bytes == null || bytes.Length == 0)
            {
                try
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? "";
                    string path = Path.Combine(baseDir, "shaders", "OutlineGlow.fxc");
                    if (File.Exists(path))
                        bytes = File.ReadAllBytes(path);
                }
                catch { }
            }

            if (bytes == null || bytes.Length == 0) return null;
            try
            {
                return new OutlineGlowEffect(graphicsDevice, bytes);
            }
            catch
            {
                return null;
            }
        }

        public EffectParameter SpriteTexture { get; }
        public EffectParameter OutlineColor { get; }
        public EffectParameter OutlineThickness { get; }
        public EffectParameter TextureSize { get; }
        public EffectParameter MatrixTransform { get; }
        public EffectParameter WorldMatrix { get; }
        public EffectParameter Viewport { get; }
    }
}
