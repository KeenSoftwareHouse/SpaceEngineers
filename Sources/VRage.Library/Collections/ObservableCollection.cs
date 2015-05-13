using System.Collections.Specialized;
using System.Collections;
using System.Collections.Generic;
using System;

namespace VRage.Collections
{
    /// <summary>
    /// Observable collection that also fix support to clear all.
    /// Don't know if ObservableCollection<T> is allocation free.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObservableCollection<T> : System.Collections.ObjectModel.ObservableCollection<T>
    {
        /// <summary>
        /// Enumerator which uses index access.
        /// Index access on Collection is O(1) operation
        /// </summary>
        public struct Enumerator: IEnumerator<T>
        {
            ObservableCollection<T> m_collection;
            int m_index;

            public Enumerator(ObservableCollection<T> collection)
            {
                m_index = -1;
                this.m_collection = collection;
            }

            public T Current
            {
                get { return m_collection[m_index]; }
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                m_index++;
                return m_index < m_collection.Count;
            }

            public void Reset()
            {
                m_index = -1;
            }
        }

        /// <summary>
        /// Clears the items.
        /// </summary>
        protected override void ClearItems()
        {
            var clear = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, this);

            OnCollectionChanged(clear);

            base.ClearItems();
        }

        /// <summary>
        /// Gets allocation free enumerator (returns struct)
        /// </summary>
        public new ObservableCollection<T>.Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public int FindIndex(Predicate<T> match)
        {
            int foundIndex = -1;
            for (int i = 0; i < Items.Count; ++i)
            {
                if (match(Items[i]))
                {
                    foundIndex = i;
                    break;
                }
            }

            return foundIndex;
        }
    }
}