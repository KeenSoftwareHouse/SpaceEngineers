using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Network
{
    public class MySyncedClass : IMySyncedValue
    {
        private int m_typeID = -1;
        internal int TypeID
        {
            get { return m_typeID; }
            set
            {
                Debug.Assert(value != -1, "Invalid typeID");
                Debug.Assert(m_typeID == -1, "Trying to change typeID twice");
                m_typeID = value;
            }
        }

        private MySyncedClass m_parent;
        public void SetParent(MySyncedClass parent)
        {
            m_parent = parent;
        }

        protected MySyncedDataStateEnum[] m_dirty = new MySyncedDataStateEnum[64]; // TODO:SK eek
        public MySyncedDataStateEnum GetDataState(int clientIndex)
        {
            Debug.Assert(clientIndex < m_dirty.Length);
            return m_dirty[clientIndex];
        }

        private List<MySyncedClass> m_syncedClass = new List<MySyncedClass>();
        private List<IMySyncedValue> m_syncedVariables = new List<IMySyncedValue>();

        public void ResetPending(int clientIndex)
        {
            Debug.Assert(clientIndex < m_dirty.Length);
            if (m_dirty[clientIndex] == MySyncedDataStateEnum.Pending)
            {
                m_dirty[clientIndex] = MySyncedDataStateEnum.UpToDate;
            }

            foreach (var mySyncedObject in m_syncedClass)
            {
                mySyncedObject.ResetPending(clientIndex);
            }

            foreach (var mySynced in m_syncedVariables)
            {
                mySynced.ResetPending(clientIndex);
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

        public void Add(MySyncedClass mySyncedObject)
        {
            mySyncedObject.SetParent(this);
            m_syncedClass.Add(mySyncedObject);
            if (!mySyncedObject.IsDefault())
            {
                Invalidate();
            }
        }

        public void Add(IMySyncedValue mySynced)
        {
            mySynced.SetParent(this);
            m_syncedVariables.Add(mySynced);
            if (!mySynced.IsDefault())
            {
                Invalidate();
            }
        }

        public void Serialize(BitStream bs, int clientIndex)
        {
            var dirty = m_dirty[clientIndex] != MySyncedDataStateEnum.UpToDate;
            bs.WriteBool(dirty);
            if (dirty)
            {
                foreach (var mySyncedObject in m_syncedClass)
                {
                    mySyncedObject.Serialize(bs, clientIndex);
                }

                foreach (var mySynced in m_syncedVariables)
                {
                    mySynced.Serialize(bs, clientIndex);
                }
                m_dirty[clientIndex] = MySyncedDataStateEnum.Pending;
            }
        }

        public void Deserialize(BitStream bs)
        {
            bool fieldExists = bs.ReadBool();

            if (fieldExists)
            {
                foreach (var mySyncedObject in m_syncedClass)
                {
                    mySyncedObject.Deserialize(bs);
                }

                foreach (var mySynced in m_syncedVariables)
                {
                    mySynced.Deserialize(bs);
                }
            }
        }

        public bool IsDefault()
        {
            foreach (var sync in m_syncedVariables)
            {
                if (!sync.IsDefault())
                {
                    return false;
                }
            }

            foreach (var sync in m_syncedClass)
            {
                if (!sync.IsDefault())
                {
                    return false;
                }
            }

            return true;
        }

        public void SerializeDefault(BitStream bs, int clientIndex = -1)
        {
            bool isDefault = IsDefault();
            bs.WriteBool(!isDefault);
            if (!isDefault)
            {
                foreach (var syncObj in m_syncedClass)
                {
                    syncObj.SerializeDefault(bs, clientIndex);
                }

                foreach (var syncVar in m_syncedVariables)
                {
                    syncVar.SerializeDefault(bs, clientIndex);
                }
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

        public void DeserializeDefault(BitStream bs)
        {
            bool isDefault = bs.ReadBool();

            if (!isDefault)
            {
                foreach (var mySyncedObject in m_syncedClass)
                {
                    mySyncedObject.DeserializeDefault(bs);
                }

                foreach (var mySynced in m_syncedVariables)
                {
                    mySynced.DeserializeDefault(bs);
                }
            }
        }
    }
}
