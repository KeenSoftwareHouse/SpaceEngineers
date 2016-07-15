using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VRage
{
    /// <summary>
    /// Specifies a static read-only default value field for structs
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class StructDefaultAttribute : Attribute { }

    public static class MyStructDefault
    {
        public static FieldInfo GetDefaultFieldInfo(Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (field.IsInitOnly && field.GetCustomAttribute(typeof(StructDefaultAttribute)) != null)
                    return field;
            }

            return null;
        }
        public static T GetDefaultValue<T>(Type type)
            where T : struct
        {
            FieldInfo field = GetDefaultFieldInfo(typeof(T));
            if (field == null)
                return new T();

            return (T)field.GetValue(null);
        }
    }
}
