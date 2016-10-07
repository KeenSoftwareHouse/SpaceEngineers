using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace System.Reflection
{
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
    public delegate void Getter<T, TMember>(ref T obj, out TMember value);
    public delegate void Setter<T, TMember>(ref T obj, ref TMember value);
#endif // !XB1

    public static class MemberAccess
    {
        // .NET 4.5 has it
        //public static T GetCustomAttribute<T>(this MemberInfo memberInfo)
        //    where T : Attribute
        //{
        //    return (T)Attribute.GetCustomAttribute(memberInfo, typeof(T));
        //}

        // .NET 4.5 has it
        //public static T[] GetCustomAttributes<T>(this MemberInfo memberInfo)
        //    where T : Attribute
        //{
        //    return Array.ConvertAll(Attribute.GetCustomAttributes(memberInfo, typeof(T)), item => (T)item);
        //}

        public static bool IsMemberPublic(this MemberInfo memberInfo)
        {
            switch(memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return (((FieldInfo)memberInfo).Attributes & FieldAttributes.Public) == FieldAttributes.Public;
                case MemberTypes.Property:
                    {
                        var p = (PropertyInfo)memberInfo;
                        var getter = p.GetGetMethod();
                        var setter = p.GetSetMethod();

                        return getter != null && setter != null 
                            && (getter.Attributes & MethodAttributes.Public) == MethodAttributes.Public
                            && (setter.Attributes & MethodAttributes.Public) == MethodAttributes.Public;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
                return ((PropertyInfo)memberInfo).PropertyType;
            else if (memberInfo is FieldInfo)
                return ((FieldInfo)memberInfo).FieldType;
            else if (memberInfo is MethodInfo)
                return ((MethodInfo)memberInfo).ReturnType;
            else
                throw new InvalidOperationException("Member info must be PropertyInfo, FieldInfo or MethodInfo");
        }

#if !XB1 // !XB1_SYNC_NOREFLECTION
        public static Func<T, TMember> CreateGetter<T, TMember>(this MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
                return ((PropertyInfo)memberInfo).CreateGetter<T, TMember>();
            else if (memberInfo is FieldInfo)
                return ((FieldInfo)memberInfo).CreateGetter<T, TMember>();
            else
                throw new InvalidOperationException("Member info must be PropertyInfo, FieldInfo");
        }

        public static Action<T, TMember> CreateSetter<T, TMember>(this MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
                return ((PropertyInfo)memberInfo).CreateSetter<T, TMember>();
            else if (memberInfo is FieldInfo)
                return ((FieldInfo)memberInfo).CreateSetter<T, TMember>();
            else
                throw new InvalidOperationException("Member info must be PropertyInfo, FieldInfo");
        }
#endif // !XB1

#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
        public static Getter<T, TMember> CreateGetterRef<T, TMember>(this MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
                return ((PropertyInfo)memberInfo).CreateGetterRef<T, TMember>();
            else if (memberInfo is FieldInfo)
                return ((FieldInfo)memberInfo).CreateGetterRef<T, TMember>();
            else
                throw new InvalidOperationException("Member info must be PropertyInfo, FieldInfo");
        }

        public static Setter<T, TMember> CreateSetterRef<T, TMember>(this MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
                return ((PropertyInfo)memberInfo).CreateSetterRef<T, TMember>();
            else if (memberInfo is FieldInfo)
                return ((FieldInfo)memberInfo).CreateSetterRef<T, TMember>();
            else
                throw new InvalidOperationException("Member info must be PropertyInfo, FieldInfo");
        }
#endif // !XB1

#if !XB1 // !XB1_SYNC_NOREFLECTION
        public static bool CheckGetterSignature<T, TMember>(this MemberInfo memberInfo)
        {
            if (!typeof(T).IsAssignableFrom(memberInfo.DeclaringType))
                return false;

            if (memberInfo is PropertyInfo)
                return typeof(TMember).IsAssignableFrom(((PropertyInfo)memberInfo).PropertyType);
            else if (memberInfo is FieldInfo)
                return typeof(TMember).IsAssignableFrom(((FieldInfo)memberInfo).FieldType);
            else
                throw new InvalidOperationException("Member info must be PropertyInfo, FieldInfo");
        }

        public static bool CheckSetterSignature<T, TMember>(this MemberInfo memberInfo)
        {
            if (!typeof(T).IsAssignableFrom(memberInfo.DeclaringType))
                return false;

            if (memberInfo is PropertyInfo)
                return typeof(TMember).IsAssignableFrom(((PropertyInfo)memberInfo).PropertyType);
            else if (memberInfo is FieldInfo)
                return typeof(TMember).IsAssignableFrom(((FieldInfo)memberInfo).FieldType);
            else
                throw new InvalidOperationException("Member info must be PropertyInfo, FieldInfo");
        }
#endif // !XB1
    }
}

#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
namespace VRage.Reflection
{
    public interface IMySetGetMemberDataHelper
    {
        //Note: Very slow and ugly solution but we have at least something for now.
        object GetMemberData(MemberInfo m);
    }
}
#endif // XB1
