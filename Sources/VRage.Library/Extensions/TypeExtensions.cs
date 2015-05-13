using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace VRage
{
    public static class MemberHelper<T>
    {
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
    }

    public static class MemberHelper
    {
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

            Debug.Assert(!(me.Member is PropertyInfo), "Creating member expression of property, this won't work when obfuscated!");
            Exceptions.ThrowIf<ArgumentNullException>(me == null, "Selector must be a member access expression", "selector");

            return me.Member;
        }
    }
}