using System;
using System.Collections.Generic;

namespace VRage.Collections
{
    /// <summary>
    /// Dictionary wrapper that does not allow modification.
    /// </summary>
    public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public IDictionary<TKey, TValue> Dictionary { private get; set; }
        public bool HasDictionary { get { return Dictionary != null; } }

        private const string NotSupported_ReadOnly = "Dictionary is readonly.";

        /// <summary>
        /// Creates a ReadOnlyDictionary without filling Dictionary. All operations will throw exceptions until Dictionary is set.
        /// </summary>
        public ReadOnlyDictionary() { }

        /// <summary>
        /// Creates a ReadonlyDictionary for the specified IDictionary.
        /// Elements are not copied; changes to dictionary will affect the new ReadonlyDictionary.
        /// </summary>
        /// <param name="dictionary">IDictionary for which this ReadonlyDictionary will be created.</param>
        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
        { Dictionary = dictionary; }

        #region Public Functions

        /// <see cref="System.Collections.Generic.IDictionary<TKey,TValue>.ContainsKey(TKey key)"/>
        public bool ContainsKey(TKey key)
        { return Dictionary.ContainsKey(key); }

        /// <see cref="System.Collections.Generic.IDictionary<TKey,TValue>.Keys"/>
        public ICollection<TKey> Keys
        { get { return Dictionary.Keys; } }

        /// <see cref="System.Collections.Generic.IDictionary<TKey,TValue>.TryGetValue(TKey key, out TValue value)"/>
        public bool TryGetValue(TKey key, out TValue value)
        { return Dictionary.TryGetValue(key, out value); }

        /// <see cref="System.Collections.Generic.IDictionary<TKey,TValue>.Values"/>
        public ICollection<TValue> Values
        { get { return Dictionary.Values; } }

        /// <see cref="System.Collections.Generic.IDictionary<TKey,TValue>.this[TKey key]"/>
        public TValue this[TKey key]
        {
            get
            { return Dictionary[key]; }
        }

        /// <see cref="System.Collections.Generic.ICollection<TKey,TValue>.Contains(KeyValuePair<TKey, TValue> item)"/>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        { return Dictionary.Contains(item); }

        /// <see cref="System.Collections.Generic.ICollection<TKey,TValue>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)"/>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        { Dictionary.CopyTo(array, arrayIndex); }

        /// <see cref="System.Collections.Generic.ICollection<TKey,TValue>.Count"/>
        public int Count
        { get { return Dictionary.Count; } }

        /// <summary>true</summary>
        /// <see cref="System.Collections.Generic.ICollection<TKey,TValue>.IsReadOnly"/>
        public bool IsReadOnly
        { get { return true; } }

        /// <see cref="System.Collections.Generic.IEnumerable<TKey,TValue>.GetEnumerator()"/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        { return Dictionary.GetEnumerator(); }

        #endregion

        #region IDictionary<TKey,TValue> Members

        /// <summary>Throws a NotSupportedException.</summary>
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        { throw new NotSupportedException(NotSupported_ReadOnly); }

        /// <see cref="System.Collections.Generic.IDictionary<TKey,TValue>.ContainsKey(TKey key)"/>
        bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
        { return ContainsKey(key); }

        /// <see cref="System.Collections.Generic.IDictionary<TKey,TValue>.Keys"/>
        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        { get { return Keys; } }

        /// <summary>Throws a NotSupportedException.</summary>
        bool IDictionary<TKey, TValue>.Remove(TKey key)
        { throw new NotSupportedException(NotSupported_ReadOnly); }

        /// <see cref="System.Collections.Generic.IDictionary<TKey,TValue>.TryGetValue(TKey key, out TValue value)"/>
        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        { return TryGetValue(key, out value); }

        /// <see cref="System.Collections.Generic.IDictionary<TKey,TValue>.Values"/>
        ICollection<TValue> IDictionary<TKey, TValue>.Values
        { get { return Values; } }

        /// <summary>set throws a NotSupportedException</summary>
        /// <see cref="System.Collections.Generic.IDictionary<TKey,TValue>.this[TKey key]"/>
        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get
            { return Dictionary[key]; }
            set
            { throw new NotSupportedException(NotSupported_ReadOnly); }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        /// <summary>Throws a NotSupportedException.</summary>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        { throw new NotSupportedException(NotSupported_ReadOnly); }

        /// <summary>Throws a NotSupportedException.</summary>
        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        { throw new NotSupportedException(NotSupported_ReadOnly); }

        /// <see cref="System.Collections.Generic.ICollection<TKey,TValue>.Contains(KeyValuePair<TKey, TValue> item)"/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        { return Contains(item); }

        /// <see cref="System.Collections.Generic.ICollection<TKey,TValue>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)"/>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        { CopyTo(array, arrayIndex); }

        /// <see cref="System.Collections.Generic.ICollection<TKey,TValue>.Count"/>
        int ICollection<KeyValuePair<TKey, TValue>>.Count
        { get { return Count; } }

        /// <summary>true</summary>
        /// <see cref="System.Collections.Generic.ICollection<TKey,TValue>.IsReadOnly"/>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        { get { return true; } }

        /// <summary>Throws a NotSupportedException.</summary>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        { throw new NotSupportedException(NotSupported_ReadOnly); }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        /// <see cref="System.Collections.Generic.IEnumerable<TKey,TValue>.GetEnumerator()"/>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        { return GetEnumerator(); }

        #endregion

        #region IEnumerable Members

        /// <see cref="System.Collections.Generic.IEnumerable<TKey,TValue>.GetEnumerator()"/>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }

        #endregion
    }
}
