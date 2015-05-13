using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VRage.Collections
{
    public class MySwapQueue
    {
        public static MySwapQueue<T> Create<T>()
            where T : class,new()
        {
            return new MySwapQueue<T>(new T(), new T(), new T());
        }
    }

    /// <summary>
    /// Holds three objects in safe manner, use when Reader requires only last valid data.
    /// One object is used for reading, one for writing and third is used as buffer, so reader/writer don't have to wait on the other.
    /// </summary>
    public class MySwapQueue<T>
        where T : class
    {
        T m_read;
        T m_write;
        T m_waitingData;
        T m_unusedData;

        public T Read
        {
            get { return m_read; }
        }

        public T Write
        {
            get { return m_write; }
        }

        public MySwapQueue(Func<T> factoryMethod)
            : this(factoryMethod(), factoryMethod(), factoryMethod())
        {
        }

        public MySwapQueue(T first, T second, T third)
        {
            m_read = first;
            m_write = second;
            m_unusedData = third;
            m_waitingData = null;
        }

        /// <summary>
        /// Updates data for reading if there's something new
        /// Returns true when Read was updated, returns false when Read was not changed
        /// </summary>
        public bool RefreshRead()
        {
            // First set unused data
            if (Interlocked.CompareExchange(ref m_unusedData, m_read, null) == null)
            {
                // Unused data was empty, so there must be waiting data
                m_read = Interlocked.Exchange(ref m_waitingData, null);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Commits Write and replaces write with new object ready for new writing
        /// </summary>
        public void CommitWrite()
        {
            m_write = Interlocked.Exchange(ref m_waitingData, m_write);
            if (m_write == null)
            {
                // Waiting data was empty, so there must be unused data
                m_write = Interlocked.Exchange(ref m_unusedData, null);
            }
        }
    }
}
