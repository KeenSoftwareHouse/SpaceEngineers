using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

#if !UNSHARPER //Expressions not supported.

namespace System.Reflection
{
    public static class PropertyAccess
    {
#if !XB1 // !XB1_SYNC_NOREFLECTION
        public static Func<T, TProperty> CreateGetter<T, TProperty>(this PropertyInfo propertyInfo)
        {
            var t = typeof(T);
            var propertyType = typeof(TProperty);
            ParameterExpression paramExpression = Expression.Parameter(t, "value");
            Expression getArg = propertyInfo.DeclaringType == t ? (Expression)paramExpression : (Expression)Expression.Convert(paramExpression, propertyInfo.DeclaringType);
            Expression propertyGetterExpression = Expression.Property(getArg, propertyInfo);

            if (propertyType != propertyInfo.PropertyType)
                propertyGetterExpression = Expression.Convert(propertyGetterExpression, propertyType);

            return Expression.Lambda<Func<T, TProperty>>(propertyGetterExpression, paramExpression).Compile();
        }

        public static Action<T, TProperty> CreateSetter<T, TProperty>(this PropertyInfo propertyInfo)
        {
            var t = typeof(T);
            var propertyType = typeof(TProperty);
            ParameterExpression paramExpression = Expression.Parameter(t);
            ParameterExpression paramExpression2 = Expression.Parameter(propertyType);

            Expression setInst = propertyInfo.DeclaringType == t ? (Expression)paramExpression : (Expression)Expression.Convert(paramExpression, propertyInfo.DeclaringType);
            Expression setVal = propertyInfo.PropertyType == propertyType ? (Expression)paramExpression2 : (Expression)Expression.Convert(paramExpression2, propertyInfo.PropertyType);

            MemberExpression propertySetterExpression = Expression.Property(setInst, propertyInfo);
            return Expression.Lambda<Action<T, TProperty>>(Expression.Assign(propertySetterExpression, setVal), paramExpression, paramExpression2).Compile();
        }
#endif // !XB1

#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
        public static Getter<T, TProperty> CreateGetterRef<T, TProperty>(this PropertyInfo propertyInfo)
        {
            var t = typeof(T);
            var propertyType = typeof(TProperty);
            ParameterExpression objParam = Expression.Parameter(t.MakeByRefType(), "value");
            Expression getArg = propertyInfo.DeclaringType == t ? (Expression)objParam : (Expression)Expression.Convert(objParam, propertyInfo.DeclaringType);
            Expression propertyGetterExpression = Expression.Property(getArg, propertyInfo);

            if (propertyType != propertyInfo.PropertyType)
                propertyGetterExpression = Expression.Convert(propertyGetterExpression, propertyType);

            ParameterExpression outParam = Expression.Parameter(propertyType.MakeByRefType(), "out");
            var assign = Expression.Assign(outParam, propertyGetterExpression);

            return Expression.Lambda<Getter<T, TProperty>>(assign, objParam, outParam).Compile();
        }

        public static Setter<T, TProperty> CreateSetterRef<T, TProperty>(this PropertyInfo propertyInfo)
        {
            var t = typeof(T);
            var propertyType = typeof(TProperty);
            ParameterExpression paramExpression = Expression.Parameter(t.MakeByRefType());
            ParameterExpression paramExpression2 = Expression.Parameter(propertyType.MakeByRefType());

            Expression setInst = propertyInfo.DeclaringType == t ? (Expression)paramExpression : (Expression)Expression.Convert(paramExpression, propertyInfo.DeclaringType);
            Expression setVal = propertyInfo.PropertyType == propertyType ? (Expression)paramExpression2 : (Expression)Expression.Convert(paramExpression2, propertyInfo.PropertyType);

            MemberExpression propertySetterExpression = Expression.Property(setInst, propertyInfo);
            return Expression.Lambda<Setter<T, TProperty>>(Expression.Assign(propertySetterExpression, setVal), paramExpression, paramExpression2).Compile();
        }
#endif // !XB1
    }
}

#endif
