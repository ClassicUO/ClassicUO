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