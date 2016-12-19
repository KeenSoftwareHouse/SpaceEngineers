using ProtoBuf;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DroneBehaviorDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public float StrafeWidth = 10f;
        [ProtoMember]
        public float StrafeHeight = 10f;
        [ProtoMember]
        public float StrafeDepth = 5f;
        [ProtoMember]
        public float MinStrafeDistance = 2f;

        [ProtoMember]
        public bool AvoidCollisions = true;
        [ProtoMember]
        public bool RotateToPlayer = true;
        [ProtoMember]
        public bool UseStaticWeaponry = true;
        [ProtoMember]
        public bool UseTools = true;
        [ProtoMember]
        public bool UseRammingBehavior = false;
        [ProtoMember]
        public bool CanBeDisabled = true;
        [ProtoMember]
        public bool UsePlanetHover = false;
        [ProtoMember]
        public string AlternativeBehavior = "";

        [ProtoMember]
        public float PlanetHoverMin = 2f;
        [ProtoMember]
        public float PlanetHoverMax = 25f;
        [ProtoMember]
        public float SpeedLimit = 50f;
        [ProtoMember]
        public float PlayerYAxisOffset = 0.9f;
        [ProtoMember]
        public float TargetDistance = 200f;
        [ProtoMember]
        public float MaxManeuverDistance = 250f;
        [ProtoMember]
        public float StaticWeaponryUsage = 300f;
        [ProtoMember]
        public float RammingBehaviorDistance = 75f;
        [ProtoMember]
        public float ToolsUsage = 8f;

        [ProtoMember]
        public int WaypointDelayMsMin = 1000;
        [ProtoMember]
        public int WaypointDelayMsMax = 3000;
        [ProtoMember]
        public int WaypointMaxTime = 15000;
        [ProtoMember]
        public float WaypointThresholdDistance = 0.5f;
        [ProtoMember]
        public int LostTimeMs = 20000;
    }
}
