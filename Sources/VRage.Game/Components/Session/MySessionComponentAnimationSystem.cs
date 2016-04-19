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

        private List<MyAnimationControllerComponent> m_skinnedEntityComponentsToAdd = new List<MyAnimationControllerComponent>(32);
        private List<MyAnimationControllerComponent> m_skinnedEntityComponentsToRemove = new List<MyAnimationControllerComponent>(32);

        private FastResourceLock m_lock = new FastResourceLock();

        public override void LoadData()
        {
            m_skinnedEntityComponents.Clear();
            m_skinnedEntityComponentsToAdd.Clear();
            m_skinnedEntityComponentsToRemove.Clear();
            MySessionComponentAnimationSystem.Static = this;
        }

        protected override void UnloadData()
        {
            m_skinnedEntityComponents.Clear();
            m_skinnedEntityComponentsToAdd.Clear();
            m_skinnedEntityComponentsToRemove.Clear();
        }

        public override void UpdateBeforeSimulation()
        {
            ProfilerShort.Begin("New Animation System");

            using (m_lock.AcquireExclusiveUsing())
            {
                foreach (var toRemove in m_skinnedEntityComponentsToRemove)
                {
                    if (m_skinnedEntityComponents.Contains(toRemove))
                        m_skinnedEntityComponents.Remove(toRemove);
                }
                m_skinnedEntityComponentsToRemove.Clear();

                foreach (var toAdd in m_skinnedEntityComponentsToAdd)
                {
                    m_skinnedEntityComponents.Add(toAdd);
                }
                m_skinnedEntityComponentsToAdd.Clear();
            }

            foreach (MyAnimationControllerComponent skinnedEntityComp in m_skinnedEntityComponents)
                skinnedEntityComp.Update();
            ProfilerShort.End();
        }

        /// <summary>
        /// Register entity component.
        /// </summary>
        internal void RegisterEntityComponent(MyAnimationControllerComponent entityComponent)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                m_skinnedEntityComponentsToAdd.Add(entityComponent);
            }
        }

        /// <summary>
        /// Unregister entity component.
        /// </summary>
        internal void UnregisterEntityComponent(MyAnimationControllerComponent entityComponent)
        {
            using (m_lock.AcquireExclusiveUsing())
            {
                m_skinnedEntityComponentsToRemove.Add(entityComponent);
            }
        }
    }
}
