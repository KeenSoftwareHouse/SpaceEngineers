using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Serialization
{
    public abstract class MySerializer
    {
        protected internal abstract void Clone(ref object value, MySerializeInfo info);
        protected internal abstract bool Equals(ref object a, ref object b, MySerializeInfo info);

        protected internal abstract void Read(BitStream stream, out object value, MySerializeInfo info);
        protected internal abstract void Write(BitStream stream, object value, MySerializeInfo info);

        public static T CreateAndRead<T>(BitStream stream, MySerializeInfo serializeInfo = null)
        {
            T value;
            CreateAndRead(stream, out value, serializeInfo);
            return value;
        }

        public static void CreateAndRead<T>(BitStream stream, out T value, MySerializeInfo serializeInfo = null)
        {
            MySerializationHelpers.CreateAndRead(stream, out value, MyFactory.GetSerializer<T>(), serializeInfo ?? MySerializeInfo.Default);
        }

        public static void Write<T>(BitStream stream, ref T value, MySerializeInfo serializeInfo = null)
        {
            MySerializationHelpers.Write(stream, ref value, MyFactory.GetSerializer<T>(), serializeInfo ?? MySerializeInfo.Default);
        }

        public static bool AnyNull(object a, object b)
        {
            return a == null || b == null;
        }
    }
}
