using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage
{
    public sealed class Boxed<T>
        where T : struct
    {
        public T BoxedValue;

        public Boxed(T value)
        {
            BoxedValue = value;
        }

        public override int GetHashCode()
        {
            return BoxedValue.GetHashCode();
        }

        public override string ToString()
        {
            return BoxedValue.ToString();
        }

        public static implicit operator T(Boxed<T> box)
        {
            return box.BoxedValue;
        }

        public static explicit operator Boxed<T>(T value)
        {
            return new Boxed<T>(value);
        }
    }
}
