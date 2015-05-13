using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;
using Sandbox.Common.ObjectBuilders.AI;
using VRageMath;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    public enum MyCubeTopology
    {
        Box,
        Slope,
        Corner,
        InvCorner,
        StandaloneBox,
        //This should have been called RoundedBlock or something similar, since it means it was derived from a full block,
        //but because of modding we cannot change this now.
        RoundedSlope,
        RoundSlope,
        RoundCorner,
        RoundInvCorner,
        RotatedSlope,
        RotatedCorner,

        //Slopes
        //We need separate definition for each block because of edges and physics
        Slope2Base,
        Slope2Tip,

        Corner2Base,
        Corner2Tip,

        InvCorner2Base,
        InvCorner2Tip,
    }

    public enum MyFractureMaterial
    {
        Stone,
        Wood,
    }

    public enum MyPhysicsOption
    {
        None,
        Box,
        Convex,
    }

    // Enum used to index mount point sides.
    public enum BlockSideEnum
    {
        Right = 0,
        Top = 1,
        Front = 2,
        Left = 3,
        Bottom = 4,
        Back = 5,
    }

    public enum MySymmetryAxisEnum
    {
        None,
        X,
        Y,
        Z,
        XHalfY,
        YHalfY,
        ZHalfY,
        XHalfX,
        YHalfX,
        ZHalfX,
        XHalfZ,
        YHalfZ,
        ZHalfZ,
        MinusHalfX,
        MinusHalfY,
        MinusHalfZ,
        HalfX,
        HalfY,
        HalfZ,
        XMinusHalfZ,
        YMinusHalfZ,
        ZMinusHalfZ,
        XMinusHalfX,
        YMinusHalfX,
        ZMinusHalfX,
        ZThenOffsetX
    }

    public enum MyAutorotateMode
    {
        /// <summary>
        /// When block has mount points only on one side, it will autorotate so that side is touching the surface.
        /// Otherwise, full range of rotations is allowed.
        /// </summary>
        OneDirection,

        /// <summary>
        /// When block has mount points only on two sides and those sides are opposite each other (eg. Top and Bottom),
        /// it will autorotate so that one of these sides is touching the surface. Otherwise, full range of rotations
        /// is allowed.
        /// </summary>
        OppositeDirections,

        /// <summary>
        /// When block has mountpoint on at least one side, it will autorotate so that this side is touching the surface.
        /// Otherwise, full range of rotations is allowed.
        /// </summary>
        FirstDirection,
    }

    [Flags]
    public enum MyBlockDirection
    {
        Horizontal = 1 << 0,
        Vertical = 1 << 1,
        Both = Horizontal | Vertical,
    }

    [Flags]
    public enum MyBlockRotation
    {
        None = 0,
        Horizontal = 1 << 0,
        Vertical = 1 << 1,
        Both = Horizontal | Vertical,
    }


    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CubeBlockDefinition : MyObjectBuilder_PhysicalModelDefinition
    {
        [ProtoContract]
        public class MountPoint
        {
            [XmlAttribute]
            [ProtoMember(1)]
            public BlockSideEnum Side;

            [XmlIgnore, ProtoMember(2)]
            public SerializableVector2 Start;

            [XmlIgnore, ProtoMember(3)]
            public SerializableVector2 End;

            [XmlAttribute]
            public float StartX
            {
                get { return Start.X; }
                set { Start.X = value; }
            }
            [XmlAttribute]
            public float StartY
            {
                get { return Start.Y; }
                set { Start.Y = value; }
            }

            [XmlAttribute]
            public float EndX
            {
                get { return End.X; }
                set { End.X = value; }
            }
            [XmlAttribute]
            public float EndY
            {
                get { return End.Y; }
                set { End.Y = value; }
            }

            [XmlAttribute, ProtoMember(4), DefaultValue(0)]
            public byte ExclusionMask = 0;

            [XmlAttribute, ProtoMember(5), DefaultValue(0)]
            public byte PropertiesMask = 0;

        }

        [ProtoContract]
        public class CubeBlockComponent
        {
            [XmlIgnore]
            public MyObjectBuilderType Type = typeof(MyObjectBuilder_Component); // Always Component, no need to serialize

            [XmlAttribute]
            [ProtoMember(1)]
            public string Subtype;
            [XmlAttribute]
            [ProtoMember(2)]
            public UInt16 Count;
        }

        [ProtoContract]
        public class CriticalPart
        {
            [XmlIgnore]
            public MyObjectBuilderType Type = typeof(MyObjectBuilder_Component); // Always Component, no need to serialize

            [XmlAttribute]
            [ProtoMember(1)]
            public string Subtype;
            [XmlAttribute]
            [ProtoMember(2)]
            public int Index = 0;
        }

        [ProtoContract]
        public class Variant
        {
            /// <summary>
            /// Color is used to get Color(4 bytes) as well as
            /// MyStringId value for localization.
            /// </summary>
            [XmlAttribute]
            [ProtoMember(1)]
            public string Color;
            [XmlAttribute]
            [ProtoMember(2)]
            public string Suffix;
        }

        [ProtoContract]
        public class PatternDefinition
        {
            [ProtoMember(1)]
            public MyCubeTopology CubeTopology;
            [ProtoMember(2)]
            public Side[] Sides;
            [ProtoMember(3)]
            public bool ShowEdges;
        }

        [ProtoContract]
        public class Side
        {
            [XmlAttribute]
            [ProtoMember(1)]
            [ModdableContentFile("mwm")]
            public string Model;

            [XmlIgnore]
            [ProtoMember(2)]
            public SerializableVector2I PatternSize;

            [XmlAttribute]
            public int PatternWidth
            {
                get { return PatternSize.X; }
                set { PatternSize.X = value; }
            }

            [XmlAttribute]
            public int PatternHeight
            {
                get { return PatternSize.Y; }
                set { PatternSize.Y = value; }
            }
        }

        [ProtoContract]
        public class BuildProgressModel
        {
            [XmlAttribute]
            [ProtoMember(1)]
            public float BuildPercentUpperBound;

            [XmlAttribute]
            [ProtoMember(2)]
            [ModdableContentFile("mwm")]
            public string File;

            [XmlAttribute]
            [ProtoMember(3), DefaultValue(false)]
            public bool RandomOrientation;
        }

        [ProtoContract]
        public class MyAdditionalModelDefinition
        {
            [XmlAttribute]
            [ProtoMember(1)]
            public string Type;

            [XmlAttribute]
            [ProtoMember(2)]
            [ModdableContentFile("mwm")]
            public string File;

            [XmlAttribute]
            [ProtoMember(3), DefaultValue(false)]
            public bool EnablePhysics = false;
        }

        [ProtoContract]
        public class MyGeneratedBlockDefinition
        {
            [XmlAttribute]
            [ProtoMember(1)]
            public string Type;

            [ProtoMember(2)]
            public SerializableDefinitionId Id;
        }

        [ProtoContract]
        public class MySubBlockDefinition
        {
            [XmlAttribute]
            [ProtoMember(1)]
            public string SubBlock;

            [ProtoMember(2)]
            public SerializableDefinitionId Id;
        }


        [ProtoMember(1)]
        public MyCubeSize CubeSize;
        
        [ProtoMember(2)]
        public MyBlockTopology BlockTopology;
        
        [ProtoMember(3)]
        public SerializableVector3I Size;
        
        [ProtoMember(4)]
        public SerializableVector3 ModelOffset;

        //[ProtoMember(5)]
        //[ModdableContentFile("mwm")]
        //public string Model;
        
        [ProtoMember(6)]
        public PatternDefinition CubeDefinition;

        [XmlArrayItem("Component")]
        [ProtoMember(7)]
        public CubeBlockComponent[] Components;

        [ProtoMember(8)]
        public CriticalPart CriticalComponent;

        [ProtoMember(9)]
        public MountPoint[] MountPoints;

        [ProtoMember(10)]
        public Variant[] Variants;

        [ProtoMember(11), DefaultValue(MyPhysicsOption.Box)]
        public MyPhysicsOption PhysicsOption = MyPhysicsOption.Box;

        [XmlArrayItem("Model")]
        [ProtoMember(12), DefaultValue(null)]
        public List<BuildProgressModel> BuildProgressModels = null;

        [ProtoMember(15)]
        public string BlockPairName;

        [ProtoMember(17)]
        public SerializableVector3I? Center;
        public bool ShouldSerializeCenter() { return Center.HasValue; }

        [ProtoMember(18), DefaultValue(MySymmetryAxisEnum.None)]
        public MySymmetryAxisEnum MirroringX = MySymmetryAxisEnum.None;

        [ProtoMember(19), DefaultValue(MySymmetryAxisEnum.None)]
        public MySymmetryAxisEnum MirroringY = MySymmetryAxisEnum.None;

        [ProtoMember(20), DefaultValue(MySymmetryAxisEnum.None)]
        public MySymmetryAxisEnum MirroringZ = MySymmetryAxisEnum.None;

        [ProtoMember(21), DefaultValue(1.0f)]
        public float DeformationRatio = 1.0f;

        [ProtoMember(22)]
        public string EdgeType;

        [ProtoMember(23), DefaultValue(10.0f)]
        public float BuildTimeSeconds = 10.0f;

        [ProtoMember(24), DefaultValue(1.0f)]
        public float DisassembleRatio = 1.0f;

        [ProtoMember(25)]
        public MyAutorotateMode AutorotateMode = MyAutorotateMode.OneDirection;

        [ProtoMember(26)]
        public string MirroringBlock;

        [ProtoMember(27)]
        public bool UseModelIntersection = false;

        //[ProtoMember(28)]
        //public bool IDModule = false;

        [ProtoMember(29)]
        public string PrimarySound;

        [ProtoMember(30), DefaultValue(null)] 
        public string BuildType = null;

        [XmlArrayItem("Template")]
        [ProtoMember(32), DefaultValue(null)]
        public string[] CompoundTemplates = null;

        [XmlArrayItem("Definition")]
        [ProtoMember(33), DefaultValue(null)]
        public MySubBlockDefinition[] SubBlockDefinitions = null;

        [ProtoMember(34), DefaultValue(null)] 
        public string MultiBlock = null;

        [ProtoMember(36), DefaultValue(null)]
        public string NavigationDefinition = null;

        [ProtoMember(37), DefaultValue(true)]
        public bool GuiVisible = true;

        [XmlArrayItem("BlockVariant")]
        [ProtoMember(38), DefaultValue(null)]
        public SerializableDefinitionId[] BlockVariants = null;

        // Forward direction - can be horizontal and horizontal+vertical (vertical only not supported)
        [ProtoMember(39), DefaultValue(MyBlockDirection.Both)]
        public MyBlockDirection Direction = MyBlockDirection.Both;

        // Allowed rotation
        [ProtoMember(40), DefaultValue(MyBlockRotation.Both)]
        public MyBlockRotation Rotation = MyBlockRotation.Both;

        [XmlArrayItem("GeneratedBlock")]
        [ProtoMember(41), DefaultValue(null)]
        public SerializableDefinitionId[] GeneratedBlocks = null;

        [ProtoMember(42), DefaultValue(null)]
        public string GeneratedBlockType = null;

        // Defines if the block is mirrored version of some other block (mirrored block is usually used as block stage)
        [ProtoMember(43), DefaultValue(false)]
        public bool Mirrored = false;

        [ProtoMember(44), DefaultValue(null)]
        public int DamageEffectId;

        // Defines if the block is deformed by a skeleton by default (round blocks)
        [ProtoMember(45), DefaultValue(null)]
        public List<BoneInfo> Skeleton;

        // Defines if the block can be randomly rotated when line/plane building is applied to it.
        [ProtoMember(46), DefaultValue(false)]
        public bool RandomRotation = false;

        // Temporary flag that tells the oxygen system to treat this block as a full block
        [ProtoMember(47), DefaultValue(false)]
        public bool IsAirTight = false;
    }
}
