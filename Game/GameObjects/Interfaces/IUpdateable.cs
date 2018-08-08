using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.GameObjects.Interfaces
{
    public interface IUpdateable
    {
        void Update(in double frameMS);
    }
}
