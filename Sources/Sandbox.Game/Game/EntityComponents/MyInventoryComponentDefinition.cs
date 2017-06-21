using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
    [MyDefinitionType(typeof(MyObjectBuilder_InventoryComponentDefinition))]
    public class MyInventoryComponentDefinition : MyComponentDefinitionBase
    {
        public float Volume;
        public float Mass;
        public bool RemoveEntityOnEmpty;
        public bool MultiplierEnabled;
        public int MaxItemCount;

        public MyInventoryConstraint InputConstraint;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_InventoryComponentDefinition;
            Volume = ob.Volume;
            if (ob.Size != null) 
            {
                Vector3 size = ob.Size.Value;
                Volume = size.Volume;
            }
            Mass = ob.Mass;
            RemoveEntityOnEmpty = ob.RemoveEntityOnEmpty;
            MultiplierEnabled = ob.MultiplierEnabled;
            MaxItemCount = ob.MaxItemCount;

            if (ob.InputConstraint != null)
            {
                InputConstraint = new MyInventoryConstraint("Input", whitelist: ob.InputConstraint.IsWhitelist);

                foreach (var constraint in ob.InputConstraint.Entries)
                {
                    if (string.IsNullOrEmpty(constraint.SubtypeName))
                        InputConstraint.AddObjectBuilderType(constraint.TypeId);
                    else
                        InputConstraint.Add(constraint);
                }
            }
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_InventoryComponentDefinition;

            ob.Volume = Volume;
            ob.Mass = Mass;
            ob.RemoveEntityOnEmpty = RemoveEntityOnEmpty;
            ob.MultiplierEnabled = MultiplierEnabled;
            ob.MaxItemCount = MaxItemCount;

            if (InputConstraint != null)
            {
                ob.InputConstraint = new MyObjectBuilder_InventoryComponentDefinition.InventoryConstraintDefinition
                {
                    IsWhitelist = InputConstraint.IsWhitelist
                };

                foreach (var type in InputConstraint.ConstrainedTypes)
                    ob.InputConstraint.Entries.Add(new MyDefinitionId(type));

                foreach (var id in InputConstraint.ConstrainedIds)
                    ob.InputConstraint.Entries.Add(id);
            }

            return ob;
        }
    }
}
