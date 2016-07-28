using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace System.Linq.Expressions
{
    public static class ExpressionExtension
    {
#if XB1
#else
        public static Func<T, TMember> CreateGetter<T, TMember>(this Expression<Func<T, TMember>> expression)
        {
            Debug.Assert(expression.Body is MemberExpression, "Expression is not property or field selector");
            Debug.Assert(((MemberExpression)expression.Body).Member is PropertyInfo || ((MemberExpression)expression.Body).Member is FieldInfo, "Expression is not property or field selector");
            Debug.Assert(Obfuscator.CheckAttribute(((MemberExpression)expression.Body).Member), "Missing obfuscation attribute: [Obfuscation(Feature = Obfuscator.NoRename, Exclude = true)]");

            var member = (MemberExpression)expression.Body;
            if (member.Member is PropertyInfo)
            {
                ParameterExpression paramExpression = Expression.Parameter(typeof(T), "value");
                Expression propertyGetterExpression = Expression.Property(paramExpression, (PropertyInfo)member.Member);
                return Expression.Lambda<Func<T, TMember>>(propertyGetterExpression, paramExpression).Compile();
            }
            else
            {
                var info = (FieldInfo)member.Member;
                return info.CreateGetter<T, TMember>();
            }
        }
        
        public static Action<T, TMember> CreateSetter<T, TMember>(this Expression<Func<T, TMember>> expression)
        {
            Debug.Assert(expression.Body is MemberExpression, "Expression is not property or field selector");
            Debug.Assert(((MemberExpression)expression.Body).Member is PropertyInfo || ((MemberExpression)expression.Body).Member is FieldInfo, "Expression is not property or field selector");
            Debug.Assert(Obfuscator.CheckAttribute(((MemberExpression)expression.Body).Member), "Missing obfuscation attribute: [Obfuscation(Feature = Obfuscator.NoRename, Exclude = true)]");

            var member = (MemberExpression)expression.Body;
            if (member.Member is PropertyInfo)
            {
                ParameterExpression paramExpression = Expression.Parameter(typeof(T));
                ParameterExpression paramExpression2 = Expression.Parameter(typeof(TMember));
                MemberExpression propertyGetterExpression = Expression.Property(paramExpression, (PropertyInfo)member.Member);
                return Expression.Lambda<Action<T, TMember>>(Expression.Assign(propertyGetterExpression, paramExpression2), paramExpression, paramExpression2).Compile();
            }
            else
            {
                var info = (FieldInfo)member.Member;
                return info.CreateSetter<T, TMember>();
            }
        }

        //public static Func<T, TProperty> CreateGetter<T, TProperty>(this PropertyInfo propertyInfo)
        //{
        //    var t = typeof(T);
        //    var propertyType = typeof(TProperty);
        //    ParameterExpression paramExpression = Expression.Parameter(t, "value");
        //    Expression getArg = propertyInfo.DeclaringType == t ? (Expression)paramExpression : (Expression)Expression.Convert(paramExpression, propertyInfo.DeclaringType);
        //    Expression propertyGetterExpression = Expression.Property(getArg, propertyInfo);

        //    if (propertyType != propertyInfo.PropertyType)
        //        propertyGetterExpression = Expression.Convert(propertyGetterExpression, propertyType);

        //    return Expression.Lambda<Func<T, TProperty>>(propertyGetterExpression, paramExpression).Compile();
        //}

        //public static Action<T, TProperty> CreateSetter<T, TProperty>(this PropertyInfo propertyInfo)
        //{
        //    var t = typeof(T);
        //    var propertyType = typeof(TProperty);
        //    ParameterExpression paramExpression = Expression.Parameter(t);
        //    ParameterExpression paramExpression2 = Expression.Parameter(propertyType);

        //    Expression setInst = propertyInfo.DeclaringType == t ? (Expression)paramExpression : (Expression)Expression.Convert(paramExpression, propertyInfo.DeclaringType);
        //    Expression setVal = propertyInfo.PropertyType == propertyType ? (Expression)paramExpression2 : (Expression)Expression.Convert(paramExpression2, propertyInfo.PropertyType);

        //    MemberExpression propertySetterExpression = Expression.Property(setInst, propertyInfo);
        //    return Expression.Lambda<Action<T, TProperty>>(Expression.Assign(propertySetterExpression, setVal), paramExpression, paramExpression2).Compile();
        //}

        public static TDelegate StaticCall<TDelegate>(this MethodInfo info)
        {
            var args = info.GetParameters().Select(s => Expression.Parameter(s.ParameterType, s.Name)).ToArray();

            var call = Expression.Call(info, (IEnumerable<Expression>)args);
            return Expression.Lambda<TDelegate>(call, args).Compile();
        }

        public static TDelegate InstanceCall<TDelegate>(this MethodInfo info)
        {
            var invoke = typeof(TDelegate).GetMethod("Invoke");
            var delegateArgs = invoke.GetParameters();
            var infoArgs = info.GetParameters();
            var lambdaArgs = delegateArgs.Select(s => Expression.Parameter(s.ParameterType, s.Name)).ToArray();
            var callArgs = new Expression[infoArgs.Length];

            for (int i = 0; i < infoArgs.Length; i++)
            {
                if (infoArgs[i].ParameterType == delegateArgs[i].ParameterType)
                {
                    callArgs[i] = lambdaArgs[i + 1];
                }
                else
                {
                    callArgs[i] = Expression.Convert(lambdaArgs[i + 1], infoArgs[i].ParameterType);
                }
            }

            var instance = delegateArgs[0].ParameterType == info.DeclaringType ? (Expression)lambdaArgs[0] : (Expression)Expression.Convert(lambdaArgs[0], info.DeclaringType);
            var call = Expression.Call(instance, info, callArgs);
            return Expression.Lambda<TDelegate>(call, lambdaArgs).Compile();
        }
#endif
    }
}
