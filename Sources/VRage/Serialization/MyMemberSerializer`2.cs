using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Library.Collections;
#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
using VRage.Reflection;
#endif // XB1

namespace VRage.Serialization
{
    public sealed class MyMemberSerializer<TOwner, TMember> : MyMemberSerializer<TOwner>
    {
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
        Getter<TOwner, TMember> m_getter;
        Setter<TOwner, TMember> m_setter;
#endif // !XB1

        MySerializer<TMember> m_serializer;
        MemberInfo m_memberInfo;

        public sealed override void Init(MemberInfo memberInfo, MySerializeInfo info)
        {
            if (m_serializer != null)
                throw new InvalidOperationException("Already initialized");

#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
            m_getter = memberInfo.CreateGetterRef<TOwner, TMember>();
            m_setter = memberInfo.CreateSetterRef<TOwner, TMember>();
#endif // !XB1
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
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
            TMember value;
            m_getter(ref original, out value);
            m_serializer.Clone(ref value);
            m_setter(ref clone, ref value);
#else // XB1
            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#endif // XB1
        }

        public override bool Equals(ref TOwner a, ref TOwner b)
        {
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
            TMember valA, valB;
            m_getter(ref a, out valA);
            m_getter(ref b, out valB);
            return m_serializer.Equals(ref valA, ref valB);
#else // XB1
            if (a is IMySetGetMemberDataHelper)
            {
                TMember valA, valB;
                valA = (TMember)((IMySetGetMemberDataHelper)a).GetMemberData(m_memberInfo);
                valB = (TMember)((IMySetGetMemberDataHelper)b).GetMemberData(m_memberInfo);
                return m_serializer.Equals(ref valA, ref valB);
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
                return false;
            }
#endif // XB1
        }

        public sealed override void Read(BitStream stream, ref TOwner obj, MySerializeInfo info)
        {
            TMember result;
            if (MySerializationHelpers.CreateAndRead<TMember>(stream, out result, m_serializer, info ?? m_info))
            {
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
                m_setter(ref obj, ref result);
#else // XB1
                System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#endif // XB1
            }
        }

        public sealed override void Write(BitStream stream, ref TOwner obj, MySerializeInfo info)
        {
            try
            {
#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
                TMember value;
                m_getter(ref obj, out value);
                MySerializationHelpers.Write<TMember>(stream, ref value, m_serializer, info ?? m_info);
#else // XB1
                System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#endif // XB1
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
