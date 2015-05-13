using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    public class MySyncedClass
    {
        public ulong entityId = 0;

        private MySyncedClass m_parent;

        public bool IsDirty { get { return m_dirty; } }
        private bool m_dirty;

        private List<MySyncedClass> m_syncedObjects = new List<MySyncedClass>();
        private List<IMySyncedValue> m_syncedVariables = new List<IMySyncedValue>();

        public void Invalidate()
        {
            m_dirty = true;
            if (m_parent != null)
            {
                m_parent.Invalidate();
            }
        }

        public MySyncedClass(MySyncedClass parent = null)
        {
            m_parent = parent;
            Invalidate();
        }

        public void Add(MySyncedClass mySyncedObject)
        {
            mySyncedObject.m_parent = this;
            m_syncedObjects.Add(mySyncedObject);
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

        internal void Serialize(BitStream bs)
        {
            bs.Write(m_dirty);
            if (m_dirty)
            {
                foreach (var mySyncedObject in m_syncedObjects)
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

        internal void Deserialize(BitStream bs)
        {
            bool success;

            bool fieldExists;
            success = bs.Read(out fieldExists);
            Debug.Assert(success, "Failed to read synced class existance");

            if (fieldExists)
            {
                foreach (var mySyncedObject in m_syncedObjects)
                {
                    mySyncedObject.Deserialize(bs);
                }

                foreach (var mySynced in m_syncedVariables)
                {
                    mySynced.Deserialize(bs);
                }
            }
        }
    }
}
