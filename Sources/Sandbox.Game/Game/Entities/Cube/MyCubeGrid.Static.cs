using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;

using VRage.Utils;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender;
using VRage;
using Sandbox.Graphics.GUI;
using System.Text;

using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Plugins;
using System.Reflection;
using Sandbox.Common.Components;
using Sandbox.Game.Entities;
using VRage.Voxels;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Localization;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using VRage.Library.Utils;
using VRage.Import;
using MyFileSystem = VRage.FileSystem.MyFileSystem;
using VRage.Components;
using VRage.ObjectBuilders;
using Sandbox.Game.Entities.Character;

namespace Sandbox.Game.Entities
{
    partial class MyCubeGrid
    {
        class AreaConnectivityTest : IMyGridConnectivityTest
        {
            Dictionary<Vector3I, Vector3I> m_lookup = new Dictionary<Vector3I, Vector3I>();
            MyBlockOrientation m_orientation;
            MyCubeBlockDefinition m_definition;
            Vector3I m_posInGrid;
            Vector3I m_blockMin;
            Vector3I m_blockMax;
            Vector3I m_stepDelta;

            public void Initialize(ref MyBlockBuildArea area, MyCubeBlockDefinition definition)
            {
                m_definition = definition;
                m_orientation = new MyBlockOrientation(area.OrientationForward, area.OrientationUp);
                m_posInGrid = area.PosInGrid;
                m_blockMin = area.BlockMin;
                m_blockMax = area.BlockMax;
                m_stepDelta = area.StepDelta;
                m_lookup.Clear();
            }

            public void AddBlock(Vector3UByte offset)
            {
                Vector3I pos = m_posInGrid + offset * m_stepDelta;
                Vector3I cube;
                for (cube.X = m_blockMin.X; cube.X <= m_blockMax.X; cube.X++)
                {
                    for (cube.Y = m_blockMin.Y; cube.Y <= m_blockMax.Y; cube.Y++)
                    {
                        for (cube.Z = m_blockMin.Z; cube.Z <= m_blockMax.Z; cube.Z++)
                        {
                            m_lookup.Add(pos + cube, pos);
                        }
                    }
                }
            }

            public void GetConnectedBlocks(Vector3I minI, Vector3I maxI, Dictionary<Vector3I, ConnectivityResult> outOverlappedCubeBlocks)
            {
                Vector3I cube;
                for (cube.X = minI.X; cube.X <= maxI.X; cube.X++)
                {
                    for (cube.Y = minI.Y; cube.Y <= maxI.Y; cube.Y++)
                    {
                        for (cube.Z = minI.Z; cube.Z <= maxI.Z; cube.Z++)
                        {
                            Vector3I pos;
                            if (m_lookup.TryGetValue(cube, out pos) && !outOverlappedCubeBlocks.ContainsKey(pos))
                            {
                                outOverlappedCubeBlocks.Add(pos, new ConnectivityResult() { Definition = m_definition, FatBlock = null, Position = pos, Orientation = m_orientation });
                            }
                        }
                    }
                }
            }
        }

        private const double GRID_PLACING_AREA_FIX_VALUE = 0.11;
        const string EXPORT_DIRECTORY = "ExportedModels";
        const string SOURCE_DIRECTORY = "SourceModels";
        private static List<MyObjectBuilder_CubeGrid[]> m_prefabs = new List<MyObjectBuilder_CubeGrid[]>();
        private static List<MyEntity> m_tmpResultList = new List<MyEntity>();
        static int materialID = 0;
        static Vector2 tumbnailMultiplier = new Vector2();

        private static float m_maxDimensionPreviousRow = 0.0f;
        private static Vector3D m_newPositionForPlacedObject = new Vector3D(0, 0, 0);
        private const int m_numRowsForPlacedObjects = 4;

        public static bool ShowSenzorGizmos { get; set; }
        public static bool ShowGravityGizmos { get; set; }
        public static bool ShowCenterOfMass {get; set;}
        public static bool ShowGridPivot { get; set; }
        public static bool ShowAntennaGizmos { get; set; }
        public static bool ShowStructuralIntegrity { get; set; }

        public static HashSet<MyCubeGrid> StaticGrids                                   = new HashSet<MyCubeGrid>();
        private static List<MyLineSegmentOverlapResult<MyEntity>> m_lineOverlapList = new List<MyLineSegmentOverlapResult<MyEntity>>();
        private static List<HkRigidBody> m_physicsBoxQueryList                      = new List<HkRigidBody>();
        private static Dictionary<Vector3I, MySlimBlock> m_tmpBoneSet               = new Dictionary<Vector3I, MySlimBlock>(Vector3I.Comparer);
        private static MyDisconnectHelper m_disconnectHelper                        = new MyDisconnectHelper();
        private static List<NeighborOffsetIndex> m_neighborOffsetIndices            = new List<NeighborOffsetIndex>(26);
        private static List<float> m_neighborDistances                              = new List<float>(26);
        private static List<Vector3I> m_neighborOffsets                             = new List<Vector3I>(26);

        private static MyRandom m_deformationRng = new MyRandom();

        // Caching structures to avoid allocations in functions.
        private static readonly List<Vector3I> m_cacheRayCastCells = new List<Vector3I>();
        private static readonly Dictionary<Vector3I, ConnectivityResult> m_cacheNeighborBlocks = new Dictionary<Vector3I, ConnectivityResult>();
        private static readonly List<MyCubeBlockDefinition.MountPoint> m_cacheMountPointsA = new List<MyCubeBlockDefinition.MountPoint>();
        private static readonly List<MyCubeBlockDefinition.MountPoint> m_cacheMountPointsB = new List<MyCubeBlockDefinition.MountPoint>();
        private static readonly List<MyPhysics.HitInfo> m_tmpHitList = new List<MyPhysics.HitInfo>();
        private static readonly HashSet<Vector3UByte> m_tmpAreaMountpointPass = new HashSet<Vector3UByte>();
        private static AreaConnectivityTest m_areaOverlapTest = new AreaConnectivityTest();

        private static readonly List<Vector3I> m_tmpCubeNeighbours = new List<Vector3I>();

        private static readonly Type m_gridSystemsType = ChooseGridSystemsType();


        public static void GetCubeParts(
            MyCubeBlockDefinition block,
            Vector3I inputPosition,
            Matrix rotation,
            float gridSize,
            List<string> outModels,
            List<MatrixD> outLocalMatrices,
            List<Vector3> outLocalNormals,
            List<Vector2> outPatternOffsets)
        {
            // CH:TODO: Is rotation argument really needed as a Matrix? It should suffice for it to be MyBlockOrientation

            outModels.Clear();
            outLocalMatrices.Clear();
            outLocalNormals.Clear();
            outPatternOffsets.Clear();

            if (block.CubeDefinition == null)
                return;

            Base6Directions.Direction forward = Base6Directions.GetDirection(Vector3I.Round(rotation.Forward));
            Base6Directions.Direction up = Base6Directions.GetDirection(Vector3I.Round(rotation.Up));
            MyCubeGridDefinitions.GetTopologyUniqueOrientation(block.CubeDefinition.CubeTopology, new MyBlockOrientation(forward, up)).GetMatrix(out rotation);

            MyTileDefinition[] tiles = MyCubeGridDefinitions.GetCubeTiles(block);

            int count = tiles.Length;
            int start = 0;
            int avoidZeroMirrorOffset = 32768;
            float epsilon = 0.01f;

            for (int i = 0; i < count; i++)
            {
                var entry = tiles[start + i];
                var localMatrix = (MatrixD)entry.LocalMatrix * rotation;
                var localNormal = Vector3.Transform(entry.Normal, rotation.GetOrientation());

                var position = inputPosition;
                if(block.CubeDefinition.CubeTopology == MyCubeTopology.Slope2Base)
                {
                    var addition = new Vector3I(Vector3.Sign(localNormal.MaxAbsComponent()));
                    position += addition;
                }
                

                string modelPath = block.CubeDefinition.Model[i];
                Vector2I patternSize = block.CubeDefinition.PatternSize[i];

                int scale = (int)MyModels.GetModelOnlyData(modelPath).PatternScale;

                patternSize = new Vector2I(patternSize.X * scale, patternSize.Y * scale);

                const float sinConst = 10;

                int u = 0;
                int v = 0;

                float yAxis = Vector3.Dot(Vector3.UnitY, localNormal);
                float xAxis = Vector3.Dot(Vector3.UnitX, localNormal);
                float zAxis = Vector3.Dot(Vector3.UnitZ, localNormal);
                if (MyUtils.IsZero(Math.Abs(yAxis) - 1, epsilon))
                {
                    int patternRow = (position.X + avoidZeroMirrorOffset) / patternSize.Y;
                    int offset = (MyMath.Mod(patternRow + (int)(patternRow * Math.Sin(patternRow * sinConst)), patternSize.X));
                    u = MyMath.Mod(position.Z + position.Y + offset + avoidZeroMirrorOffset, patternSize.X);
                    v = MyMath.Mod(position.X + avoidZeroMirrorOffset, patternSize.Y);
                    if (Math.Sign(yAxis) == 1)
                        v = (patternSize.Y - 1) - v;
                }
                else if (MyUtils.IsZero(Math.Abs(xAxis) - 1, epsilon))
                {
                    int patternRow = (position.Z + avoidZeroMirrorOffset) / patternSize.Y;
                    int offset = (MyMath.Mod(patternRow + (int)(patternRow * Math.Sin(patternRow * sinConst)), patternSize.X));
                    u = MyMath.Mod(position.X + position.Y + offset + avoidZeroMirrorOffset, patternSize.X);
                    v = MyMath.Mod(position.Z + avoidZeroMirrorOffset, patternSize.Y);
                    if (Math.Sign(xAxis) == 1)
                        v = (patternSize.Y - 1) - v;
                }
                else if (MyUtils.IsZero(Math.Abs(zAxis) - 1, epsilon))
                {
                    int patternRow = (position.Y + avoidZeroMirrorOffset) / patternSize.Y;
                    int offset = (MyMath.Mod(patternRow + (int)(patternRow * Math.Sin(patternRow * sinConst)), patternSize.X));
                    u = MyMath.Mod(position.X + offset + avoidZeroMirrorOffset, patternSize.X);
                    v = MyMath.Mod(position.Y + avoidZeroMirrorOffset, patternSize.Y);
                    if (Math.Sign(zAxis) == 1)
                        u = (patternSize.X - 1) - u;

                }
                else if (MyUtils.IsZero(xAxis, epsilon))
                {   //slope in YZ
                    u = MyMath.Mod(position.X + avoidZeroMirrorOffset, patternSize.X);
                    v = MyMath.Mod(position.Z + avoidZeroMirrorOffset, patternSize.Y);

                    if (Math.Sign(zAxis) == -1)
                    {
                        if (Math.Sign(yAxis) == 1)
                        {
                            //v = (patternSize.Y - 1) - v;
                            //u = (patternSize.X - 1) - u;
                        }
                        else
                        {
                            // u = (patternSize.X - 1) - u;
                            v = (patternSize.Y - 1) - v;
                        }
                    }
                    else
                    {
                        if (Math.Sign(yAxis) == -1)
                        {
                            //u = (patternSize.X - 1) - u;
                            v = (patternSize.Y - 1) - v;
                        }
                        else
                        {
                            //u = (patternSize.X - 1) - u;
                            //  v = (patternSize.Y - 1) - v;
                        }
                    }
                }
                else if (MyUtils.IsZero(zAxis, epsilon))
                {   //slope in XY
                    u = MyMath.Mod(position.Z + avoidZeroMirrorOffset, patternSize.X);
                    v = MyMath.Mod(position.Y + avoidZeroMirrorOffset, patternSize.Y);
                    if (Math.Sign(xAxis) == 1)
                    {
                        if (Math.Sign(yAxis) == 1)
                        {
                            //u = (patternSize.X - 1) - u;
                            //v = (patternSize.Y - 1) - v;
                        }
                        else
                        {
                            u = (patternSize.X - 1) - u;
                            v = (patternSize.Y - 1) - v;
                        }
                    }
                    else
                    {
                        if (Math.Sign(yAxis) == 1)
                        {
                            u = (patternSize.X - 1) - u;
                            // v = (patternSize.Y - 1) - v;
                        }
                        else
                        {
                            // u = (patternSize.X - 1) - u;
                            v = (patternSize.Y - 1) - v;
                        }
                    }
                }
                else if (MyUtils.IsZero(yAxis, epsilon))
                {   //slope in XZ
                    u = MyMath.Mod(position.Y + avoidZeroMirrorOffset, patternSize.X);
                    v = MyMath.Mod(position.Z + avoidZeroMirrorOffset, patternSize.Y);
                    if (Math.Sign(zAxis) == -1)
                    {
                        if (Math.Sign(xAxis) == 1)
                        {
                            //u = (patternSize.X - 1) - u;
                            v = (patternSize.Y - 1) - v;
                        }
                        else
                        {
                            u = (patternSize.X - 1) - u;
                            v = (patternSize.Y - 1) - v;
                        }
                    }
                    else
                    {
                        if (Math.Sign(xAxis) == 1)
                        {
                            u = (patternSize.X - 1) - u;
                            //v = (patternSize.Y - 1) - v;
                        }
                        else
                        {
                            //u = (patternSize.X - 1) - u;
                            // v = (patternSize.Y - 1) - v;
                        }
                    }
                }

                localMatrix.Translation = inputPosition * gridSize;

                if (entry.DontOffsetTexture)
                {
                    u = 0;
                    v = 0;
                }

                Vector2 uv = new Vector2(u, v);
                Vector2 patternOffset = uv / patternSize;

                outPatternOffsets.Add(patternOffset);
                outModels.Add(modelPath);
                outLocalMatrices.Add(localMatrix);
                outLocalNormals.Add(localNormal);
            }
        }

