using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.Collections;
using VRage.Library;

namespace ParallelTasks
{
    /// <summary>
    /// A thread safe, non-blocking, object pool.
    /// </summary>
    /// <typeparam name="T">The type of item to store. Must be a class with a parameterless constructor.</typeparam>
    public class Pool<T>
        : Singleton<Pool<T>>
        where T : class, new()
    {
        private readonly MyConcurrentDictionary<Thread, MyConcurrentQueue<T>> m_instances;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pool&lt;T&gt;"/> class.
        /// </summary>
        public Pool()
        {
#if WINDOWS_PHONE
            m_instances = new MyConcurrentDictionary<Thread, MyConcurrentQueue<T>>(1);
#else
            m_instances = new MyConcurrentDictionary<Thread, MyConcurrentQueue<T>>(MyEnvironment.ProcessorCount);
#endif
        }

        /// <summary>
        /// Gets an instance from the pool.
        /// </summary>
        /// <returns>An instance of <typeparamref name="T"/>.</returns>
        public T Get(Thread thread)
        {
            MyConcurrentQueue<T> queue;

            if (!m_instances.TryGetValue(thread, out queue))
            {
                queue = new MyConcurrentQueue<T>();
                m_instances.Add(thread, queue);
            }

            T instance;
            if (!queue.TryDequeue(out instance))
            {
                instance = new T();
            }

            return instance;
        }

        /// <summary>
        /// Returns an instance to the pool, so it is available for re-use.
        /// It is advised that the item is reset to a default state before being returned.
        /// </summary>
        /// <param name="instance">The instance to return to the pool.</param>
        public void Return(Thread thread, T instance)
        {
            MyConcurrentQueue<T> queue = m_instances[thread];

            queue.Enqueue(instance);
        }


        public void Clean()
        {
            foreach (var instance in m_instances)
            {
                instance.Value.Clear();
            }
        }
    }
}
