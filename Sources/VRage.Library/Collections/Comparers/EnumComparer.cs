#if !XB1
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace VRage
{
    /// <summary>
    /// A fast and efficient implementation of <see cref="IEqualityComparer{T}"/> for Enum types.
    /// Useful for dictionaries that use Enums as their keys.
    /// </summary>
    /// <remarks>
    /// var dict = new Dictionary&lt;DayOfWeek, 
    /// string&gt;(EnumComparer&lt;DayOfWeek&gt;.Instance);
    /// </remarks>
    /// <typeparam name="TEnum">The type of the Enum.</typeparam>
    public sealed class EnumComparer<TEnum> : IEqualityComparer<TEnum>, IComparer<TEnum>
        where TEnum : struct, IComparable, IConvertible, IFormattable
    {
        /// <summary>
        /// 
        /// </summary>
        private static readonly Func<TEnum, TEnum, bool> equalsFunct;

        /// <summary>
        /// 
        /// </summary>
        private static readonly Func<TEnum, int> getHashCodeFunct;

        private static readonly Func<TEnum, TEnum, int> compareToFunct;

        /// <summary>
        /// The singleton accessor.
        /// </summary>
        private static readonly EnumComparer<TEnum> instance;

        // ReSharper restore StaticFieldInGenericType

        /// <summary>
        /// Initializes the <see cref="EnumComparer{TEnum}"/> class
        /// by generating the GetHashCode and Equals methods.
        /// </summary>
        static EnumComparer()
        {
            getHashCodeFunct = GenerateGetHashCodeFunct();
            equalsFunct = GenerateEqualsFunct();
            compareToFunct = GenerateCompareToFunct();

            instance = new EnumComparer<TEnum>();
        }

        /// <summary>
        /// A private constructor to prevent user instantiation.
        /// </summary>
        private EnumComparer()
        {
            AssertTypeIsEnum();
            AssertUnderlyingTypeIsSupported();
        }

        /// <summary>
        /// The singleton accessor.
        /// </summary>
        public static EnumComparer<TEnum> Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type <typeparamref name="TEnum"/> 
        /// to compare.</param>
        /// <param name="y">The second object of type <typeparamref name="TEnum"/> 
        /// to compare.</param>
        /// <returns>
        /// true if the specified objects are equal; otherwise, false.
        /// </returns>
        public bool Equals(TEnum x, TEnum y)
        {
            // call the generated method
            return equalsFunct(x, y);
        }

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"/> 
        /// for which a hash code is to be returned.</param>
        /// <returns>A hash code for the specified object.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The type of <paramref name="obj"/> is a reference type and 
        /// <paramref name="obj"/> is null.
        /// </exception>
        public int GetHashCode(TEnum obj)
        {
            // call the generated method
            return getHashCodeFunct(obj);
        }

        /// <summary>
        /// Asserts the type is enum.
        /// </summary>
        private static void AssertTypeIsEnum()
        {
            if (typeof(TEnum).IsEnum)
            {
                return;
            }

            var message = string.Format("The type parameter {0} is not an Enum. LcgEnumComparer supports Enums only.", typeof(TEnum));

            throw new NotSupportedException(message);
        }

        /// <summary>
        /// Asserts the underlying type is supported.
        /// </summary>
        private static void AssertUnderlyingTypeIsSupported()
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(TEnum));

            ICollection<Type> supportedTypes = new[]
                                                   {
                                                       typeof (byte), typeof (sbyte), typeof (short), typeof (ushort),
                                                       typeof (int), typeof (uint), typeof (long), typeof (ulong)
                                                   };

            if (supportedTypes.Contains(underlyingType))
            {
                return;
            }

            var message =
                string.Format("The underlying type of the type parameter {0} is {1}. " +
                              "LcgEnumComparer only supports Enums with underlying type of " +
                              "byte, sbyte, short, ushort, int, uint, long, or ulong.",
                              typeof(TEnum), underlyingType);

            throw new NotSupportedException(message);
        }

        /// <summary>
        /// Generates a comparison method similar to this:
        /// <code>
        /// bool Equals(TEnum x, TEnum y)
        /// {
        ///     return x == y;
        /// }
        /// </code>
        /// </summary>
        /// <returns>The generated method.</returns>
        private static Func<TEnum, TEnum, bool> GenerateEqualsFunct()
        {
            var xParam = Expression.Parameter(typeof(TEnum), "x");
            var yParam = Expression.Parameter(typeof(TEnum), "y");
            var equalExpression = Expression.Equal(xParam, yParam);

            return Expression.Lambda<Func<TEnum, TEnum, bool>>(equalExpression, new[] { xParam, yParam }).Compile();
        }

        /// <summary>
        /// Generates a GetHashCode method similar to this:
        /// <code>
        /// int GetHashCode(TEnum obj)
        /// {
        ///     return ((int)obj).GetHashCode();
        /// }
        /// </code>
        /// </summary>
        /// <returns>The generated method.</returns>
        private static Func<TEnum, int> GenerateGetHashCodeFunct()
        {
            var objParam = Expression.Parameter(typeof(TEnum), "obj");
            var underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
            var convertExpression = Expression.Convert(objParam, underlyingType);
            var getHashCodeMethod = underlyingType.GetMethod("GetHashCode");
            var getHashCodeExpression = Expression.Call(convertExpression, getHashCodeMethod);

            return Expression.Lambda<Func<TEnum, int>>(getHashCodeExpression, new[] { objParam }).Compile();
        }

        private static Func<TEnum, TEnum, int> GenerateCompareToFunct()
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
            var xParam = Expression.Parameter(typeof(TEnum), "x");
            var yParam = Expression.Parameter(typeof(TEnum), "y");

            var xConv = Expression.Convert(xParam, underlyingType);
            var yConv = Expression.Convert(yParam, underlyingType);
            var method = underlyingType.GetMethod("CompareTo", new Type[] { underlyingType });
            var expression = Expression.Call(xConv, method, yConv);
            return Expression.Lambda<Func<TEnum, TEnum, int>>(expression, new[] { xParam, yParam }).Compile();
        }

        public int Compare(TEnum x, TEnum y)
        {
            return compareToFunct(x, y);
        }
    }
}
#endif // !XB1
