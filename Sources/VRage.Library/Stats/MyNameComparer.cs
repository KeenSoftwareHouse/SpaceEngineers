using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Stats
{
    class MyNameComparer : Comparer<KeyValuePair<string, MyStat>>
    {
        public override int Compare(KeyValuePair<string, MyStat> x, KeyValuePair<string, MyStat> y)
        {
            return x.Key.CompareTo(y.Key);
        }
    }
}
