using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using VRage.Game.Entity;

namespace Sandbox.Game.Entities
{
    public class MyCompositeGameLogicComponent : MyGameLogicComponent
    {
        private ICollection<MyGameLogicComponent> m_logicComponents;

        private MyCompositeGameLogicComponent(ICollection<MyGameLogicComponent> logicComponents)
        {
            m_logicComponents = logicComponents;
        }

        public static MyGameLogicComponent Create(ICollection<MyGameLogicComponent> logicComponents, MyEntity entity)
        {
            foreach (MyGameLogicComponent item in logicComponents)
                item.SetContainer(entity.Components);

            switch (logicComponents.Count)
            {
                case 0: 
                    return null;
                case 1: 
                    return logicComponents.First();
                default:
                    return new MyCompositeGameLogicComponent(logicComponents);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            foreach (var component in m_logicComponents)
            {
                component.UpdateOnceBeforeFrame();
            }
        }

        public override void UpdateBeforeSimulation()
        {
            foreach (var component in m_logicComponents)
            {
                component.UpdateBeforeSimulation();
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            foreach (var component in m_logicComponents)
            {
                component.UpdateBeforeSimulation10();
            }
        }

        public override void UpdateBeforeSimulation100()
        {
            foreach (var component in m_logicComponents)
            {
                component.UpdateBeforeSimulation100();
            }
        }

        public override void UpdateAfterSimulation()
        {
            foreach (var component in m_logicComponents)
            {
                component.UpdateAfterSimulation();
            }
        }

        public override void UpdateAfterSimulation10()
        {
            foreach (var component in m_logicComponents)
            {
                component.UpdateAfterSimulation10();
            }
        }

        public override void UpdateAfterSimulation100()
        {
            foreach (var component in m_logicComponents)
            {
                component.UpdateAfterSimulation100();
            }
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            foreach (var component in m_logicComponents)
            {
                component.Init(objectBuilder);
            }
        }

        public override void MarkForClose()
        {
            foreach (var component in m_logicComponents)
            {
                component.MarkForClose();
            }
        }

        public override void Close()
        {
            foreach (var component in m_logicComponents)
            {
                component.Close();
            }
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            foreach (var component in m_logicComponents)
            {
                // TODO Can we do better?
                MyObjectBuilder_EntityBase builder = component.GetObjectBuilder(copy);
                if (builder != null)
                    return builder;
            }
            return null;
        }

        public override T GetAs<T>()
        {
            foreach (var component in m_logicComponents)
            {
                if (component is T)
                {
                    return component as T;
                }
            }
            return null;
        } 
    }
}
