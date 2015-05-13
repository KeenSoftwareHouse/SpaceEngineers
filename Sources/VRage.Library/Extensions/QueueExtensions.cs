using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    public static class QueueExtensions
    {
        public static bool TryDequeue<T>(this Queue<T> queue, out T result)
        {

            if (queue.Count > 0)
            {
                result = queue.Dequeue();
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }

        public static bool TryDequeueSync<T>(this Queue<T> queue, out T result)
        {
            var collection = (ICollection)queue;

            Debug.Assert(collection.IsSynchronized, "Collection must be synchronized for this type of access");

            lock (collection.SyncRoot)
            {
                return queue.TryDequeue(out result);
            }
        }
    }
}
