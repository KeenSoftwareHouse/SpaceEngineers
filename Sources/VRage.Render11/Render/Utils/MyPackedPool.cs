using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRageRender
{
    struct MyPackedPoolHandle
    {
        internal readonly int INDEX;

        internal MyPackedPoolHandle(int index)
        {
            INDEX = index;
        }
    }

    class MyPackedIndexer
    {
        internal int m_free;
        internal int[] m_nextFree;
        internal int[] m_backref;
        internal int[] m_indirection;

        internal int Size { get; private set; }

        private int m_sizeLimit;

        public MyPackedIndexer(int sizeLimit)
        {
            m_sizeLimit = sizeLimit;

            m_indirection = new int[m_sizeLimit];
            m_nextFree = new int[m_sizeLimit];
            m_backref = new int[m_sizeLimit];

            for (int i = 0; i < m_sizeLimit; i++)
            {
                m_nextFree[i] = i + 1;
                m_backref[i] = -1;
            }
        }

        public int GetIndex(MyPackedPoolHandle handle)
        {
            return m_indirection[handle.INDEX];
        }

        public void Free(MyPackedPoolHandle handle)
        {
            var handleIndex = handle.INDEX;
            m_nextFree[handleIndex] = m_free;
            m_free = handleIndex;

            // update indirection
            m_indirection[m_backref[Size - 1]] = m_indirection[handleIndex];
            m_backref[m_indirection[handleIndex]] = m_backref[Size - 1];
            m_indirection[handleIndex] = m_sizeLimit;

            Size -= 1;
        }

        public MyPackedPoolHandle Allocate()
        {
            var handle = new MyPackedPoolHandle(m_free);
            m_backref[Size] = m_free;
            m_indirection[m_free] = Size;
            m_free = m_nextFree[m_free];
            Size += 1;
            return handle;
        }

        public void Clear()
        {
            Size = 0;
            for (int i = 0; i < m_sizeLimit; i++)
            {
                m_nextFree[i] = i + 1;
                m_backref[i] = -1;
            }
        }
    }

    class MyPackedIndexerDynamic
    {
        internal int m_free;
        internal int[] m_nextFree;
        internal int[] m_backref;
        internal int[] m_indirection;

        internal int Size { get; private set; }

        private int m_sizeLimit;

        internal int Capacity { get { return m_sizeLimit; } }

        public MyPackedIndexerDynamic(int startingSize)
        {
            m_sizeLimit = startingSize;

            m_indirection = new int[m_sizeLimit];
            m_nextFree = new int[m_sizeLimit];
            m_backref = new int[m_sizeLimit];

            for (int i = 0; i < m_sizeLimit; i++)
            {
                m_nextFree[i] = i + 1;
                m_backref[i] = -1;
            }
        }

        public void Extend(int newSize)
        {
            Debug.Assert(newSize > m_sizeLimit);

            Array.Resize(ref m_nextFree, newSize);
            Array.Resize(ref m_backref, newSize);
            Array.Resize(ref m_indirection, newSize);

            for (int i = m_sizeLimit; i < newSize; i++)
            {
                m_nextFree[i] = i + 1;
                m_backref[i] = -1;
            }

            m_sizeLimit = newSize;
        }

        public int GetIndex(MyPackedPoolHandle handle)
        {
            return m_indirection[handle.INDEX];
        }

        public void Free(MyPackedPoolHandle handle)
        {
            var handleIndex = handle.INDEX;
            m_nextFree[handleIndex] = m_free;
            m_free = handleIndex;

            // update indirection
            m_indirection[m_backref[Size - 1]] = m_indirection[handleIndex];
            m_backref[m_indirection[handleIndex]] = m_backref[Size - 1];
            m_indirection[handleIndex] = m_sizeLimit;

            Size -= 1;
        }

        public MyPackedPoolHandle Allocate()
        {
            if(Size == m_sizeLimit)
            {
                var newSize = m_sizeLimit * ( m_sizeLimit > 1024 ? 2 : 1.5f );
                Extend((int)Math.Ceiling(newSize));
            }

            var handle = new MyPackedPoolHandle(m_free);
            m_backref[Size] = m_free;
            m_indirection[m_free] = Size;
            m_free = m_nextFree[m_free];
            Size += 1;
            return handle;
        }

        public void Clear()
        {
            Size = 0;
            for (int i = 0; i < m_sizeLimit; i++)
            {
                m_nextFree[i] = i + 1;
                m_backref[i] = -1;
            }
        }
    }
}

namespace VRageRender
{

    class MyPackedPool<T> where T : struct
    {
        internal T[] m_entities;

        MyPackedIndexerDynamic m_indexer;

        public int Size { get { return m_indexer.Size; } }

        public MyPackedPool(int startingSize)
        {
            m_indexer = new MyPackedIndexerDynamic(startingSize);
            m_entities = new T[startingSize];
        }

        public T[] Data { get { return m_entities; } }

        public T GetByHandle(MyPackedPoolHandle handle)
        {
            return m_entities[m_indexer.GetIndex(handle)];
        }

        public int AsIndex(MyPackedPoolHandle handle)
        {
            return m_indexer.GetIndex(handle);
        }

        public void Free(MyPackedPoolHandle handle)
        {
            // swap with last
            m_entities[m_indexer.GetIndex(handle)] = m_entities[m_indexer.Size - 1];
            m_entities[m_indexer.Size - 1] = new T();

            m_indexer.Free(handle);
        }

        public MyPackedPoolHandle Allocate()
        {
            var handle = m_indexer.Allocate();
            if(m_indexer.Capacity != m_entities.Length)
            {
                Array.Resize(ref m_entities, m_indexer.Capacity);
            }

            m_entities[m_indexer.Size - 1] = new T();
            return handle;
        }

        public void Clear()
        {
            for (int i = 0; i < m_indexer.Size; i++)
            {
                m_entities[i] = default(T);
            }
            m_indexer.Clear();
        }
    }
}
