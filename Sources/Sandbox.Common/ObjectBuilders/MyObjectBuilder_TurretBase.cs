using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TurretBase : MyObjectBuilder_UserControllableGun
    {
        [ProtoMember]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember, DefaultValue(800)]
        public float Range = 800;

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
        public bool IsShooting;

        [ProtoMember]
        public MyObjectBuilder_GunBase GunBase;

        [ProtoMember]
        public bool EnableIdleRotation = true;

        [ProtoMember]
        public bool PreviousIdleRotationState = true;

        [ProtoMember, DefaultValue(true)]
        public bool TargetCharacters = true;

        [ProtoMember, DefaultValue(true)]
        public bool TargetSmallGrids = true;

        [ProtoMember, DefaultValue(true)]
        public bool TargetLargeGrids = true;

        [ProtoMember, DefaultValue(true)]
        public bool TargetStations = true;

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
            {
                Inventory.Clear();
            }

            RemainingAmmo = 0;
        }
    }
}