        public static void CheckAreaConnectivity(MyCubeGrid grid, ref MyBlockBuildArea area, List<Vector3UByte> validOffsets, HashSet<Vector3UByte> resultFailList)
        {
            try
            {
                var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(area.DefinitionId) as MyCubeBlockDefinition;
                if (definition == null)
                {
                    Debug.Fail("Block definition not found");
                    return;
                }

                Quaternion orientation = Base6Directions.GetOrientation(area.OrientationForward, area.OrientationUp);
                Vector3I stepDir = area.StepDelta;

                // Step 1: Add blocks which are connected directly to existing grid
                for (int i = validOffsets.Count - 1; i >= 0; i--)
                {
                    Vector3I center = area.PosInGrid + validOffsets[i] * stepDir;

                    if (MyCubeGrid.CheckConnectivity(grid, definition, ref orientation, ref center))
                    {
                        m_tmpAreaMountpointPass.Add(validOffsets[i]);
                        validOffsets.RemoveAtFast(i);
                    }
                }

                // Step 2: Add remaining blocks which are connected to any newly added block
                m_areaOverlapTest.Initialize(ref area, definition);
                foreach (var block in m_tmpAreaMountpointPass) m_areaOverlapTest.AddBlock(block);

                int prevCount = int.MaxValue;
                while (validOffsets.Count > 0 && validOffsets.Count < prevCount)
                {
                    prevCount = validOffsets.Count;

                    for (int i = validOffsets.Count - 1; i >= 0; i--)
                    {
                        Vector3I center = area.PosInGrid + validOffsets[i] * stepDir;

                        if (MyCubeGrid.CheckConnectivity(m_areaOverlapTest, definition, ref orientation, ref center))
                        {
                            m_tmpAreaMountpointPass.Add(validOffsets[i]);
                            m_areaOverlapTest.AddBlock(validOffsets[i]);
                            validOffsets.RemoveAtFast(i);
                        }
                    }
                }

                // Step 3: Remaining blocks failed
                foreach (var item in validOffsets)
                {
                    resultFailList.Add(item);
                }

                validOffsets.Clear();
                validOffsets.AddHashset(m_tmpAreaMountpointPass);
            }
            finally
            {
                m_tmpAreaMountpointPass.Clear();
            }
        }
        
