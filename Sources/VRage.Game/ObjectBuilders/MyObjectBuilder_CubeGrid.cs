using System.Collections.Generic;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using System.ComponentModel;
using System;
using VRage.ObjectBuilders;
using VRage;
using VRage.ModAPI;

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
        [XmlArrayItem("MyObjectBuilder_CubeBlock", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_CubeBlock>))]
        public List<MyObjectBuilder_CubeBlock> CubeBlocks = new List<MyObjectBuilder_CubeBlock>();

        [ProtoMember]
        public bool IsStatic;

        [ProtoMember]
        public List<BoneInfo> Skeleton;

        [ProtoMember]
        public SerializableVector3 LinearVelocity;

        [ProtoMember]
        public SerializableVector3 AngularVelocity;

        [ProtoMember]
        public SerializableVector3I? XMirroxPlane;

        [ProtoMember]
        public SerializableVector3I? YMirroxPlane;

        [ProtoMember]
        public SerializableVector3I? ZMirroxPlane;

        [ProtoMember, DefaultValue(false)]
        public bool XMirroxOdd = false;

        [ProtoMember, DefaultValue(false)]
        public bool YMirroxOdd = false;

        [ProtoMember, DefaultValue(false)]
        public bool ZMirroxOdd = false;

        [ProtoMember, DefaultValue(true)]
        public bool DampenersEnabled = true;

        [ProtoMember]
        public List<MyObjectBuilder_ConveyorLine> ConveyorLines = new List<MyObjectBuilder_ConveyorLine>();

        [ProtoMember]
        public List<MyObjectBuilder_BlockGroup> BlockGroups = new List<MyObjectBuilder_BlockGroup>();

        [ProtoMember]
        public bool Handbrake;

        [ProtoMember]
        public string DisplayName;

        [ProtoMember]
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

        [NonSerialized]
        public bool CreatePhysics = true;

        [NonSerialized]
        public bool EnableSmallToLargeConnections = true;
    }
}
