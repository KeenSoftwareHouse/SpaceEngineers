using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

using VRageMath;
using System;
using System.IO;
using System.Linq;
using VRage.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.AI.Pathfinding;
using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Engine.Utils;
using VRage;
using Sandbox.Common.ObjectBuilders.VRageData;
using Sandbox.Game;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.FileSystem;

namespace Sandbox.Definitions
{
    public class MyCubeDefinition
    {
        public MyCubeTopology CubeTopology;
        public bool ShowEdges;

        public string[] Model;
        public Vector2I[] PatternSize;
    }

    public class MyCubeBlockDefinitionGroup
    {
        private static int m_sizeCount = Enum.GetValues(typeof(MyCubeSize)).Length;
        private readonly MyCubeBlockDefinition[] m_definitions;

        public MyCubeBlockDefinition this[MyCubeSize size]
        {
            get { return m_definitions[(int)size]; }
            set
            {
                Debug.Assert(m_definitions[(int)size] == null, "You're overwriting an existing definition in the group. Is this what you want?");
                m_definitions[(int)size] = value;
            }
        }

        public int SizeCount
        {
            get { return m_sizeCount; }
        }

        public MyCubeBlockDefinition Large
        {
            get { return this[MyCubeSize.Large]; }
        }

        public MyCubeBlockDefinition Small
        {
            get { return this[MyCubeSize.Small]; }
        }

        public MyCubeBlockDefinition Any
        {
            get
            {
                foreach (var def in m_definitions)
                    if (def != null)
                        return def;
                return null;
            }
        }

        public MyCubeBlockDefinition AnyPublic
        {
            get
            {
                foreach (var def in m_definitions)
                    if (def != null && def.Public)
                        return def;
                return null;
            }
        }

        internal MyCubeBlockDefinitionGroup()
        {
            m_definitions = new MyCubeBlockDefinition[m_sizeCount];
        }

    }

    [MyDefinitionType(typeof(MyObjectBuilder_CubeBlockDefinition))]
    public class MyCubeBlockDefinition : MyPhysicalModelDefinition
    {
        public class Component
        {
            public MyComponentDefinition Definition;
            public int Count;
        }

        public class BuildProgressModel
        {
            /// <summary>
            /// Upper bound when the model is no longer shown. If model is first in array
            /// and has build percentage of 0.33, it will be shown between 0% and 33% of
            /// build progress.
            /// </summary>
            public float BuildRatioUpperBound;

            public string File;

            public bool RandomOrientation;
        }

        public struct MountPoint
        {
            public Vector3I Normal;
            public Vector3 Start;
            public Vector3 End;

            /*
             * Exclusion and Properties masks allow us to specify some mount points which will fail test even if they overlap their neighbor.
             * The check is done like this: ((l.ExclusionMask & r.PropertiesMask) != 0 || (l.PropertiesMask & r.ExclusionMask) != 0) &&
             *                              (block type of l and block type of r are different)
             * If this check passes, mount point pair is skipped.
             * currently used bits in the mask are:
             *      0x1 - penetrating mount point (enters neighboring block)
             *      0x2 - thin mount point (penetrating mount would pass through looking like a glitch)
             */

            /// <summary>
            /// Excluded properties when attaching to this mount point. Bitwise & with
            /// other mount points properties mask must result in 0 to allow attaching.
            /// </summary>
            public byte ExclusionMask;

            /// <summary>
            /// Properties when attaching this mount point. Bitwise & with other mount 
            /// points exclusion mask must result in 0 to allow attaching.
            /// </summary>
            public byte PropertiesMask;

            public MyObjectBuilder_CubeBlockDefinition.MountPoint GetObjectBuilder(Vector3I cubeSize)
            {
                MyObjectBuilder_CubeBlockDefinition.MountPoint ob = new MyObjectBuilder_CubeBlockDefinition.MountPoint();
                ob.Side = NormalToBlockSide(Normal);

                Vector3 localStart;
                Vector3 localEnd;

                MyCubeBlockDefinition.UntransformMountPointPosition(ref Start, (int)ob.Side, cubeSize, out localStart);
                MyCubeBlockDefinition.UntransformMountPointPosition(ref End, (int)ob.Side, cubeSize, out localEnd);

                ob.Start = new SerializableVector2(localStart.X, localStart.Y);
                ob.End = new SerializableVector2(localEnd.X, localEnd.Y);

                ob.ExclusionMask = ExclusionMask;
                ob.PropertiesMask = PropertiesMask;

                return ob;
            }
        }

