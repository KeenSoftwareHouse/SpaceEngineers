using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game.Components;

namespace VRage.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    internal class MySessionComponentAnimationSystem : MySessionComponentBase
    {
        // Static reference to this session component.
        public static MySessionComponentAnimationSystem Static = null;
        // All registered skinned entity components.
        private HashSet<MyAnimationControllerComponent> m_skinnedEntityComponents = new HashSet<MyAnimationControllerComponent>();

        public override void LoadData()
        {
            MySessionComponentAnimationSystem.Static = this;
        }

        public override void UpdateBeforeSimulation()
        {
            ProfilerShort.Begin("New Animation System");
            foreach (MyAnimationControllerComponent skinnedEntityComp in m_skinnedEntityComponents)
                skinnedEntityComp.Update();
            ProfilerShort.End();
        }

        /// <summary>
        /// Register entity component.
        /// </summary>
        internal void RegisterEntityComponent(MyAnimationControllerComponent entityComponent)
        {
            Debug.Assert(m_skinnedEntityComponents.Contains(entityComponent) == false, "Entity component was already registered.");
            m_skinnedEntityComponents.Add(entityComponent);
        }

        /// <summary>
        /// Unregister entity component.
        /// </summary>
        internal void UnregisterEntityComponent(MyAnimationControllerComponent entityComponent)
        {
            if (m_skinnedEntityComponents.Contains(entityComponent))
            {
                m_skinnedEntityComponents.Remove(entityComponent);
            }
            else
            {
                Debug.Assert(false, "Entity component was not found, cannot be unregistered.");
            }
        }
    }
}
