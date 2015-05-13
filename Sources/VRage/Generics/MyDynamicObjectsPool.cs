using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Generics
{
    /// <summary>
    /// Dynamic object pool. It's allocate new instance when necessary.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MyDynamicObjectPool<T> where T : class, new()
    {
        private Stack<T> m_poolStack;

        public int Count { get { return m_poolStack.Count; } }

        public MyDynamicObjectPool(int capacity)
        {
            m_poolStack = new Stack<T>(capacity);
            Preallocate(capacity);
        }

        private void Preallocate(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T instance = new T();
                m_poolStack.Push(instance);
            }
        }

        public T Allocate()
        {
            if (m_poolStack.Count == 0)
            {
                Preallocate(1);
            }
            T item = m_poolStack.Pop();
            return item;
        }

        public void Deallocate(T item)
        {
            m_poolStack.Push(item);
        }

        public void TrimToSize(int size)
        {
            while (m_poolStack.Count > size)
                m_poolStack.Pop();
            m_poolStack.TrimExcess();
        }
    }
}
