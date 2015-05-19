using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    // TODO:SK only for structs and have string special case?
    // TODO:SK different locks
    public abstract class MySyncedBase<T> : IMySyncedValue, IEquatable<T>
    {
        public abstract void Write(ref T value, BitStream s);
        public abstract bool Read(out T value, BitStream s);

        private bool m_dirty;
        public bool IsDirty { get { return m_dirty; } }

        public bool IsDefault()
        {
            return m_value.Equals(default(T));
        }

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

        // Client is not supposed to modify the returned value
        // Server should call Set() with the new value after changing it
        public T Get()
        {
            Debug.Assert(m_value != null);
            lock (this)
            {
                return m_value;
            }
        }

        public void Set(T value)
        {
            Debug.Assert(value != null);
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
            return typeof(T) + ": " + m_value.ToString();
        }

        public override int GetHashCode()
        {
            return m_value.GetHashCode();
        }

        public void Serialize(BitStream bs)
        {
            Debug.Assert(m_value != null);
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
                    Debug.Assert(success, "Failed to read synced value");
                }
            }
        }

        public void SerializeDefault(BitStream bs)
        {
            Debug.Assert(m_value != null);
            lock (this)
            {
                bool isDefault = IsDefault();
                bs.Write(!isDefault);
                if (!isDefault)
                {
                    Write(ref m_value, bs);
                }
            }
        }

        public void DeserializeDefault(BitStream bs)
        {
            bool success;

            bool isDefault;
            success = bs.Read(out isDefault);
            Debug.Assert(success, "Failed to read synced value defaultness");

            if (!isDefault)
            {
                lock (this)
                {
                    success = Read(out m_value, bs);
                    Debug.Assert(success, "Failed to read synced value");
                }
            }
        }

        public bool Equals(T other)
        {
            return Equals(other);
        }
    }
}