        public static bool CheckMergeConnectivity(MyCubeGrid hitGrid, MyCubeGrid gridToMerge, Vector3I gridOffset)
        {
            // CH: Beware, this funtion seems horribly inefficient! Think twice before using it (e.g. don't use it in a 10000x loop) or optimize it :-)
            MatrixI mergeTransform = hitGrid.CalculateMergeTransform(gridToMerge, gridOffset);
            Quaternion mergeOri;
            mergeTransform.GetBlockOrientation().GetQuaternion(out mergeOri);

            foreach (var block in gridToMerge.GetBlocks())
            {
                Quaternion ori;
                Vector3I pos = Vector3I.Transform(block.Position, mergeTransform);
                block.Orientation.GetQuaternion(out ori);
                //Matrix mergeGridMatrix = Matrix.CreateFromQuaternion(mergeOri);
                //Matrix oriMatrix = Matrix.CreateFromQuaternion(ori);
                //Matrix result = oriMatrix * mergeGridMatrix;
                //Quaternion quatFromMatrix = Quaternion.CreateFromRotationMatrix(result);
                // Seems that quaternion multiplication is reversed to matrix multiplication!
                ori = mergeOri * ori;
                if (MyCubeGrid.CheckConnectivity(hitGrid, block.BlockDefinition, ref ori, ref pos))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Performs check whether cube block given by its definition, rotation and position is connected to some other
        /// block in a given grid.
        /// </summary>
        /// <param name="grid">Grid in which the check is performed.</param>
        /// <param name="def">Definition of cube block for checking.</param>
        /// <param name="rotation">Rotation of the cube block within grid.</param>
        /// <param name="position">Position of the cube block within grid.</param>
        /// <returns>True when there is a connectable neighbor connected by a mount point, otherwise false.</returns>
        internal static bool CheckConnectivity(IMyGridConnectivityTest grid, MyCubeBlockDefinition def, ref Quaternion rotation, ref Vector3I position)
        {
            ProfilerShort.Begin("MyCubeBuilder.CheckMountPoints");
            try
            {
                var mountPoints = def.MountPoints;
                if (mountPoints == null)
                    return false;

                var center = def.Center;
                var size = def.Size;
                Vector3I rotatedSize;
                Vector3I rotatedCenter;
                Vector3I.Transform(ref center, ref rotation, out rotatedCenter);
                Vector3I.Transform(ref size, ref rotation, out rotatedSize);

                for (int i = 0; i < mountPoints.Length; ++i)
                {
                    var thisMountPoint = mountPoints[i];
                    Vector3 centeredStart = thisMountPoint.Start - center;
                    Vector3 centeredEnd = thisMountPoint.End - center;
                    if (MyFakes.ENABLE_TEST_BLOCK_CONNECTIVITY_CHECK)
                    {
                        // This code is used to avoid mixed start, end values, overlapped values on sides. So as result we need precise neighbour(s). Not neighbours touched on edges.

                        // Start and end sometimes have exchanged values
                        Vector3 start = Vector3.Min(thisMountPoint.Start, thisMountPoint.End);
                        Vector3 end = Vector3.Max(thisMountPoint.Start, thisMountPoint.End);

                        // Clamp only overlapped values on sides not thickness of aabb of mount point
                        Vector3I clampMask = Vector3I.One - Vector3I.Abs(thisMountPoint.Normal);
                        Vector3I invClampMask = Vector3I.One - clampMask;
                        Vector3 clampedStart = invClampMask * start + Vector3.Clamp(start, Vector3.Zero, size) * clampMask + 0.001f * clampMask;
                        Vector3 clampedEnd = invClampMask * end + Vector3.Clamp(end, Vector3.Zero, size) * clampMask - 0.001f * clampMask;

                        centeredStart = clampedStart - center;
                        centeredEnd = clampedEnd - center;
                    }

                    var centeredStartI = Vector3I.Floor(centeredStart);
                    var centeredEndI = Vector3I.Floor(centeredEnd);

                    Vector3 rotatedStart, rotatedEnd;
                    Vector3.Transform(ref centeredStart, ref rotation, out rotatedStart);
                    Vector3.Transform(ref centeredEnd, ref rotation, out rotatedEnd);

                    Vector3I rotatedStartICorrect, rotatedEndICorrect;
                    Vector3I.Transform(ref centeredStartI, ref rotation, out rotatedStartICorrect);
                    Vector3I.Transform(ref centeredEndI, ref rotation, out rotatedEndICorrect);

                    // Correction of rotation. Normally we perform computations in integers and so these are not needed, but when
                    // transforming floats, we can end up rotating eg. 0.5f to -0.5f which, after Floor operation, would be like rotation of 0 to -1 (ie. wrong).
                    // By rotating both floored and floating point versions, I can find out what change occured and handle it accordingly.
                    var rotatedStartI = Vector3I.Floor(rotatedStart);
                    var rotatedEndI = Vector3I.Floor(rotatedEnd);
                    var correctionStart = rotatedStartICorrect - rotatedStartI;
                    var correctionEnd = rotatedEndICorrect - rotatedEndI;
                    rotatedStart += correctionStart;
                    rotatedEnd += correctionEnd;

                    Vector3 gridPosStart = position + rotatedStart;
                    Vector3 gridPosEnd = position + rotatedEnd;

                    m_cacheNeighborBlocks.Clear();

                    var currentMin = Vector3.Min(gridPosStart, gridPosEnd);
                    var currentMax = Vector3.Max(gridPosStart, gridPosEnd);

                    var minI = Vector3I.Floor(currentMin);
                    var maxI = Vector3I.Floor(currentMax);
                    grid.GetConnectedBlocks(minI, maxI, m_cacheNeighborBlocks);
                    //MyCubeBuilder.Static.GetOverlappedGizmoBlocks(minI, maxI, m_cacheNeighborBlocks);                   

                    if (m_cacheNeighborBlocks.Count == 0)
                        continue;

                    Vector3I transformedNormal;
                    Vector3I.Transform(ref thisMountPoint.Normal, ref rotation, out transformedNormal);
                    Debug.Assert(transformedNormal.RectangularLength() == 1);

                    minI -= transformedNormal;
                    maxI -= transformedNormal;

                    Vector3I transformedNormalNegative = -transformedNormal;

                    foreach (var neighbor in m_cacheNeighborBlocks.Values)
                    {
                        if (neighbor.Position == position)
                        {
                            if (MyFakes.ENABLE_COMPOUND_BLOCKS)
                            {
                                // If this neighbor does not want to connect, check another one
                                if (neighbor.FatBlock != null && neighbor.FatBlock.CheckConnectionAllowed && !neighbor.FatBlock.ConnectionAllowed(ref minI, ref maxI, ref transformedNormalNegative, def))
                                    continue;

                                if (neighbor.FatBlock is MyCompoundCubeBlock)
                                {
                                    MyCompoundCubeBlock compoundBlock = neighbor.FatBlock as MyCompoundCubeBlock;
                                    foreach (var blockInCompound in compoundBlock.GetBlocks())
                                    {
                                        if (CheckNeighborMountPointsForCompound(currentMin, currentMax, thisMountPoint, ref transformedNormal, def, neighbor.Position, blockInCompound.BlockDefinition, blockInCompound.Orientation,
                                            m_cacheMountPointsA))
                                            return true;
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {

                            // If this neighbor does not want to connect, check another one
                            if (neighbor.FatBlock != null && neighbor.FatBlock.CheckConnectionAllowed && !neighbor.FatBlock.ConnectionAllowed(ref minI, ref maxI, ref transformedNormalNegative, def))
                                continue;

                            if (neighbor.FatBlock is MyCompoundCubeBlock)
                            {
                                MyCompoundCubeBlock compoundBlock = neighbor.FatBlock as MyCompoundCubeBlock;
                                foreach (var blockInCompound in compoundBlock.GetBlocks())
                                {
                                    if (CheckNeighborMountPoints(currentMin, currentMax, thisMountPoint, ref transformedNormal, def, neighbor.Position, blockInCompound.BlockDefinition, blockInCompound.Orientation,
                                        m_cacheMountPointsA))
                                        return true;
                                }
                            }
                            else
                            {
                                if (CheckNeighborMountPoints(currentMin, currentMax, thisMountPoint, ref transformedNormal, def, neighbor.Position, neighbor.Definition, neighbor.Orientation, m_cacheMountPointsA))
                                    return true;
                            }
                        }
                    }
                }

                return false;
            }
            finally
            {
                m_cacheNeighborBlocks.Clear();
                ProfilerShort.End();
            }
        }

        /// <summary>
        /// Performs check whether small cube block given by its definition, rotation  can be connected to large grid. 
        /// Function checks whether a mount point on placed block exists in opposite direction than addNomal.
        /// </summary>
        /// <param name="grid">Grid in which the check is performed.</param>
        /// <param name="def">Definition of small cube block for checking.</param>
        /// <param name="rotation">Rotation of the small cube block.</param>
        /// <param name="addNormal">Grid hit normal.</param>
        /// <returns>True when small block can be connected, otherwise false.</returns>
        internal static bool CheckConnectivitySmallBlockToLargeGrid(MyCubeGrid grid, MyCubeBlockDefinition def, ref Quaternion rotation, ref Vector3I addNormal)
        {
            Debug.Assert(grid.GridSizeEnum == MyCubeSize.Large);
            Debug.Assert(def.CubeSize  == MyCubeSize.Small);

            ProfilerShort.Begin("MyCubeBuilder.CheckMountPoints");
            try
            {
                var mountPoints = def.MountPoints;
                if (mountPoints == null)
                    return false;

                for (int i = 0; i < mountPoints.Length; ++i)
                {
                    var thisMountPoint = mountPoints[i];

                    Vector3I transformedNormal;
                    Vector3I.Transform(ref thisMountPoint.Normal, ref rotation, out transformedNormal);
                    Debug.Assert(transformedNormal.RectangularLength() == 1);

                    if (addNormal == -transformedNormal)
                        return true;
                }

                return false;
            }
            finally
            {
                m_cacheNeighborBlocks.Clear();
                ProfilerShort.End();
            }
        }

        public static bool CheckNeighborMountPoints(Vector3 currentMin, Vector3 currentMax, MyCubeBlockDefinition.MountPoint thisMountPoint, ref Vector3I thisMountPointTransformedNormal,
            MyCubeBlockDefinition myDefinition, Vector3I neighborPosition, MyCubeBlockDefinition neighborDefinition, MyBlockOrientation neighborOrientation, 
            List<MyCubeBlockDefinition.MountPoint> otherMountPoints)
        {
            var currentBox = new BoundingBox(currentMin - neighborPosition, currentMax - neighborPosition);

            TransformMountPoints(otherMountPoints, neighborDefinition, ref neighborOrientation);

            foreach (var otherMountPoint in otherMountPoints)
            {
                // Skip mount points which exclude themselves (are not allowed to touch).
                if (((thisMountPoint.ExclusionMask & otherMountPoint.PropertiesMask) != 0 ||
                      (thisMountPoint.PropertiesMask & otherMountPoint.ExclusionMask) != 0) &&
                    myDefinition.Id != neighborDefinition.Id)
                    continue;

                if (MyFakes.ENABLE_TEST_BLOCK_CONNECTIVITY_CHECK && (thisMountPointTransformedNormal + otherMountPoint.Normal != Vector3I.Zero))
                    continue;

                var otherBox = new BoundingBox(Vector3.Min(otherMountPoint.Start, otherMountPoint.End), Vector3.Max(otherMountPoint.Start, otherMountPoint.End));
                if (currentBox.Intersects(otherBox))
                    return true;
            }

            return false;
        }

        public static bool CheckNeighborMountPointsForCompound(Vector3 currentMin, Vector3 currentMax, MyCubeBlockDefinition.MountPoint thisMountPoint, ref Vector3I thisMountPointTransformedNormal,
            MyCubeBlockDefinition myDefinition, Vector3I neighborPosition, MyCubeBlockDefinition neighborDefinition, MyBlockOrientation neighborOrientation,
            List<MyCubeBlockDefinition.MountPoint> otherMountPoints)
        {
            var currentBox = new BoundingBox(currentMin - neighborPosition, currentMax - neighborPosition);

            TransformMountPoints(otherMountPoints, neighborDefinition, ref neighborOrientation);

            foreach (var otherMountPoint in otherMountPoints)
            {
                // Skip mount points which exclude themselves (are not allowed to touch).
                if (((thisMountPoint.ExclusionMask & otherMountPoint.PropertiesMask) != 0 ||
                      (thisMountPoint.PropertiesMask & otherMountPoint.ExclusionMask) != 0) &&
                    myDefinition.Id != neighborDefinition.Id)
                    continue;

                // Check normals on compound side with the same direction (we are in the same block)
                if (MyFakes.ENABLE_TEST_BLOCK_CONNECTIVITY_CHECK && (thisMountPointTransformedNormal - otherMountPoint.Normal != Vector3I.Zero))
                    continue;

                var otherBox = new BoundingBox(Vector3.Min(otherMountPoint.Start, otherMountPoint.End), Vector3.Max(otherMountPoint.Start, otherMountPoint.End));
                if (currentBox.Intersects(otherBox))
                    return true;
            }

            return false;
        }


        /// <summary>
        /// Checkes whether blocks A and B have matching mount point on one of their sides. Each block is given by its
        /// definition, rotation and position in grid. Position has to be relative to same center. Also, normal relative to block A specifies
        /// wall which is used for checking.
        /// </summary>
        public static bool CheckMountPointsForSide(MyCubeBlockDefinition defA, ref MyBlockOrientation orientationA, ref Vector3I positionA, ref Vector3I normalA,
                                                   MyCubeBlockDefinition defB, ref MyBlockOrientation orientationB, ref Vector3I positionB)
        {
            TransformMountPoints(m_cacheMountPointsA, defA, ref orientationA);
            TransformMountPoints(m_cacheMountPointsB, defB, ref orientationB);
            return CheckMountPointsForSide(m_cacheMountPointsA, ref orientationA, ref positionA, defA.Id, ref normalA, m_cacheMountPointsB, ref orientationB, ref positionB, defB.Id);
        }

        /// <summary>
        /// Checkes whether blocks A and B have matching mount point on one of their sides. Each block is given by its
        /// definition, rotation and position in grid. Position has to be relative to same center. Also, normal relative to block A specifies
        /// wall which is used for checking.
        /// </summary>
        public static bool CheckMountPointsForSide(List<MyCubeBlockDefinition.MountPoint> transormedA, ref MyBlockOrientation orientationA, ref Vector3I positionA, MyDefinitionId idA, ref Vector3I normalA,
                                                   List<MyCubeBlockDefinition.MountPoint> transormedB, ref MyBlockOrientation orientationB, ref Vector3I positionB, MyDefinitionId idB)
        {
            var offsetAB = positionB - positionA;

            var normalB = -normalA;
            for (int i = 0; i < transormedA.Count; ++i)
            {
                var mountPointA = transormedA[i];
                if (mountPointA.Normal != normalA)
                    continue;

                var minA = Vector3.Min(mountPointA.Start, mountPointA.End);
                var maxA = Vector3.Max(mountPointA.Start, mountPointA.End);

                minA -= offsetAB;
                maxA -= offsetAB;
                var bboxA = new BoundingBox(minA, maxA);

                for (int j = 0; j < transormedB.Count; ++j)
                {
                    var mountPointB = transormedB[j];
                    if (mountPointB.Normal != normalB)
                        continue;

                    // Skip mount points which exclude themselves (are not allowed to touch).
                    if (((mountPointA.ExclusionMask & mountPointB.PropertiesMask) != 0 || (mountPointA.PropertiesMask & mountPointB.ExclusionMask) != 0) &&
                        idA != idB)
                        continue;

                    var bboxB = new BoundingBox(Vector3.Min(mountPointB.Start, mountPointB.End), Vector3.Max(mountPointB.Start, mountPointB.End));
                    if (bboxA.Intersects(bboxB))
                        return true;
                }
            }

            return false;
        }

        #region model export

        private static void ConvertNextGrid(bool placeOnly)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                  timeoutInMiliseconds: 1000,
                  styleEnum: MyMessageBoxStyleEnum.Info,
                  buttonType: MyMessageBoxButtonsType.NONE_TIMEOUT,
                  messageText: new StringBuilder(MyTexts.GetString(MySpaceTexts.ConvertingObjs)),
                  callback: (result) =>
                  {
                      ConvertNextPrefab(m_prefabs, placeOnly);
                  }));
        }

        private static void ConvertNextPrefab(List<MyObjectBuilder_CubeGrid[]> prefabs, bool placeOnly)
        {
            if (prefabs.Count > 0)
            {
                MyObjectBuilder_CubeGrid[] currentPrefab = prefabs[0];
                int modelNumber = prefabs.Count;

                prefabs.RemoveAt(0);
                if (placeOnly)
                {                   
                    BoundingSphere boundingSphere = GetBoundingSphereForGrids(currentPrefab);

                    float maxSize = boundingSphere.Radius;
                    m_maxDimensionPreviousRow = VRageMath.MathHelper.Max(maxSize, m_maxDimensionPreviousRow);
                    if (prefabs.Count % m_numRowsForPlacedObjects != 0)
                    {
                        m_newPositionForPlacedObject.X += (2.0f * maxSize + 10.0f);
                    }
                    else
                    {
                        m_newPositionForPlacedObject.X = -(2.0f * maxSize + 10.0f);
                        m_newPositionForPlacedObject.Z -= (2.0f * m_maxDimensionPreviousRow + 30.0f);
                        m_maxDimensionPreviousRow = 0.0f;
                    }
                    PlacePrefabToWorld(currentPrefab, MySector.MainCamera.Position + m_newPositionForPlacedObject);
                    ConvertNextPrefab(m_prefabs, placeOnly);
                }
                else
                {
                    List<MyCubeGrid> prefabGrids = new List<MyCubeGrid>();
                    foreach (var currentGrid in currentPrefab)
                    {
                        prefabGrids.Add(MyEntities.CreateFromObjectBuilderAndAdd(currentGrid) as MyCubeGrid);
                    }
                    ExportToObjFile(prefabGrids, true,false);
                    foreach (var grid in prefabGrids)
                    {
                        grid.Close();
                    }
                }
            }
            else
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                         styleEnum: MyMessageBoxStyleEnum.Info,
                                         buttonType: MyMessageBoxButtonsType.OK,
                                         messageText: new StringBuilder(MyTexts.GetString(MySpaceTexts.ConvertToObjDone))));
            }
        }

        private static BoundingSphere GetBoundingSphereForGrids(MyObjectBuilder_CubeGrid[] currentPrefab)
        {
            BoundingSphere boundingSphere = new BoundingSphere(Vector3.Zero, float.MinValue);
            foreach (var gridBuilder in currentPrefab)
            {
                BoundingSphere localSphere = gridBuilder.CalculateBoundingSphere();
                MatrixD gridTransform = gridBuilder.PositionAndOrientation.HasValue ? gridBuilder.PositionAndOrientation.Value.GetMatrix() : MatrixD.Identity;
                boundingSphere.Include(localSphere.Transform(gridTransform));
            }
            return boundingSphere;
        }

        public static void StartConverting(bool placeOnly)
        {
            string folder = Path.Combine(MyFileSystem.UserDataPath, SOURCE_DIRECTORY);
            if (Directory.Exists(folder) == false)
            {
                return;
            }
            m_prefabs.Clear();
            foreach (var zipFile in Directory.GetFiles(folder, "*.zip"))
            {
                foreach (var file in MyFileSystem.GetFiles(zipFile, "*.sbc", VRage.FileSystem.MySearchOption.AllDirectories))
                {
                    if (MyFileSystem.FileExists(file))
                    {
                        MyObjectBuilder_Definitions loadedPrefab = null;
                        MyObjectBuilderSerializer.DeserializeXML(file, out loadedPrefab);
                        if (loadedPrefab.Prefabs[0].CubeGrids != null)
                        {
                            m_prefabs.Add(loadedPrefab.Prefabs[0].CubeGrids);
                        }                      
                    }
                }

            }
            ConvertNextPrefab(m_prefabs, placeOnly);
        }

