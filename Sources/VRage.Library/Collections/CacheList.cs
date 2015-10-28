using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
    public class CacheList<T> : List<T>, IDisposable
    {
        public CacheList<T> Empty
        {
            get
            {
                Debug.Assert(Count == 0, "Cache list was not cleared!");
                return this;
            }
        }

        public CacheList()
        {
        }

        public CacheList(int capacity)
            : base(capacity)
        {
        }

        void IDisposable.Dispose()
        {
            Clear();
        }
    }
}
