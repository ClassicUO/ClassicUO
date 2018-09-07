#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
//    
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Collections.Generic;
using ClassicUO.Utility;

namespace ClassicUO
{
    public static class Service
    {
        static readonly Dictionary<Type, object> m_Services = new Dictionary<Type, object>();
        private static Log _logger;

        static Service()
        {
            _logger = new Log();
        }

        public static T Register<T>(T service)
        {
            Type type = typeof(T);
            if (m_Services.ContainsKey(type))
            {
                _logger.Message(LogTypes.Error, string.Format("Attempted to register service of type {0} twice.", type.Name));
                m_Services.Remove(type);
            }
            m_Services.Add(type, service);
            _logger.Message(LogTypes.Trace, string.Format("Initialized service : {0}", type.Name));
            return service;
        }

        public static void Unregister<T>()
        {
            Type type = typeof(T);
            if (m_Services.ContainsKey(type))
            {
                m_Services.Remove(type);
            }
            else
            {
                _logger.Message(LogTypes.Error, string.Format("Attempted to unregister service of type {0}, but no service of this type (or type and equality) is registered.", type.Name));
            }
        }

        public static bool Has<T>()
        {
            Type type = typeof(T);
            return m_Services.ContainsKey(type);
        }

        public static T Get<T>(bool failIfNotRegistered = true)
        {
            Type type = typeof(T);
            if (m_Services.ContainsKey(type))
            {
                return (T)m_Services[type];
            }
            if (failIfNotRegistered)
            {
                _logger.Message(LogTypes.Error, string.Format("Attempted to get service service of type {0}, but no service of this type is registered.", type.Name));
            }
            return default(T);
        }
    }
}
