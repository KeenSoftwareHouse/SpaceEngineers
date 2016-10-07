using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Generics;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace VRageRender
{
#if !XB1

    [PreloadRequired]
    internal static class MyObjectPoolManager
    {
#if XB1 // XB1_ALLINONEASSEMBLY
        private static bool m_registered = false;
#endif // XB1

        private static readonly Dictionary<Type, MyTuple<MyGenericObjectPool, CleanerDelegate>> m_poolsByType = new Dictionary<Type, MyTuple<MyGenericObjectPool, CleanerDelegate>>();
        internal delegate void CleanerDelegate(object objectToClean);

        internal static T Allocate<T>() where T : class
        {
            return (T)Allocate(typeof(T));
        }

        internal static object Allocate(Type typeToAllocate)
        {
            MyGenericObjectPool objectPool = null;
            CleanerDelegate cleanerDelegate = null;
            MyTuple<MyGenericObjectPool, CleanerDelegate> poolDelegatePair;
            if (m_poolsByType.TryGetValue(typeToAllocate, out poolDelegatePair))
            {
                objectPool = poolDelegatePair.Item1;
                cleanerDelegate = poolDelegatePair.Item2;
            }
            else
            {
                Debug.Fail("No type registered for " + typeToAllocate.ToString());
                return null;
            }

            object allocatedObject = null;
            if (objectPool.AllocateOrCreate(out allocatedObject))
                cleanerDelegate(allocatedObject);

            return allocatedObject;
        }

        internal static void Deallocate<T>(T objectToDeallocate) where T : class
        {
            MyGenericObjectPool objectPool = null;
            CleanerDelegate cleanerDelegate = null;
            MyTuple<MyGenericObjectPool, CleanerDelegate> poolDelegatePair;
            if (m_poolsByType.TryGetValue(objectToDeallocate.GetType(), out poolDelegatePair))
            {
                objectPool = poolDelegatePair.Item1;
                cleanerDelegate = poolDelegatePair.Item2;
            }
            else
            {
                Debug.Fail("No type registered for " + objectToDeallocate.GetType().ToString());
                return;
            }

            cleanerDelegate(objectToDeallocate);

            objectPool.Deallocate(objectToDeallocate);
        }

        internal static void Init<T>(ref T objectToInit) where T : class
        {
            MyTuple<MyGenericObjectPool, CleanerDelegate> poolDelegatePair;
            if (objectToInit == null)
                objectToInit = Allocate<T>();
            else
            {
                if (m_poolsByType.TryGetValue(typeof(T), out poolDelegatePair))
                {
                    poolDelegatePair.Item2(objectToInit);
                }
                else
                    Debug.Fail("No type registered for " + objectToInit.GetType().ToString());
            }
        }

        #region Reflection
        static MyObjectPoolManager()
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            RegisterPoolsFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            RegisterPoolsFromAssembly(Assembly.GetCallingAssembly());
#endif // !XB1
        }

        private static void RegisterPoolsFromAssembly(Assembly assembly)
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            System.Diagnostics.Debug.Assert(m_registered == false);
            if (m_registered == true)
                return;
            m_registered = true;
            foreach (var type in MyAssembly.GetTypes())
#else // !XB1
            foreach(var type in assembly.GetTypes())
#endif // !XB1
            {
                var customAttributes = type.GetCustomAttributes(typeof(PooledObjectAttribute), false);
                if(customAttributes != null && customAttributes.Length > 0)
                {
                    Debug.Assert(customAttributes.Length == 1);
                    PooledObjectAttribute attribute = (PooledObjectAttribute)customAttributes[0];
                    var methods = type.GetMethods();
                    bool delegateFound = false;
                    foreach(var method in methods)
                    {
                        var methodAttributes = method.GetCustomAttributes(typeof(PooledObjectCleanerAttribute), false);
                        if (methodAttributes != null && methodAttributes.Length > 0)
                        {
                            Debug.Assert(methodAttributes.Length == 1);
                            MyGenericObjectPool objectPool = new MyGenericObjectPool(attribute.PoolPreallocationSize, type);
                            CleanerDelegate cleanerDelegate = method.CreateDelegate<CleanerDelegate>();

                            // Make sure everything in the pool is always clean
                            foreach (var objectInPool in objectPool.Unused)
                                cleanerDelegate(objectInPool);

                            m_poolsByType.Add(type, MyTuple.Create(objectPool, cleanerDelegate));
                            delegateFound = true;
                            break;
                        }
                    }

                    if(!delegateFound)
                        Debug.Fail("Pooled type does not have a cleaner method.");
                }
            }
        }
        #endregion
    }

