using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ObjectBuilders;

namespace Sandbox.Game.EntityComponents
{
    [MyDefinitionType(typeof(MyObjectBuilder_EntityStatComponentDefinition))]
    public class MyEntityStatComponentDefinition : MyComponentDefinitionBase
    {
        public List<MyDefinitionId> Stats;
        public List<string> Scripts;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var objectBuilder = builder as MyObjectBuilder_EntityStatComponentDefinition;

            Stats = new List<MyDefinitionId>();
            foreach (var stat in objectBuilder.Stats)
            {
                Stats.Add(stat);
            }

            Scripts = objectBuilder.Scripts;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var builder = base.GetObjectBuilder() as MyObjectBuilder_EntityStatComponentDefinition;

            builder.Stats = new List<SerializableDefinitionId>();
            foreach (var stat in Stats)
            {
                builder.Stats.Add(stat);
            }
            builder.Scripts = Scripts;

            return builder;
        }
    }
}
