using System.Collections.Generic;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using System.ComponentModel;
using System;
using VRage.ObjectBuilders;
using VRage;
using VRage.ModAPI;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    public enum MyCubeSize : byte
    {
        Large = 0,
        Small = 1,
    }

    public enum MyBlockTopology : byte
    {
        Cube = 0,
        TriangleMesh = 1,
    }

    [ProtoContract]
    public struct BoneInfo
    {
        [ProtoMember]
        public SerializableVector3I BonePosition;

        [ProtoMember]
        public SerializableVector3UByte BoneOffset;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CubeGrid : MyObjectBuilder_EntityBase
    {
        [ProtoMember]
        public MyCubeSize GridSizeEnum;
        [ProtoMember]
        [DynamicItem(typeof(MyObjectBuilderDynamicSerializer), true)]
        [XmlArrayItem("MyObjectBuilder_CubeBlock", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_CubeBlock>))]
        public List<MyObjectBuilder_CubeBlock> CubeBlocks = new List<MyObjectBuilder_CubeBlock>();

        [ProtoMember, DefaultValue(false)]
        public bool IsStatic = false;

        [ProtoMember]
        public List<BoneInfo> Skeleton;
        public bool ShouldSerializeSkeleton() { return Skeleton != null && Skeleton.Count != 0; }

        [ProtoMember]
        [Serialize(MyObjectFlags.DefaultZero)]
        public SerializableVector3 LinearVelocity;
        public bool ShouldSerializeLinearVelocity() { return LinearVelocity != new SerializableVector3(0f, 0f, 0f); }

        [ProtoMember]
        [Serialize(MyObjectFlags.DefaultZero)]
        public SerializableVector3 AngularVelocity;
        public bool ShouldSerializeAngularVelocity() { return AngularVelocity != new SerializableVector3(0f, 0f, 0f); }

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public SerializableVector3I? XMirroxPlane;
        public bool ShouldSerializeXMirroxPlane() { return XMirroxPlane.HasValue; }

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public SerializableVector3I? YMirroxPlane;
        public bool ShouldSerializeYMirroxPlane() { return YMirroxPlane.HasValue; }

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public SerializableVector3I? ZMirroxPlane;
        public bool ShouldSerializeZMirroxPlane() { return ZMirroxPlane.HasValue; }

        [ProtoMember, DefaultValue(false)]
        public bool XMirroxOdd = false;

        [ProtoMember, DefaultValue(false)]
        public bool YMirroxOdd = false;

        [ProtoMember, DefaultValue(false)]
        public bool ZMirroxOdd = false;

        [ProtoMember, DefaultValue(true)]
        public bool DampenersEnabled = true;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public List<MyObjectBuilder_ConveyorLine> ConveyorLines = new List<MyObjectBuilder_ConveyorLine>();
        public bool ShouldSerializeConveyorLines() { return ConveyorLines != null && ConveyorLines.Count != 0; }

        [ProtoMember]
        public List<MyObjectBuilder_BlockGroup> BlockGroups = new List<MyObjectBuilder_BlockGroup>();
        public bool ShouldSerializeBlockGroups() { return BlockGroups != null && BlockGroups.Count != 0; }

        [ProtoMember, DefaultValue(false)]
        public bool Handbrake = false;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string DisplayName;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public float[] OxygenAmount;

        [ProtoMember]
        public bool DestructibleBlocks = true;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);

            foreach (var blockBuilder in CubeBlocks)
            {
                blockBuilder.Remap(remapHelper);
            }
        }

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public Vector3D? JumpDriveDirection;
        public bool ShouldSerializeJumpDriveDirection() { return JumpDriveDirection.HasValue; }

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public long? JumpElapsedTicks;
        public bool ShouldSerializeJumpElapsedTicks() { return JumpElapsedTicks.HasValue; }

        [DefaultValue(true)]
        public bool CreatePhysics = true;

        [DefaultValue(true)]
        public bool EnableSmallToLargeConnections = true;
    }
}