#else // XB1

    public interface IMyPooledObjectCleaner
    {
        void ObjectCleaner();
    }

    [PreloadRequired]
    internal static class MyObjectPoolManager
    {
#if XB1 // XB1_ALLINONEASSEMBLY
        private static bool m_registered = false;
#endif // XB1

        private static readonly Dictionary<Type, MyGenericObjectPool> m_poolsByType = new Dictionary<Type, MyGenericObjectPool>();

        internal static T Allocate<T>() where T : class
        {
            return (T)Allocate(typeof(T));
        }

        internal static object Allocate(Type typeToAllocate)
        {
            MyGenericObjectPool objectPool;
            if (!m_poolsByType.TryGetValue(typeToAllocate, out objectPool))
            {
                Debug.Fail("No type registered for " + typeToAllocate.ToString());
                return null;
            }

            object allocatedObject = null;
            if (objectPool.AllocateOrCreate(out allocatedObject))
                ((IMyPooledObjectCleaner)allocatedObject).ObjectCleaner();

            return allocatedObject;
        }

        internal static void Deallocate<T>(T objectToDeallocate) where T : class
        {
            MyGenericObjectPool objectPool;
            if (!m_poolsByType.TryGetValue(objectToDeallocate.GetType(), out objectPool))
            {
                Debug.Fail("No type registered for " + objectToDeallocate.GetType().ToString());
                return;
            }

            ((IMyPooledObjectCleaner)objectToDeallocate).ObjectCleaner();

            objectPool.Deallocate(objectToDeallocate);
        }

        internal static void Init<T>(ref T objectToInit) where T : class
        {
            MyGenericObjectPool objectPool;
            if (objectToInit == null)
                objectToInit = Allocate<T>();
            else
            {
                if (m_poolsByType.TryGetValue(typeof(T), out objectPool))
                {
                    ((IMyPooledObjectCleaner)objectToInit).ObjectCleaner();
                }
                else
                    Debug.Fail("No type registered for " + objectToInit.GetType().ToString());
            }
        }

        #region Reflection
        static MyObjectPoolManager()
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            RegisterPoolsFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            RegisterPoolsFromAssembly(Assembly.GetCallingAssembly());
#endif // !XB1
        }

        private static void RegisterPoolsFromAssembly(Assembly assembly)
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            System.Diagnostics.Debug.Assert(m_registered == false);
            if (m_registered == true)
                return;
            m_registered = true;
            foreach (var type in MyAssembly.GetTypes())
#else // !XB1
            foreach(var type in assembly.GetTypes())
#endif // !XB1
            {
                var customAttributes = type.GetCustomAttributes(typeof(PooledObjectAttribute), false);
                if (customAttributes != null && customAttributes.Length > 0)
                {
                    Debug.Assert(customAttributes.Length == 1);
                    PooledObjectAttribute attribute = (PooledObjectAttribute)customAttributes[0];
                    var methods = type.GetMethods();
                    bool delegateFound = false;
                    {
                        {
                            MyGenericObjectPool objectPool = new MyGenericObjectPool(attribute.PoolPreallocationSize, type);

                            // Make sure everything in the pool is always clean
                            foreach (var objectInPool in objectPool.Unused)
                                ((IMyPooledObjectCleaner)objectInPool).ObjectCleaner();

                            m_poolsByType.Add(type, objectPool);
                            delegateFound = true;
                        }
                    }

                    if (!delegateFound)
                        Debug.Fail("Pooled type does not have a cleaner method.");
                }
            }
        }
        #endregion
    }

