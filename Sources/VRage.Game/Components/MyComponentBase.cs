using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;

namespace VRage.Components
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MyEntityComponentDescriptor : System.Attribute
    {
        public Type EntityBuilderType;
        public string[] EntityBuilderSubTypeNames;

        public MyEntityComponentDescriptor(Type entityBuilderType, params string[] entityBuilderSubTypeNames)
        {
            EntityBuilderType = entityBuilderType;
            EntityBuilderSubTypeNames = entityBuilderSubTypeNames;
        }
    }

    public abstract class MyComponentBase
    {
        //this is needed as compatibility 
        [Obsolete("This property will be removed in future. Please use Container.Entity instead.")]
        public IMyEntity Entity { get { return Container != null ? Container.Entity : null; } }  // to be obsolete once components are finished
        public MyComponentContainer Container { get; set; }

        public virtual void OnAddedToContainer(MyComponentContainer container)
        {
            Container = container;
        }

        public virtual void OnRemovedFromContainer(MyComponentContainer container)
        {
            Container = null;
        }

        public virtual T GetAs<T>() where T : MyComponentBase
        {
            return this as T;
        }
    }
}
