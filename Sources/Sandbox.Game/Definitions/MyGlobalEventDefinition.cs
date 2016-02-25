using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_GlobalEventDefinition))]
    public class MyGlobalEventDefinition : MyDefinitionBase
    {
        public TimeSpan? MinActivationTime;
        public TimeSpan? MaxActivationTime;
        public TimeSpan? FirstActivationTime;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            // Backward compatibility with definitions of events that have MyObjectBuilder_GlobalEventDefinition as the TypeId 
            if (builder.Id.TypeId == typeof(MyObjectBuilder_GlobalEventDefinition))
            {
                builder.Id = new VRage.ObjectBuilders.SerializableDefinitionId(typeof(MyObjectBuilder_GlobalEventBase), builder.Id.SubtypeName);
            }

            base.Init(builder);

            var eventBuilder = builder as MyObjectBuilder_GlobalEventDefinition;

            // This ensures that either both min and max activation time are specified or neither of them is
            if (eventBuilder.MinActivationTimeMs.HasValue && !eventBuilder.MaxActivationTimeMs.HasValue)
            {
                eventBuilder.MaxActivationTimeMs = eventBuilder.MinActivationTimeMs;
            }
            if (eventBuilder.MaxActivationTimeMs.HasValue && !eventBuilder.MinActivationTimeMs.HasValue)
            {
                eventBuilder.MinActivationTimeMs = eventBuilder.MaxActivationTimeMs;
            }

            Debug.Assert(eventBuilder.FirstActivationTimeMs.HasValue || eventBuilder.MinActivationTimeMs.HasValue, "Global event definition has to have either the FirstActivationTime or [Min/Max]ActivationTime specified");

            if (eventBuilder.MinActivationTimeMs.HasValue)
                MinActivationTime = TimeSpan.FromTicks(eventBuilder.MinActivationTimeMs.Value * TimeSpan.TicksPerMillisecond);
            if (eventBuilder.MaxActivationTimeMs.HasValue)
                MaxActivationTime = TimeSpan.FromTicks(eventBuilder.MaxActivationTimeMs.Value * TimeSpan.TicksPerMillisecond);
            if (eventBuilder.FirstActivationTimeMs.HasValue)
                FirstActivationTime = TimeSpan.FromTicks(eventBuilder.FirstActivationTimeMs.Value * TimeSpan.TicksPerMillisecond);
        }
    }
}
