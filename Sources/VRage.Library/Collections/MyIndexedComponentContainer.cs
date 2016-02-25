using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace VRage.Library.Collections
{
    using __helper_namespace;

    namespace __helper_namespace
    {
        internal class TypeComparer : IComparer<Type>
        {
            public int Compare(Type x, Type y)
            {
                return string.CompareOrdinal(x.AssemblyQualifiedName, y.AssemblyQualifiedName);
            }

            public static readonly TypeComparer Instance = new TypeComparer();
        }

        internal class TypeIndexTupleComparer : IComparer<MyTuple<Type, int>>
        {
            public int Compare(MyTuple<Type, int> x, MyTuple<Type, int> y)
            {
                return string.CompareOrdinal(x.Item1.AssemblyQualifiedName, y.Item1.AssemblyQualifiedName);
            }

            public static readonly TypeComparer Instance = new TypeComparer();
        }

        internal class TypeListComparer : IEqualityComparer<List<Type>>
        {
            public bool Equals(List<Type> x, List<Type> y)
            {
                if (x.Count == y.Count)
                {
                    for (int i = 0; i < x.Count; i++)
                    {
                        if (x[i] != y[i]) return false;
                    }
                    return true;
                }
                return false;
            }

            public int GetHashCode(List<Type> obj)
            {
                return obj.GetHashCode();
            }
        }
    }
    
    public class MyComponentContainerTemplate<T> where T : class
    {
        internal MyIndexedComponentContainer<Func<Type, T>> Components = new MyIndexedComponentContainer<Func<Type, T>>();

        public MyComponentContainerTemplate(List<Type> types, List<Func<Type, T>> compoentFactories)
        {
            for (int i = 0; i < types.Count; i++)
            {
                Components.Add(types[i], compoentFactories[i]);
            }
        }
    }

    public class ComponentIndex
    {
        public readonly List<Type> Types;
        public readonly Dictionary<Type, int> Index = new Dictionary<Type, int>();

        public ComponentIndex(List<Type> typeList)
        {
            for (int i = 0; i < typeList.Count; ++i)
                Index[typeList[i]] = i;

            Types = typeList;
        }
    }

    public class IndexHost
    {
        private readonly ComponentIndex NullIndex = new ComponentIndex(new List<Type>());

        private readonly Dictionary<List<Type>, WeakReference> m_indexes;

        public IndexHost()
        {
            m_indexes = new Dictionary<List<Type>, WeakReference>(new TypeListComparer());

            // This will never be removed because we keep a strong reference
            m_indexes[NullIndex.Types] = new WeakReference(NullIndex);
        }

        private ComponentIndex GetForTypes(List<Type> types)
        {
            // Assume the list is sorted at this stage.
            Debug.Assert(types.IsSorted(TypeComparer.Instance));

            ComponentIndex index;
            WeakReference reference;
            if (!m_indexes.TryGetValue(types, out reference) || !reference.IsAlive)
            {
                if (reference == null) reference = new WeakReference(null);

                index = new ComponentIndex(types);

                m_indexes[types] = reference;
            }
            else
            {
                index = (ComponentIndex)reference.Target;
            }

            return index;
        }

        public ComponentIndex GetAfterInsert(ComponentIndex current, Type newType, out int insertionPoint)
        {
            List<Type> newList = current.Types.ToList();

            insertionPoint = ~newList.BinarySearch(newType, TypeComparer.Instance);

            newList.Insert(insertionPoint, newType);

            return GetForTypes(newList);
        }

        public ComponentIndex GetAfterRemove(ComponentIndex current, Type oldType, out int removalPoint)
        {
            List<Type> newList = current.Types.ToList();

            removalPoint = current.Index[oldType];

            newList.RemoveAt(removalPoint);

            return GetForTypes(newList);
        }

        public ComponentIndex GetEmptyComponentIndex()
        {
            return NullIndex;
        }
    }

    public class MyIndexedComponentContainer<T> where T : class
    {
        #region Indexing

        #endregion

        private static readonly IndexHost Host = new IndexHost();

        private ComponentIndex m_componentIndex;
        private readonly List<T> m_components = new List<T>();

        public MyIndexedComponentContainer()
        {
            m_componentIndex = Host.GetEmptyComponentIndex();
        }

        /**
         * Create a component from a template.
         */
        public MyIndexedComponentContainer(MyComponentContainerTemplate<T> template)
        {
            m_components.Capacity = template.Components.Count;

            for (int i = 0; i < template.Components.Count; ++i)
            {
                var factory = template.Components[i];
                var type = template.Components.m_componentIndex.Types[i];
                m_components.Add(factory.Invoke(type));
            }

            m_componentIndex = template.Components.m_componentIndex;
        }

        public T this[int index]
        {
            get
            {
                return m_components[index];
            }
        }

        public T this[Type type]
        {
            get
            {
                return m_components[m_componentIndex.Index[type]];
            }
        }

        public int Count
        {
            get { return m_components.Count; }
        }

        public TComponent GetComponent<TComponent>() where TComponent : T
        {
            return (TComponent)m_components[m_componentIndex.Index[typeof(TComponent)]];
        }

        public TComponent TryGetComponent<TComponent>() where TComponent : class, T
        {
            int index;
            if (m_componentIndex.Index.TryGetValue(typeof(TComponent), out index))
                return (TComponent)m_components[index];
            return null;
        }

        public void Add(Type slot, T component)
        {
            if (m_componentIndex.Index.ContainsKey(slot))
            {
                Debug.Fail("Component type already in container.");
                return;
            }

            int insert;
            m_componentIndex = Host.GetAfterInsert(m_componentIndex, slot, out insert);

            m_components.Insert(insert, component);
        }

        public void Remove(Type slot)
        {
            if (!m_componentIndex.Index.ContainsKey(slot))
            {
                Debug.Fail("Component type not in container.");
                return;
            }

            int remove;
            m_componentIndex = Host.GetAfterRemove(m_componentIndex, slot, out remove);
            m_components.RemoveAt(remove);
        }

        public void Clear()
        {
            m_components.Clear();
            m_componentIndex = Host.GetEmptyComponentIndex();
        }

        public bool Contains<TComponent>() where TComponent : T
        {
            return m_componentIndex.Index.ContainsKey(typeof(TComponent));
        }
    }
}