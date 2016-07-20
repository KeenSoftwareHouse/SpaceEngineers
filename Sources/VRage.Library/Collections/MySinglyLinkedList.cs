using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VRage.Collections
{
    public class MySinglyLinkedList<V>: IList<V>
    {
        internal class Node
        {
            public Node Next;
            public V Data;

            public Node(Node next, V data)
            {
                Next = next;
                Data = data;
            }
        }

        public struct Enumerator: IEnumerator<V>
        {
            internal Node m_previousNode;
            internal Node m_currentNode;
            internal MySinglyLinkedList<V> m_list;

            public Enumerator(MySinglyLinkedList<V> parentList)
            {
                m_list = parentList;
                m_currentNode = null;
                m_previousNode = null;
            }

            public V Current
            {
                get { return m_currentNode.Data; }
            }

            public bool HasCurrent
            {
                get { return m_currentNode != null; }
            }

            public void Dispose() {}

            object System.Collections.IEnumerator.Current
            {
                get { return m_currentNode.Data; }
            }

            public bool MoveNext()
            {
                if (m_currentNode == null)
                {
                    // Test whether we removed the last node using RemoveCurrent()
                    if (m_previousNode != null)
                        return false;

                    m_currentNode = m_list.m_rootNode;
                    m_previousNode = null;
                }
                else
                {
                    m_previousNode = m_currentNode;
                    m_currentNode = m_currentNode.Next;
                }
                return m_currentNode != null;
            }

            // After this operation, the enumerator moves to the next node.
            // Also, all enumerators pointing on this node or the next one are invalidated!
            public V RemoveCurrent()
            {
                if (m_currentNode == null)
                    throw new InvalidOperationException();

                if (m_previousNode == null)
                {
                    m_currentNode = m_currentNode.Next;
                    return m_list.PopFirst();
                }
                else
                {
                    m_previousNode.Next = m_currentNode.Next;

                    if (m_list.m_lastNode == m_currentNode)
                    {
                        m_list.m_lastNode = m_previousNode;
                    }

                    var node = m_currentNode;
                    m_currentNode = m_currentNode.Next;
                    m_list.m_count--;

                    return node.Data;
                }
            }

            // After this operation, the enumerator still points at the current node.
            // Also, enumerators poiting at the current node are invalidated!
            public void InsertBeforeCurrent(V toInsert)
            {
                var newNode = new Node(m_currentNode, toInsert);

                if (m_currentNode == null)
                {
                    if (m_previousNode == null)
                    {
                        if (m_list.m_count != 0) throw new InvalidOperationException("Inserting into a MySinglyLinkedList using an uninitialized enumerator!");

                        m_list.m_rootNode = newNode;
                        m_list.m_lastNode = newNode;
                    }
                    else
                    {
                        m_previousNode.Next = newNode;
                        m_list.m_lastNode = newNode;
                    }
                }
                else
                {
                    if (m_previousNode == null)
                    {
                        m_list.m_rootNode = newNode;
                    }
                    else
                    {
                        m_previousNode.Next = newNode;
                    }
                }

                m_previousNode = newNode;
                m_list.m_count++;
            }

            public void Reset()
            {
                m_currentNode = null;
                m_previousNode = null;
            }
        }

        private Node m_rootNode;
        private Node m_lastNode;
        private int m_count;

        public MySinglyLinkedList()
        {
            m_rootNode = null;
            m_lastNode = null;
            m_count = 0;
        }

        public int IndexOf(V item)
        {
            int i = 0;
            foreach (var containedItem in this)
            {
                if (containedItem.Equals(item))
                    return i;
                i++;
            }

            return -1;
        }

        public void Insert(int index, V item)
        {
            if (index < 0 || index > m_count)
            {
                throw new IndexOutOfRangeException();
            }

            if (index == 0)
            {
                Prepend(item);
            }
            else if (index == m_count)
            {
                Add(item);
            }
            else
            {
                Enumerator e = this.GetEnumerator();
                for (int i = 0; i < index; i++)
                {
                    e.MoveNext();
                }

                var newNode = new Node(e.m_currentNode.Next, item);
                e.m_currentNode.Next = newNode;
                m_count++;
            }
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= m_count)
            {
                throw new IndexOutOfRangeException();
            }

            if (index == 0)
            {
                m_rootNode = m_rootNode.Next;
                m_count--;

                if (m_count == 0)
                    m_lastNode = null;

                return;
            }

            Enumerator e = this.GetEnumerator();
            for (int i = 0; i < index; i++) e.MoveNext();

            e.m_currentNode.Next = e.m_currentNode.Next.Next;
            m_count--;

            if (m_count == index)
                m_lastNode = e.m_currentNode;

            return;
        }

        /// <summary>
        /// Splits the list into two.
        /// This list's end will be the node pointed by newLastPosition and the newly created list will begin with the next node.
        /// </summary>
        /// <param name="newLastPosition">Enumerator that points to the new last position in the list.</param>
        /// <param name="newCount">New number of elements in this list. If set to -1, it is calculated automatically,
        /// but that would make the split an O(N) operation. Beware: If you set this parameter, be sure to always set the
        /// correct number, otherwise, you'd cause both lists (this one and the returned one) to return a wrong number of
        /// elements in the future.</param>
        /// <returns>The newly created list</returns>
        public MySinglyLinkedList<V> Split(MySinglyLinkedList<V>.Enumerator newLastPosition, int newCount = -1)
        {
            Debug.Assert(newLastPosition.m_list == this, "Enumerator belongs to a different list");
            Debug.Assert(newLastPosition.m_currentNode != null, "Enumerator has to point at a valid list item");
            Debug.Assert(newCount > 0 && newCount <= m_count);

            if (newCount == -1)
            {
                newCount = 1;
                var node = m_rootNode;
                while (node != newLastPosition.m_currentNode)
                {
                    newCount++;
                    node = node.Next;
                }
            }

            MySinglyLinkedList<V> retList = new MySinglyLinkedList<V>();
            retList.m_rootNode = newLastPosition.m_currentNode.Next;
            retList.m_lastNode = retList.m_rootNode == null ? null : this.m_lastNode;
            retList.m_count = this.m_count - newCount;

            this.m_lastNode = newLastPosition.m_currentNode;
            this.m_lastNode.Next = null;
            this.m_count = newCount;

            return retList;
        }

        public V this[int index]
        {
            get
            {
                if (index < 0 || index >= m_count)
                {
                    throw new IndexOutOfRangeException();
                }

                Enumerator e = this.GetEnumerator();
                for (int i = -1; i < index; i++) e.MoveNext();
                return e.Current;
            }
            set
            {
                if (index < 0 || index >= m_count)
                {
                    throw new IndexOutOfRangeException();
                }

                Enumerator e = this.GetEnumerator();
                for (int i = -1; i < index; i++) e.MoveNext();
                e.m_currentNode.Data = value;
            }
        }

        public void Add(V item)
        {
            if (m_lastNode == null)
            {
                Prepend(item);
            }
            else
            {
                m_lastNode.Next = new Node(null, item);
                m_count++;
                m_lastNode = m_lastNode.Next;
            }
        }

        public void Append(V item)
        {
            Add(item);
        }

        public void Prepend(V item)
        {
            m_rootNode = new Node(m_rootNode, item);
            m_count++;

            if (m_count == 1)
                m_lastNode = m_rootNode;
        }

        public void Merge(MySinglyLinkedList<V> otherList)
        {
            if (m_lastNode == null)
            {
                m_rootNode = otherList.m_rootNode;
                m_lastNode = otherList.m_lastNode;
            }
            else
            {
                if (otherList.m_lastNode != null)
                {
                    m_lastNode.Next = otherList.m_rootNode;
                    m_lastNode = otherList.m_lastNode;
                }
            }
            m_count += otherList.m_count;

            // Erase other list to avoid node sharing
            otherList.m_count = 0;
            otherList.m_lastNode = null;
            otherList.m_rootNode = null;
        }

        public V PopFirst()
        {
            if (m_count == 0)
                throw new InvalidOperationException();

            var node = m_rootNode;
            if (node == m_lastNode)
                m_lastNode = null;
            m_rootNode = node.Next;
            m_count--;
            return node.Data;
        }

        public V First()
        {
            if (m_count == 0)
                throw new InvalidOperationException();

            return m_rootNode.Data;
        }

        public V Last()
        {
            if (m_count == 0)
                throw new InvalidOperationException();

            return m_lastNode.Data;
        }

        public void Clear()
        {
            m_rootNode = null;
            m_lastNode = null;
            m_count = 0;
        }

        public bool Contains(V item)
        {
            foreach (var containedItem in this)
            {
                if (containedItem.Equals(item))
                    return true;
            }
            return false;
        }

        public void CopyTo(V[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex] = item;
                arrayIndex++;
            }
        }

        public int Count
        {
            get { return m_count; }
        }

        public void Reverse()
        {
            if (m_count <= 1)
                return;

            Node previousNode = null;
            Node currentNode = m_rootNode;
            while (currentNode != m_lastNode)
            {
                Node nextNode = currentNode.Next;

                currentNode.Next = previousNode;

                previousNode = currentNode;
                currentNode = nextNode;
            }

            Node tmp = m_rootNode;
            m_rootNode = m_lastNode;
            m_lastNode = tmp;
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        // Testing method
        public bool VerifyConsistency()
        {
            bool consistent = true;

            if (m_lastNode == null)
            {
                Debug.Assert(m_rootNode == null);
                Debug.Assert(m_count == 0);
                consistent = consistent && m_rootNode == null && m_count == 0;
            }
            if (m_rootNode == null)
            {
                Debug.Assert(m_lastNode == null);
                Debug.Assert(m_count == 0);
                consistent = consistent && m_lastNode == null && m_count == 0;
            }
            if (m_rootNode == m_lastNode)
            {
                Debug.Assert(m_rootNode == null || m_count == 1);
                consistent = consistent && (m_rootNode == null || m_count == 1);
            }

            int i = 0;
            Node node = m_rootNode;
            while (node != null)
            {
                node = node.Next;
                i++;

                Debug.Assert(i <= m_count);
                consistent = consistent && i <= m_count;
            }

            Debug.Assert(i == m_count);
            consistent = consistent && i == m_count;

            return consistent;
        }

        public bool Remove(V item)
        {
            var current = m_rootNode;
            if (current == null) return false;

            if (m_rootNode.Data.Equals(item))
            {
                m_rootNode = m_rootNode.Next;
                m_count--;

                if (m_count == 0)
                    m_lastNode = null;

                return true;
            }

            var next = current.Next;
            while (next != null)
            {
                if (next.Data.Equals(item))
                {
                    current.Next = next.Next;
                    m_count--;

                    if (next == m_lastNode)
                        m_lastNode = current;

                    return true;
                }

                current = next;
                next = next.Next;
            }

            return false;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            Debug.Fail("Allocation");
            return new Enumerator(this);
        }

        IEnumerator<V> IEnumerable<V>.GetEnumerator()
        {
            Debug.Fail("Allocation");
            return new Enumerator(this);
        }
    }
}
