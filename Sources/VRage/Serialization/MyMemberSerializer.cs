using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Serialization
{
    public abstract class MyMemberSerializer
    {
        protected MySerializeInfo m_info;

        public MySerializeInfo Info
        {
            get { return m_info; }
        }

        public abstract void Init(MemberInfo memberInfo, MySerializeInfo info);

        /// <summary>
        /// Makes clone of object member.
        /// </summary>
        public abstract void Clone(object original, object clone);

        /// <summary>
        /// Tests equality of object members.
        /// </summary>
        public new abstract bool Equals(object a, object b);

        public abstract void Read(BitStream stream, object obj, MySerializeInfo info);
        public abstract void Write(BitStream stream, object obj, MySerializeInfo info);
    }
}
