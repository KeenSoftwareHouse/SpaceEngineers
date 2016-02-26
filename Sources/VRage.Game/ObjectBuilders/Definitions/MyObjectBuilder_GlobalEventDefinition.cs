using ProtoBuf;
using System.Diagnostics;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    /// <summary>
    /// Defines the event type.
    /// Each event type has its assigned handler method and data class type.
    /// Multiple event definitions can have the same event type.
    /// </summary>
    public enum MyGlobalEventTypeEnum
    {
        InvalidEventType = 0,

        SpawnNeutralShip = 1, // Keep here for compatibility (mostly just for our testers)
        MeteorShower = 2, // Unused. Use MeteorWave
        MeteorWave = 3,
        SpawnCargoShip = 4,
        April2014 = 5

        // Last ID is 5
    }

    [ProtoContract]
    [MyObjectBuilderDefinition(LegacyName: "EventDefinition")]
    public class MyObjectBuilder_GlobalEventDefinition : MyObjectBuilder_DefinitionBase
    {
        // Obsolete! Get accessor is missing on purpose! Use DefinitionId instead
        [ProtoMember]
        private MyGlobalEventTypeEnum EventType
        {
            set { Debug.Assert(false, "Setting an EventType on MyObjectBuilder_GlobalEventDefinition is obsolete!"); }
            get { Debug.Assert(false, "Getting an EventType on MyObjectBuilder_GlobalEventDefinition is obsolete!"); return MyGlobalEventTypeEnum.InvalidEventType; }
        }

        [ProtoMember]
        public long? MinActivationTimeMs;
        public bool ShouldSerializeMinActivationTime() { return MinActivationTimeMs.HasValue; }

        [ProtoMember]
        public long? MaxActivationTimeMs;
        public bool ShouldSerializeMaxActivationTime() { return MaxActivationTimeMs.HasValue; }

        [ProtoMember]
        public long? FirstActivationTimeMs;
        public bool ShouldSerializeFirstActivationTime() { return FirstActivationTimeMs.HasValue; }
    }
}