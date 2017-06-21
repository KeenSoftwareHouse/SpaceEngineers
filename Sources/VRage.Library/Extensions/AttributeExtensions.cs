//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

namespace System.Reflection
{
    public static class AttributeExtensions
    {
        public static bool HasAttribute<T>(this MemberInfo element) where T : Attribute
        {
            return Attribute.IsDefined(element, typeof(T));
        }

//        public static bool HasAttribute<T>(this ParameterInfo element) where T : Attribute
//        {
//            return Attribute.IsDefined(element, typeof(T));
//        }

//        public static bool HasAttribute<T>(this MemberInfo element, bool inherit) where T : Attribute
//        {
//            return Attribute.IsDefined(element, typeof(T), inherit);
//        }

//        public static bool HasAttribute<T>(this ParameterInfo element, bool inherit) where T : Attribute
//        {
//            return Attribute.IsDefined(element, typeof(T), inherit);
//        }

//        public static T GetCustomAttribute<T>(this MemberInfo element) where T : Attribute
//        {
//            return (T)Attribute.GetCustomAttribute(element, typeof(T));
//        }

//        public static T GetCustomAttribute<T>(this ParameterInfo element) where T : Attribute
//        {
//            return (T)Attribute.GetCustomAttribute(element, typeof(T));
//        }

//        public static T GetCustomAttribute<T>(this MemberInfo element, bool inherit) where T : Attribute
//        {
//            return (T)Attribute.GetCustomAttribute(element, typeof(T), inherit);
//        }

//        public static T GetCustomAttribute<T>(this ParameterInfo element, bool inherit) where T : Attribute
//        {
//            return (T)Attribute.GetCustomAttribute(element, typeof(T), inherit);
//        }

//        public static T[] GetCustomAttributes<T>(this MemberInfo element) where T : Attribute
//        {
//            return (T[])Attribute.GetCustomAttributes(element, typeof(T));
//        }

//        public static T[] GetCustomAttributes<T>(this ParameterInfo element) where T : Attribute
//        {
//            return (T[])Attribute.GetCustomAttributes(element, typeof(T));
//        }

//        public static T[] GetCustomAttributes<T>(this MemberInfo element, bool inherit) where T : Attribute
//        {
//            return (T[])Attribute.GetCustomAttributes(element, typeof(T), inherit);
//        }

//        public static T[] GetCustomAttributes<T>(this ParameterInfo element, bool inherit) where T : Attribute
//        {
//            return (T[])Attribute.GetCustomAttributes(element, typeof(T), inherit);
//        }
    }
}
