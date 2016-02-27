using Sandbox.Game.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;

namespace Sandbox.Game.EntityComponents.Systems
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyTimerComponentSystem : MySessionComponentBase
    {
        private const int UPDATE_FRAME = 100;

        public static MyTimerComponentSystem Static;

        private List<MyTimerComponent> m_timerComponents = new List<MyTimerComponent>();
        private int m_frameCounter;

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

            if (++m_frameCounter % UPDATE_FRAME == 0)
            {
                m_frameCounter = 0;
                UpdateTimerComponents();
            }
        }

        private void UpdateTimerComponents()
        {
            foreach (var timerComponent in m_timerComponents)
                timerComponent.Update();
        }

        public void Register(MyTimerComponent timerComponent)
        {
            m_timerComponents.Add(timerComponent);
        }

        public void Unregister(MyTimerComponent timerComponent)
        {
            m_timerComponents.Remove(timerComponent);
        }

    }
}
