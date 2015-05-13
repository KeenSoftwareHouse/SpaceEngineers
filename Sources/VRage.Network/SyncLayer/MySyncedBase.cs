using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public abstract class MySyncedBase<T> : IMySyncedValue, IEquatable<T>
    {
        public abstract void Write(ref T value, BitStream s);
        public abstract bool Read(out T value, BitStream s);

        private bool m_dirty;
        public bool IsDirty { get { return m_dirty; } }

        private T m_value;

        private MySyncedClass m_parent;

        public void SetParent(MySyncedClass parent)
        {
            Debug.Assert(m_parent == null);
            m_parent = parent;
        }

        public void Invalidate()
        {
            m_dirty = true;
            if (m_parent != null)
            {
                m_parent.Invalidate();
            }
        }

        public static implicit operator T(MySyncedBase<T> self)
        {
            return self.Get();
        }

        //If you change the value of the returned class, you should call Invalidate() or Set() to make the new value synchronize
        public T Get()
        {
            lock (this)
            {
                return m_value;
            }
        }

        public void Set(T value)
        {
            lock (this)
            {
                if (!value.Equals(m_value))
                {
                    m_value = value;
                    Invalidate();
                }
            }
        }

        public override string ToString()
        {
            return m_value.GetType() + ": " + m_value.ToString();
        }

        public override int GetHashCode()
        {
            return m_value.GetHashCode();
        }

        public void Serialize(BitStream bs)
        {
            lock (this)
            {
                bs.Write(m_dirty);
                if (m_dirty)
                {
                    Write(ref m_value, bs);
                    m_dirty = false;
                }
            }
        }

        public void Deserialize(BitStream bs)
        {
            bool success;

            bool fieldExists;
            success = bs.Read(out fieldExists);
            Debug.Assert(success, "Failed to read synced value existance");

            if (fieldExists)
            {
                lock (this)
                {
                    success = Read(out m_value, bs);
                    Debug.Assert(success, "Failed to read synced int value");
                }
            }
        }

        public bool Equals(T other)
        {
            return Equals(other);
        }
    }
}
