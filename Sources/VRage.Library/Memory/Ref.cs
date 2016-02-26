using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage
{
    /// <summary>
    /// Good for struct wrapping to store it as ref value in dictionary.
    /// </summary>
    public class Ref<T>
    {
        public T Value;
    }
}
