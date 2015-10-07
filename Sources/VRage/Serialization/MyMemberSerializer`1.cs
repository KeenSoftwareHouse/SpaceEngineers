using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Serialization
{
    public abstract class MyMemberSerializer<TOwner> : MyMemberSerializer
    {
        /// <summary>
        /// Makes clone of object member.
        /// </summary>
        public abstract void Clone(ref TOwner original, ref TOwner clone);

        /// <summary>
        /// Tests equality of object members.
        /// </summary>
        public abstract bool Equals(ref TOwner a, ref TOwner b);

        public abstract void Read(BitStream stream, ref TOwner obj, MySerializeInfo info);
        public abstract void Write(BitStream stream, ref TOwner obj, MySerializeInfo info);

        public sealed override void Clone(object original, object clone)
        {
            TOwner aa = (TOwner)original;
            TOwner bb = (TOwner)clone;
            Clone(aa, bb);
        }

        public sealed override bool Equals(object a, object b)
        {
            TOwner aa = (TOwner)a;
            TOwner bb = (TOwner)b;
            return Equals(ref aa, ref bb);
        }

        public sealed override void Read(BitStream stream, object obj, MySerializeInfo info)
        {
            TOwner aa = (TOwner)obj;
            Read(stream, ref aa, info);
        }

        public sealed override void Write(BitStream stream, object obj, MySerializeInfo info)
        {
            TOwner aa = (TOwner)obj;
            Write(stream, ref aa, info);
        }
    }
}
