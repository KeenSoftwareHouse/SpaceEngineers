using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRage;
using VRage.Game;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    public enum LandingGearMode
    {
        Unlocked = 0,
        ReadyToLock = 1,
        Locked = 2,
    }


    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_LandingGear : MyObjectBuilder_FunctionalBlock
    {
        // This value is taken somewhere from Havok
        public const float MaxSolverImpulse = 1e8f;

        [ProtoMember]
        public bool IsLocked;

        [ProtoMember]
        public float BrakeForce = MaxSolverImpulse;

        [ProtoMember]
        public bool AutoLock = true;

        [ProtoMember]
        public bool FirstLockAttempt = true;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string LockSound;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string UnlockSound;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string FailedAttachSound;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public long? AttachedEntityId =null;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public MyDeltaTransform? MasterToSlave;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public Vector3? GearPivotPosition;


        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public CompressedPositionOrientation? OtherPivot;

        [ProtoMember]
        public LandingGearMode LockMode = LandingGearMode.Unlocked;
    }
}
