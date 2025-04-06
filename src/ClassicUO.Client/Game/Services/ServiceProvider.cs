using System;
using System.Collections.Generic;
using ClassicUO.Sdk;

namespace ClassicUO.Game.Services
{
    internal static class ServiceProvider
    {
        private static readonly Dictionary<Type, IService> _services = new Dictionary<Type, IService>();

        public static void Register<T>(T service) where T : IService
        {
            _services[typeof(T)] = service;
        }

        public static T Get<T>() where T : IService
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }

            Log.Error($"service {typeof(T)} not registered in ServiceProvider.");
            return default;
        }

        public static void Unregister<T>() where T : IService
        {
            _services.Remove(typeof(T));
        }

        public static void Clear()
        {
            _services.Clear();
        }
    }
}