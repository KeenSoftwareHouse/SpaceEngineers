using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Utils;

//  Object pool generic class - but simple version
//
//  Difference to MyObjectsPool is in that this class doesn't allow removing individual objects. Only all objects by calling ClearAllAllocated().
//  So it can be used for example in draw call where we need to use lot of objects that live only in that draw call and then are cleared.  

namespace VRage.Generics
{
    public class MyObjectsPoolSimple<T> where T : class, new()
    {
        //  Preallocated array from which pool gives objects
        T[] m_items;

        int m_nextAllocateIndex;

        
        //  Class doesn't support parameter-less constructor
        private MyObjectsPoolSimple() { }

        public MyObjectsPoolSimple(int capacity)
        {
            //  Pool should contain at least one preallocated item!
            MyDebug.AssertRelease(capacity > 0);

            ClearAllAllocated();
    
            //  Preallocate items
            m_items = new T[capacity];
            for (int i = 0; i < m_items.Length; i++)
            {
                m_items[i] = new T();
            }
        }

        //  Allocates new object in the pool and returns reference to it.
        //  If pool doesn't have free object (it's full), null is returned. But this shouldn't happen if capacity is chosen carefully.
        public T Allocate()
        {
            if (m_nextAllocateIndex >= m_items.Length)
            {
                return null;
            }

            T ret = m_items[m_nextAllocateIndex];
            m_nextAllocateIndex++;
            return ret;
        }

        //  Return count of active items in the pool
        public int GetAllocatedCount()
        {
            return m_nextAllocateIndex;
        }

        //  Return max number of items in the pool
        public int GetCapacity()
        {
            return m_items.Length;
        }

        //  Clear all objects. It won't really delete them (calling destructor, etc), just mark as deleted, so GetActiveCount() will return zero.
        public void ClearAllAllocated()
        {
            m_nextAllocateIndex = 0;
        }

        //  Return reference to item from array, but only if object with requested index was allocated through Allocate().
        //  Use this method to access already allocated objects. It won't allow access indexes that weren't allocated.
        //  IMPORTANT: Don't iterate over whole array, iterate only to GetActiveCount()
        public T GetAllocatedItem(int index)
        {
            MyDebug.AssertDebug(index < m_nextAllocateIndex);
            return m_items[index];
        }

        public void Sort(IComparer<T> comparer)
        {
            Debug.Assert(comparer != null);
            // Nothing to sort as 
            if (m_nextAllocateIndex <= 1)
                return;

            Array.Sort(m_items, 0, m_nextAllocateIndex, comparer);
        }
    }
}