        public MyCubeSize CubeSize;
        public MyBlockTopology BlockTopology = MyBlockTopology.TriangleMesh;
        public Vector3I Size;
        public Vector3 ModelOffset;
        public bool UseModelIntersection = false;
        public MyCubeDefinition CubeDefinition;

        // Following group of properties is set by the MyDefinitionManager class
        /// <summary>
        /// Index 0 is first component on stack, the one which is build first and destroyed last.
        /// </summary>
        public Component[] Components;
        public UInt16 CriticalGroup;

        public float CriticalIntegrityRatio;
        public float OwnershipIntegrityRatio;
        public float MaxIntegrity;

        public int? DamageEffectID = null;//defaults to no effect

        public MountPoint[] MountPoints;
        public Dictionary<Vector3I, Dictionary<Vector3I, bool>> IsCubePressurized;
        public MyBlockNavigationDefinition NavigationDefinition;
        public Color Color;
        public List<MyCubeBlockDefinition> Variants = new List<MyCubeBlockDefinition>();
        public MyCubeBlockDefinition UniqueVersion;
        public MyPhysicsOption PhysicsOption;
        public MyStringId? DisplayNameVariant;
        public string BlockPairName;
        public float DeformationRatio;
        public float IntegrityPointsPerSec;

        public string EdgeType;

        public List<BoneInfo> Skeleton;
        public Dictionary<Vector3I, Vector3> Bones;

        public bool IsAirTight = false;

        /// <summary>
        /// Building type - always lower case.
        /// </summary>
        public string BuildType;

        /// <summary>
        /// Allowed cube block directions.
        /// </summary>
        public MyBlockDirection Direction { get; private set; }
        /// <summary>
        /// Allowed cube block rotations.
        /// </summary>
        public MyBlockRotation Rotation { get; private set; }

        public MyDefinitionId[] GeneratedBlockDefinitions;
        public MyStringId GeneratedBlockType;
        public bool IsGeneratedBlock { get { return GeneratedBlockType != MyStringId.NullOrEmpty; } }

        public string[] CompoundTemplates;

        public string MultiBlock;

        /// <summary>
        /// Map from dummy name subblock definition.
        /// </summary>
        public Dictionary<string, MyDefinitionId> SubBlockDefinitions;

        /// <summary>
        /// Array of block stages. Stage represents other block definition which have different UV mapping, mirrored model, etc (stone rounded corner...). Stages can be cycled when building cubes. 
        /// </summary>
        public MyDefinitionId[] BlockStages;

        /// <summary>
        /// Models used when building. They are sorted in ascending order according to their percentage.
        /// </summary>
        public BuildProgressModel[] BuildProgressModels;

        public Vector3I Center
        {
            get { return m_center; }
        }
        private Vector3I m_center;

        public MySymmetryAxisEnum SymmetryX
        {
            get { return m_symmetryX; }
        }
        private MySymmetryAxisEnum m_symmetryX = MySymmetryAxisEnum.None;

        public MySymmetryAxisEnum SymmetryY
        {
            get { return m_symmetryY; }
        }
        private MySymmetryAxisEnum m_symmetryY = MySymmetryAxisEnum.None;

        public MySymmetryAxisEnum SymmetryZ
        {
            get { return m_symmetryZ; }
        }
        private MySymmetryAxisEnum m_symmetryZ = MySymmetryAxisEnum.None;

        private StringBuilder m_displayNameTextCache;
        public float DisassembleRatio;
        public MyAutorotateMode AutorotateMode;

        public string MirroringBlock
        {
            get { return m_mirroringBlock; }
        }
        private string m_mirroringBlock;

        public MySoundPair PrimarySound;

        public int Points;


        public override String DisplayNameText
        {
            get
            {
                if (!DisplayNameVariant.HasValue)
                    return base.DisplayNameText;

                if (m_displayNameTextCache == null)
                    m_displayNameTextCache = new StringBuilder();
                m_displayNameTextCache.Clear();
                return m_displayNameTextCache
                    .Append(base.DisplayNameText)
                    .Append(' ')
                    .Append(MyTexts.GetString(DisplayNameVariant.Value)).ToString();
            }
        }

        public bool GuiVisible { get; private set; }
        public bool Mirrored { get; private set; }
        public bool RandomRotation { get; private set; }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_CubeBlockDefinition;
            MyDebug.AssertDebug(ob != null);

