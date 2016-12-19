using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace VRage.Replication
{
    internal abstract class MyReplicablesBase
    {
        #region Fields

        static readonly HashSet<IMyReplicable> m_empty = new HashSet<IMyReplicable>();

        Stack<HashSet<IMyReplicable>> m_hashSetPool = new Stack<HashSet<IMyReplicable>>();
        MyConcurrentDictionary<IMyReplicable, HashSet<IMyReplicable>> m_parentToChildren = new MyConcurrentDictionary<IMyReplicable, HashSet<IMyReplicable>>();
        MyConcurrentDictionary<IMyReplicable, IMyReplicable> m_childToParent = new MyConcurrentDictionary<IMyReplicable, IMyReplicable>();
        protected CacheList<IMyReplicable> m_tmpList = new CacheList<IMyReplicable>();

        #endregion

        #region Public

        public void GetAllChildren(IMyReplicable replicable, List<IMyReplicable> resultList)
        {
            foreach (var child in GetChildren(replicable))
            {
                resultList.Add(child);
                GetAllChildren(child, resultList);
            }
        }

        /// <summary>
        /// Refreshes all children.
        /// </summary>
        public void RefreshChildrenHierarchy(IMyReplicable replicable)
        {
            using (m_tmpList)
            {
                GetAllChildren(replicable, m_tmpList);
                foreach (var child in m_tmpList)
                {
                    Refresh(child);
                }
            }
        } 

       

        /// <summary>
        /// Sets or resets replicable (updates child status and parent).
        /// Returns true if replicable is root, otherwise false.
        /// </summary>
        public void Add(IMyReplicable replicable, out IMyReplicable parent)
        {
            if (replicable.HasToBeChild && TryGetParent(replicable, out parent)) // Add as child
            {
                AddChild(replicable, parent);
            }
            else // Add as root
            {
                parent = null;
                AddRoot(replicable);
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

        public abstract void IterateRange(Action<IMyReplicable> p);

        public abstract void IterateRoots(Action<IMyReplicable> p);

        public virtual void GetReplicablesInBox(BoundingBoxD aabb, List<IMyReplicable> list)
        {
        }


        #endregion

        #region Implementation

        HashSet<IMyReplicable> Obtain()
        {
            return m_hashSetPool.Count > 0 ? m_hashSetPool.Pop() : new HashSet<IMyReplicable>();
        }

        HashSetReader<IMyReplicable> GetChildren(IMyReplicable replicable)
        {
            return m_parentToChildren.GetValueOrDefault(replicable, m_empty);
        }

        bool TryGetParent(IMyReplicable replicable, out IMyReplicable parent)
        {
            parent = replicable.GetParent();
            return parent != null;
        }


        /// <summary>
        /// Refreshes replicable, updates it's child status and parent.
        /// Returns true if replicable is root.
        /// </summary>
        bool Refresh(IMyReplicable replicable)
        {
            IMyReplicable parent;
            if (replicable.HasToBeChild && TryGetParent(replicable, out parent)) // Replicable is child
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
                    RemoveRoot(replicable);
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
                    AddRoot(replicable);
                    return true;
                }
                else if (ContainsRoot(replicable)) // Replicable was root
                {
                    Debug.Assert(!replicable.HasToBeChild, "Cannot have child in roots");

                    // Nothing to do, was root and is root
                    return true;
                }
                else
                {
                    return false; // Replicable was removed meanwhile
                }
            }
        }

        abstract protected void AddRoot(IMyReplicable replicable);

        abstract protected void RemoveRoot(IMyReplicable replicable);

        abstract protected bool ContainsRoot(IMyReplicable replicable);

        /// <summary>
        /// Removes replicable, children should be already removed
        /// </summary>
        virtual protected void Remove(IMyReplicable replicable)
        {
            IMyReplicable parent;
            if (m_childToParent.TryGetValue(replicable, out parent)) // Replicable is child
            {
                RemoveChild(replicable, parent);
            }
            Debug.Assert(!m_parentToChildren.ContainsKey(replicable), "Removing parent before children are removed");

            if (ContainsRoot(replicable))
                RemoveRoot(replicable);
        }

        protected void AddChild(IMyReplicable replicable, IMyReplicable parent)
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

        protected void RemoveChild(IMyReplicable replicable, IMyReplicable parent)
        {
            m_childToParent.Remove(replicable);
            var children = m_parentToChildren[parent];
            children.Remove(replicable);
            if (children.Count == 0)
            {
                m_parentToChildren.Remove(parent);
                m_hashSetPool.Push(children);
            }
        }

        #endregion
    }
}
