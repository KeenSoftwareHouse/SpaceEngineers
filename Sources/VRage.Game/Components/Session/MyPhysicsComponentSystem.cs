using System.Collections.Generic;
using VRage.Game.Components;

namespace VRage.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class MyPhysicsComponentSystem  : MySessionComponentBase
    {
        public static MyPhysicsComponentSystem Static;

        private List<MyPhysicsComponentBase> m_physicsComponents = new List<MyPhysicsComponentBase>();

        public override void LoadData()
        {
            base.LoadData();
            Static = this;
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            Static = null;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            foreach (var component in m_physicsComponents)
            {
                if (component.Definition != null)
                {
                    if (component.Definition.UpdateFlags != 0)
                        component.UpdateFromSystem();
                }
            }
        }

        public void Register(MyPhysicsComponentBase component)
        {
            m_physicsComponents.Add(component);
        }

        public void Unregister(MyPhysicsComponentBase component)
        {
            m_physicsComponents.Remove(component);
        }

    }
}
