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
        [ProtoMember(1)]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember(2), DefaultValue(800)]
        public float Range = 800;

        [ProtoMember(4)]
        public int RemainingAmmo;

        [ProtoMember(5)]
        public long Target;

        [ProtoMember(6), DefaultValue(true)]
        public bool TargetMeteors = true;

        [ProtoMember(7), DefaultValue(false)]
        public bool TargetMoving = false;

        [ProtoMember(8), DefaultValue(false)]
        public bool TargetMissiles = false;

        [ProtoMember(9)]
        public bool IsPotentialTarget;

        [ProtoMember(10)]
        public long? PreviousControlledEntityId;

        [ProtoMember(11)]
        public float Rotation;

        [ProtoMember(12)]
        public float Elevation;

        [ProtoMember(13)]
        public bool IsShooting;

        [ProtoMember(14)]
        public MyObjectBuilder_GunBase GunBase;

        [ProtoMember(15)]
        public bool EnableIdleRotation = true;

        [ProtoMember(16)]
        public bool PreviousIdleRotationState = true;

        [ProtoMember(17), DefaultValue(true)]
        public bool TargetCharacters = true;

        [ProtoMember(18), DefaultValue(true)]
        public bool TargetSmallGrids = true;

        [ProtoMember(19), DefaultValue(true)]
        public bool TargetLargeGrids = true;

        [ProtoMember(20), DefaultValue(true)]
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
