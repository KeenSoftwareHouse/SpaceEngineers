#if !XB1 // !XB1_SYNC_NOREFLECTION
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System.Linq.Expressions
{
    public static class FieldExtensions
    {
        public static Func<T, TMember> CreateGetter<T, TMember>(this FieldInfo info)
        {
            if(!typeof(T).IsAssignableFrom(info.DeclaringType))
                throw new ArgumentException("T must be assignable from field declaring type: " + info.DeclaringType);

            ParameterExpression objParm = Expression.Parameter(typeof(T), "obj");
            Expression convert = Expression.Convert(objParm, info.DeclaringType);
            MemberExpression fieldExpr = Expression.Field(convert, info.Name);
            return Expression.Lambda<Func<T, TMember>>(fieldExpr, objParm).Compile();
        }

        public static Action<T, TMember> CreateSetter<T, TMember>(this FieldInfo info)
        {
            if (!typeof(T).IsAssignableFrom(info.DeclaringType))
                throw new ArgumentException("T must be assignable from field declaring type: " + info.DeclaringType);

            ParameterExpression objParm = Expression.Parameter(typeof(T), "obj");
            Expression convert = Expression.Convert(objParm, info.DeclaringType);
            ParameterExpression valueParm = Expression.Parameter(info.FieldType, "value");
            MemberExpression memberExpr = Expression.Field(convert, info.Name);
            Expression assignExpr = Expression.Assign(memberExpr, valueParm);
            return Expression.Lambda<Action<T, TMember>>(assignExpr, objParm, valueParm).Compile();
        }
    }
}
#endif // !XB1