            this.Size                  = ob.Size;
            this.Model                 = ob.Model;
            this.UseModelIntersection  = ob.UseModelIntersection;
            this.CubeSize              = ob.CubeSize;
            this.ModelOffset           = ob.ModelOffset;
            this.BlockTopology         = ob.BlockTopology;
            this.PhysicsOption         = ob.PhysicsOption;
            this.BlockPairName         = ob.BlockPairName;
            this.m_center              = ob.Center ?? ((Size - 1) / 2);
            this.m_symmetryX           = ob.MirroringX;
            this.m_symmetryY           = ob.MirroringY;
            this.m_symmetryZ           = ob.MirroringZ;
            this.DeformationRatio      = ob.DeformationRatio;
            this.EdgeType              = ob.EdgeType;
            this.AutorotateMode        = ob.AutorotateMode;
            this.m_mirroringBlock      = ob.MirroringBlock;
            this.MultiBlock            = ob.MultiBlock;
            this.GuiVisible            = ob.GuiVisible;
            this.Rotation              = ob.Rotation;
            this.Direction             = ob.Direction;
            this.Mirrored              = ob.Mirrored;
            this.RandomRotation        = ob.RandomRotation;
            this.BuildType             = ob.BuildType != null ? ob.BuildType.ToLower() : null;
            this.GeneratedBlockType    = MyStringId.GetOrCompute(ob.GeneratedBlockType != null ? ob.GeneratedBlockType.ToLower() : null);
            if (ob.DamageEffectId != 0)
                this.DamageEffectID = ob.DamageEffectId;

            this.Points = ob.Points;

            this.CompoundTemplates = ob.CompoundTemplates;
            Debug.Assert(this.CompoundTemplates == null || this.CompoundTemplates.Length > 0, "Wrong compound templates, array is empty");

            if (ob.SubBlockDefinitions != null)
            {
                SubBlockDefinitions = new Dictionary<string, MyDefinitionId>();

                foreach (var definition in ob.SubBlockDefinitions)
                {
                    MyDefinitionId defId;
                    if (SubBlockDefinitions.TryGetValue(definition.SubBlock, out defId))
                    {
                        MyDebug.AssertDebug(false, "Subblock definition already defined!");
                        continue;
                    }

                    defId = definition.Id;
                    SubBlockDefinitions.Add(definition.SubBlock, defId);
                }
            }

            if (ob.BlockVariants != null)
            {
                BlockStages = new MyDefinitionId[ob.BlockVariants.Length];

                for (int i = 0; i < ob.BlockVariants.Length; ++i)
                {
                    BlockStages[i] = ob.BlockVariants[i];
                }
            }

            var cubeDef = ob.CubeDefinition;
            if (cubeDef != null)
            {
                MyCubeDefinition tmp = new MyCubeDefinition();
                tmp.CubeTopology = cubeDef.CubeTopology;
                tmp.ShowEdges = cubeDef.ShowEdges;

                var sides = cubeDef.Sides;
                tmp.Model = new string[sides.Length];
                tmp.PatternSize = new Vector2I[sides.Length];
                for (int j = 0; j < sides.Length; ++j)
                {
                    var side = sides[j];
                    tmp.Model[j] = side.Model;
                    tmp.PatternSize[j] = side.PatternSize;
                }
                this.CubeDefinition = tmp;
            }

