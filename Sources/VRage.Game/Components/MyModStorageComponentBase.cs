using System;
using System.Collections;
using System.Collections.Generic;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Serialization;

namespace VRage.Game.Components
{
    public abstract class MyModStorageComponentBase : MyEntityComponentBase, IDictionary<Guid, string>
    {
        protected IDictionary<Guid, string> m_storageData = new Dictionary<Guid, string>();

        public override string ComponentTypeDebugString
        {
            get { return "Mod Storage"; }
        }

        /// <summary>
        /// Gets a value from the Storage dictionary with the specified key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="guid"></param>
        /// <returns></returns>
        /// <remarks>This can throw exceptions</remarks>
        public abstract string GetValue(Guid guid);

        /// <summary>
        /// Trys to a value from the Storage dictionary with the specified key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="guid"></param>
        /// <param name="value"></param>
        /// <returns><b>true</b> on success; <b>false</b> on failure</returns>
        public abstract bool TryGetValue(Guid guid, out string value);

        /// <summary>
        /// Stores a value with the specified key into the Storage dictionary.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="guid"></param>
        /// <param name="value"></param>
        public abstract void SetValue(Guid guid, string value);

        /// <summary>
        /// Removes a value with the specified key from the Storage dictionary.
        /// </summary>
        /// <param name="guid"></param>
        public abstract bool RemoveValue(Guid guid);

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var ob = (MyObjectBuilder_ModStorageComponent)base.Serialize(copy);
            ob.Storage = new SerializableDictionary<Guid, string>((Dictionary<Guid, string>)m_storageData);
            return ob;
        }

        #region IDictionary
        public string this[Guid key]
        {
            get
            {
                return GetValue(key);
            }

            set
            {
                SetValue(key, value);
            }
        }

        public int Count
        {
            get
            {
                return m_storageData.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return m_storageData.IsReadOnly;
            }
        }

        public ICollection<Guid> Keys
        {
            get
            {
                return m_storageData.Keys;
            }
        }

        public ICollection<string> Values
        {
            get
            {
                return m_storageData.Values;
            }
        }

        public void Add(KeyValuePair<Guid, string> item)
        {
            m_storageData.Add(item);
        }

        public void Add(Guid key, string value)
        {
            SetValue(key, value);
        }

        public void Clear()
        {
            m_storageData.Clear();
        }

        public bool Contains(KeyValuePair<Guid, string> item)
        {
            return m_storageData.Contains(item);
        }

        public bool ContainsKey(Guid key)
        {
            return m_storageData.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<Guid, string>[] array, int arrayIndex)
        {
            m_storageData.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<Guid, string>> GetEnumerator()
        {
            return m_storageData.GetEnumerator();
        }

        public bool Remove(KeyValuePair<Guid, string> item)
        {
            return m_storageData.Remove(item);
        }

        public bool Remove(Guid key)
        {
            return RemoveValue(key);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_storageData.GetEnumerator();
        }
        #endregion IDictionary    
    }
}
