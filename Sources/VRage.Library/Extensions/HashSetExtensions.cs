using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    public static class HashSetExtensions
    {
        public static T FirstElement<T>(this HashSet<T> hashset)
        {
            var e = hashset.GetEnumerator();
            e.MoveNext();
            return e.Current;
        }
    }
}
