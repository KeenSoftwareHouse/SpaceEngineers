// Implemention from Joe Duffy's blog:
// http://www.bluebytesoftware.com/blog/2008/08/12/BuildingACustomThreadPoolSeriesPart2AWorkStealingQueue.aspx

using System;
using System.Threading;

// This class has bugs, don't use it!
// TODO: Make this obsolete, can't do it ATM because other things are using it.
class Deque<T>
{
    private const int INITIAL_SIZE = 32;
    private T[] m_array = new T[INITIAL_SIZE];
    private int m_mask = INITIAL_SIZE - 1;
    private volatile int m_headIndex = 0;
    private volatile int m_tailIndex = 0;
    private object m_foreignLock = new object();

    public bool IsEmpty
    {
        get { return m_headIndex >= m_tailIndex; }
    }

    public int Count
    {
        get { return m_tailIndex - m_headIndex; }
    }

    public void LocalPush(T obj)
    {
        lock (m_foreignLock)
        {
            int tail = m_tailIndex;
            if (tail < m_headIndex + m_mask)
            {
                m_array[tail & m_mask] = obj;
                m_tailIndex = tail + 1;
            }
            else
            {

                int head = m_headIndex;
                int count = m_tailIndex - m_headIndex;

                if (count >= m_mask)
                {
                    T[] newArray = new T[m_array.Length << 1];
                    for (int i = 0; i < count; i++)
                        newArray[i] = m_array[(i + head) & m_mask];

                    // Reset the field values, incl. the mask.
                    m_array = newArray;
                    m_headIndex = 0;
                    m_tailIndex = tail = count;
                    m_mask = (m_mask << 1) | 1;
                }
                m_array[tail & m_mask] = obj;
                m_tailIndex = tail + 1;
            }
        }
    }

    public bool LocalPop(ref T obj)
    {
        lock (m_foreignLock)
        {
            int tail = m_tailIndex;
            if (m_headIndex >= tail)
                return false;

#pragma warning disable 0420

            tail -= 1;
            Interlocked.Exchange(ref m_tailIndex, tail);

            if (m_headIndex <= tail)
            {
                obj = m_array[tail & m_mask];
                return true;
            }
            else
            {

                if (m_headIndex <= tail)
                {
                    // Element still available. Take it.
                    obj = m_array[tail & m_mask];
                    return true;
                }
                else
                {
                    // We lost the race, element was stolen, restore the tail.
                    m_tailIndex = tail + 1;
                    return false;
                }
            }
        }
    }

    public bool TrySteal(ref T obj)
    {
        bool taken = false;
        try
        {
            taken = Monitor.TryEnter(m_foreignLock);
            if (taken)
            {
                int head = m_headIndex;
                Interlocked.Exchange(ref m_headIndex, head + 1);
                if (head < m_tailIndex)
                {
                    obj = m_array[head & m_mask];
                    return true;
                }
                else
                {
                    m_headIndex = head;
                    return false;
                }
            }
        }
        finally
        {
            if (taken)
                Monitor.Exit(m_foreignLock);
        }

        return false;
    }

    public void Clear()
    {
        for (int i = 0; i < m_array.Length; i++)
        {
            m_array[i] = default(T);
        }
        m_headIndex = 0;
        m_tailIndex = 0;
    }
}