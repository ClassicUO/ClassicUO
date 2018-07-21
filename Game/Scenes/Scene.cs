using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Scenes
{
    public abstract class Scene
    {
        protected Scene()
        {
            ChainActions = new List<Func<bool>>();
        }

        public List<Func<bool>> ChainActions { get; }


        public virtual void Load()
        {

        }

        public virtual void Unload()
        {

        }

        public void Update()
        {

        }

    }
}
