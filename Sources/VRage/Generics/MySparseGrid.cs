using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRageMath;

namespace VRage.Generics
{
    public class MySparseGrid<TItemData, TCellData> : IDictionary<Vector3I, TItemData>
    {
        public class Cell
        {
            internal Dictionary<Vector3I, TItemData> m_items = new Dictionary<Vector3I, TItemData>();

            public TCellData CellData;

            public DictionaryReader<Vector3I, TItemData> Items
            {
                get { return new DictionaryReader<Vector3I, TItemData>(m_items); }
            }
        }

        int m_itemCount = 0;
        Dictionary<Vector3I, Cell> m_cells = new Dictionary<Vector3I, Cell>();
        HashSet<Vector3I> m_dirtyCells = new HashSet<Vector3I>();

        public readonly int CellSize;

        public DictionaryReader<Vector3I, Cell> Cells
        {
            get { return new DictionaryReader<Vector3I, Cell>(m_cells); }
        }

        public HashSetReader<Vector3I> DirtyCells
        {
            get { return m_dirtyCells; }
        }

        public int ItemCount
        {
            get { return m_itemCount; }
        }

        public int CellCount
        {
            get { return m_cells.Count; }
        }

        public MySparseGrid(int cellSize)
        {
            CellSize = cellSize;
        }

        public Vector3I Add(Vector3I pos, TItemData data)
        {
            var cell = pos / CellSize;
            GetCell(cell, true).m_items.Add(pos, data);
            MarkDirty(cell);
            m_itemCount++;
            return cell;
        }

        public bool Contains(Vector3I pos)
        {
            var cell = GetCell(pos / CellSize, false);
            return cell != null && cell.m_items.ContainsKey(pos);
        }

        public bool Remove(Vector3I pos, bool removeEmptyCell = true)
        {
            var cellPos = pos / CellSize;
            var cell = GetCell(cellPos, false);
            if (cell != null && cell.m_items.Remove(pos))
            {
                MarkDirty(cellPos);
                m_itemCount--;
                if (removeEmptyCell && cell.m_items.Count == 0)
                {
                    m_cells.Remove(cellPos);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Clear()
        {
            m_cells.Clear();
            m_itemCount = 0;
        }

        /// <summary>
        /// Clears cells, but keep them preallocated
        /// </summary>
        public void ClearCells()
        {
            foreach (var c in m_cells)
            {
                c.Value.m_items.Clear();
            }
            m_itemCount = 0;
        }

        public TItemData Get(Vector3I pos)
        {
            return GetCell(pos / CellSize, false).m_items[pos];
        }

        public bool TryGet(Vector3I pos, out TItemData data)
        {
            var cell = GetCell(pos / CellSize, false);
            if (cell != null)
            {
                return cell.m_items.TryGetValue(pos, out data);
            }
            else
            {
                data = default(TItemData);
                return false;
            }
        }

        public Cell GetCell(Vector3I cell)
        {
            return m_cells[cell];
        }

        public bool TryGetCell(Vector3I cell, out Cell result)
        {
            return m_cells.TryGetValue(cell, out result);
        }

        private Cell GetCell(Vector3I cell, bool createIfNotExists)
        {
            Cell result;
            if (!m_cells.TryGetValue(cell, out result) && createIfNotExists)
            {
                result = new Cell();
                m_cells[cell] = result;
            }
            return result;
        }

        public bool IsDirty(Vector3I cell)
        {
            return m_dirtyCells.Contains(cell);
        }

        public void MarkDirty(Vector3I cell)
        {
            m_dirtyCells.Add(cell);
        }

        public void UnmarkDirty(Vector3I cell)
        {
            m_dirtyCells.Remove(cell);
        }

        public void UnmarkDirtyAll()
        {
            m_dirtyCells.Clear();
        }

        public Dictionary<Vector3I, Cell>.Enumerator GetEnumerator()
        {
            return m_cells.GetEnumerator();
        }

        #region Dictionary implementation
        void IDictionary<Vector3I, TItemData>.Add(Vector3I key, TItemData value)
        {
            Add(key, value);
        }

        bool IDictionary<Vector3I, TItemData>.ContainsKey(Vector3I key)
        {
            return Contains(key);
        }

        ICollection<Vector3I> IDictionary<Vector3I, TItemData>.Keys
        {
            get { throw new InvalidOperationException("Operation not supported"); }
        }

        bool IDictionary<Vector3I, TItemData>.Remove(Vector3I key)
        {
            return Remove(key);
        }

        bool IDictionary<Vector3I, TItemData>.TryGetValue(Vector3I key, out TItemData value)
        {
            return TryGet(key, out value);
        }

        ICollection<TItemData> IDictionary<Vector3I, TItemData>.Values
        {
            get { throw new InvalidOperationException("Operation not supported"); }
        }

        TItemData IDictionary<Vector3I, TItemData>.this[Vector3I key]
        {
            get
            {
                return Get(key);
            }
            set
            {
                Remove(key);
                Add(key, value);
            }
        }

        void ICollection<KeyValuePair<Vector3I, TItemData>>.Add(KeyValuePair<Vector3I, TItemData> item)
        {
            Add(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<Vector3I, TItemData>>.Clear()
        {
            Clear();
        }

        bool ICollection<KeyValuePair<Vector3I, TItemData>>.Contains(KeyValuePair<Vector3I, TItemData> item)
        {
            throw new InvalidOperationException("Operation not supported");
        }

        void ICollection<KeyValuePair<Vector3I, TItemData>>.CopyTo(KeyValuePair<Vector3I, TItemData>[] array, int arrayIndex)
        {
            throw new InvalidOperationException("Operation not supported");
        }

        int ICollection<KeyValuePair<Vector3I, TItemData>>.Count
        {
            get { return m_itemCount; }
        }

        bool ICollection<KeyValuePair<Vector3I, TItemData>>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<KeyValuePair<Vector3I, TItemData>>.Remove(KeyValuePair<Vector3I, TItemData> item)
        {
            throw new InvalidOperationException("Operation not supported");
        }

        IEnumerator<KeyValuePair<Vector3I, TItemData>> IEnumerable<KeyValuePair<Vector3I, TItemData>>.GetEnumerator()
        {
            throw new InvalidOperationException("Operation not supported");
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new InvalidOperationException("Operation not supported");
        }
        #endregion
    }
}
