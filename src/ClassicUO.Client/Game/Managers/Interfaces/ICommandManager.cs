// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    internal interface ICommandManager
    {
        void Initialize();

        void Register(string name, Action<string[]> callback);

        void UnRegister(string name);

        void UnRegisterAll();

        void Execute(string name, params string[] args);

        void OnHueTarget(Entity entity);
    }
}
