// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace ClassicUO.Renderer
{
    public abstract class RenderPass
    {
        public string Name { get; }

        protected RenderPass(string name)
        {
            Name = name;
        }

        public abstract void Execute(UltimaBatcher2D batcher, RenderTargets renderTargets);
    }

    public class RenderPipeline
    {
        private readonly List<RenderPass> _passes = new();

        public void Clear()
        {
            _passes.Clear();
        }

        public void Add(RenderPass pass)
        {
            _passes.Add(pass);
        }

        public void Execute(UltimaBatcher2D batcher, RenderTargets renderTargets)
        {
            foreach (var pass in _passes)
            {
                pass.Execute(batcher, renderTargets);
            }

            _passes.Clear();
        }
    }
}
