using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VRage.Collections
{
    public class ThreadSafeStore<TKey, TValue>
    {
        private readonly object m_lock = new object();
        private Dictionary<TKey, TValue> m_store;
        private readonly Func<TKey, TValue> m_creator;

        public ThreadSafeStore(Func<TKey, TValue> creator)
        {
            if (creator == null)
            {
                throw new ArgumentNullException("creator");
            }
            this.m_creator = creator;
            this.m_store = new Dictionary<TKey, TValue>();
        }

        public TValue Get(TKey key)
        {
            TValue value;
            if (!this.m_store.TryGetValue(key, out value))
            {
                return this.AddValue(key);
            }
            return value;
        }

        public TValue Get(TKey key, Func<TKey, TValue> creator)
        {
            TValue value;
            if (!this.m_store.TryGetValue(key, out value))
            {
                return this.AddValue(key, creator);
            }
            return value;
        }

        private TValue AddValue(TKey key, Func<TKey, TValue> creator = null)
        {
			Func<TKey, TValue> cc = creator ?? m_creator;
            TValue value = cc(key);
            TValue result;
            lock (this.m_lock)
            {
                if (this.m_store == null)
                {
                    this.m_store = new Dictionary<TKey, TValue>();
                    this.m_store[key] = value;
                }
                else
                {
                    TValue checkValue;
                    if (this.m_store.TryGetValue(key, out checkValue))
                    {
                        result = checkValue;
                        return result;
                    }
                    Dictionary<TKey, TValue> newStore = new Dictionary<TKey, TValue>(this.m_store);
                    newStore[key] = value;
                    Thread.MemoryBarrier();
                    this.m_store = newStore;
                }
                result = value;
            }
            return result;
        }
    }
}
