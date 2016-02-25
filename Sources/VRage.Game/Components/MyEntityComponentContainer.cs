using System;
using System.Diagnostics;
using VRage.ModAPI;

namespace VRage.Game.Components
{
    // This is needed only for ModAPI compatibility
    public interface IMyComponentContainer
    { }

    public class MyEntityComponentContainer : MyComponentContainer, IMyComponentContainer
    {
        public IMyEntity Entity { get; private set; }

        public event Action<Type, MyEntityComponentBase> ComponentAdded;
        public event Action<Type, MyEntityComponentBase> ComponentRemoved;

        public MyEntityComponentContainer(IMyEntity entity)
        {
            Entity = entity;
        }

        public override void Init(MyContainerDefinition definition)
        {
            if (definition.Flags != null)
                Entity.Flags |= definition.Flags.Value;
        }

        protected override void OnComponentAdded(Type t, MyComponentBase component)
        {
            base.OnComponentAdded(t, component);

            var entityComponent = component as MyEntityComponentBase;
            Debug.Assert(entityComponent != null, "The component added to the entity component container was not derived from MyEntityComponentBase!");

            var handler = ComponentAdded;
            if (handler != null && entityComponent != null)
                handler(t, entityComponent);
        }

        protected override void OnComponentRemoved(Type t, MyComponentBase component)
        {
            base.OnComponentRemoved(t, component);

            var entityComponent = component as MyEntityComponentBase;

            var handler = ComponentRemoved;
            if (handler != null && entityComponent != null)
                handler(t, entityComponent);
        }
	}
}
