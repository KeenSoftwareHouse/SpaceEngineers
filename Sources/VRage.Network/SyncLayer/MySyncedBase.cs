using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Network
{
    // TODO:SK only for structs and have string special case?
    // TODO:SK different locks
    public abstract class MySyncedBase<T> : IMySyncedValue, IEquatable<T>
    {
        public abstract void Write(ref T value, BitStream s);
        public abstract void Read(out T value, BitStream s);

        protected MySyncedDataStateEnum[] m_dirty = new MySyncedDataStateEnum[64]; // TODO:SK eek
        public MySyncedDataStateEnum GetDataState(int clientIndex)
        {
            Debug.Assert(clientIndex < m_dirty.Length);
            return m_dirty[clientIndex];
        }

        public bool IsDefault()
        {
            return Equals(default(T));
        }

        protected T m_value;

        private MySyncedClass m_parent;

        public void SetParent(MySyncedClass parent)
        {
            Debug.Assert(m_parent == null);
            m_parent = parent;
        }

        public void ResetPending(int clientIndex)
        {
            Debug.Assert(clientIndex < m_dirty.Length);
            if (m_dirty[clientIndex] == MySyncedDataStateEnum.Pending)
            {
                m_dirty[clientIndex] = MySyncedDataStateEnum.UpToDate;
            }
        }

        public void Invalidate()
        {
            for (int i = 0; i < m_dirty.Length; i++)
            {
                m_dirty[i] = MySyncedDataStateEnum.Outdated;
            }
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
                if (!Equals(value))
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
                var dirty = m_dirty[clientIndex] != MySyncedDataStateEnum.UpToDate;
                bs.WriteBool(dirty);
                if (dirty)
                {
                    Write(ref m_value, bs);
                    m_dirty[clientIndex] = MySyncedDataStateEnum.Pending;
                }
            }
        }

        public virtual void Deserialize(BitStream bs)
        {
            bool fieldExists;
            fieldExists = bs.ReadBool();

            if (fieldExists)
            {
                lock (this)
                {
                    Read(out m_value, bs);
                }
            }
        }

        public virtual void SerializeDefault(BitStream bs, int clientIndex = -1)
        {
            Debug.Assert(m_value != null);
            lock (this)
            {
                bool isDefault = IsDefault();
                bs.WriteBool(!isDefault);
                if (!isDefault)
                {
                    Write(ref m_value, bs);
                }

                if (clientIndex == -1)
                {
                    for (int i = 0; i < m_dirty.Length; i++)
                    {
                        m_dirty[i] = MySyncedDataStateEnum.Pending;
                    }
                }
                else
                {
                    Debug.Assert(clientIndex < m_dirty.Length);
                    m_dirty[clientIndex] = MySyncedDataStateEnum.Pending;
                }
            }
        }

        public virtual void DeserializeDefault(BitStream bs)
        {
            bool isDefault = bs.ReadBool();

            if (!isDefault)
            {
                lock (this)
                {
                    Read(out m_value, bs);
                }
            }
        }

        public virtual bool Equals(T other)
        {
            return EqualityComparer<T>.Default.Equals(this.m_value, other);
        }
    }
}
