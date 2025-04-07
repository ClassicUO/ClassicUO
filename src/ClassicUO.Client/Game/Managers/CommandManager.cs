// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.Resources;
using ClassicUO.Sdk;
using ClassicUO.Services;

namespace ClassicUO.Game.Managers
{
    internal sealed class CommandManager
    {
        private readonly Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();
        private readonly WorldService _worldService = ServiceProvider.Get<WorldService>();

        public void Initialize()
        {
            Register
            (
                "info",
                s =>
                {
                    if (ServiceProvider.Get<ManagersService>().TargetManager.IsTargeting)
                    {
                        ServiceProvider.Get<ManagersService>().TargetManager.CancelTarget();
                    }

                    ServiceProvider.Get<ManagersService>().TargetManager.SetTargeting(CursorTarget.SetTargetClientSide, CursorType.Target, TargetType.Neutral);
                }
            );

            Register
            (
                "datetime",
                s =>
                {
                    if (_worldService.World.Player != null)
                    {
                        GameActions.Print(_worldService.World, string.Format(ResGeneral.CurrentDateTimeNowIs0, DateTime.Now));
                    }
                }
            );

            Register
            (
                "hue",
                s =>
                {
                    if (ServiceProvider.Get<ManagersService>().TargetManager.IsTargeting)
                    {
                        ServiceProvider.Get<ManagersService>().TargetManager.CancelTarget();
                    }

                    ServiceProvider.Get<ManagersService>().TargetManager.SetTargeting(CursorTarget.HueCommandTarget, CursorType.Target, TargetType.Neutral);
                }
            );


            Register
            (
                "debug",
                s =>
                {
                    CUOEnviroment.Debug = !CUOEnviroment.Debug;

                }
            );
        }


        public void Register(string name, Action<string[]> callback)
        {
            name = name.ToLower();

            if (!_commands.ContainsKey(name))
            {
                _commands.Add(name, callback);
            }
            else
            {
                Log.Error($"Attempted to register command: '{name}' twice.");
            }
        }

        public void UnRegister(string name)
        {
            name = name.ToLower();

            if (_commands.ContainsKey(name))
            {
                _commands.Remove(name);
            }
        }

        public void UnRegisterAll()
        {
            _commands.Clear();
        }

        public void Execute(string name, params string[] args)
        {
            name = name.ToLower();

            if (_commands.TryGetValue(name, out var action))
            {
                action.Invoke(args);
            }
            else
            {
                Log.Warn($"Command: '{name}' not exists");
            }
        }

        public void OnHueTarget(Entity entity)
        {
            if (entity != null)
            {
                ServiceProvider.Get<ManagersService>().TargetManager.Target(entity);
            }

            Mouse.LastLeftButtonClickTime = 0;
            GameActions.Print(_worldService.World, string.Format(ResGeneral.ItemID0Hue1, entity?.Graphic ?? 0, entity?.Hue ?? 0));
        }
    }
}