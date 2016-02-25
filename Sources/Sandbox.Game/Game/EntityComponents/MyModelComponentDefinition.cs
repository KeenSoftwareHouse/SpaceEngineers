using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
    [MyDefinitionType(typeof(MyObjectBuilder_ModelComponentDefinition))]
    public class MyModelComponentDefinition : MyComponentDefinitionBase
    {
        public Vector3 Size; // in metres
        public float Mass; // in kg
        public float Volume; // in m3
        public string Model;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            
            var modelDefinition = builder as MyObjectBuilder_ModelComponentDefinition;

            Size = modelDefinition.Size;
            Mass = modelDefinition.Mass;
            Model = modelDefinition.Model;
            Volume = modelDefinition.Volume.HasValue ? modelDefinition.Volume.Value / 1000f : modelDefinition.Size.Volume;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_ModelComponentDefinition;

            ob.Size = Size;
            ob.Mass = Mass;
            ob.Model = Model;
            ob.Volume = Volume * 1000;

            return ob;
        }
    }
}