            var components = ob.Components;
            MyDebug.AssertDebug(components != null);
            MyDebug.AssertDebug(components.Length != 0);
            float mass = 0.0f;
            float criticalIntegrity = 0f;
            float ownershipIntegrity = 0f;
            if (components != null && components.Length != 0)
            {
                Components = new MyCubeBlockDefinition.Component[components.Length];

                float integrity = 0.0f;
                int criticalTypeCounter = 0;
                for (int j = 0; j < components.Length; ++j)
                {
                    var component = components[j];

                    MyCubeBlockDefinition.Component tmp = new MyCubeBlockDefinition.Component()
                    {
                        Count = component.Count,
                        Definition = MyDefinitionManager.Static.GetComponentDefinition(new MyDefinitionId(component.Type, component.Subtype))
                    };

                    if (component.Type == typeof(MyObjectBuilder_Component) && component.Subtype == "Computer")
                    {
                        if (ownershipIntegrity == 0)
                            ownershipIntegrity = integrity + tmp.Definition.MaxIntegrity;
                    }

                    integrity += tmp.Count * tmp.Definition.MaxIntegrity;
                    if (component.Type == ob.CriticalComponent.Type &&
                        component.Subtype == ob.CriticalComponent.Subtype)
                    {
                        if (criticalTypeCounter == ob.CriticalComponent.Index)
                        {
                            CriticalGroup = (UInt16)j;
                            criticalIntegrity = integrity-1;
                        }
                        ++criticalTypeCounter;
                    }

                    mass += tmp.Count * tmp.Definition.Mass;

                    Components[j] = tmp;
                }

                MaxIntegrity = integrity;

                if (ob.MaxIntegrity != 0)
                    MaxIntegrity = ob.MaxIntegrity;

                IntegrityPointsPerSec = MaxIntegrity / ob.BuildTimeSeconds;
                DisassembleRatio = ob.DisassembleRatio;
                if(!MyPerGameSettings.Destruction)
                    Mass = mass;
            }
            else
            {
                if (ob.MaxIntegrity != 0)
                    MaxIntegrity = ob.MaxIntegrity;
            }

            if (criticalIntegrity > MaxIntegrity)
                criticalIntegrity = MaxIntegrity;

            CriticalIntegrityRatio = criticalIntegrity / MaxIntegrity;
            OwnershipIntegrityRatio = ownershipIntegrity / MaxIntegrity;

            if (ob.BuildProgressModels != null)
            {
                ob.BuildProgressModels.Sort((a, b) => a.BuildPercentUpperBound.CompareTo(b.BuildPercentUpperBound));
                this.BuildProgressModels = new BuildProgressModel[ob.BuildProgressModels.Count];
                for (int i = 0; i < BuildProgressModels.Length; ++i)
                {
                    var builderModel = ob.BuildProgressModels[i];
                    if (!string.IsNullOrEmpty(builderModel.File))
                    {
                        this.BuildProgressModels[i] = new BuildProgressModel()
                        {
                            BuildRatioUpperBound = builderModel.BuildPercentUpperBound * CriticalIntegrityRatio,
                            File = builderModel.File,
                            RandomOrientation = builderModel.RandomOrientation
                        };
                    }
                }
            }

            if (ob.GeneratedBlocks != null)
            {
                this.GeneratedBlockDefinitions = new MyDefinitionId[ob.GeneratedBlocks.Length];

                for (int i = 0; i < ob.GeneratedBlocks.Length; ++i)
                {
                    var genBlockId = ob.GeneratedBlocks[i];
                    Debug.Assert(!string.IsNullOrEmpty(genBlockId.SubtypeName));
                    Debug.Assert(!string.IsNullOrEmpty(genBlockId.TypeIdString));

                    this.GeneratedBlockDefinitions[i] = genBlockId;
                }
            }

            Skeleton = ob.Skeleton;
            if (Skeleton != null)
            {
                Bones = new Dictionary<Vector3I,Vector3>(ob.Skeleton.Count);
                foreach (var bone in Skeleton)
                {
                    Bones[bone.BonePosition] = Vector3UByte.Denormalize(bone.BoneOffset, MyDefinitionManager.Static.GetCubeSize(ob.CubeSize));
                }
            }

            IsAirTight = ob.IsAirTight;

            InitMountPoints(ob);
            InitPressurization();

            InitNavigationInfo(ob, ob.NavigationDefinition);

            CheckBuildProgressModels();
            // Components and CriticalComponent will be initialized elsewhere

            this.PrimarySound = new MySoundPair(ob.PrimarySound);
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            MyObjectBuilder_CubeBlockDefinition ob = (MyObjectBuilder_CubeBlockDefinition)base.GetObjectBuilder();

