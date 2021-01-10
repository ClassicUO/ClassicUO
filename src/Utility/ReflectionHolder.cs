#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Collections.Generic;
using System.Reflection;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Utility
{
    internal static class ReflectionHolder
    {
        public static Dictionary<string, string> GetGameObjectProperties<T>(T obj) where T : GameObject
        {
            PropertyInfo[] props = obj?.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            FieldInfo[] fields = obj?.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

            Dictionary<string, string> dict = new Dictionary<string, string>();

            if (props != null)
            {
                foreach (PropertyInfo prop in props)
                {
                    if (prop.PropertyType.IsByRef)
                    {
                    }
                    else
                    {
                        object value = prop.GetValue(obj, null);

                        dict[prop.Name] = value == null ? "null" : value.ToString();
                    }
                }
            }

            if (fields != null)
            {
                foreach (FieldInfo prop in fields)
                {
                    if (prop.FieldType.IsByRef)
                    {
                    }
                    else
                    {
                        object value = prop.GetValue(obj);

                        dict[prop.Name] = value == null ? "null" : value.ToString();
                    }
                }
            }

            return dict;
        }
    }
}