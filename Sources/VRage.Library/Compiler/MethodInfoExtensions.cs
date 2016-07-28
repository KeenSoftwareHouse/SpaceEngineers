#if !XB1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Diagnostics;

#if !UNSHARPER

namespace System
{
    public static class MethodInfoExtensions
    {
#if !XB1
        public static TDelegate CreateDelegate<TDelegate>(this MethodInfo method, object instance)
            where TDelegate : class
        {
            return CreateDelegate<TDelegate>(method,
                (typeArguments, parameterExpressions) =>
                {
                    Expression<Func<Object>> instanceExpression = () => instance;
                    return Expression.Call(Expression.Convert(instanceExpression.Body,
                                                              instance.GetType()),
                                           method.Name,
                                           typeArguments,
                                           ProvideStrongArgumentsFor(method,
                                                                     parameterExpressions));
                });
        }
#endif // !XB1

        public static TDelegate CreateDelegate<TDelegate>(this MethodInfo method)
            where TDelegate : class
        {
            return CreateDelegate<TDelegate>(method, 
                (typeArguments, parameterExpressions) =>
                    Expression.Call(method, ProvideStrongArgumentsFor(method, parameterExpressions)));
        }

        private static TDelegate CreateDelegate<TDelegate>(MethodBase method, Func<Type[], ParameterExpression[], MethodCallExpression> getCallExpression)
        {
            var parameterExpressions = ExtractParameterExpressionsFrom<TDelegate>();
            CheckParameterCountsAreEqual(parameterExpressions, method.GetParameters());

            var call = getCallExpression(GetTypeArgumentsFor(method), parameterExpressions);

            return Expression.Lambda<TDelegate>(call, parameterExpressions).Compile();
        }

        public static ParameterExpression[] ExtractParameterExpressionsFrom<TDelegate>()
        {
            return typeof(TDelegate)
                .GetMethod("Invoke")
                .GetParameters()
                .Select(s => Expression.Parameter(s.ParameterType))
                .ToArray();
        }

        private static void CheckParameterCountsAreEqual(IEnumerable<ParameterExpression> delegateParameters, IEnumerable<ParameterInfo> methodParameters)
        {
            if (delegateParameters.Count() != methodParameters.Count())
            {
                throw new InvalidOperationException("The number of parameters of the requested delegate does not match the number parameters of the specified method.");
            }
        }

        private static Type[] GetTypeArgumentsFor(MethodBase method)
        {
            return null;
            //var typeArguments = method.GetGenericArguments();
            //return (typeArguments.Length > 0) ? typeArguments : null;
        }

        private static Expression[] ProvideStrongArgumentsFor(MethodInfo method, ParameterExpression[] parameterExpressions)
        {
            return method.GetParameters().Select((parameter, index) => Expression.Convert(parameterExpressions[index], parameter.ParameterType)).ToArray();
        }
    }
}

#endif
#endif // !XB1
