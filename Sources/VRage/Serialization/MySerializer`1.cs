using VRage.Library.Collections;

namespace VRage.Serialization
{
    public abstract class MySerializer<T> : MySerializer
    {
        public static bool IsValueType
        {
            get { return typeof(T).IsValueType; }
        }
        public static bool IsClass
        {
            get { return !IsValueType; }
        }

        /// <summary>
        /// In-place clone.
        /// Primitive and immutable types can implements this as empty method.
        /// Reference types must create new instance a fill it's members.
        /// </summary>
        public abstract void Clone(ref T value);

        /// <summary>
        /// Tests equality.
        /// </summary>
        public abstract bool Equals(ref T a, ref T b);

        public abstract void Read(BitStream stream, out T value, MySerializeInfo info);
        public abstract void Write(BitStream stream, ref T value, MySerializeInfo info);

        protected sealed internal override void Clone(ref object value, MySerializeInfo info)
        {
            T inst = (T)value;
            Clone(ref inst);
            value = inst;
        }

        protected sealed internal override bool Equals(ref object a, ref object b, MySerializeInfo info)
        {
            T instA = (T)a;
            T instB = (T)b;
            return Equals(ref instA, ref instB);
        }

        protected sealed internal override void Read(BitStream stream, out object value, MySerializeInfo info)
        {
            T obj;
            Read(stream, out obj, info);
            value = obj;
        }

        protected sealed internal override void Write(BitStream stream, object value, MySerializeInfo info)
        {
            T obj = (T)value;
            Write(stream, ref obj, info);
        }
    }
}
