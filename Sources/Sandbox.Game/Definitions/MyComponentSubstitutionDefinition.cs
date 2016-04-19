using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ComponentSubstitutionDefinition))]
    public class MyComponentSubstitutionDefinition : MyDefinitionBase
    { 
        public MyDefinitionId RequiredComponent;

        public Dictionary<MyDefinitionId, int> ProvidingComponents = new Dictionary<MyDefinitionId,int>(10);

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            MyObjectBuilder_ComponentSubstitutionDefinition ob = builder as MyObjectBuilder_ComponentSubstitutionDefinition;

            System.Diagnostics.Debug.Assert(ob != null, "Wrong object builder type!");
            
            RequiredComponent = ob.RequiredComponentId;
            if (ob.ProvidingComponents != null)
            {
                foreach (var comp in ob.ProvidingComponents)
                {
                    Debug.Assert(comp.Amount == 1, "Component substitution definition has amount of more than one! The code is not prepared for this!");
                    ProvidingComponents[comp.Id] = comp.Amount;
                }
            }            
        }

        public bool IsProvidedByComponent(MyDefinitionId componentId, int accessibleAmount, out int providedCount)
        {
            int value = 0;
            providedCount = 0;
            if (RequiredComponent == componentId)
            {
                providedCount = accessibleAmount;
                return true;
            }
            if (ProvidingComponents.TryGetValue(componentId, out value))
            {
                if (value <= accessibleAmount)
                {
                    providedCount = accessibleAmount / value;
                    return true;
                }
            }
            return false;
        }

        public bool IsProvidedByComponents(Dictionary<MyDefinitionId, VRage.MyFixedPoint> m_componentCounts, out int providedCount)
        {
            int summary = 0;
            foreach (var comp in m_componentCounts)
            {
                int count;
                if (IsProvidedByComponent(comp.Key, (int)comp.Value, out count))
                {
                    summary += count;
                }
            }
            providedCount = summary;
            return providedCount >= 0;   
        }
    }
}