#endif // XB1

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    /// <summary>
    /// Using this attribute on a class requires a public static method with
    /// the PooledObjectCleaner attribute in the class. There should be a public
    /// parameterless constructor defined too.
    /// </summary>
    public class PooledObjectAttribute : System.Attribute
    {
        internal int PoolPreallocationSize;
        public PooledObjectAttribute(int poolPreallocationSize = 2)
        {
            PoolPreallocationSize = poolPreallocationSize;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    /// <summary>
    /// Anything in the pool has the method with this attribute called to
    /// make sure everything stored is in a proper, cleaned state.
    /// </summary>
    public class PooledObjectCleanerAttribute : System.Attribute
    {

    }

    /// <summary>
    /// A copy of MyObjectsPool that handles types a little different for the MyObjectPoolManager
    /// </summary>
    class MyGenericObjectPool
    {
        private readonly Type m_storedType;

        Queue<object> m_unused;
        HashSet<object> m_active;
        HashSet<object> m_marked;

        //  Count of items allowed to store in this pool.
        int m_baseCapacity;

        public QueueReader<object> Unused
        {
            get { return new QueueReader<object>(m_unused); }
        }

        public HashSetReader<object> Active
        {
            get { return new HashSetReader<object>(m_active); }
        }

        public int ActiveCount
        {
            get { return m_active.Count; }
        }

        public int BaseCapacity
        {
            get { return m_baseCapacity; }
        }

        public int Capacity
        {
            get { return m_unused.Count + m_active.Count; }
        }

        private MyGenericObjectPool(Type storedType)
        {
        }

        public MyGenericObjectPool(int baseCapacity, Type storedTypeOverride)
        {
            Debug.Assert(!storedTypeOverride.IsValueType, "MyGenericObjectPool should only be used with reference types");
            m_storedType = storedTypeOverride;
            Construct(baseCapacity);
        }

        private void Construct(int baseCapacity)
        {
            //  Pool should contain at least one preallocated item!
            Debug.Assert(baseCapacity > 0);

            m_baseCapacity = baseCapacity;
            m_unused = new Queue<object>(m_baseCapacity);
            m_active = new HashSet<object>();
            m_marked = new HashSet<object>();

            for (int i = 0; i < m_baseCapacity; i++)
            {
                m_unused.Enqueue(Activator.CreateInstance(m_storedType));
            }
        }

        /// <summary>
        /// Returns true when new item was allocated
        /// </summary>
        public bool AllocateOrCreate(out object item)
        {
            bool create = (m_unused.Count == 0);
            if (create)
                item = Activator.CreateInstance(m_storedType);
            else
                item = m_unused.Dequeue();
            m_active.Add(item);
            return create;
        }

        public void Deallocate(object item)
        {
            Debug.Assert(m_active.Contains(item), "Deallocating item which is not in active set of the pool.");
            m_active.Remove(item);
            m_unused.Enqueue(item);
        }

        public void MarkForDeallocate(object item)
        {
            Debug.Assert(m_active.Contains(item), "Marking item which is not in active set of the pool.");
            m_marked.Add(item);
        }

        public void DeallocateAllMarked()
        {
            foreach (var marked in m_marked)
            {
                Deallocate(marked);
            }
            m_marked.Clear();
        }

        public void DeallocateAll()
        {
            foreach (var active in m_active)
            {
                m_unused.Enqueue(active);
            }
            m_active.Clear();
            m_marked.Clear();
        }

        public void TrimToBaseCapacity()
        {
            while (Capacity > BaseCapacity && m_unused.Count > 0)
            {
                m_unused.Dequeue();
            }
            m_unused.TrimExcess();
            m_active.TrimExcess();
            m_marked.TrimExcess();
            Debug.Assert(Capacity == BaseCapacity, "Could not trim to base capacity (possibly due to active objects).");
        }
    }
}
