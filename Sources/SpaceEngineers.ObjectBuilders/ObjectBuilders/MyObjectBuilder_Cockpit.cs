using ProtoBuf;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Serialization;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_Cockpit : MyObjectBuilder_ShipController
    {
        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_Character Pilot;

        [ProtoMember]
        public MyPositionAndOrientation? PilotRelativeWorld;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable | MyObjectFlags.Dynamic ,DynamicSerializerType = typeof(MyObjectBuilderDynamicSerializer))]
        public MyObjectBuilder_AutopilotBase Autopilot;

        public void ClearPilotAndAutopilot()
        {
            Pilot = null;
            Autopilot = null;
            AttachedPlayerId = null;
        }

        [ProtoMember]
        public SerializableDefinitionId? PilotGunDefinition;

        [ProtoMember]
        public bool IsInFirstPersonView;

        [ProtoMember]
        public float OxygenLevel;

        [ProtoMember]
        public long? AttachedPlayerId;

        public override void SetupForProjector()
        {
            OxygenLevel = 0f;
            if (ComponentContainer != null)
            {
                var comp = ComponentContainer.Components.Find((s) => s.Component.TypeId == typeof(MyObjectBuilder_Inventory));
                if (comp != null)
                    (comp.Component as MyObjectBuilder_Inventory).Clear();
            }
            base.SetupForProjector();
        }
    }
}
