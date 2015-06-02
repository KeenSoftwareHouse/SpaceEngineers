using System;
using System.Collections;
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

        protected BitArray m_dirty = new BitArray(MyRakNetSyncLayer.MaxClients);
        public bool IsDirty(int clientIndex)
        {
            Debug.Assert(clientIndex < m_dirty.Length);
            return m_dirty[clientIndex];
        }

        public bool IsDefault()
        {
            return m_value.Equals(default(T));
        }

        protected T m_value;

        private MySyncedClass m_parent;

        public void SetParent(MySyncedClass parent)
        {
            Debug.Assert(m_parent == null);
            m_parent = parent;
        }

        public void Invalidate()
        {
            m_dirty.SetAll(true);
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

        public virtual void Serialize(BitStream bs, int clientIndex)
        {
            Debug.Assert(m_value != null);
            lock (this)
            {
                var dirty = m_dirty[clientIndex];
                bs.Write(dirty);
                if (dirty)
                {
                    Write(ref m_value, bs);
                    m_dirty[clientIndex] = false;
                }
            }
        }

        public virtual void Deserialize(BitStream bs)
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

        public virtual void SerializeDefault(BitStream bs, int clientIndex = -1)
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

                if (clientIndex == -1)
                {
                    m_dirty.SetAll(false);
                }
                else
                {
                    Debug.Assert(clientIndex < m_dirty.Length);
                    m_dirty[clientIndex] = false;
                }
            }
        }

        public virtual void DeserializeDefault(BitStream bs)
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
