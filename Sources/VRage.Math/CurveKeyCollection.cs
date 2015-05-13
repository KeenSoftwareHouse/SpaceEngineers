using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace VRageMath
{
    /// <summary>
    /// Contains the CurveKeys making up a Curve.
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    [Serializable]
    public class CurveKeyCollection : ICollection<CurveKey>, IEnumerable<CurveKey>, IEnumerable
    {
        private List<CurveKey> Keys = new List<CurveKey>();
        internal bool IsCacheAvailable = true;
        internal float TimeRange;
        internal float InvTimeRange;

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The array index of the element.</param>
        public CurveKey this[int index]
        {
            get
            {
                return this.Keys[index];
            }
            set
            {
                if (value == (CurveKey)null)
                    throw new ArgumentNullException();
                if ((double)this.Keys[index].Position == (double)value.Position)
                {
                    this.Keys[index] = value;
                }
                else
                {
                    this.Keys.RemoveAt(index);
                    this.Add(value);
                }
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the CurveKeyCollection.
        /// </summary>
        public int Count
        {
            get
            {
                return this.Keys.Count;
            }
        }

        /// <summary>
        /// Returns a value indicating whether the CurveKeyCollection is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Determines the index of a CurveKey in the CurveKeyCollection.
        /// </summary>
        /// <param name="item">CurveKey to locate in the CurveKeyCollection.</param>
        public int IndexOf(CurveKey item)
        {
            return this.Keys.IndexOf(item);
        }

        /// <summary>
        /// Removes the CurveKey at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            this.Keys.RemoveAt(index);
            this.IsCacheAvailable = false;
        }

        /// <summary>
        /// Adds a CurveKey to the CurveKeyCollection.
        /// </summary>
        /// <param name="item">The CurveKey to add.</param>
        public void Add(CurveKey item)
        {
            if (item == (CurveKey)null)
                throw new ArgumentNullException();
            int index = this.Keys.BinarySearch(item);
            if (index >= 0)
            {
                while (index < this.Keys.Count && (double)item.Position == (double)this.Keys[index].Position)
                    ++index;
            }
            else
                index = ~index;
            this.Keys.Insert(index, item);
            this.IsCacheAvailable = false;
        }

        /// <summary>
        /// Removes all CurveKeys from the CurveKeyCollection.
        /// </summary>
        public void Clear()
        {
            this.Keys.Clear();
            this.TimeRange = this.InvTimeRange = 0.0f;
            this.IsCacheAvailable = false;
        }

        /// <summary>
        /// Determines whether the CurveKeyCollection contains a specific CurveKey.
        /// </summary>
        /// <param name="item">true if the CurveKey is found in the CurveKeyCollection; false otherwise.</param>
        public bool Contains(CurveKey item)
        {
            return this.Keys.Contains(item);
        }

        /// <summary>
        /// Copies the CurveKeys of the CurveKeyCollection to an array, starting at the array index provided.
        /// </summary>
        /// <param name="array">The destination of the CurveKeys copied from CurveKeyCollection. The array must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in the array to start copying from.</param>
        public void CopyTo(CurveKey[] array, int arrayIndex)
        {
            this.Keys.CopyTo(array, arrayIndex);
            this.IsCacheAvailable = false;
        }

        /// <summary>
        /// Removes the first occurrence of a specific CurveKey from the CurveKeyCollection.
        /// </summary>
        /// <param name="item">The CurveKey to remove from the CurveKeyCollection.</param>
        public bool Remove(CurveKey item)
        {
            this.IsCacheAvailable = false;
            return this.Keys.Remove(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the CurveKeyCollection.
        /// </summary>
        public IEnumerator<CurveKey> GetEnumerator()
        {
            return (IEnumerator<CurveKey>)this.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Keys.GetEnumerator();
        }

        /// <summary>
        /// Creates a copy of the CurveKeyCollection.
        /// </summary>
        public CurveKeyCollection Clone()
        {
            return new CurveKeyCollection()
            {
                Keys = new List<CurveKey>((IEnumerable<CurveKey>)this.Keys),
                InvTimeRange = this.InvTimeRange,
                TimeRange = this.TimeRange,
                IsCacheAvailable = true
            };
        }

        internal void ComputeCacheValues()
        {
            this.TimeRange = this.InvTimeRange = 0.0f;
            if (this.Keys.Count > 1)
            {
                this.TimeRange = this.Keys[this.Keys.Count - 1].Position - this.Keys[0].Position;
                if ((double)this.TimeRange > 1.40129846432482E-45)
                    this.InvTimeRange = 1f / this.TimeRange;
            }
            this.IsCacheAvailable = true;
        }
    }
}
