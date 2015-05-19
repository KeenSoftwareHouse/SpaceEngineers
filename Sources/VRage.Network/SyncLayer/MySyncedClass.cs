using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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

        internal ulong EntityId { get; set; }

        private MySyncedClass m_parent;
        public void SetParent(MySyncedClass parent)
        {
            m_parent = parent;
        }

        private bool m_dirty;
        public bool IsDirty { get { return m_dirty; } }

        private List<MySyncedClass> m_syncedClass = new List<MySyncedClass>();
        private List<IMySyncedValue> m_syncedVariables = new List<IMySyncedValue>();

        private Dictionary<ulong, uint> m_pending;

        public void InitPending()
        {
            Debug.Assert(m_pending == null);
            m_pending = new Dictionary<ulong, uint>();
        }

        public bool IsPending(ulong steamID)
        {
            Debug.Assert(steamID != 0);
            Debug.Assert(m_pending != null);
            return m_pending.ContainsKey(steamID);
        }

        public void Invalidate()
        {
            m_dirty = true;
            if (m_parent != null)
            {
                m_parent.Invalidate();
            }
        }

        public void Add(MySyncedClass mySyncedObject)
        {
            mySyncedObject.SetParent(this);
            m_syncedClass.Add(mySyncedObject);
            if (mySyncedObject.IsDirty)
            {
                Invalidate();
            }
        }

        public void Add(IMySyncedValue mySynced)
        {
            mySynced.SetParent(this);
            m_syncedVariables.Add(mySynced);
            if (mySynced.IsDirty)
            {
                Invalidate();
            }
        }

        public void Serialize(BitStream bs)
        {
            bs.Write(m_dirty);
            if (m_dirty)
            {
                foreach (var mySyncedObject in m_syncedClass)
                {
                    mySyncedObject.Serialize(bs);
                }

                foreach (var mySynced in m_syncedVariables)
                {
                    mySynced.Serialize(bs);
                }
                m_dirty = false;
            }
        }

        public void Deserialize(BitStream bs)
        {
            bool success;

            bool fieldExists;
            success = bs.Read(out fieldExists);
            Debug.Assert(success, "Failed to read synced class existance");

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

        public void SerializeDefault(BitStream bs)
        {
            bool isDefault = IsDefault();
            bs.Write(!isDefault);
            if (!isDefault)
            {
                foreach (var mySyncedObject in m_syncedClass)
                {
                    mySyncedObject.SerializeDefault(bs);
                }

                foreach (var mySynced in m_syncedVariables)
                {
                    mySynced.SerializeDefault(bs);
                }
            }
        }

        public void DeserializeDefault(BitStream bs)
        {
            bool success;

            bool isDefault;
            success = bs.Read(out isDefault);
            Debug.Assert(success, "Failed to read synced class defaultness");

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
