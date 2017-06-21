using VRage.ObjectBuilders;
using ProtoBuf;
using System.ComponentModel;
using VRage.Game;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_TurretBase : MyObjectBuilder_UserControllableGun
    {
        [ProtoMember]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember, DefaultValue(600)]
        public float Range = 600;

        [ProtoMember]
        public int RemainingAmmo;

        [ProtoMember]
        public long Target;

        [ProtoMember, DefaultValue(true)]
        public bool TargetMeteors = true;

        [ProtoMember, DefaultValue(false)]
        public bool TargetMoving = false;

        [ProtoMember, DefaultValue(false)]
        public bool TargetMissiles = false;

        [ProtoMember]
        public bool IsPotentialTarget;

        [ProtoMember]
        public long? PreviousControlledEntityId;

        [ProtoMember]
        public float Rotation;

        [ProtoMember]
        public float Elevation;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_GunBase GunBase;

        [ProtoMember]
        public bool EnableIdleRotation = true;

        [ProtoMember]
        public bool PreviousIdleRotationState = true;

        [ProtoMember]
        public bool TargetCharacters = true;

        [ProtoMember, DefaultValue(true)]
        public bool TargetSmallGrids = true;

        [ProtoMember, DefaultValue(true)]
        public bool TargetLargeGrids = true;

        [ProtoMember, DefaultValue(true)]
        public bool TargetStations = true;

        [ProtoMember, DefaultValue(true)]
        public bool TargetNeutrals = true;

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
                Inventory.Clear();
            if (ComponentContainer != null)
            {
                var comp = ComponentContainer.Components.Find((s) => s.Component.TypeId == typeof(MyObjectBuilder_Inventory));
                if (comp != null)
                    (comp.Component as MyObjectBuilder_Inventory).Clear();
            }

            RemainingAmmo = 0;
        }
    }
}