            ob.Size = this.Size;
            ob.Model = this.Model;
            ob.UseModelIntersection = this.UseModelIntersection;
            ob.CubeSize = this.CubeSize;
            ob.ModelOffset = this.ModelOffset;
            ob.BlockTopology = this.BlockTopology;
            ob.PhysicsOption = this.PhysicsOption;
            ob.BlockPairName = this.BlockPairName;
            ob.Center = this.m_center;
            ob.MirroringX = this.m_symmetryX;
            ob.MirroringY = this.m_symmetryY;
            ob.MirroringZ = this.m_symmetryZ;
            ob.DeformationRatio = this.DeformationRatio;
            ob.EdgeType = this.EdgeType;
            ob.AutorotateMode = this.AutorotateMode;
            ob.MirroringBlock = this.m_mirroringBlock;
            ob.MultiBlock = this.MultiBlock;
            ob.GuiVisible = this.GuiVisible;
            ob.Rotation = this.Rotation;
            ob.Direction = this.Direction;
            ob.Mirrored = this.Mirrored;
            ob.BuildType = this.BuildType;
            ob.GeneratedBlockType = this.GeneratedBlockType.ToString();
            ob.DamageEffectId = this.DamageEffectID.HasValue ? this.DamageEffectID.Value : 0;
            ob.CompoundTemplates = this.CompoundTemplates;
            ob.Icon = Icon;
            ob.Points = this.Points;
            //ob.SubBlockDefinitions = SubBlockDefinitions;
            //ob.BlockVariants = BlockVariants;

            
            if (Components != null)
            {
                List<MyObjectBuilder_CubeBlockDefinition.CubeBlockComponent> compObs = new List<MyObjectBuilder_CubeBlockDefinition.CubeBlockComponent>();
                foreach (var comp in Components)
                {
                    var compOb = new MyObjectBuilder_CubeBlockDefinition.CubeBlockComponent();
                    compOb.Count = (ushort)comp.Count;
                    compOb.Type = comp.Definition.Id.TypeId;
                    compOb.Subtype = comp.Definition.Id.SubtypeName;
                    compObs.Add(compOb);
                }

                ob.Components = compObs.ToArray();
            }

            ob.CriticalComponent = new MyObjectBuilder_CubeBlockDefinition.CriticalPart()
            {
                Index = 0,
                Subtype = ob.Components[0].Subtype,
                Type = ob.Components[0].Type
            };

            List<MyObjectBuilder_CubeBlockDefinition.MountPoint> mountPoints = null;
            if (MountPoints != null)
            {
                mountPoints = new List<MyObjectBuilder_CubeBlockDefinition.MountPoint>(); 
                foreach (var mountPoint in MountPoints)
                {
                    MyObjectBuilder_CubeBlockDefinition.MountPoint mpOb = mountPoint.GetObjectBuilder(Size);
                    mountPoints.Add(mpOb);
                }

                ob.MountPoints = mountPoints.ToArray();
            }

            return ob;
        }

        public bool RatioEnoughForOwnership(float ratio)
        {
            return ratio >= OwnershipIntegrityRatio;
        }

        public bool RationEnoughForDamageEffect(float ratio)
        {
            return ratio < CriticalIntegrityRatio;//tied to red line
        }

        /// <summary>
        /// Tells, whether a model change is needed, if the block changes integrity from A to B or vice versa.
        /// </summary>
        public bool ModelChangeIsNeeded(float percentageA, float percentageB)
        {
            if (percentageA > percentageB) return false;
            if (percentageA == 0.0f) return true; // Needed for new models
            if (BuildProgressModels == null) return false; // If no construction models are present

            int i = 0;
            while (i < BuildProgressModels.Length)
            {
                if (percentageA <= BuildProgressModels[i].BuildRatioUpperBound)
                    break;
                ++i;
            }
            if (i >= BuildProgressModels.Length)
                return false;
            if (percentageB > BuildProgressModels[i].BuildRatioUpperBound)
                return true;
            return false;
        }

        public float FinalModelThreshold()
        {
            if (BuildProgressModels == null || BuildProgressModels.Length == 0)
            {
                return 0.0f;
            }
            return BuildProgressModels[BuildProgressModels.Length - 1].BuildRatioUpperBound;
        }
        
        [Conditional("DEBUG")]
        private void CheckBuildProgressModels()
        {
            if (BuildProgressModels == null)
                return;

            foreach (var model in BuildProgressModels)
            {
                Debug.Assert(model != null, string.Format("Build progress model is null"));
                if (model == null)
                    continue;

                var path   = model.File;
                var fsPath = Path.IsPathRooted(path) ? path : Path.Combine(MyFileSystem.ContentPath, path);
                Debug.Assert(MyFileSystem.FileExists(fsPath) || MyFileSystem.FileExists(fsPath + ".mwm"),
                    string.Format("Build progress model does not exists: '{0}'", path));
            }
        }

