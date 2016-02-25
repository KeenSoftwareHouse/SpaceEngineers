using System;
using System.Diagnostics;
using VRage.ModAPI;

namespace VRage.Game.Components
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

    // This is needed only for ModAPI compatibility
    public interface IMyComponentBase
    {
        void SetContainer(IMyComponentContainer container);

        void OnAddedToContainer();
        void OnRemovedFromContainer();

        void OnAddedToScene();
        void OnRemovedFromScene();
    }
   
    public abstract class MyEntityComponentBase : MyComponentBase
    {
        public MyEntityComponentContainer Container
        {
            get
            {
                return ContainerBase as MyEntityComponentContainer;
            }
        }

        public IMyEntity Entity
        {
            get
            {
                var container = ContainerBase as MyEntityComponentContainer;
                Debug.Assert(ContainerBase == null || container != null, "MyEntityComponentBase was inserted into a container that was not of type MyEntityComponentContainer!");
                return container == null ? null : container.Entity;
            }
        }

        /// <summary>
        /// Name of the base component type for debug purposes (e.g.: "Position")
        /// </summary>
        public abstract string ComponentTypeDebugString { get; }

        #region Events for replicable layer

        public static event Action<MyEntityComponentBase> OnAfterAddedToContainer;

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            var handler = OnAfterAddedToContainer;
            if (handler != null)
                handler(this);
        }

        public event Action<MyEntityComponentBase> BeforeRemovedFromContainer;

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();

            var handler = BeforeRemovedFromContainer;
            if (handler != null)
            {
                handler(this);
            }
        }


        #endregion
    }

}
