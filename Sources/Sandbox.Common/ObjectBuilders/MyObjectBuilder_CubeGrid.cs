using System.Collections.Generic;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using System.ComponentModel;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;

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
        [ProtoMember(1)]
        public SerializableVector3I BonePosition;

        [ProtoMember(2)]
        public SerializableVector3UByte BoneOffset;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CubeGrid : MyObjectBuilder_EntityBase
    {
        [ProtoMember(44)]
        public MyCubeSize GridSizeEnum;
        [ProtoMember(46)]
        [XmlArrayItem("MyObjectBuilder_CubeBlock", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_CubeBlock>))]
        public List<MyObjectBuilder_CubeBlock> CubeBlocks = new List<MyObjectBuilder_CubeBlock>();

        [ProtoMember(47)]
        public bool IsStatic;

        [ProtoMember(48)]
        public List<BoneInfo> Skeleton;

        [ProtoMember(49)]
        public SerializableVector3 LinearVelocity;

        [ProtoMember(50)]
        public SerializableVector3 AngularVelocity;

        [ProtoMember(51)]
        public SerializableVector3I? XMirroxPlane;

        [ProtoMember(52)]
        public SerializableVector3I? YMirroxPlane;

        [ProtoMember(53)]
        public SerializableVector3I? ZMirroxPlane;

        [ProtoMember(54), DefaultValue(false)]
        public bool XMirroxOdd = false;

        [ProtoMember(55), DefaultValue(false)]
        public bool YMirroxOdd = false;

        [ProtoMember(56), DefaultValue(false)]
        public bool ZMirroxOdd = false;

        [ProtoMember(57), DefaultValue(true)]
        public bool DampenersEnabled = true;

        [ProtoMember(58)]
        public List<MyObjectBuilder_ConveyorLine> ConveyorLines = new List<MyObjectBuilder_ConveyorLine>();

        [ProtoMember(59)]
        public List<MyObjectBuilder_BlockGroup> BlockGroups = new List<MyObjectBuilder_BlockGroup>();

        [ProtoMember(60)]
        public bool Handbrake;

        [ProtoMember(61)]
        public string DisplayName;

        [ProtoMember(62)]
        public float[] OxygenAmount;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);

            foreach (var blockBuilder in CubeBlocks)
            {
                blockBuilder.Remap(remapHelper);
            }
        }

        [NonSerialized]
        public bool CreatePhysics = true;

        [NonSerialized]
        public bool EnableSmallToLargeConnections = true;
    }
}
