using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Network;

namespace VRage.Replication
{
    public class MyReplicables
    {
        static readonly HashSet<IMyReplicable> m_empty = new HashSet<IMyReplicable>();

        Stack<HashSet<IMyReplicable>> m_pool = new Stack<HashSet<IMyReplicable>>();
        MyConcurrentDictionary<IMyReplicable, HashSet<IMyReplicable>> m_parentToChildren = new MyConcurrentDictionary<IMyReplicable, HashSet<IMyReplicable>>();
        MyConcurrentDictionary<IMyReplicable, IMyReplicable> m_childToParent = new MyConcurrentDictionary<IMyReplicable, IMyReplicable>();
        List<IMyReplicable> m_tmpList = new List<IMyReplicable>();

        HashSet<IMyReplicable> m_roots = new HashSet<IMyReplicable>();
        List<IMyReplicable> m_updateQueue = new List<IMyReplicable>();

        public HashSetReader<IMyReplicable> Roots
        {
            get { return m_roots; }
        }

        HashSet<IMyReplicable> Obtain()
        {
            return m_pool.Count > 0 ? m_pool.Pop() : new HashSet<IMyReplicable>();
        }

        public HashSetReader<IMyReplicable> GetChildren(IMyReplicable replicable)
        {
            return m_parentToChildren.GetValueOrDefault(replicable, m_empty);
        }

        public void GetChildren(IMyReplicable replicable, List<IMyReplicable> resultChildren)
        {
            foreach (var child in GetChildren(replicable))
            {
                resultChildren.Add(child);
            }
        }

        public IMyReplicable GetNextForUpdate()
        {
            while (m_updateQueue.Count > 0)
            {
                IMyReplicable replicable = m_updateQueue[0];
                m_updateQueue.RemoveAt(0);

                if (Refresh(replicable))
                {
                    m_updateQueue.Add(replicable);
                    return replicable;
                }
            }
            return null;
        }

        /// <summary>
        /// Sets or resets replicable (updates child status and parent).
        /// Returns true if replicable is root, otherwise false.
        /// </summary>
        public void Add(IMyReplicable replicable, out IMyReplicable parent)
        {
            if (replicable.IsChild && TryGetDependency(replicable, out parent)) // Add as child
            {
                AddChild(replicable, parent);
            }
            else // Add as root
            {
                Debug.Assert(!replicable.IsChild);
                parent = null;
                m_roots.Add(replicable);
                m_updateQueue.Add(replicable);
            }
        }

        void GetAllChildren(IMyReplicable replicable, List<IMyReplicable> resultList)
        {
            foreach (var child in GetChildren(replicable))
            {
                resultList.Add(child);
                GetAllChildren(child, resultList);
            }
        }

        bool TryGetDependency(IMyReplicable replicable, out IMyReplicable parent)
        {
            parent = replicable.GetDependency();
            return parent != null;
        }

        /// <summary>
        /// Refreshes all children.
        /// </summary>
        public void RefreshChildrenHierarchy(IMyReplicable replicable)
        {
            try
            {
                GetAllChildren(replicable, m_tmpList);
                foreach (var child in m_tmpList)
                {
                    Refresh(child);
                }
            }
            finally
            {
                m_tmpList.Clear();
            }
        }

        /// <summary>
        /// Refreshes replicable, updates it's child status and parent.
        /// Returns true if replicable is root.
        /// </summary>
        public bool Refresh(IMyReplicable replicable)
        {
            IMyReplicable parent;
            if (replicable.IsChild && TryGetDependency(replicable, out parent)) // Replicable is child
            {
                IMyReplicable oldParent;
                if (m_childToParent.TryGetValue(replicable, out oldParent)) // Replicable was child
                {
                    if (oldParent != parent) // Replicable was child with different parent
                    {
                        RemoveChild(replicable, oldParent);
                        AddChild(replicable, parent);
                    }
                }
                else // Replicable was root
                {
                    m_roots.Remove(replicable);
                    AddChild(replicable, parent);
                }
                return false;
            }
            else
            {
                IMyReplicable oldParent;
                if (m_childToParent.TryGetValue(replicable, out oldParent)) // Replicable was child
                {
                    RemoveChild(replicable, oldParent);
                    m_roots.Add(replicable);
                    return true;
                }
                else if (m_roots.Contains(replicable)) // Replicable was root
                {
                    // Nothing to do, was root and is root
                    return true;
                }
                else
                {
                    return false; // Replicable was removed meanwhile
                }
            }
        }

        /// <summary>
        /// Removes replicable, children should be already removed
        /// </summary>
        public void Remove(IMyReplicable replicable)
        {
            IMyReplicable parent;
            if (m_childToParent.TryGetValue(replicable, out parent)) // Replicable is child
            {
                RemoveChild(replicable, parent);
            }
            Debug.Assert(!m_parentToChildren.ContainsKey(replicable), "Removing parent before children are removed");
            m_roots.Remove(replicable);
            m_updateQueue.Remove(replicable);         
        }

        void AddChild(IMyReplicable replicable, IMyReplicable parent)
        {
            HashSet<IMyReplicable> children;
            if (!m_parentToChildren.TryGetValue(parent, out children))
            {
                children = Obtain();
                m_parentToChildren[parent] = children;
            }
            children.Add(replicable);
            m_childToParent[replicable] = parent;
        }

        void RemoveChild(IMyReplicable replicable, IMyReplicable parent)
        {
            m_childToParent.Remove(replicable);
            var children = m_parentToChildren[parent];
            children.Remove(replicable);
            if (children.Count == 0)
            {
                m_parentToChildren.Remove(parent);
                m_pool.Push(children);
            }
        }

        /// <summary>
        /// Removes replicable with all children, children of children, etc.
        /// </summary>
        public void RemoveHierarchy(IMyReplicable replicable)
        {
            var children = m_parentToChildren.GetValueOrDefault(replicable, m_empty);
            while (children.Count > 0)
            {
                var e = children.GetEnumerator();
                e.MoveNext();
                RemoveHierarchy(e.Current);
            }
            Remove(replicable);
        }

        /// <summary>
        /// Removes all children of replicable.
        /// Children of children should be already removed!
        /// </summary>
        public void RemoveChildren(IMyReplicable replicable)
        {
            var children = m_parentToChildren.GetValueOrDefault(replicable, m_empty);
            while (children.Count > 0)
            {
                var e = children.GetEnumerator();
                e.MoveNext();
                Remove(e.Current);
            }
        }
    }
}
