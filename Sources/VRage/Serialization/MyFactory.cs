using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Collections;
using VRage.Network;

namespace VRage.Serialization
{
    public static class MyFactory
    {
        static ThreadSafeStore<Type, MySerializer> m_serializers = new ThreadSafeStore<Type, MySerializer>(CreateSerializerInternal);
        static Dictionary<Type, Type> m_serializerTypes = new Dictionary<Type, Type>();

        static MyFactory()
        {
            RegisterFromAssembly(Assembly.GetExecutingAssembly());
        }

        public static MySerializer<T> GetSerializer<T>()
        {
            return (MySerializer<T>)GetSerializer(typeof(T));
        }

        public static MySerializer GetSerializer(Type t)
        {
            return m_serializers.Get(t);
        }

        public static MySerializeInfo CreateInfo(MemberInfo member)
        {
            return MySerializeInfo.Create(member);
        }

        public static MyMemberSerializer<TOwner> CreateMemberSerializer<TOwner>(MemberInfo member)
        {
            return (MyMemberSerializer<TOwner>)CreateMemberSerializer(member, typeof(TOwner));
        }

        public static MyMemberSerializer CreateMemberSerializer(MemberInfo member, Type ownerType)
        {
            var serializer = (MyMemberSerializer)Activator.CreateInstance(typeof(MyMemberSerializer<,>).MakeGenericType(ownerType, member.GetMemberType()));
            serializer.Init(member, CreateInfo(member));
            return serializer;
        }

        static MySerializer CreateSerializerInternal(Type t)
        {
            Type serializerType;
            lock (m_serializerTypes)
            {
                m_serializerTypes.TryGetValue(t, out serializerType);
            }

            if (serializerType != null)
            {
                return (MySerializer)Activator.CreateInstance(serializerType);
            }
            else if (t.IsEnum)
            {
                return (MySerializer)Activator.CreateInstance(typeof(MySerializerEnum<>).MakeGenericType(t));
            }
            else if (t.IsArray)
            {
                return (MySerializer)Activator.CreateInstance(typeof(MySerializerArray<>).MakeGenericType(t.GetElementType()));
            }
            else if (typeof(IMyNetObject).IsAssignableFrom(t))
            {
                return (MySerializer)Activator.CreateInstance(typeof(MySerializerNetObject<>).MakeGenericType(t));
            }
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return (MySerializer)Activator.CreateInstance(typeof(MySerializerNullable<>).MakeGenericType(t.GetGenericArguments()[0]));
            }
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                return (MySerializer)Activator.CreateInstance(typeof(MySerializerList<>).MakeGenericType(t.GetGenericArguments()[0]));
            }
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var args = t.GetGenericArguments();
                return (MySerializer)Activator.CreateInstance(typeof(MySerializerDictionary<,>).MakeGenericType(args[0], args[1]));

            }
            else if (t.IsClass || t.IsStruct())
            {
                return (MySerializer)Activator.CreateInstance(typeof(MySerializerObject<>).MakeGenericType(t));
            }

            throw new InvalidOperationException("No serializer found for type: " + t.Name);
        }

        public static void Register(Type serializedType, Type serializer)
        {
            lock (m_serializerTypes)
            {
                m_serializerTypes.Add(serializedType, serializer);
            }
        }

        public static void RegisterFromAssembly(Assembly assembly)
        {
            foreach (var t in assembly.GetTypes())
            {
                if (!t.IsGenericType && t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(MySerializer<>))
                {
                    Type argType = t.BaseType.GetGenericArguments()[0];
                    Register(argType, t);
                }
            }
        }
    }
}
