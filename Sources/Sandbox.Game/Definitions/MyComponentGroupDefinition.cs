using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ComponentGroupDefinition))]
    public class MyComponentGroupDefinition : MyDefinitionBase
    {
        private MyObjectBuilder_ComponentGroupDefinition m_postprocessBuilder;

        public bool IsValid
        {
            get
            {
                return m_components.Count != 0;
            }
        }

        private List<MyComponentDefinition> m_components;

        public MyComponentGroupDefinition()
        {
            m_components = new List<MyComponentDefinition>();
        }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            m_postprocessBuilder = builder as MyObjectBuilder_ComponentGroupDefinition;
            MyDebug.AssertDebug(m_postprocessBuilder != null);
        }

        public void Postprocess()
        {
            bool valid = true;

            int max = 0;
            foreach (var component in m_postprocessBuilder.Components)
            {
                if (component.Amount > max)
                    max = component.Amount;
            }

            for (int i = 0; i < max; ++i)
            {
                m_components.Add(null);
            }

            foreach (var component in m_postprocessBuilder.Components)
            {
                MyComponentDefinition componentDef;
                var compoDefId = new MyDefinitionId(typeof(MyObjectBuilder_Component), component.SubtypeId);
                MyDefinitionManager.Static.TryGetDefinition<MyComponentDefinition>(compoDefId, out componentDef);
                MyDebug.AssertDebug(componentDef != null, "Cannot find component " + compoDefId.ToString());
                if (componentDef == null)
                    valid = false;

                SetComponentDefinition(component.Amount, componentDef);
            }

            for (int i = 0; i < m_components.Count; ++i)
            {
                MyDebug.AssertDebug(m_components[i] != null, "Missing component definition for amount "+(i+1).ToString()+" in component group "+this.Id.ToString());
                if (m_components[i] == null)
                    valid = false;
            }

            if (valid == false)
            {
                m_components.Clear();
            }
        }

        public void SetComponentDefinition(int amount, MyComponentDefinition definition)
        {
            Debug.Assert(amount > 0 && amount <= m_components.Count, "Setting invalid component definition in a group!");
            if (amount <= 0 || amount > m_components.Count)
            {
                return;
            }

            m_components[amount - 1] = definition;
        }

        public MyComponentDefinition GetComponentDefinition(int amount)
        {
            Debug.Assert(amount > 0 && amount <= m_components.Count, "Getting invalid component definition in a group!");
            if (amount > 0 && amount <= m_components.Count)
                return m_components[amount - 1];
            else
                return null;
        }

        public int GetComponentNumber()
        {
            return m_components.Count;
        }
    }
}
