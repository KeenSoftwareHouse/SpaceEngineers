using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace VRage.Serialization
{
    /// <summary>
    /// Serializer for empty class, does nothing
    /// </summary>
    public class TupleSerializer : ISerializer<MyTuple>
    {
        void ISerializer<MyTuple>.Serialize(ByteStream destination, ref MyTuple data)
        {
        }

        void ISerializer<MyTuple>.Deserialize(ByteStream source, out MyTuple data)
        {
        }
    }

    public class TupleSerializer<T1> : ISerializer<MyTuple<T1>>
    {
        public readonly ISerializer<T1> m_serializer1;

        public TupleSerializer(ISerializer<T1> serializer1)
        {
            m_serializer1 = serializer1;
        }

        void ISerializer<MyTuple<T1>>.Serialize(ByteStream destination, ref MyTuple<T1> data)
        {
            m_serializer1.Serialize(destination, ref data.Item1);
        }

        void ISerializer<MyTuple<T1>>.Deserialize(ByteStream source, out MyTuple<T1> data)
        {
            m_serializer1.Deserialize(source, out data.Item1);
        }
    }

    public class TupleSerializer<T1, T2> : ISerializer<MyTuple<T1, T2>>
    {
        public readonly ISerializer<T1> m_serializer1;
        public readonly ISerializer<T2> m_serializer2;

        public TupleSerializer(ISerializer<T1> serializer1, ISerializer<T2> serializer2)
        {
            m_serializer1 = serializer1;
            m_serializer2 = serializer2;
        }

        void ISerializer<MyTuple<T1, T2>>.Serialize(ByteStream destination, ref MyTuple<T1, T2> data)
        {
            m_serializer1.Serialize(destination, ref data.Item1);
            m_serializer2.Serialize(destination, ref data.Item2);
        }

        void ISerializer<MyTuple<T1, T2>>.Deserialize(ByteStream source, out MyTuple<T1, T2> data)
        {
            m_serializer1.Deserialize(source, out data.Item1);
            m_serializer2.Deserialize(source, out data.Item2);
        }
    }

    public class TupleSerializer<T1, T2, T3> : ISerializer<MyTuple<T1, T2, T3>>
    {
        public readonly ISerializer<T1> m_serializer1;
        public readonly ISerializer<T2> m_serializer2;
        public readonly ISerializer<T3> m_serializer3;

        public TupleSerializer(ISerializer<T1> serializer1, ISerializer<T2> serializer2, ISerializer<T3> serializer3)
        {
            m_serializer1 = serializer1;
            m_serializer2 = serializer2;
            m_serializer3 = serializer3;
        }

        void ISerializer<MyTuple<T1, T2, T3>>.Serialize(ByteStream destination, ref MyTuple<T1, T2, T3> data)
        {
            m_serializer1.Serialize(destination, ref data.Item1);
            m_serializer2.Serialize(destination, ref data.Item2);
            m_serializer3.Serialize(destination, ref data.Item3);
        }

        void ISerializer<MyTuple<T1, T2, T3>>.Deserialize(ByteStream source, out MyTuple<T1, T2, T3> data)
        {
            m_serializer1.Deserialize(source, out data.Item1);
            m_serializer2.Deserialize(source, out data.Item2);
            m_serializer3.Deserialize(source, out data.Item3);
        }
    }

    public class TupleSerializer<T1, T2, T3, T4> : ISerializer<MyTuple<T1, T2, T3, T4>>
    {
        public readonly ISerializer<T1> m_serializer1;
        public readonly ISerializer<T2> m_serializer2;
        public readonly ISerializer<T3> m_serializer3;
        public readonly ISerializer<T4> m_serializer4;

        public TupleSerializer(ISerializer<T1> serializer1, ISerializer<T2> serializer2, ISerializer<T3> serializer3, ISerializer<T4> serializer4)
        {
            m_serializer1 = serializer1;
            m_serializer2 = serializer2;
            m_serializer3 = serializer3;
            m_serializer4 = serializer4;
        }

        void ISerializer<MyTuple<T1, T2, T3, T4>>.Serialize(ByteStream destination, ref MyTuple<T1, T2, T3, T4> data)
        {
            m_serializer1.Serialize(destination, ref data.Item1);
            m_serializer2.Serialize(destination, ref data.Item2);
            m_serializer3.Serialize(destination, ref data.Item3);
            m_serializer4.Serialize(destination, ref data.Item4);
        }

        void ISerializer<MyTuple<T1, T2, T3, T4>>.Deserialize(ByteStream source, out MyTuple<T1, T2, T3, T4> data)
        {
            m_serializer1.Deserialize(source, out data.Item1);
            m_serializer2.Deserialize(source, out data.Item2);
            m_serializer3.Deserialize(source, out data.Item3);
            m_serializer4.Deserialize(source, out data.Item4);
        }
    }

    public class TupleSerializer<T1, T2, T3, T4, T5> : ISerializer<MyTuple<T1, T2, T3, T4, T5>>
    {
        public readonly ISerializer<T1> m_serializer1;
        public readonly ISerializer<T2> m_serializer2;
        public readonly ISerializer<T3> m_serializer3;
        public readonly ISerializer<T4> m_serializer4;
        public readonly ISerializer<T5> m_serializer5;

        public TupleSerializer(ISerializer<T1> serializer1, ISerializer<T2> serializer2, ISerializer<T3> serializer3, ISerializer<T4> serializer4, ISerializer<T5> serializer5)
        {
            m_serializer1 = serializer1;
            m_serializer2 = serializer2;
            m_serializer3 = serializer3;
            m_serializer4 = serializer4;
            m_serializer5 = serializer5;
        }

        void ISerializer<MyTuple<T1, T2, T3, T4, T5>>.Serialize(ByteStream destination, ref MyTuple<T1, T2, T3, T4, T5> data)
        {
            m_serializer1.Serialize(destination, ref data.Item1);
            m_serializer2.Serialize(destination, ref data.Item2);
            m_serializer3.Serialize(destination, ref data.Item3);
            m_serializer4.Serialize(destination, ref data.Item4);
            m_serializer5.Serialize(destination, ref data.Item5);
        }

        void ISerializer<MyTuple<T1, T2, T3, T4, T5>>.Deserialize(ByteStream source, out MyTuple<T1, T2, T3, T4, T5> data)
        {
            m_serializer1.Deserialize(source, out data.Item1);
            m_serializer2.Deserialize(source, out data.Item2);
            m_serializer3.Deserialize(source, out data.Item3);
            m_serializer4.Deserialize(source, out data.Item4);
            m_serializer5.Deserialize(source, out data.Item5);
        }
    }

    public class TupleSerializer<T1, T2, T3, T4, T5, T6> : ISerializer<MyTuple<T1, T2, T3, T4, T5, T6>>
    {
        public readonly ISerializer<T1> m_serializer1;
        public readonly ISerializer<T2> m_serializer2;
        public readonly ISerializer<T3> m_serializer3;
        public readonly ISerializer<T4> m_serializer4;
        public readonly ISerializer<T5> m_serializer5;
        public readonly ISerializer<T6> m_serializer6;

        public TupleSerializer(ISerializer<T1> serializer1, ISerializer<T2> serializer2, ISerializer<T3> serializer3, ISerializer<T4> serializer4, ISerializer<T5> serializer5, ISerializer<T6> serializer6)
        {
            m_serializer1 = serializer1;
            m_serializer2 = serializer2;
            m_serializer3 = serializer3;
            m_serializer4 = serializer4;
            m_serializer5 = serializer5;
            m_serializer6 = serializer6;
        }

        void ISerializer<MyTuple<T1, T2, T3, T4, T5, T6>>.Serialize(ByteStream destination, ref MyTuple<T1, T2, T3, T4, T5, T6> data)
        {
            m_serializer1.Serialize(destination, ref data.Item1);
            m_serializer2.Serialize(destination, ref data.Item2);
            m_serializer3.Serialize(destination, ref data.Item3);
            m_serializer4.Serialize(destination, ref data.Item4);
            m_serializer5.Serialize(destination, ref data.Item5);
            m_serializer6.Serialize(destination, ref data.Item6);
        }

        void ISerializer<MyTuple<T1, T2, T3, T4, T5, T6>>.Deserialize(ByteStream source, out MyTuple<T1, T2, T3, T4, T5, T6> data)
        {
            m_serializer1.Deserialize(source, out data.Item1);
            m_serializer2.Deserialize(source, out data.Item2);
            m_serializer3.Deserialize(source, out data.Item3);
            m_serializer4.Deserialize(source, out data.Item4);
            m_serializer5.Deserialize(source, out data.Item5);
            m_serializer6.Deserialize(source, out data.Item6);
        }
    }
}
