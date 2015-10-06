using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Serialization
{
    public sealed class MyMemberSerializer<TOwner, TMember> : MyMemberSerializer<TOwner>
    {
        Getter<TOwner, TMember> m_getter;
        Setter<TOwner, TMember> m_setter;

        MySerializer<TMember> m_serializer;
        MemberInfo m_memberInfo;

        public sealed override void Init(MemberInfo memberInfo, MySerializeInfo info)
        {
            if (m_serializer != null)
                throw new InvalidOperationException("Already initialized");

            m_getter = memberInfo.CreateGetterRef<TOwner, TMember>();
            m_setter = memberInfo.CreateSetterRef<TOwner, TMember>();
            m_serializer = MyFactory.GetSerializer<TMember>();
            m_info = info;
            m_memberInfo = memberInfo;
        }

        public override string ToString()
        {
            return String.Format("{2} {0}.{1}", m_memberInfo.DeclaringType.Name, m_memberInfo.Name, m_memberInfo.GetMemberType().Name);
        }

        public override void Clone(ref TOwner original, ref TOwner clone)
        {
            TMember value;
            m_getter(ref original, out value);
            m_serializer.Clone(ref value);
            m_setter(ref clone, ref value);
        }

        public override bool Equals(ref TOwner a, ref TOwner b)
        {
            TMember valA, valB;
            m_getter(ref a, out valA);
            m_getter(ref b, out valB);
            return m_serializer.Equals(ref valA, ref valB);
        }

        public sealed override void Read(BitStream stream, ref TOwner obj, MySerializeInfo info)
        {
            TMember result;
            if (MySerializationHelpers.CreateAndRead<TMember>(stream, out result, m_serializer, info ?? m_info))
            {
                m_setter(ref obj, ref result);
            }
        }

        public sealed override void Write(BitStream stream, ref TOwner obj, MySerializeInfo info)
        {
            try
            {
                TMember value;
                m_getter(ref obj, out value);
                MySerializationHelpers.Write<TMember>(stream, ref value, m_serializer, info ?? m_info);
            }
            catch (MySerializeException e)
            {
                string err;
                switch (e.Error)
                {
                    case MySerializeErrorEnum.DynamicNotAllowed:
                        err = String.Format("Error serializing {0}.{1}, member contains inherited type, but it's not allowed, consider adding attribute [Serialize(MyObjectFlags.Dynamic)]", m_memberInfo.DeclaringType.Name, m_memberInfo.Name);
                        break;

                    case MySerializeErrorEnum.NullNotAllowed:
                        err = String.Format("Error serializing {0}.{1}, member contains null, but it's not allowed, consider adding attribute [Serialize(MyObjectFlags.Nullable)]", m_memberInfo.DeclaringType.Name, m_memberInfo.Name);
                        break;

                    default:
                        err = "Unknown serialization error";
                        break;
                }
                //Debug.WriteLine(err);
                throw new InvalidOperationException(err, e);
            }
        }
    }
}
