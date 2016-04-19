using System.Collections.Generic;
using System.Diagnostics;
using VRage.Collections;

namespace VRage.Game.Components
{
    /// <summary>
    /// When creating a new aggregate component type, derive from this interface so that you can use extension methods
    /// AddComponent and RemoveComponent
    /// </summary>
    public interface IMyComponentAggregate
    {
        void AfterComponentAdd(MyComponentBase component);
        void BeforeComponentRemove(MyComponentBase component);

        // Note: This should never be null
        MyAggregateComponentList ChildList { get; }
        MyComponentContainer ContainerBase { get; }
    }

    public static class MyComponentAggregateExtensions
    {
        public static void AddComponent(this IMyComponentAggregate aggregate, MyComponentBase component)
        {
            Debug.Assert(aggregate != component, "Can not add to itself!");
            if (component.ContainerBase != null)
            {
                component.OnBeforeRemovedFromContainer();
            }
            aggregate.ChildList.AddComponent(component);
            component.SetContainer(aggregate.ContainerBase);
            aggregate.AfterComponentAdd(component);
        }

        /// <summary>
        /// Adds to list but doesn't change ownership
        /// </summary>
        public static void AttachComponent(this IMyComponentAggregate aggregate, MyComponentBase component)
        {
            Debug.Assert(aggregate != component, "Can not add to itself!");
            aggregate.ChildList.AddComponent(component);         
        }

        public static bool RemoveComponent(this IMyComponentAggregate aggregate, MyComponentBase component)
        {
            int index = aggregate.ChildList.GetComponentIndex(component);
            if (index != -1)
            {
                aggregate.BeforeComponentRemove(component);
                component.SetContainer(null);
                aggregate.ChildList.RemoveComponentAt(index);
                return true;
            }

            foreach (var child in aggregate.ChildList.Reader)
            {
                var childAggregate = child as IMyComponentAggregate;
                if (childAggregate == null) continue;

                bool removed = childAggregate.RemoveComponent(component);
                if (removed) return true;
            }

            return false;
        }

        /// <summary>
        /// Removes from list, but doesn't change ownership
        /// </summary>
        public static void DetachComponent(this IMyComponentAggregate aggregate, MyComponentBase component)
        {
            int index = aggregate.ChildList.GetComponentIndex(component);
            if (index != -1)
            {
                aggregate.ChildList.RemoveComponentAt(index);
            }
        }

        public static void GetComponentsFlattened(this IMyComponentAggregate aggregate, List<MyComponentBase> output)
        {
            foreach (var child in aggregate.ChildList.Reader)
            {
                var childAggregate = child as IMyComponentAggregate;
                if (childAggregate != null)
                {
                    childAggregate.GetComponentsFlattened(output);
                }
                else
                {
                    output.Add(child);
                }
            }
        }
    }

    public sealed class MyAggregateComponentList
    {
        private List<MyComponentBase> m_components = new List<MyComponentBase>();
        public ListReader<MyComponentBase> Reader { get { return new ListReader<MyComponentBase>(m_components); } }

        public void AddComponent(MyComponentBase component)
        {
            m_components.Add(component);
        }

        public void RemoveComponentAt(int index)
        {
            m_components.RemoveAtFast(index);
        }

        public int GetComponentIndex(MyComponentBase component)
        {
            return m_components.IndexOf(component);
        }

        public bool RemoveComponent(MyComponentBase component)
        {
            if (Contains(component))
            {
                component.OnBeforeRemovedFromContainer();                
            }
            else 
            {
                return false;
            }

            if (m_components.Remove(component))
            {
                return true;
            }
            foreach (var childComponent in m_components)
            {
                if (childComponent is IMyComponentAggregate)
                {
                    var childAggregate = (childComponent as IMyComponentAggregate);                    
                    if (childAggregate.ChildList.RemoveComponent(component))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        public bool Contains(MyComponentBase component)
        {
            if (m_components.Contains(component))
            {               
                    return true;               
            }
            foreach (var childComponent in m_components)
            {
                if (childComponent is IMyComponentAggregate)
                {
                    var childAggregate = (childComponent as IMyComponentAggregate);
                    if (childAggregate.ChildList.Contains(component))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
