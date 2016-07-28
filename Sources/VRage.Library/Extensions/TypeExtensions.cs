using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace VRage
{
    public static class MemberHelper<T>
    {
#if XB1
        // Not suported on XB1 version

        //public static Func<T,TValue> GetMember<TValue>(Func<T,TValue> selector)
        //{
        //    Debug.Assert(false, "Check this is working properly");
        //    return selector;
        //}
#else
        /// <summary>
        ///  Gets the memberinfo of field/property on instance class.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="selector">The selector.</param>
        /// <returns></returns>
        public static MemberInfo GetMember<TValue>(Expression<Func<T,TValue>> selector)
        {
            Exceptions.ThrowIf<ArgumentNullException>(selector == null, "selector");

            var me = selector.Body as MemberExpression;

            Debug.Assert(!(me.Member is PropertyInfo), "Creating member expression of property, this won't work when obfuscated!");
            Exceptions.ThrowIf<ArgumentNullException>(me == null, "Selector must be a member access expression", "selector");

            return me.Member;
        }
#endif
    }

    public static class MemberHelper
    {
#if XB1
    // Not suported on XB1 version

    //public static Func<TValue> GetMember<TValue>(Func<TValue> selector)
    //{
    //    return selector;
    //}

#else
        /// <summary>
        /// Gets the memberinfo of field/property on static class.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="selector">The selector.</param>
        /// <returns></returns>
        public static MemberInfo GetMember<TValue>(Expression<Func<TValue>> selector)
        {
            Exceptions.ThrowIf<ArgumentNullException>(selector == null, "selector");

            var me = selector.Body as MemberExpression;

            //Debug.Assert(!(me.Member is PropertyInfo), "Creating member expression of property, this won't work when obfuscated!");
            Exceptions.ThrowIf<ArgumentNullException>(me == null, "Selector must be a member access expression", "selector");

            return me.Member;
        }
#endif
    }

    public static class TypeExtensions
    {
        public static bool IsStruct(this Type type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(decimal);
        }

        public static IEnumerable<MemberInfo> GetDataMembers(this Type t, bool fields, bool properties, bool nonPublic, bool inherited, bool _static, bool instance, bool read, bool write)
        {
            var flags = BindingFlags.DeclaredOnly | BindingFlags.Public;
            if (nonPublic) flags |= BindingFlags.NonPublic;
            if (_static) flags |= BindingFlags.Static;
            if (instance) flags |= BindingFlags.Instance;

            IEnumerable<MemberInfo> members = t.GetMembers(flags);
            if (inherited && t.IsClass && t != typeof(object))
            {
                var baseClass = t.BaseType;
                while (baseClass != typeof(object) && baseClass != null)
                {
                    members = members.Concat(baseClass.GetMembers(flags));
                    baseClass = baseClass.BaseType;
                }
            }

            return members.Where(s => (fields ? s.MemberType == MemberTypes.Field : false) || (properties ? CheckProperty(s, read, write) : false));
        }

        static bool CheckProperty(MemberInfo info, bool read, bool write)
        {
            var p = info as PropertyInfo;
            return p != null && (!read || p.CanRead) && (!write || p.CanWrite);
        }

        public static Type FindGenericBaseTypeArgument(this Type type, Type genericTypeDefinition)
        {
            var result = FindGenericBaseTypeArguments(type, genericTypeDefinition);
            return result.Length > 0 ? result[0] : null;
        }

        public static Type[] FindGenericBaseTypeArguments(this Type type, Type genericTypeDefinition)
        {
            Debug.Assert(genericTypeDefinition.IsGenericTypeDefinition, "genericTypeDefinition must be generic type definition");

            if (type.IsValueType || type.IsInterface)
                return Type.EmptyTypes;

            while(type != typeof(Object))
            {
                if(type.IsGenericType)
                {
                    var gen = type.GetGenericTypeDefinition();
                    if(gen == genericTypeDefinition)
                    {
                        return type.GetGenericArguments();
                    }
                }
                type = type.BaseType;
            }
            return Type.EmptyTypes;
        }

        public static bool HasDefaultConstructor(this Type type)
        {
            return !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null;
        }
    }
}