        public static void ConvertPrefabsToObjs()
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    timeoutInMiliseconds: 1000,
                    styleEnum: MyMessageBoxStyleEnum.Info,
                    buttonType: MyMessageBoxButtonsType.NONE_TIMEOUT,
                    messageText: new StringBuilder(MyTexts.GetString(MySpaceTexts.ConvertingObjs)),
                    callback: (result) =>
                    {
                        StartConverting(false);
                    }));
        }

        public static void PackFiles(string path,string objectName)
        {         
            if (Directory.Exists(path) == false)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    styleEnum: MyMessageBoxStyleEnum.Error,
                    buttonType: MyMessageBoxButtonsType.OK,
                    messageText: new StringBuilder(string.Format(MyTexts.GetString(MySpaceTexts.ExportToObjFailed), path))));
                return;
            }
            using (var arc = VRage.Compression.MyZipArchive.OpenOnFile(Path.Combine(path, objectName + "_objFiles.zip"), FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                PackFilesToDirectory(path, "*.png", arc);
                PackFilesToDirectory(path, "*.obj", arc);
                PackFilesToDirectory(path, "*.mtl", arc);
            }
            using (var arc = VRage.Compression.MyZipArchive.OpenOnFile(Path.Combine(path, objectName+ ".zip"), FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                PackFilesToDirectory(path, objectName+".png", arc);
                PackFilesToDirectory(path, "*.sbc", arc);
            }

            RemoveFilesFromDirectory(path,"*.png");
            RemoveFilesFromDirectory(path, "*.sbc");
            RemoveFilesFromDirectory(path, "*.obj");
            RemoveFilesFromDirectory(path, "*.mtl");
        }

        private static void RemoveFilesFromDirectory(string path,string fileType)
        {
            string[] filePaths = Directory.GetFiles(path, fileType);
            foreach (string filePath in filePaths)
            {
                File.Delete(filePath);
            }
        }

        private static void PackFilesToDirectory(string path,  string searchString , VRage.Compression.MyZipArchive arc)
        {
            int len = path.Length + 1;
            foreach (var file in Directory.GetFiles(path, searchString, SearchOption.AllDirectories))
            {
                using (var inStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var outStream = arc.AddFile(file.Substring(len), VRage.Compression.CompressionMethodEnum.Deflated, VRage.Compression.DeflateOptionEnum.Maximum).GetStream(FileMode.Open, FileAccess.Write))
                    {
                        inStream.CopyTo(outStream, 0x1000);
                    }
                }
            }
        }

        public static void ExportObject(MyCubeGrid baseGrid, bool convertModelsFromSBC, bool exportObjAndSBC = false)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                   timeoutInMiliseconds: 1000,
                   styleEnum: MyMessageBoxStyleEnum.Info,
                   buttonType: MyMessageBoxButtonsType.NONE_TIMEOUT,
                   messageText: new StringBuilder(MyTexts.GetString(MySpaceTexts.ExportingToObj)),
                   callback: (result) =>
                   {
                       List<MyCubeGrid> gridsToExport = new List<MyCubeGrid>();
                       var gridGroup = MyCubeGridGroups.Static.Logical.GetGroup(baseGrid);
                       foreach (var node in gridGroup.Nodes)
                       {
                           gridsToExport.Add(node.NodeData);
                       }
                       ExportToObjFile(gridsToExport, convertModelsFromSBC, exportObjAndSBC);
                   }));
        }

        private static void ExportToObjFile(List<MyCubeGrid> baseGrids, bool convertModelsFromSBC, bool exportObjAndSBC)
        {       
            materialID = 0;
            var datetimePrefix = MyValueFormatter.GetFormatedDateTimeForFilename(DateTime.Now);
            var name = MyUtils.StripInvalidChars(baseGrids[0].DisplayName.Replace(' ', '_'));
            string baseFolderPath = MyFileSystem.UserDataPath;
            string exportFolder = EXPORT_DIRECTORY;

            if (convertModelsFromSBC == false || exportObjAndSBC)
            {
                baseFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                exportFolder = MyPerGameSettings.GameNameSafe + "_" + EXPORT_DIRECTORY;
            }

            string folder = Path.Combine(baseFolderPath, exportFolder, name);
            int retryCount = 0;
            while (Directory.Exists(folder))
            {
                ++retryCount;
                folder = Path.Combine(baseFolderPath, exportFolder, string.Format("{0}_{1:000}", name, retryCount));
            }

            MyUtils.CreateFolder(folder);

            if (convertModelsFromSBC == false || exportObjAndSBC)
            {
                bool isModded = false;
                var prefabPath = Path.Combine(folder, name + ".sbc");
                foreach (var grid in baseGrids)
                {
                    foreach (var block in grid.CubeBlocks)
                    {
                        if (false == block.BlockDefinition.Context.IsBaseGame)
                        {
                            isModded = true;
                            break;
                        }
                    }
                }
                if (isModded == false)
                {
                    CreatePrefabFile(baseGrids, name, prefabPath);
                    MyRenderProxy.TakeScreenshot(tumbnailMultiplier, Path.Combine(folder, name + ".png"), false, true, false);

                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                    styleEnum: MyMessageBoxStyleEnum.Info,
                                    buttonType: MyMessageBoxButtonsType.OK,
                                    messageText: new StringBuilder(string.Format(MyTexts.GetString(MySpaceTexts.ExportToObjComplete), folder)),
                                    callback: (result) =>
                                    {
                                        PackFiles(folder, name);
                                    }));
                }
                else
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        styleEnum: MyMessageBoxStyleEnum.Error,
                                                     buttonType: MyMessageBoxButtonsType.OK,
                                                     messageText: new StringBuilder(string.Format(MyTexts.GetString(MySpaceTexts.ExportToObjModded), folder))));
                }
            }

            if (exportObjAndSBC || convertModelsFromSBC)
            {
                List<Vector3> vertices = new List<Vector3>();
                List<TriangleWithMaterial> triangles = new List<TriangleWithMaterial>();
                List<Vector2> uvs = new List<Vector2>();
                Dictionary<string, MyExportModel.Material> materials = new Dictionary<string, MyExportModel.Material>();
                int currVerticesCount = 0;
                try
                {
                     GetModelDataFromGrid(baseGrids, vertices, triangles, uvs, materials, currVerticesCount);


                    var filename = Path.Combine(folder, name + ".obj");
                    var matFilename = Path.Combine(folder, name + ".mtl");

                    CreateObjFile(name, filename, matFilename, vertices, triangles, uvs, materials, currVerticesCount);

                    List<renderColoredTextureProperties> texturesToRender = new List<renderColoredTextureProperties>();

                    CreateMaterialFile(folder, matFilename, materials, texturesToRender);

                    if (texturesToRender.Count > 0)
                    {
                        VRageRender.MyRenderProxy.RenderColoredTextures(texturesToRender);
                    }

                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                   timeoutInMiliseconds: 1000,
                   styleEnum: MyMessageBoxStyleEnum.Info,
                   buttonType: MyMessageBoxButtonsType.NONE_TIMEOUT,
                   messageText: new StringBuilder(string.Format(MyTexts.GetString(MySpaceTexts.ExportToObjComplete), folder)),
                   callback: (result) =>
                   {
                       ConvertNextGrid(false);
                   }));
                }
                catch (Exception e)
                {
                    MySandboxGame.Log.WriteLine("Error while exporting to obj file.");
                    MySandboxGame.Log.WriteLine(e.ToString());

                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        styleEnum: MyMessageBoxStyleEnum.Error,
                        buttonType: MyMessageBoxButtonsType.OK,
                        messageText: new StringBuilder(string.Format(MyTexts.GetString(MySpaceTexts.ExportToObjFailed), folder))));
                }
            }
        }

        private static void CreatePrefabFile(List<MyCubeGrid> baseGrid, string name, string prefabPath)
        {
            Vector2I screenResolution = MyRenderProxy.BackBufferResolution;

            tumbnailMultiplier.X = 400.0f / screenResolution.X;
            tumbnailMultiplier.Y = 400.0f / screenResolution.Y;

            List<MyObjectBuilder_CubeGrid> gridToexport = new List<MyObjectBuilder_CubeGrid>();
            foreach (var grid in baseGrid)
            {
                gridToexport.Add((MyObjectBuilder_CubeGrid)grid.GetObjectBuilder());
            }
            MyPrefabManager.SavePrefabToPath(name, prefabPath, gridToexport);
        }

        private static void GetModelDataFromGrid(List<MyCubeGrid> baseGrid, List<Vector3> vertices, List<TriangleWithMaterial> triangles, List<Vector2> uvs, Dictionary<string, MyExportModel.Material> materials, int currVerticesCount)
        {
            var baseGridWorldInv = MatrixD.Invert(baseGrid[0].WorldMatrix);
            MatrixD localToExport;
            foreach (var grid in baseGrid)
            {
                localToExport = grid.WorldMatrix * baseGridWorldInv;
                foreach (var cell in grid.RenderData.Cells)
                {
                    HashSet<MyCubePart> parts = cell.Value.CubeParts;
                    foreach (var part in parts)
                    {
                        Vector3 HSV = new Vector3(part.InstanceData.ColorMaskHSV.X, part.InstanceData.ColorMaskHSV.Y, part.InstanceData.ColorMaskHSV.Z);
                        Vector2 offsetUV = new Vector2(part.InstanceData.GetTextureOffset(0), part.InstanceData.GetTextureOffset(1));
                        ExtractModelDataForObj(part.Model, part.InstanceData.LocalMatrix * (Matrix)localToExport, vertices, triangles, uvs, ref offsetUV, materials, ref currVerticesCount, HSV);
                    }
                }

                foreach (var block in grid.GetBlocks())
                {
                    if (block.FatBlock != null)
                    {
                        if (block.FatBlock is MyPistonBase)
                        {
                            //for piston to update position correctly when exporting
                            //without this piston will always be exported as fully retracted
                            block.FatBlock.UpdateOnceBeforeFrame();                    
                        }
                        ExtractModelDataForObj(block.FatBlock.Model, block.FatBlock.PositionComp.LocalMatrix * (Matrix)localToExport, vertices, triangles, uvs, ref Vector2.Zero, materials, ref currVerticesCount, block.ColorMaskHSV);
                        ProcessChildrens(vertices, triangles, uvs, materials, ref currVerticesCount, block.FatBlock.PositionComp.LocalMatrix * (Matrix)localToExport, block.ColorMaskHSV, block.FatBlock.Hierarchy.Children);
                    }
                }
            }
        }

        private static void CreateObjFile(string name, string filename, string matFilename, List<Vector3> vertices, List<TriangleWithMaterial> triangles, List<Vector2> uvs, Dictionary<string, MyExportModel.Material> materials, int currVerticesCount)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                writer.WriteLine(string.Format("mtllib {0}", Path.GetFileName(matFilename)));
                writer.WriteLine(string.Empty);
                writer.WriteLine("#");
                writer.WriteLine(string.Format("# {0}", name));
                writer.WriteLine("#");
                writer.WriteLine(string.Empty);
                writer.WriteLine("# vertices");
                foreach (var v in vertices)
                {
                    writer.WriteLine(string.Format("v {0} {1} {2}", v.X, v.Y, v.Z));
                }

                writer.WriteLine(string.Format("# {0} vertices", currVerticesCount));
                writer.WriteLine(string.Empty);

                writer.WriteLine("# texture coordinates");
                foreach (var uv in uvs)
                {
                    writer.WriteLine(string.Format("vt {0} {1}", uv.X, uv.Y));
                }
                writer.WriteLine(string.Format("# {0} texture coords", uvs.Count));
                writer.WriteLine(string.Empty);

                writer.WriteLine("# faces");
                writer.WriteLine(string.Format("o {0}", name));
                for (int i = 0; i < materials.Count; ++i)
                {
                    string materialName = materials.ElementAt(i).Value.Name;
                    writer.WriteLine(string.Empty);
                    writer.WriteLine(string.Format("g {0}_part{1}", name, i + 1));
                    writer.WriteLine(string.Format("usemtl {0}", materialName));
                    writer.WriteLine("s off");
                    for (int j = 0; j < triangles.Count; ++j)
                    {
                        if (materialName == triangles[j].material)
                        {
                            writer.WriteLine(string.Format("f {0}/{0} {1}/{1} {2}/{2}", triangles[j].triangle.I0, triangles[j].triangle.I1, triangles[j].triangle.I2));
                        }
                    }
                }
                writer.WriteLine(string.Format("# {0} faces", triangles.Count));
            }
        }

        private static void CreateMaterialFile(string folder, string matFilename, Dictionary<string, MyExportModel.Material> materials, List<renderColoredTextureProperties> texturesToRender)
        {
            using (StreamWriter writer = new StreamWriter(matFilename))
            {
                for (int i = 0; i < materials.Count; ++i)
                {
                    MyExportModel.Material mat = materials.ElementAt(i).Value;
                    string materialName = mat.Name;
                    writer.WriteLine(string.Format("newmtl {0}", materialName));
                    writer.WriteLine("Ka 1.000 1.000 1.000");
                    writer.WriteLine("Ks 0.000 0.000 0.000");
                    writer.WriteLine("d 1.0");
                    writer.WriteLine("Tr 0.0000");
                    writer.WriteLine("Tf 1.0000 1.0000 1.0000");
                    writer.WriteLine("illum 2");
                    if (mat.IsGlass)
                    {
                        foreach (var material in MyDefinitionManager.Static.GetTransparentMaterialDefinitions())
                        {
                            if(mat.DiffuseTexture.Equals(material.Texture, StringComparison.OrdinalIgnoreCase))
                            {
                                writer.WriteLine("Kd {0} {1} {2}", material.Color.Y, material.Color.Z, material.Color.W);
                            };
                        }

                        continue;
                    }

                    renderColoredTextureProperties textureToRenderProperties = new renderColoredTextureProperties();
                    textureToRenderProperties.ColorMaskHSV = mat.ColorMaskHSV;
                    textureToRenderProperties.TextureName = mat.DiffuseTexture;
                    textureToRenderProperties.PathToSave = Path.Combine(folder, mat.NewDiffuseTexture);
                    texturesToRender.Add(textureToRenderProperties);

                    writer.WriteLine("Kd 1.000 1.000 1.000");

                    string srcDiffuseTex = mat.NewDiffuseTexture;

                    if (!String.IsNullOrEmpty(srcDiffuseTex))
                    {
                        writer.WriteLine(string.Format("map_Ka {0}", Path.GetFileName(srcDiffuseTex)));
                        writer.WriteLine(string.Format("map_Kd {0}", Path.GetFileName(srcDiffuseTex)));
                    }

                    if (i < materials.Count - 1)
                    {
                        writer.WriteLine(string.Empty);
                    }
                }              
            }
        }

        private static void ProcessChildrens(List<Vector3> vertices, List<TriangleWithMaterial> triangles, List<Vector2> uvs, Dictionary<string, MyExportModel.Material> materials, ref int currVerticesCount, Matrix parentMatrix,Vector3 HSV, List<MyHierarchyComponentBase> childrens)
         {
            foreach (var c in childrens)
            {
                var child = c.Container.Entity;
                MyModel model = (child as MyEntity).Model;
                if (null != model)
                {
                    ExtractModelDataForObj(model, child.LocalMatrix * parentMatrix, vertices, triangles, uvs, ref Vector2.Zero, materials, ref currVerticesCount,HSV);
                }
                ProcessChildrens(vertices, triangles, uvs, materials, ref currVerticesCount, child.LocalMatrix * parentMatrix, HSV, child.Hierarchy.Children);
            }
        }

        public static void PlacePrefabsToWorld()
        {
            m_newPositionForPlacedObject = MySession.ControlledEntity.Entity.PositionComp.GetPosition();
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                              timeoutInMiliseconds: 1000,
                              styleEnum: MyMessageBoxStyleEnum.Info,
                              buttonType: MyMessageBoxButtonsType.NONE_TIMEOUT,
                              messageText: new StringBuilder(MyTexts.GetString(MySpaceTexts.PlacingObjectsToScene)),
                              callback: (result) =>
                              {
                                  StartConverting(true);
                              }));
        }

        public static void PlacePrefabToWorld(MyObjectBuilder_CubeGrid[] currentPrefab, Vector3D position, List<MyCubeGrid> createdGrids = null)
        {        
            Vector3D newPosition = Vector3D.Zero;
            Vector3D positionOffset = Vector3D.Zero;
            bool firstIteration = true;
            MyEntities.RemapObjectBuilderCollection(currentPrefab);
            foreach (var gridBuilder in currentPrefab)
            {
                if (gridBuilder.PositionAndOrientation.HasValue)
                {
                    if (firstIteration)
                    {
                        positionOffset = position - gridBuilder.PositionAndOrientation.Value.Position;
                        firstIteration = false;
                        newPosition = position;
                    }
                    else
                    {
                        newPosition = gridBuilder.PositionAndOrientation.Value.Position + positionOffset;
                    }
                }

                MyPositionAndOrientation originalPos = gridBuilder.PositionAndOrientation.Value;
                originalPos.Position = newPosition;
                gridBuilder.PositionAndOrientation = originalPos; 

                MyCubeGrid currentGrid = MyEntities.CreateFromObjectBuilder(gridBuilder) as MyCubeGrid;

                if (currentGrid != null)
                {                
                    currentGrid.ClearSymmetries();
                    currentGrid.Physics.LinearVelocity = Vector3D.Zero;
                    currentGrid.Physics.AngularVelocity = Vector3D.Zero;
                    if (createdGrids != null)
                    {
                        createdGrids.Add(currentGrid);
                    }
                    MyEntities.Add(currentGrid, true);
                }
            }  
        }

        #endregion

         /// <summary>
        /// Obtain grid that player is aiming/looking at.
        /// </summary>
        public static MyCubeGrid GetTargetGrid()
        {
            MyEntity entity = MyCubeBuilder.Static.FindClosestGrid();
            if (entity == null)
            {
                entity = GetTargetEntity();
            }
            return entity as MyCubeGrid;
        }

        /// <summary>
        /// Obtain entity that player is aiming/looking at.
        /// </summary>
        public static MyEntity GetTargetEntity()
        {
            var line = new LineD(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 10000);

            m_tmpHitList.Clear();
            MyPhysics.CastRay(line.From, line.To, m_tmpHitList, MyPhysics.DefaultCollisionLayer);
            // Remove character hits.
            m_tmpHitList.RemoveAll(delegate(MyPhysics.HitInfo hit)
            {
                return (hit.HkHitInfo.Body.GetEntity() == MySession.ControlledEntity.Entity);
            });

            if (m_tmpHitList.Count == 0)
                return null;

            return m_tmpHitList[0].HkHitInfo.Body.GetEntity() as MyEntity;
        }

        public static bool TryRayCastGrid(ref LineD worldRay, out MyCubeGrid hitGrid, out Vector3D worldHitPos)
        {
            try
            {
                MyPhysics.CastRay(worldRay.From, worldRay.To, m_tmpHitList);
                foreach (var hit in m_tmpHitList)
                {
                    var cubeGrid = hit.HkHitInfo.Body.GetEntity() as MyCubeGrid;
                    if (cubeGrid == null)
                        continue;

                    worldHitPos = hit.Position;
                    VRageRender.MyRenderProxy.DebugDrawAABB(new BoundingBoxD(worldHitPos - 0.01, worldHitPos + 0.01), Color.Wheat.ToVector3(), 1f, 1f, true);
                    hitGrid = cubeGrid;
                    return true;
                }

                hitGrid = default(MyCubeGrid);
                worldHitPos = default(Vector3D);
                return false;
            }
            finally
            {
                m_tmpHitList.Clear();
            }
        }

        public static bool TestBlockPlacementArea(
            MyCubeGrid targetGrid,
            ref MyGridPlacementSettings settings,
            MyBlockOrientation blockOrientation,
            MyCubeBlockDefinition blockDefinition,
            ref Vector3D translation,
            ref Quaternion rotation,
            ref Vector3 halfExtents,
            ref BoundingBoxD localAabb,
            MyEntity ignoredEntity = null)
        {
            MyCubeGrid touchingGrid;
            return TestBlockPlacementArea(targetGrid, ref settings, blockOrientation, blockDefinition, ref translation, ref rotation, ref halfExtents, ref localAabb, out touchingGrid, ignoredEntity: ignoredEntity);
        }

        public static bool TestBlockPlacementArea(
            MyCubeGrid targetGrid,
            ref MyGridPlacementSettings settings,
            MyBlockOrientation blockOrientation,
            MyCubeBlockDefinition blockDefinition,
            ref Vector3D translation,
            ref Quaternion rotation,
            ref Vector3 halfExtents,
            ref BoundingBoxD localAabb,
            out MyCubeGrid touchingGrid,
            MyEntity ignoredEntity = null)
        {
            touchingGrid = null;

            if (blockDefinition != null && blockDefinition.UseModelIntersection)
            {
                var model = MyModels.GetModelOnlyData(blockDefinition.Model);
                if(model != null)
                    model.CheckLoadingErrors(blockDefinition.Context);

                if (model != null && model.HavokCollisionShapes != null)
                {
                    int shapeCount = model.HavokCollisionShapes.Length;
                    HkShape[] shapes = new HkShape[shapeCount];
                    for (int q = 0; q < shapeCount; ++q)
                    {
                        shapes[q] = model.HavokCollisionShapes[q];
                    }

                    var shape = new HkListShape(shapes, shapeCount, HkReferencePolicy.None);

                    Quaternion q2 = Quaternion.CreateFromForwardUp(Base6Directions.GetVector(blockOrientation.Forward), Base6Directions.GetVector(blockOrientation.Up));
                    rotation = rotation * q2;
                    MyPhysics.GetPenetrationsShape(shape, ref translation, ref rotation, m_physicsBoxQueryList, MyPhysics.CharacterCollisionLayer);

                    shape.Base.RemoveReference();
                }
                else
                {
                    Debug.Assert(m_physicsBoxQueryList.Count == 0, "List not cleared");
                    MyPhysics.GetPenetrationsBox(ref halfExtents, ref translation, ref rotation, m_physicsBoxQueryList, MyPhysics.CharacterCollisionLayer);
                }
            }
            else
            {
                Debug.Assert(m_physicsBoxQueryList.Count == 0, "List not cleared");
                MyPhysics.GetPenetrationsBox(ref halfExtents, ref translation, ref rotation, m_physicsBoxQueryList, MyPhysics.CharacterCollisionLayer);
            }

            var worldMatrix = targetGrid != null ? targetGrid.WorldMatrix : MatrixD.Identity;
            return TestPlacementAreaInternal(targetGrid, ref settings, blockDefinition, blockOrientation, ref localAabb, ignoredEntity, ref worldMatrix, out touchingGrid);
        }

        public static bool TestPlacementAreaCube(
            MyCubeGrid targetGrid,
            ref MyGridPlacementSettings settings,
            Vector3I min,
            Vector3I max,
            MyBlockOrientation blockOrientation,
            MyCubeBlockDefinition blockDefinition,
            MyEntity ignoredEntity = null)
        {
            MyCubeGrid touchingGrid = null;
            return TestPlacementAreaCube(targetGrid, ref settings, min, max, blockOrientation, blockDefinition, out touchingGrid, ignoredEntity: ignoredEntity);
        }

        /// <summary>
        /// Test cube block placement area in grid.
        /// </summary>
        public static bool TestPlacementAreaCube(
            MyCubeGrid targetGrid,
            ref MyGridPlacementSettings settings,
            Vector3I min,
            Vector3I max,
            MyBlockOrientation blockOrientation,
            MyCubeBlockDefinition blockDefinition,
            out MyCubeGrid touchingGrid,
            MyEntity ignoredEntity = null)
        {
            touchingGrid = null;

            var worldMatrix = targetGrid != null ? targetGrid.WorldMatrix : MatrixD.Identity;
            var gridSize = targetGrid != null ? targetGrid.GridSize : MyDefinitionManager.Static.GetCubeSize(MyCubeSize.Large);

            Vector3 halfExtents = ((max - min) * gridSize + gridSize) / 2;
            if (MyFakes.ENABLE_BLOCK_PLACING_IN_OCCUPIED_AREA)
                halfExtents -= new Vector3D(GRID_PLACING_AREA_FIX_VALUE);
            else
                halfExtents -= new Vector3(0.03f, 0.03f, 0.03f); //allows touching blocks like wheels
            var matrix = MatrixD.CreateTranslation((max + min) * 0.5f * gridSize) * worldMatrix;
            var localAabb = BoundingBoxD.CreateInvalid();
            localAabb.Include(min * gridSize - gridSize / 2);
            localAabb.Include(max * gridSize + gridSize / 2);

            Vector3D translation = matrix.Translation;
            Quaternion rotation = Quaternion.CreateFromRotationMatrix(matrix);

            return TestBlockPlacementArea(targetGrid, ref settings, blockOrientation, blockDefinition, ref translation, ref rotation, ref halfExtents, ref localAabb, out touchingGrid, ignoredEntity);
        }

        public static bool TestPlacementAreaCubeNoAABBInflate(
            MyCubeGrid targetGrid,
            ref MyGridPlacementSettings settings,
            Vector3I min,
            Vector3I max,
            MyBlockOrientation blockOrientation,
            MyCubeBlockDefinition blockDefinition,
            out MyCubeGrid touchingGrid,
            MyEntity ignoredEntity = null)
        {
            touchingGrid = null;

            var worldMatrix = targetGrid != null ? targetGrid.WorldMatrix : MatrixD.Identity;
            var gridSize = targetGrid != null ? targetGrid.GridSize : MyDefinitionManager.Static.GetCubeSize(MyCubeSize.Large);

            Vector3 halfExtents = ((max - min) * gridSize + gridSize) / 2;
            var matrix = MatrixD.CreateTranslation((max + min) * 0.5f * gridSize) * worldMatrix;
            var localAabb = BoundingBoxD.CreateInvalid();
            localAabb.Include(min * gridSize - gridSize / 2);
            localAabb.Include(max * gridSize + gridSize / 2);

            Vector3D translation = matrix.Translation;
            Quaternion rotation = Quaternion.CreateFromRotationMatrix(matrix);

            return TestBlockPlacementArea(targetGrid, ref settings, blockOrientation, blockDefinition, ref translation, ref rotation, ref halfExtents, ref localAabb, out touchingGrid, ignoredEntity);
        }

        public static bool TestPlacementArea(MyCubeGrid targetGrid, ref MyGridPlacementSettings settings, BoundingBoxD localAabb, bool dynamicBuildMode, MyEntity ignoredEntity = null)
        {
            ProfilerShort.Begin("TestStart");
            var worldMatrix = targetGrid.WorldMatrix;

            Vector3 halfExtents = localAabb.HalfExtents;
            halfExtents += settings.SearchHalfExtentsDeltaAbsolute; //this works for SE
            if (MyFakes.ENABLE_BLOCK_PLACING_IN_OCCUPIED_AREA)
                halfExtents -= new Vector3D(GRID_PLACING_AREA_FIX_VALUE);
            Vector3D translation = localAabb.Transform(ref worldMatrix).Center;
            Quaternion quaternion = Quaternion.CreateFromRotationMatrix(worldMatrix);
            quaternion.Normalize();
            ProfilerShort.End();

            ProfilerShort.Begin("Havok.GetPenetrationsBox");
            Debug.Assert(m_physicsBoxQueryList.Count == 0, "List not cleared");
            MyPhysics.GetPenetrationsBox(ref halfExtents, ref translation, ref quaternion, m_physicsBoxQueryList, MyPhysics.CharacterCollisionLayer);
            ProfilerShort.End();

            MyCubeGrid touchingGrid;
            return TestPlacementAreaInternal(targetGrid, ref settings, null, null, ref localAabb, ignoredEntity, ref worldMatrix, out touchingGrid, dynamicBuildMode: dynamicBuildMode);
        }

        public static bool TestPlacementAreaWithEntities(MyCubeGrid targetGrid, bool targetGridIsStatic, ref MyGridPlacementSettings settings, BoundingBoxD localAabb, bool dynamicBuildMode, MyEntity ignoredEntity = null)
        {
            ProfilerShort.Begin("Test start with entities");
            var worldMatrix = targetGrid.WorldMatrix;

            Vector3 halfExtents = localAabb.HalfExtents;
            halfExtents += settings.SearchHalfExtentsDeltaAbsolute; //this works for SE
            if (MyFakes.ENABLE_BLOCK_PLACING_IN_OCCUPIED_AREA)
                halfExtents -= new Vector3D(GRID_PLACING_AREA_FIX_VALUE);
            Vector3D translation = localAabb.Transform(ref worldMatrix).Center;
            Quaternion quaternion = Quaternion.CreateFromRotationMatrix(worldMatrix);
            quaternion.Normalize();
            ProfilerShort.End();

            ProfilerShort.Begin("get top most entities");

            m_tmpResultList.Clear();
            BoundingBoxD box = targetGrid.PositionComp.WorldAABB;
            MyGamePruningStructure.GetAllTopMostEntitiesInBox<MyEntity>(ref box, m_tmpResultList);
            ProfilerShort.End();

            return TestPlacementAreaInternalWithEntities(targetGrid, targetGridIsStatic, ref settings, ref localAabb, ignoredEntity, ref worldMatrix, dynamicBuildMode: dynamicBuildMode);
        }

        public static bool TestPlacementArea(MyCubeGrid targetGrid, bool targetGridIsStatic, ref MyGridPlacementSettings settings, BoundingBoxD localAabb, bool dynamicBuildMode, MyEntity ignoredEntity = null)
        {
            ProfilerShort.Begin("TestStart");
            var worldMatrix = targetGrid.WorldMatrix;

            Vector3 halfExtents = localAabb.HalfExtents;
            halfExtents += settings.SearchHalfExtentsDeltaAbsolute; //this works for SE
            if (MyFakes.ENABLE_BLOCK_PLACING_IN_OCCUPIED_AREA)
                halfExtents -= new Vector3D(GRID_PLACING_AREA_FIX_VALUE);
            Vector3D translation = localAabb.Transform(ref worldMatrix).Center;
            Quaternion quaternion = Quaternion.CreateFromRotationMatrix(worldMatrix);
            quaternion.Normalize();
            ProfilerShort.End();

            ProfilerShort.Begin("Havok.GetPenetrationsBox");

            Debug.Assert(m_physicsBoxQueryList.Count == 0, "List not cleared");
            MyPhysics.GetPenetrationsBox(ref halfExtents, ref translation, ref quaternion, m_physicsBoxQueryList, MyPhysics.CharacterCollisionLayer);
            ProfilerShort.End();

            MyCubeGrid touchingGrid;
            return TestPlacementAreaInternal(targetGrid, targetGridIsStatic, ref settings, null, null, ref localAabb, ignoredEntity, ref worldMatrix, out touchingGrid, dynamicBuildMode: dynamicBuildMode);
        }

        public static bool TestBlockPlacementArea(MyCubeBlockDefinition blockDefinition, MyBlockOrientation? blockOrientation, MatrixD worldMatrix, ref MyGridPlacementSettings settings, BoundingBoxD localAabb, bool dynamicBuildMode,
            MyEntity ignoredEntity = null)
        {
            ProfilerShort.Begin("TestStart");
            Vector3 halfExtents = localAabb.HalfExtents;
            halfExtents += settings.SearchHalfExtentsDeltaAbsolute; //this works for SE
            if (MyFakes.ENABLE_BLOCK_PLACING_IN_OCCUPIED_AREA)
                halfExtents -= new Vector3D(GRID_PLACING_AREA_FIX_VALUE);
            Vector3D translation = localAabb.Transform(ref worldMatrix).Center;
            Quaternion quaternion = Quaternion.CreateFromRotationMatrix(worldMatrix);
            quaternion.Normalize();
            ProfilerShort.End();

            ProfilerShort.Begin("Havok.GetPenetrationsBox");
            Debug.Assert(m_physicsBoxQueryList.Count == 0, "List not cleared");
            MyPhysics.GetPenetrationsBox(ref halfExtents, ref translation, ref quaternion, m_physicsBoxQueryList, MyPhysics.CharacterCollisionLayer);
            ProfilerShort.End();

            MyCubeGrid touchingGrid;
            return TestPlacementAreaInternal(null, ref settings, blockDefinition, blockOrientation, ref localAabb, ignoredEntity, ref worldMatrix, out touchingGrid, dynamicBuildMode: dynamicBuildMode);
        }

        #region Private
        private static void ExtractModelDataForObj(
            MyModel model,
            Matrix matrix,
            List<Vector3> vertices,
            List<TriangleWithMaterial> triangles,
            List<Vector2> uvs,
            ref Vector2 offsetUV,
            Dictionary<string, MyExportModel.Material> materials,
            ref int currVerticesCount,
            Vector3 colorMaskHSV)
        {
            if (false == model.HasUV)
            {
                model.LoadUV = true;
                model.UnloadData();
                model.LoadData();
            }

            MyExportModel renderModel = new MyExportModel(model);

            int modelVerticesCount = renderModel.GetVerticesCount();

            List<HalfVector2> modelUVs = GetUVsForModel(renderModel, modelVerticesCount);
            Debug.Assert(modelUVs.Count == modelVerticesCount, "wrong UVs for model");
            if (modelUVs.Count != modelVerticesCount)
            {
                return;
            }

            //we need new material for every HSV and texture combination, therefore we need to create new materials for each model
            List<MyExportModel.Material> newModelMaterials = CreateMaterialsForModel(materials, colorMaskHSV, renderModel);

            for (int i = 0; i < modelVerticesCount; ++i)
            {
                vertices.Add(Vector3.Transform(model.GetVertex(i), matrix));
                Vector2 localUV = modelUVs[i].ToVector2()/model.PatternScale + offsetUV;
                uvs.Add(new Vector2(localUV.X, -localUV.Y));
            }

            for (int i = 0; i < renderModel.GetTrianglesCount(); ++i)
            {
                int matID = -1;
                for (int j = 0; j < newModelMaterials.Count; ++j)
                {
                    if (i <= newModelMaterials[j].LastTri)
                    {
                        matID = j;
                        break;
                    }
                }
                Debug.Assert(matID != -1, "Triangle with no material");

                var t = renderModel.GetTriangle(i);
                string materialName = "EmptyMaterial";
                if (matID != -1)
                {
                    materialName = newModelMaterials[matID].Name;
                }
                triangles.Add(new TriangleWithMaterial()
                {
                    triangle = new MyTriangleVertexIndices(t.I0 + 1 + currVerticesCount, t.I1 + 1 + currVerticesCount, t.I2 + 1 + currVerticesCount),
                    material = materialName,
                });
            }
            currVerticesCount += modelVerticesCount;
        }

        private static List<HalfVector2> GetUVsForModel(MyExportModel renderModel, int modelVerticesCount)
        {
            return renderModel.GetTexCoords().ToList();
        }

        private static List<MyExportModel.Material> CreateMaterialsForModel(Dictionary<string, MyExportModel.Material> materials, Vector3 colorMaskHSV, MyExportModel renderModel)
        {
            List<MyExportModel.Material> newModelMaterials = new List<MyExportModel.Material>();
            List<MyExportModel.Material> modelMaterials = renderModel.GetMaterials();
            foreach (var material in modelMaterials)
            {
                string diffuseTextureName = GetDiffuseTextureName(material.DiffuseTexture);
                bool materialFound = false;
                foreach (var savedMaterial in materials)
                {
                    if (savedMaterial.Value.DiffuseTexture.Equals(diffuseTextureName, StringComparison.OrdinalIgnoreCase) &&
                        Math.Abs(savedMaterial.Value.ColorMaskHSV.X - colorMaskHSV.X) < 0.01f &&
                        Math.Abs(savedMaterial.Value.ColorMaskHSV.Y - colorMaskHSV.Y) < 0.01f &&
                        Math.Abs(savedMaterial.Value.ColorMaskHSV.Z - colorMaskHSV.Z) < 0.01f)
                    {
                        MyExportModel.Material newMaterial = material;
                        newMaterial.DiffuseTexture = diffuseTextureName;
                        //each time new material is created new name for this material is assgined e.g. Material_x
                        //but model materials have they original name so we need to swap material name with created one
                        newMaterial.Name = savedMaterial.Value.Name;
                        newModelMaterials.Add(newMaterial);
                        materialFound = true;
                        break;
                    }
                }
                if (false == materialFound)
                {
                    materialID++;

                    MyExportModel.Material newMaterial = material;
                    newMaterial.Name = "material_" + materialID.ToString();                 
                    newMaterial.ColorMaskHSV = colorMaskHSV;
                    newMaterial.DiffuseTexture = diffuseTextureName;
                    newMaterial.NewDiffuseTexture = newMaterial.Name + ".png";                   
                    newModelMaterials.Add(newMaterial);
                    materials.Add(newMaterial.Name, newMaterial);
                }
            }
            return newModelMaterials;
        }

        private static string GetDiffuseTextureName(string diffuseTextureName)
        {       
            string textureName = Path.GetFileNameWithoutExtension(diffuseTextureName);
            string baseTextureName = textureName;
            if (-1 != textureName.LastIndexOf('_'))
            {
                baseTextureName = baseTextureName.Substring(0, textureName.LastIndexOf('_'));
            }
            string srcDiffuseTex = Path.Combine(MyFileSystem.ContentPath, Path.GetDirectoryName(diffuseTextureName), baseTextureName + "_me"+ Path.GetExtension(diffuseTextureName));
            if (File.Exists(srcDiffuseTex))
            {
                return Path.Combine(Path.GetDirectoryName(diffuseTextureName), baseTextureName + "_me" + Path.GetExtension(diffuseTextureName)); ;
            }
            srcDiffuseTex = Path.Combine(MyFileSystem.ContentPath, Path.GetDirectoryName(diffuseTextureName), baseTextureName + "_de" + Path.GetExtension(diffuseTextureName));
            if (File.Exists(srcDiffuseTex))
            {
                return Path.Combine(Path.GetDirectoryName(diffuseTextureName), baseTextureName + "_de" + Path.GetExtension(diffuseTextureName)); ;
            }
            return diffuseTextureName;
        }

        private static MyCubePart[] GetCubeParts(MyCubeBlockDefinition block, Vector3I position, MatrixD rotation, float gridSize)
        {
            //Called only on init - we can afford allocation here
            List<string> models = new List<string>();
            List<MatrixD> matrices = new List<MatrixD>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> patternOffsets = new List<Vector2>();

            GetCubeParts(block, position, rotation, gridSize, models, matrices, normals, patternOffsets);

            MyCubePart[] parts = new MyCubePart[models.Count];

            for (int i = 0; i < parts.Length; i++)
            {
                var part = new MyCubePart();
                part.Init(MyModels.GetModelOnlyData(models[i]), matrices[i]);
                part.InstanceData.SetTextureOffset(patternOffsets[i]);
                parts[i] = part;
            }

            return parts;
        }

        private static bool TestPlacementAreaInternal(MyCubeGrid targetGrid,
           ref MyGridPlacementSettings settings,
           MyCubeBlockDefinition blockDefinition,
           MyBlockOrientation? blockOrientation,
           ref BoundingBoxD localAabb,
           MyEntity ignoredEntity,
           ref MatrixD worldMatrix,
           out MyCubeGrid touchingGrid,
           bool dynamicBuildMode = false)
        {
            return TestPlacementAreaInternal(targetGrid, targetGrid != null ? targetGrid.IsStatic : !dynamicBuildMode,
                ref settings, blockDefinition, blockOrientation, ref localAabb, ignoredEntity, ref worldMatrix, out touchingGrid, dynamicBuildMode: dynamicBuildMode);
        }

        private static bool TestPlacementAreaInternalWithEntities(MyCubeGrid targetGrid,
         bool targetGridIsStatic,
         ref MyGridPlacementSettings settings,
         ref BoundingBoxD localAabb,
         MyEntity ignoredEntity,
         ref MatrixD worldMatrix,
         bool dynamicBuildMode = false)
        {
            ProfilerShort.Begin("TestPlacementAreaInternalWithEntities");

            MyCubeGrid touchingGrid = null;

            float gridSize = targetGrid.GridSize;
            bool isStatic = targetGridIsStatic;

            var worldAabb = localAabb.Transform(ref worldMatrix);

            bool entityOverlap = false;
            MyVoxelBase overlappedVoxelMap = null;
            bool touchingStaticGrid = false;
            foreach (var entity in m_tmpResultList)
            {
                if (ignoredEntity != null && (entity == ignoredEntity || entity.GetTopMostParent() == ignoredEntity))
                    continue;

                var body = entity.Physics;
                if (body == null)
                    continue;

                var voxelMap = entity as MyVoxelBase;
                if (voxelMap != null)
                {
                    overlappedVoxelMap = voxelMap;
                    continue;
                }

                var grid = entity as MyCubeGrid;
                if (grid != null)
                {
                    // Small on large (or large on small) always possible
                    if (isStatic == grid.IsStatic && gridSize != grid.GridSize)
                        continue;

                    TestGridPlacement(ref settings, ref worldMatrix, ref touchingGrid, gridSize, isStatic, ref worldAabb, null, null, ref entityOverlap, ref touchingStaticGrid, grid);

                    if (entityOverlap)
                    {
                        break;
                    }
                }
                else
                {
                    var character = entity as MyCharacter;
                    if (character != null && character.PositionComp.WorldAABB.Intersects(targetGrid.PositionComp.WorldAABB))
                    {
                        entityOverlap = true;
                        break;
                    }
                }
            }

            m_tmpResultList.Clear();
            ProfilerShort.End();

            if (entityOverlap)
                return false;

            if (targetGrid.IsStatic)
            {
                return true;
            }

            foreach (var block in targetGrid.GetBlocks())
            {
                if (IsInVoxels(block,false))
                    return false;
            }
            return true;
        }

        private static void TestGridPlacement(ref MyGridPlacementSettings settings, ref MatrixD worldMatrix, ref MyCubeGrid touchingGrid, float gridSize, bool isStatic, ref BoundingBoxD worldAabb, MyCubeBlockDefinition blockDefinition,
           MyBlockOrientation? blockOrientation, ref bool entityOverlap, ref bool touchingStaticGrid, MyCubeGrid grid)
        {
            var invWorldMatrix = grid.PositionComp.WorldMatrixNormalizedInv;
            var otherLocalAabb = worldAabb.Transform(ref invWorldMatrix);

            var scaledMin = (otherLocalAabb.Min + gridSize / 2) / grid.GridSize;
            var scaledMax = (otherLocalAabb.Max - gridSize / 2) / grid.GridSize;
            var min = Vector3I.Round(scaledMin);
            var max = Vector3I.Round(scaledMax);

            MyBlockOrientation? gridBlockOrientation = null;
            if (MyFakes.ENABLE_COMPOUND_BLOCKS && isStatic && grid.IsStatic && blockOrientation != null)
            {
                Matrix blockRotation;
                blockOrientation.Value.GetMatrix(out blockRotation);
                Matrix rotationInGrid = blockRotation * worldMatrix;
                rotationInGrid = rotationInGrid * invWorldMatrix;
                rotationInGrid.Translation = Vector3.Zero;

                Base6Directions.Direction forwardDir = Base6Directions.GetForward(ref rotationInGrid);
                Base6Directions.Direction upDir = Base6Directions.GetUp(ref rotationInGrid);
                if (Base6Directions.IsValidBlockOrientation(forwardDir, upDir))
                    gridBlockOrientation = new MyBlockOrientation(forwardDir, upDir);
            }

            if (!grid.CanAddCubes(min, max, gridBlockOrientation, blockDefinition))
            {
                entityOverlap = true;
                return;
            }

            if (settings.CanAnchorToStaticGrid && grid.IsTouchingAnyNeighbor(min, max))
            {
                touchingStaticGrid = true;
                if (touchingGrid == null)
                    touchingGrid = grid;
            }
        }

        private static bool TestPlacementAreaInternal(MyCubeGrid targetGrid,
           bool targetGridIsStatic,
           ref MyGridPlacementSettings settings,
           MyCubeBlockDefinition blockDefinition,
           MyBlockOrientation? blockOrientation,
           ref BoundingBoxD localAabb,
           MyEntity ignoredEntity,
           ref MatrixD worldMatrix,
           out MyCubeGrid touchingGrid,
           bool dynamicBuildMode = false)
        {
            ProfilerShort.Begin("TestPlacementAreaInternal");

            touchingGrid = null;

            float gridSize = targetGrid != null ? targetGrid.GridSize : (blockDefinition != null ? MyDefinitionManager.Static.GetCubeSize(blockDefinition.CubeSize) : MyDefinitionManager.Static.GetCubeSize(MyCubeSize.Large));
            bool isStatic = targetGridIsStatic;

            var worldAabb = localAabb.Transform(ref worldMatrix);

            bool entityOverlap = false;
            MyVoxelBase overlappedVoxelMap = null;
            bool touchingStaticGrid = false;
            foreach (var rigidBody in m_physicsBoxQueryList)
            {
                var entity = rigidBody.GetEntity();
                if (entity == null)
                    continue;

                if (ignoredEntity != null && (entity == ignoredEntity || entity.GetTopMostParent() == ignoredEntity))
                    continue;

                var body = rigidBody.GetBody();
                if (body != null && body.IsPhantom)
                    continue;

                var voxelMap = entity as MyVoxelBase;
                if (voxelMap != null)
                {
                    overlappedVoxelMap = voxelMap;
                    continue;
                }

                var grid = entity as MyCubeGrid;
                if (grid != null && ((isStatic && grid.IsStatic)
                    || (MyFakes.ENABLE_DYNAMIC_SMALL_GRID_MERGING && !isStatic && !grid.IsStatic && blockDefinition != null && blockDefinition.CubeSize == grid.GridSizeEnum)
                    || (MyFakes.ENABLE_BLOCK_PLACEMENT_ON_VOXEL && isStatic && grid.IsStatic && blockDefinition != null && blockDefinition.CubeSize == grid.GridSizeEnum)))
                {
                    // Small on large (or large on small) always possible
                    if (isStatic == grid.IsStatic && gridSize != grid.GridSize)
                        continue;

                    TestGridPlacement(ref settings, ref worldMatrix, ref touchingGrid, gridSize, isStatic, ref worldAabb, blockDefinition, blockOrientation, ref entityOverlap, ref touchingStaticGrid, grid);
                    if (entityOverlap)
                    {
                        break;
                    }
                    continue;
                }


                entityOverlap = true;
                break;
            }
            m_tmpResultList.Clear();
            m_physicsBoxQueryList.Clear();
            ProfilerShort.End();

            if (entityOverlap)
                return false;

            return TestVoxelOverlap(ref settings, ref localAabb, ref worldMatrix, ref worldAabb, ref overlappedVoxelMap, touchingStaticGrid);
        }

        private static bool TestVoxelOverlap(ref MyGridPlacementSettings settings, ref BoundingBoxD localAabb, ref MatrixD worldMatrix, ref BoundingBoxD worldAabb, ref MyVoxelBase overlappedVoxelMap, bool touchingStaticGrid)
        {
            ProfilerShort.Begin("VoxelOverlap");
            try
            {
                if (MyFakes.ENABLE_VOXEL_MAP_AABB_CORNER_TEST)
                {
                    return TestPlacementVoxelMapOverlap(overlappedVoxelMap, ref settings, ref localAabb, ref worldMatrix, touchingStaticGrid: touchingStaticGrid);
                }
                else
                {
                    if (overlappedVoxelMap == null)
                    { // Havok only detects overlap with voxel map surface. This test will detect a voxel map even if we're fully inside it.

                        overlappedVoxelMap = MySession.Static.VoxelMaps.GetVoxelMapWhoseBoundingBoxIntersectsBox(ref worldAabb, null);
                        if (overlappedVoxelMap != null)
                        {
                            //We have just test, if aabb is not completelly inside voxelmap
                            if (!overlappedVoxelMap.IsOverlapOverThreshold(worldAabb))
                                overlappedVoxelMap = null;
                        }
                    }
                    return TestPlacementVoxelMapPenetration(overlappedVoxelMap, ref settings, ref localAabb, ref worldMatrix, touchingStaticGrid: touchingStaticGrid);
                }
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        public static bool TestPlacementVoxelMapOverlap(
            MyVoxelBase voxelMap,
            ref MyGridPlacementSettings settings,
            ref BoundingBoxD localAabb,
            ref MatrixD worldMatrix,
            bool touchingStaticGrid = false)
        {
            ProfilerShort.Begin("TestPlacementVoxelMapOverlap");

            var worldAabb = localAabb.Transform(ref worldMatrix);

            const int IntersectsOrInside = 1;
            const int Outside = 2;

            int overlapState = Outside;

            if (voxelMap == null)
                voxelMap = MySession.Static.VoxelMaps.GetVoxelMapWhoseBoundingBoxIntersectsBox(ref worldAabb, null);

            if (voxelMap != null && voxelMap.IsAnyAabbCornerInside(ref worldMatrix, localAabb))
            {
                overlapState = IntersectsOrInside;
            }

            bool testPassed = true;

            switch (overlapState)
            {
                case IntersectsOrInside:
                    testPassed = settings.Penetration.MaxAllowed > 0;
                    break;
                case Outside:
                    testPassed = settings.Penetration.MinAllowed <= 0 || (settings.CanAnchorToStaticGrid && touchingStaticGrid);
                    break;
                default:
                    Debug.Fail("Invalid branch.");
                    break;
            }

            ProfilerShort.End();

            return testPassed;
        }

        private static bool TestPlacementVoxelMapPenetration(
            MyVoxelBase voxelMap, 
            ref MyGridPlacementSettings settings,
            ref BoundingBoxD localAabb,
            ref MatrixD worldMatrix,
            bool touchingStaticGrid = false)
        {
            ProfilerShort.Begin("TestPlacementVoxelMapPenetration");
            var worldAabb = localAabb.Transform(ref worldMatrix);

            float penetrationAmountNormalized = 0f;
            float penetrationRatio = 0f;
            float penetrationVolume = 0f;
            if (voxelMap != null)
            {
                float unused;
                penetrationAmountNormalized = voxelMap.GetVoxelContentInBoundingBox_Obsolete(worldAabb, out unused);
                penetrationVolume = penetrationAmountNormalized * MyVoxelConstants.VOXEL_VOLUME_IN_METERS;
                penetrationRatio = penetrationVolume / (float)worldAabb.Volume;
            }

            bool penetrationTestPassed = true;
            switch (settings.Penetration.Unit)
            {
                case MyGridPlacementSettings.PenetrationUnitEnum.Absolute:
                    penetrationTestPassed = penetrationVolume <= settings.Penetration.MaxAllowed &&
                        (penetrationVolume >= settings.Penetration.MinAllowed || (settings.CanAnchorToStaticGrid && touchingStaticGrid));
                    break;

                case MyGridPlacementSettings.PenetrationUnitEnum.Ratio:
                    penetrationTestPassed = penetrationRatio <= settings.Penetration.MaxAllowed &&
                        (penetrationRatio >= settings.Penetration.MinAllowed || (settings.CanAnchorToStaticGrid && touchingStaticGrid));
                    break;

                default:
                    Debug.Fail("Invalid branch.");
                    break;
            }

            ProfilerShort.End();

            return penetrationTestPassed;
        }


        /// <summary>
        /// Fills passed lists with mount point data, which is transformed using orientation
        /// of the block.
        /// </summary>
        /// <param name="outMountPoints">Output buffer.</param>
        /// <param name="performCorrection">True when you want to have correction performed for when rotation of fractional values would have different result than integers.</param>
        public static void TransformMountPoints(List<MyCubeBlockDefinition.MountPoint> outMountPoints, MyCubeBlockDefinition def, ref MyBlockOrientation orientation)
        {
            Debug.Assert(outMountPoints != null);

            outMountPoints.Clear();

            var defMountPoints = def.MountPoints;
            if (defMountPoints == null)
                return;

            Matrix rotation;
            orientation.GetMatrix(out rotation);

            var center = def.Center;
            for (int i = 0; i < defMountPoints.Length; ++i)
            {
                var mp = new MyCubeBlockDefinition.MountPoint();
                var centeredStart = defMountPoints[i].Start - center;
                var centeredEnd = defMountPoints[i].End - center;
                Vector3I.Transform(ref defMountPoints[i].Normal, ref rotation, out mp.Normal);
                Vector3.Transform(ref centeredStart, ref rotation, out mp.Start);
                Vector3.Transform(ref centeredEnd, ref rotation, out mp.End);
                mp.ExclusionMask = defMountPoints[i].ExclusionMask;
                mp.PropertiesMask = defMountPoints[i].PropertiesMask;

                // Correction of situations when 0.5 would get transformed to -0.5, resulting in different floor() (integer 0 is transformed to 0).
                var startICorrect = Vector3I.Floor(defMountPoints[i].Start) - center;
                var endICorrect = Vector3I.Floor(defMountPoints[i].End) - center;
                Vector3I.Transform(ref startICorrect, ref rotation, out startICorrect);
                Vector3I.Transform(ref endICorrect, ref rotation, out endICorrect);

                var startI = Vector3I.Floor(mp.Start);
                var endI = Vector3I.Floor(mp.End);
                var startCorrection = startICorrect - startI;
                var endCorrection = endICorrect - endI;

                mp.Start += startCorrection;
                mp.End += endCorrection;

                outMountPoints.Add(mp);
            }
        }

        internal static MyObjectBuilder_CubeBlock CreateBlockObjectBuilder(MyCubeBlockDefinition definition, Vector3I min, MyBlockOrientation orientation, long entityID, long owner, bool fullyBuilt)
        {
            MyObjectBuilder_CubeBlock objectBuilder = (MyObjectBuilder_CubeBlock)MyObjectBuilderSerializer.CreateNewObject(definition.Id);
            objectBuilder.BuildPercent = fullyBuilt ? 1 : MyComponentStack.MOUNT_THRESHOLD;
            objectBuilder.IntegrityPercent = fullyBuilt ? 1 : MyComponentStack.MOUNT_THRESHOLD;
            objectBuilder.EntityId = entityID;
            objectBuilder.Min = min;
            objectBuilder.BlockOrientation = orientation;

            if (definition.ContainsComputer())
            {
                objectBuilder.Owner = 0;
                objectBuilder.ShareMode = MyOwnershipShareModeEnum.All;
            }

            return objectBuilder;
        }

        private static Vector3 ConvertVariantToHsvColor(Color variantColor)
        {
            switch (variantColor.PackedValue)
            {
                case 4278190335: //red
                    return MyRenderComponentBase.OldRedToHSV;
                case 4278255615: //yellow
                    return MyRenderComponentBase.OldYellowToHSV;
                case 4294901760: //blue
                    return MyRenderComponentBase.OldBlueToHSV;
                case 4278222848: //green
                    return MyRenderComponentBase.OldGreenToHSV;
                case 4278190080: //black
                    return MyRenderComponentBase.OldBlackToHSV;
                case 4294967295: //white
                    return MyRenderComponentBase.OldWhiteToHSV;
                case 4286611584: //gray
                default:
                    return MyRenderComponentBase.OldGrayToHSV;
            }
        }

        internal static MyObjectBuilder_CubeBlock FindDefinitionUpgrade(MyObjectBuilder_CubeBlock block, out MyCubeBlockDefinition blockDefinition)
        {
            foreach (var def in MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>())
            {
                if (def.Id.SubtypeId == block.SubtypeId)
                {
                    blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(def.Id);
                    return MyObjectBuilder_CubeBlock.Upgrade(block, blockDefinition.Id.TypeId, block.SubtypeName);
                }
            }
            blockDefinition = null;
            return null;
        }

        #endregion

        #region Nested types

        private struct TriangleWithMaterial
        {
            public MyTriangleVertexIndices triangle;
            public MyTriangleVertexIndices uvIndices;
            public string material;
        }

        #endregion

        /// <summary>
        /// Converts world coordinate to static global grid uniform coordinate (virtual large grid in whole world which every large grid is snapped to). 
        /// Grid size is already used inside calculation.
        /// </summary>
        public static Vector3I StaticGlobalGrid_WorldToUGInt(Vector3D worldPos, float gridSize, bool staticGridAlignToCenter)
        {
            return Vector3I.Round(StaticGlobalGrid_WorldToUG(worldPos, gridSize, staticGridAlignToCenter));
        }

        /// <summary>
        /// Converts world coordinate to static global grid uniform coordinate (virtual large grid in whole world which every large grid is snapped to). 
        /// Grid size is already used inside calculation.
        /// </summary>
        public static Vector3D StaticGlobalGrid_WorldToUG(Vector3D worldPos, float gridSize, bool staticGridAlignToCenter)
        {
            Vector3D result = worldPos / gridSize;
            if (!staticGridAlignToCenter)
                result += Vector3D.Half;
            return result;
        }

        /// <summary>
        /// Converts static global uniform grid coordinate to world coordinate. 
        /// Grid size is already used inside calculation.
        /// </summary>
        public static Vector3D StaticGlobalGrid_UGToWorld(Vector3D ugPos, float gridSize, bool staticGridAlignToCenter)
        {
            if (staticGridAlignToCenter)
                return gridSize * ugPos;
            else
                return gridSize * (ugPos - Vector3D.Half);
        }

        private static Type ChooseGridSystemsType()
        {
            Type result = typeof(MyCubeGridSystems);
            ChooseGridSystemsType(ref result, MyPlugins.GameAssembly);
            ChooseGridSystemsType(ref result, MyPlugins.SandboxAssembly);
            ChooseGridSystemsType(ref result, MyPlugins.UserAssembly);
            return result;
        }

        private static void ChooseGridSystemsType(ref Type gridSystemsType, Assembly assembly)
        {
            if (assembly == null)
                return;

            foreach (var type in assembly.GetTypes())
            {
                if (typeof(MyCubeGridSystems).IsAssignableFrom(type))
                {
                    gridSystemsType = type;
                    break;
                }
            }
        }

        public static bool ShouldBeStatic(MyCubeGrid grid)
        {
            if (grid.GridSizeEnum == MyCubeSize.Small && MyCubeGridSmallToLargeConnection.Static != null &&
                MyCubeGridSmallToLargeConnection.Static.TestGridSmallToLargeConnection(grid))
                return true;

            foreach (var block in grid.GetBlocks())
            {
                if (IsInVoxels(block))
                    return true;
            }
            return false;
        }

        public static bool IsInVoxels(MySlimBlock block,bool checkForPhysics = true)
        {
            if (block.CubeGrid.Physics == null && checkForPhysics)
               return false;

            if (MyPerGameSettings.Destruction && block.CubeGrid.GridSizeEnum == Common.ObjectBuilders.MyCubeSize.Large)
                return block.CubeGrid.Physics.Shape.BlocksConnectedToWorld.Contains(block.Position);

            var min = (Vector3)block.Min;
            var max = (Vector3)block.Max;
            min -= 0.5f;
            max += 0.5f;
            var gridSize = block.CubeGrid.GridSize;
            min *= gridSize;
            max *= gridSize;
            BoundingBox localAabb = new BoundingBox(min, max);

            var worldMat = block.CubeGrid.WorldMatrix;

            var worldAabb = (BoundingBoxD)localAabb.Transform(worldMat);

            List<MyEntity> entities = new List<MyEntity>(); // Fine for test
            MyGamePruningStructure.GetAllEntitiesInBox(ref worldAabb, entities);
            MyVoxelBase overlappedVoxelMap = null;
            foreach (var entity in entities)
            {
                var voxelMap = entity as MyVoxelBase;
                if (voxelMap != null)
                {
                    if (voxelMap.DoOverlapSphereTest(localAabb.Size.AbsMax() / 2.0f, worldAabb.Center))
                    {
                        overlappedVoxelMap = voxelMap;
                        break;
                    }
                }
            }

            float penetrationRatio = 0.0f;
            if (overlappedVoxelMap != null)
            {
                float unused;
                var penetrationAmountNormalized = overlappedVoxelMap.GetVoxelContentInBoundingBox_Obsolete(worldAabb, out unused);
                var penetrationVolume = penetrationAmountNormalized * MyVoxelConstants.VOXEL_VOLUME_IN_METERS;
                penetrationRatio = penetrationVolume / (float)worldAabb.Volume;
            }

            return penetrationRatio > 0.125f;
        }
    }

    struct BlockMaterial
    {
        MyExportModel.Material Base;
        Color DiffuseColor;
    }

    public class MyExportModel
    {
        public struct Material
        {
            public string NewDiffuseTexture;
            public string Name;
            public int FirstTri;
            public int LastTri;
            public string DiffuseTexture;
            public string NormalTexture;
            public bool IsGlass;
            public Vector3 ColorMaskHSV;
        }
        MyModel m_model;
        List<Material> m_materials;

        public MyExportModel(MyModel model)
        {
            m_model = model;
            m_model.LoadData();
            ExtractMaterialsFromModel();
        }

        public HalfVector2[] GetTexCoords()
        {
            return m_model.TexCoords;
        }

        public List<Material> GetMaterials()
        {
            return m_materials;
        }

        public int GetVerticesCount()
        {
            return m_model.GetVerticesCount();
        }

        public int GetTrianglesCount()
        {
            return m_model.GetTrianglesCount();
        }

        public MyTriangleVertexIndices GetTriangle(int index)
        {
            return m_model.GetTriangle(index);
        }

        private void ExtractMaterialsFromModel()
        {
            m_materials = new List<Material>();

            List<MyMesh> meshList = m_model.GetMeshList();
            if (meshList != null)
            {
                foreach (var mesh in meshList)
                {
                    string meshName = Path.GetFileName(mesh.AssetName);
                    if (mesh.Material != null)
                    {
                        string materialName = mesh.Material.Name;
                        if (false == string.IsNullOrEmpty(materialName))
                        {
                            materialName = materialName.Replace(' ', '_');
                            materialName = materialName.Replace('-', '_');
                            materialName = meshName + "_" + materialName;
                            m_materials.Add(new Material()
                            {
                                Name = materialName,
                                FirstTri = mesh.IndexStart / 3,
                                LastTri = mesh.IndexStart / 3 + mesh.TriCount - 1,
                                DiffuseTexture = mesh.Material.DiffuseTexture,
                                IsGlass = mesh.Material.DrawTechnique == MyMeshDrawTechnique.GLASS,
                            });
                        }
                    }
                }
            }
        }
    }

}
