using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.Data;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;
using VRage.ObjectBuilders;

namespace VRage.Game
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
        ZThenOffsetX,
        YThenOffsetX,
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

    public struct VoxelPlacementOverride
    {
        public VoxelPlacementSettings StaticMode;
        public VoxelPlacementSettings DynamicMode;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CubeBlockDefinition : MyObjectBuilder_PhysicalModelDefinition
    {

        #region Properties Definitions

        [ProtoContract]
        public class MountPoint
        {
            [XmlAttribute]
            [ProtoMember]
            public BlockSideEnum Side;

            [XmlIgnore, ProtoMember]
            public SerializableVector2 Start;

            [XmlIgnore, ProtoMember]
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

            [XmlAttribute, ProtoMember, DefaultValue(0)]
            public byte ExclusionMask = 0;

            [XmlAttribute, ProtoMember, DefaultValue(0)]
            public byte PropertiesMask = 0;

            [XmlAttribute, ProtoMember, DefaultValue(true)]
            public bool Enabled = true;

            [XmlAttribute, ProtoMember, DefaultValue(false)]
            public bool Default = false;

        }

        [ProtoContract]
        public class CubeBlockComponent
        {
            [XmlIgnore]
            public MyObjectBuilderType Type = typeof(MyObjectBuilder_Component); // Always Component, no need to serialize

            [XmlAttribute]
            [ProtoMember]
            public string Subtype;
            [XmlAttribute]
            [ProtoMember]
            public UInt16 Count;

            [ProtoMember]
            public SerializableDefinitionId DeconstructId;
        }

        [ProtoContract]
        public class CriticalPart
        {
            [XmlIgnore]
            public MyObjectBuilderType Type = typeof(MyObjectBuilder_Component); // Always Component, no need to serialize

            [XmlAttribute]
            [ProtoMember]
            public string Subtype;
            [XmlAttribute]
            [ProtoMember]
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
            [ProtoMember]
            public string Color;
            [XmlAttribute]
            [ProtoMember]
            public string Suffix;
        }

        [ProtoContract]
        public class PatternDefinition
        {
            [ProtoMember]
            public MyCubeTopology CubeTopology;
            [ProtoMember]
            public Side[] Sides;
            [ProtoMember]
            public bool ShowEdges;
        }

        [ProtoContract]
        public class Side
        {
            [XmlAttribute]
            [ProtoMember]
            [ModdableContentFile("mwm")]
            public string Model;

            [XmlIgnore]
            [ProtoMember]
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
            [ProtoMember]
            public float BuildPercentUpperBound;

            [XmlAttribute]
            [ProtoMember]
            [ModdableContentFile("mwm")]
            public string File;

            [XmlAttribute]
            [ProtoMember, DefaultValue(false)]
            public bool RandomOrientation;

            [ProtoMember]
            [XmlArray("MountPointOverrides")]
            [XmlArrayItem("MountPoint"), DefaultValue(null)]
            public MountPoint[] MountPoints;

            [XmlAttribute]
            [ProtoMember, DefaultValue(true)]
            public bool Visible = true;
        }

        [ProtoContract]
        public class MySubBlockDefinition
        {
            [XmlAttribute]
            [ProtoMember]
            public string SubBlock;

            [ProtoMember]
            public SerializableDefinitionId Id;
        }

        [ProtoContract]
        public class EntityComponentDefinition
        {
            [XmlAttribute]
            [ProtoMember]
            public string ComponentType;

            [XmlAttribute]
            [ProtoMember]
            public string BuilderType;
        }

        [ProtoContract]
        public class CubeBlockEffectBase
        {
            [XmlAttribute]
            [ProtoMember]
            public string Name = "";

            [XmlAttribute]
            [ProtoMember]
            public float ParameterMin = float.MinValue;

            [XmlAttribute]
            [ProtoMember]
            public float ParameterMax = float.MaxValue;

            [XmlArrayItem("ParticleEffect")]
            [ProtoMember]
            public CubeBlockEffect[] ParticleEffects;
        }

        [ProtoContract]
        public class CubeBlockEffect
        {
            [XmlAttribute]
            [ProtoMember]
            public string Name = "";

            [XmlAttribute]
            [ProtoMember]
            public string Origin = "";

            [XmlAttribute]
            [ProtoMember]
            public float Delay = 0f;

            [XmlAttribute]
            [ProtoMember]
            public float Duration = 0f;

            [XmlAttribute]
            [ProtoMember]
            public bool Loop = false;

            [XmlAttribute]
            [ProtoMember]
            public float SpawnTimeMin = 0f;

            [XmlAttribute]
            [ProtoMember]
            public float SpawnTimeMax = 0f;
        }

        #endregion

        public VoxelPlacementOverride? VoxelPlacement = null;

        [ProtoMember, DefaultValue(false)]
        public bool SilenceableByShipSoundSystem;

        [ProtoMember]
        public MyCubeSize CubeSize;
        
        [ProtoMember]
        public MyBlockTopology BlockTopology;
        
        [ProtoMember]
        public SerializableVector3I Size;
        
        [ProtoMember]
        public SerializableVector3 ModelOffset;

        //[ProtoMember]
        //[ModdableContentFile("mwm")]
        //public string Model;
        
        [ProtoMember]
        public PatternDefinition CubeDefinition;

        [XmlArrayItem("Component")]
        [ProtoMember]
        public CubeBlockComponent[] Components;
        
        [XmlArrayItem("Effect")]
        [ProtoMember]
        public CubeBlockEffectBase[] Effects;

        [ProtoMember]
        public CriticalPart CriticalComponent;

        [ProtoMember]
        public MountPoint[] MountPoints;

        [ProtoMember]
        public Variant[] Variants;

        [XmlArrayItem("Component")]
        [ProtoMember]
        public EntityComponentDefinition[] EntityComponents;

        [ProtoMember, DefaultValue(MyPhysicsOption.Box)]
        public MyPhysicsOption PhysicsOption = MyPhysicsOption.Box;

        [XmlArrayItem("Model")]
        [ProtoMember, DefaultValue(null)]
        public List<BuildProgressModel> BuildProgressModels = null;

        [ProtoMember]
        public string BlockPairName;

        [ProtoMember]
        public SerializableVector3I? Center;
        public bool ShouldSerializeCenter() { return Center.HasValue; }

        [ProtoMember, DefaultValue(MySymmetryAxisEnum.None)]
        public MySymmetryAxisEnum MirroringX = MySymmetryAxisEnum.None;

        [ProtoMember, DefaultValue(MySymmetryAxisEnum.None)]
        public MySymmetryAxisEnum MirroringY = MySymmetryAxisEnum.None;

        [ProtoMember, DefaultValue(MySymmetryAxisEnum.None)]
        public MySymmetryAxisEnum MirroringZ = MySymmetryAxisEnum.None;

        [ProtoMember, DefaultValue(1.0f)]
        public float DeformationRatio = 1.0f;

        [ProtoMember]
        public string EdgeType;

        [ProtoMember, DefaultValue(10.0f)]
        public float BuildTimeSeconds = 10.0f;

        [ProtoMember, DefaultValue(1.0f)]
        public float DisassembleRatio = 1.0f;

        [ProtoMember]
        public MyAutorotateMode AutorotateMode = MyAutorotateMode.OneDirection;

        [ProtoMember]
        public string MirroringBlock;

        [ProtoMember]
        public bool UseModelIntersection = false;

        //[ProtoMember]
        //public bool IDModule = false;

        [ProtoMember]
        public string PrimarySound;

        [ProtoMember]
        public string ActionSound;

        [ProtoMember, DefaultValue(null)] 
        public string BuildType = null;

        [ProtoMember, DefaultValue(null)]
        public string BuildMaterial = null;

        [XmlArrayItem("Template")]
        [ProtoMember, DefaultValue(null)]
        public string[] CompoundTemplates = null;

        [ProtoMember, DefaultValue(true)]
        public bool CompoundEnabled = true;

        [XmlArrayItem("Definition")]
        [ProtoMember, DefaultValue(null)]
        public MySubBlockDefinition[] SubBlockDefinitions = null;

        [ProtoMember, DefaultValue(null)] 
        public string MultiBlock = null;

        [ProtoMember, DefaultValue(null)]
        public string NavigationDefinition = null;

        [ProtoMember, DefaultValue(true)]
        public bool GuiVisible = true;

        [XmlArrayItem("BlockVariant")]
        [ProtoMember, DefaultValue(null)]
        public SerializableDefinitionId[] BlockVariants = null;

        // Forward direction - can be horizontal and horizontal+vertical (vertical only not supported)
        [ProtoMember, DefaultValue(MyBlockDirection.Both)]
        public MyBlockDirection Direction = MyBlockDirection.Both;

        // Allowed rotation
        [ProtoMember, DefaultValue(MyBlockRotation.Both)]
        public MyBlockRotation Rotation = MyBlockRotation.Both;

        [XmlArrayItem("GeneratedBlock")]
        [ProtoMember, DefaultValue(null)]
        public SerializableDefinitionId[] GeneratedBlocks;

        [ProtoMember, DefaultValue(null)]
        public string GeneratedBlockType;

        // Defines if the block is mirrored version of some other block (mirrored block is usually used as block variant)
        [ProtoMember, DefaultValue(false)]
        public bool Mirrored;

        [ProtoMember, DefaultValue(0)]
        public int DamageEffectId;

        [ProtoMember, DefaultValue("")]
        public string DestroyEffect;

        [ProtoMember, DefaultValue("")]
        public string DestroySound;

        // Defines if the block is deformed by a skeleton by default (round blocks)
        [ProtoMember, DefaultValue(null)]
        public List<BoneInfo> Skeleton;

        // Defines if the block can be randomly rotated when line/plane building is applied to it.
        [ProtoMember, DefaultValue(false)]
        public bool RandomRotation;

        // Temporary flag that tells the oxygen system to treat this block as a full block
        [ProtoMember, DefaultValue(false)]
        public bool IsAirTight;

        [ProtoMember, DefaultValue(1)]
        public int Points;

        [ProtoMember, DefaultValue(0)]
        public int MaxIntegrity = 0;

        [ProtoMember, DefaultValue(1)]
        public float BuildProgressToPlaceGeneratedBlocks = 1;

        [ProtoMember, DefaultValue(null)]
        public string DamagedSound = null;

        [ProtoMember, DefaultValue(true)]
        public bool CreateFracturedPieces = true;
    }
}