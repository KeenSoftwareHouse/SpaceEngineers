using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
    public static class EmptyArray<T>
    {
        public static readonly T[] Value = new T[0];
    }
}