        private static Matrix[] m_mountPointTransforms = 
        {
            Matrix.CreateFromDir(Vector3.Right,    Vector3.Up)       * Matrix.CreateScale( 1, 1,-1),
            Matrix.CreateFromDir(Vector3.Up,       Vector3.Forward)  * Matrix.CreateScale(-1, 1, 1),
            Matrix.CreateFromDir(Vector3.Forward,  Vector3.Up)       * Matrix.CreateScale(-1, 1, 1),
            Matrix.CreateFromDir(Vector3.Left,     Vector3.Up)       * Matrix.CreateScale( 1, 1,-1),
            Matrix.CreateFromDir(Vector3.Down,     Vector3.Backward) * Matrix.CreateScale(-1, 1, 1),
            Matrix.CreateFromDir(Vector3.Backward, Vector3.Up)       * Matrix.CreateScale(-1, 1, 1),
        };

        private static Vector3[] m_mountPointWallOffsets = 
        {
            new Vector3(1.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 1.0f, 1.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
        };

        internal static void TransformMountPointPosition(ref Vector3 position, int wallIndex, Vector3I cubeSize, out Vector3 result)
        {
            Vector3.Transform(ref position, ref m_mountPointTransforms[wallIndex], out result);
            result += m_mountPointWallOffsets[wallIndex] * cubeSize;
        }

        internal static void UntransformMountPointPosition(ref Vector3 position, int wallIndex, Vector3I cubeSize, out Vector3 result)
        {
            var pos = position - m_mountPointWallOffsets[wallIndex] * cubeSize;
            Matrix inv = Matrix.Invert(m_mountPointTransforms[wallIndex]);
            Vector3.Transform(ref pos, ref inv, out result);
            
        }

        // Mapping from Base6Directions to internal mount point wall indices. The mount point wall indices are differently ordered for historical reasons
        private static int[] m_mountPointWallIndices = { 2, 5, 3, 0, 1, 4 };

        public static int GetMountPointWallIndex(Base6Directions.Direction direction)
        {
            return m_mountPointWallIndices[(int)direction];
        }

        public Vector3 MountPointLocalToBlockLocal(Vector3 coord, Base6Directions.Direction mountPointDirection)
        {
            Vector3 retval = default(Vector3);
            int wallIndex = m_mountPointWallIndices[(int)mountPointDirection];
            TransformMountPointPosition(ref coord, wallIndex, Size, out retval);
            retval -= Center;
            return retval;
        }

        public Vector3 MountPointLocalNormalToBlockLocal(Vector3 normal, Base6Directions.Direction mountPointDirection)
        {
            Vector3 retval = default(Vector3);
            int wallIndex = m_mountPointWallIndices[(int)mountPointDirection];
            Vector3.TransformNormal(ref normal, ref m_mountPointTransforms[wallIndex], out retval);
            return retval;
        }

        public static BlockSideEnum NormalToBlockSide(Vector3I normal)
        {
            for (int i = 0; i < m_mountPointTransforms.Length; i++)
            {
                Vector3I trI = new Vector3I(m_mountPointTransforms[i].Forward);
                if (normal == trI)
                    return (BlockSideEnum)i;
            }

            return BlockSideEnum.Right;
        }

        private void InitMountPoints(MyObjectBuilder_CubeBlockDefinition def)
        {
            if (MountPoints != null)
                return;

            var center = (Size - 1) / 2;

            // Order of block sides: right, top, front, left, bottom, back
            // Right = +X;    Top = +Y; Front = +Z
            //  Left = -X; Bottom = -Y;  Back = -Z
            // Side origins are always in lower left when looking at the side from outside.
            const float OFFSET_CONST = 0.001f;
            const float THICKNESS_HALF = 0.0004f;

            if (!Context.IsBaseGame)
            {
                if (def.MountPoints != null && def.MountPoints.Length == 0)
                {
                    def.MountPoints = null;
                    string msg = "Obsolete default definition of mount points in " + def.Id;
                    MyDefinitionErrors.Add(Context, msg, ErrorSeverity.Warning);
                }
            }

            if (def.MountPoints == null)
            {
                // If there are no mount points defined, cover whole walls.
                List<MountPoint> mps = new List<MountPoint>(6);
                Vector3I normalRight, normalLeft, normalTop, normalBottom, normalFront, normalBack;
                Vector3I.TransformNormal(ref Vector3I.Forward, ref m_mountPointTransforms[0], out normalRight);
                Vector3I.TransformNormal(ref Vector3I.Forward, ref m_mountPointTransforms[1], out normalTop);
                Vector3I.TransformNormal(ref Vector3I.Forward, ref m_mountPointTransforms[2], out normalFront);
                Vector3I.TransformNormal(ref Vector3I.Forward, ref m_mountPointTransforms[3], out normalLeft);
                Vector3I.TransformNormal(ref Vector3I.Forward, ref m_mountPointTransforms[4], out normalBottom);
                Vector3I.TransformNormal(ref Vector3I.Forward, ref m_mountPointTransforms[5], out normalBack);

                // Right and left walls
                {
                    Vector3 start = new Vector3(OFFSET_CONST, OFFSET_CONST, THICKNESS_HALF);
                    Vector3 end = new Vector3(Size.Z - OFFSET_CONST, Size.Y - OFFSET_CONST, -THICKNESS_HALF);
                    Vector3 s1, s2, e1, e2;
                    TransformMountPointPosition(ref start, 0, Size, out s1);
                    TransformMountPointPosition(ref end, 0, Size, out e1);
                    TransformMountPointPosition(ref start, 3, Size, out s2);
                    TransformMountPointPosition(ref end, 3, Size, out e2);
                    mps.Add(new MountPoint() { Start = s1, End = e1, Normal = normalRight });
                    mps.Add(new MountPoint() { Start = s2, End = e2, Normal = normalLeft });
                }

                // Top and bottom walls
                {
                    Vector3 start = new Vector3(OFFSET_CONST, OFFSET_CONST, THICKNESS_HALF);
                    Vector3 end = new Vector3(Size.X - OFFSET_CONST, Size.Z - OFFSET_CONST, -THICKNESS_HALF);
                    Vector3 s1, s2, e1, e2;
                    TransformMountPointPosition(ref start, 1, Size, out s1);
                    TransformMountPointPosition(ref end, 1, Size, out e1);
                    TransformMountPointPosition(ref start, 4, Size, out s2);
                    TransformMountPointPosition(ref end, 4, Size, out e2);
                    mps.Add(new MountPoint() { Start = s1, End = e1, Normal = normalTop });
                    mps.Add(new MountPoint() { Start = s2, End = e2, Normal = normalBottom });
                }

                // Front and back walls
                {
                    Vector3 start = new Vector3(OFFSET_CONST, OFFSET_CONST, THICKNESS_HALF);
                    Vector3 end = new Vector3(Size.X - OFFSET_CONST, Size.Y - OFFSET_CONST, -THICKNESS_HALF);
                    Vector3 s1, s2, e1, e2;
                    TransformMountPointPosition(ref start, 2, Size, out s1);
                    TransformMountPointPosition(ref end, 2, Size, out e1);
                    TransformMountPointPosition(ref start, 5, Size, out s2);
                    TransformMountPointPosition(ref end, 5, Size, out e2);
                    mps.Add(new MountPoint() { Start = s1, End = e1, Normal = normalFront });
                    mps.Add(new MountPoint() { Start = s2, End = e2, Normal = normalBack });
                }

                MountPoints = mps.ToArray();
                return;
            }
            else
            {
                var mpBuilders = def.MountPoints;
                MountPoints = new MountPoint[mpBuilders.Length];
                for (int i = 0; i < MountPoints.Length; ++i)
                {
                    var mpBuilder = mpBuilders[i]; // 'mp' stands for mount point
                    // I shrink mounts points a little to avoid overlaps when they are very close.
                    var mpStart  = new Vector3((Vector2)mpBuilder.Start + OFFSET_CONST, THICKNESS_HALF);
                    var mpEnd    = new Vector3((Vector2)mpBuilder.End - OFFSET_CONST, -THICKNESS_HALF);
                    var sideIdx  = (int)mpBuilder.Side;
                    var mpNormal = Vector3I.Forward;
                    TransformMountPointPosition(ref mpStart, sideIdx, Size, out mpStart);
                    TransformMountPointPosition(ref mpEnd, sideIdx, Size, out mpEnd);
                    Vector3I.TransformNormal(ref mpNormal, ref m_mountPointTransforms[sideIdx], out mpNormal);
                    MountPoints[i].Start          = mpStart;
                    MountPoints[i].End            = mpEnd;
                    MountPoints[i].Normal         = mpNormal;
                    MountPoints[i].ExclusionMask  = mpBuilder.ExclusionMask;
                    MountPoints[i].PropertiesMask = mpBuilder.PropertiesMask;
                }
            }
        }

        public void InitPressurization()
        {
            IsCubePressurized = new Dictionary<Vector3I, Dictionary<Vector3I, bool>>();

            for (int i = 0; i < Size.X; i++)
                for (int j = 0; j < Size.Y; j++)
                    for (int k = 0; k < Size.Z; k++)
                    {
                        Vector3 originalStartOffset = new Vector3(i, j, k);
                        Vector3 originalEndOffset = new Vector3(i, j, k) + Vector3.One;

                        Vector3I intOffset = new Vector3I(i, j, k);

                        IsCubePressurized[intOffset] = new Dictionary<Vector3I, bool>();

                        foreach (var direction in Base6Directions.IntDirections)
                        {
                            var normal = direction;

                            IsCubePressurized[intOffset][normal] = false;

                            if (normal.X == 1 && i != Size.X - 1)
                                continue;
                            if (normal.X == -1 && i != 0)
                                continue;

                            if (normal.Y == 1 && j != Size.Y - 1)
                                continue;
                            if (normal.Y == -1 && j != 0)
                                continue;

                            if (normal.Z == 1 && k != Size.Z - 1)
                                continue;
                            if (normal.Z == -1 && k != 0)
                                continue;

                            foreach (var mountPoint in MountPoints)
                            {
                                if (normal == mountPoint.Normal)
                                {
                                    int wallIndex = MyCubeBlockDefinition.GetMountPointWallIndex(Base6Directions.GetDirection(ref normal));
                                    Vector3I blockSize = Size;
                                    Vector3 originalStart = mountPoint.Start;
                                    Vector3 originalEnd = mountPoint.End;
                                    Vector3 start, end;
                                    MyCubeBlockDefinition.UntransformMountPointPosition(ref originalStart, wallIndex, blockSize, out start);
                                    MyCubeBlockDefinition.UntransformMountPointPosition(ref originalEnd, wallIndex, blockSize, out end);
                                    Vector3 endOffset;
                                    Vector3 startOffset;
                                    MyCubeBlockDefinition.UntransformMountPointPosition(ref originalStartOffset, wallIndex, blockSize, out startOffset);
                                    MyCubeBlockDefinition.UntransformMountPointPosition(ref originalEndOffset, wallIndex, blockSize, out endOffset);

                                    Vector3 eo = new Vector3(Math.Max(startOffset.X, endOffset.X), Math.Max(startOffset.Y, endOffset.Y), Math.Max(startOffset.Z, endOffset.Z));
                                    Vector3 so = new Vector3(Math.Min(startOffset.X, endOffset.X), Math.Min(startOffset.Y, endOffset.Y), Math.Min(startOffset.Z, endOffset.Z));

                                    if (start.X - 0.05 <= so.X && end.X + 0.05 > eo.X &&
                                        start.Y - 0.05 <= so.Y && end.Y + 0.05 > eo.Y)
                                    {
                                        IsCubePressurized[intOffset][normal] = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
        }

        public void InitNavigationInfo(MyObjectBuilder_CubeBlockDefinition blockDef, string infoSubtypeId)
        {
            if (MyPerGameSettings.EnableAi)
            {
                if (infoSubtypeId == "Default")
                {
                    MyDefinitionManager.Static.SetDefaultNavDef(this);
                }
                else
                {
                    MyDefinitionId id = new MyDefinitionId(typeof(MyObjectBuilder_BlockNavigationDefinition), infoSubtypeId);
                    MyDefinitionManager.Static.TryGetDefinition(id, out NavigationDefinition);
                }

                if (NavigationDefinition != null && NavigationDefinition.Mesh != null)
                    NavigationDefinition.Mesh.MakeStatic();
            }
        }

        public bool ContainsComputer()
        {
            var count = Components.Count((x) => (x.Definition.Id.TypeId == typeof(MyObjectBuilder_Component)) && (x.Definition.Id.SubtypeName == "Computer"));
            return count > 0;
        }

        public MyCubeBlockDefinition GetGeneratedBlockDefinition(MyStringId additionalModelType)
        {
            if (GeneratedBlockDefinitions == null)
                return null;

            foreach (var genBlockDefId in GeneratedBlockDefinitions)
            {
                MyCubeBlockDefinition genBlockDef;
                MyDefinitionManager.Static.TryGetCubeBlockDefinition(genBlockDefId, out genBlockDef);
                if (genBlockDef != null && genBlockDef.IsGeneratedBlock && genBlockDef.GeneratedBlockType == additionalModelType)
                    return genBlockDef;
            }

            return null;
        }
    }
}
