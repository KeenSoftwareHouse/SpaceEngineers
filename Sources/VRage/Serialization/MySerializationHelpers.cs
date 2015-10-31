using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Serialization
{
    public static class MySerializationHelpers
    {
        public static bool CreateAndRead<TMember>(BitStream stream, out TMember result, MySerializer<TMember> serializer, MySerializeInfo info)
        {
            if (ReadNullable(stream, info.IsNullable))
            {
                if (MySerializer<TMember>.IsClass && info.IsDynamic)
                {
                    Type type = typeof(TMember);
                    bool readType = true;

                    if (info.IsDynamicDefault)
                    {
                        readType = stream.ReadBool();
                    }

                    if (readType)
                    {
                        type = stream.ReadDynamicType(typeof(TMember), info.DynamicSerializer);
                    }

                    object value;
                    MyFactory.GetSerializer(type).Read(stream, out value, info);
                    result = (TMember)value;
                }
                else
                {
                    serializer.Read(stream, out result, info);
                }
                return true;
            }
            else
            {
                result = default(TMember);
                return false;
            }
        }

        //public static void Read<TMember>(BitStream stream, ref TMember value, MySerializer<TMember> serializer, MySerializeInfo info)
        //{
        //    if (info.IsNullable)
        //        throw new InvalidOperationException("Read does not support nullable");

        //    if (MySerializer<TMember>.IsClass && info.IsDynamic)
        //    {
        //        MyFactory.GetSerializer(value.GetType()).Read(stream, value, info);
        //    }
        //    else
        //    {
        //        serializer.Read(stream, ref value, info);
        //    }
        //}

        public static void Write<TMember>(BitStream stream, ref TMember value, MySerializer<TMember> serializer, MySerializeInfo info)
        {
            if (WriteNullable(stream, ref value, info.IsNullable, serializer))
            {
                if (MySerializer<TMember>.IsClass && info.IsDynamic)
                {
                    var memberType = typeof(TMember);
                    var valueType = value.GetType();

                    bool writeType = true;

                    if (info.IsDynamicDefault)
                    {
                        writeType = memberType != valueType;
                        stream.WriteBool(writeType);
                    }

                    if (writeType)
                    {
                        stream.WriteDynamicType(memberType, valueType, info.DynamicSerializer);
                    }
                    MyFactory.GetSerializer(value.GetType()).Write(stream, value, info);
                }
                else if (MySerializer<TMember>.IsValueType || value.GetType() == typeof(TMember))
                {
                    serializer.Write(stream, ref value, info);
                }
                else
                {
                    throw new MySerializeException(MySerializeErrorEnum.DynamicNotAllowed);
                }
            }
        }

        static bool ReadNullable(BitStream stream, bool isNullable)
        {
            if (isNullable)
            {
                return stream.ReadBool();
            }
            return true;
        }

        static bool WriteNullable<T>(BitStream stream, ref T value, bool isNullable, MySerializer<T> serializer)
        {
            if (isNullable)
            {
                T def = default(T);
                bool hasValue = !serializer.Equals(ref value, ref def);
                stream.WriteBool(hasValue);
                return hasValue;
            }
            else
            {
                if (!typeof(T).IsValueType && value == null)
                    throw new MySerializeException(MySerializeErrorEnum.NullNotAllowed);

                return true;
            }
        }
    }
}
