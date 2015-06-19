#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Collections;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using Sandbox.Engine.Multiplayer;

#endregion

namespace Sandbox.Game.Entities
{
    public abstract class MyBlockBuilderBase : MySessionComponentBase
    {
        protected static readonly MyStringId[] m_rotationControls = new MyStringId[]
        {
            MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE,
            MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE,
            MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE,
            MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE,
            MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE,
            MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE,
        };

        public static float DEFAULT_BLOCK_BUILDING_DISTANCE = MyFakes.ENABLE_CUBE_BUILDER_DYNAMIC_MODE ? 20f : 50f;
        public static float MAX_BLOCK_BUILDING_DISTANCE = MyFakes.ENABLE_CUBE_BUILDER_DYNAMIC_MODE ? 20f : 50f;
        public static float MIN_BLOCK_BUILDING_DISTANCE = 1f;

        protected static readonly int[] m_rotationDirections = new int[6] { -1, 1, 1, -1, 1, -1 };
        private static readonly List<MyPhysics.HitInfo> m_tmpHitList = new List<MyPhysics.HitInfo>();

        protected MyCubeGrid m_currentGrid;
        protected internal abstract MyCubeGrid CurrentGrid {get; protected set; }
        protected MatrixD m_invGridWorldMatrix = MatrixD.Identity;

        protected MyVoxelMap m_currentVoxelMap;
        protected internal abstract MyVoxelMap CurrentVoxelMap { get; protected set; }

        protected abstract MyCubeBlockDefinition CurrentBlockDefinition { get; set; }


        // Current hit info from havok's cast ray.
        protected MyPhysics.HitInfo? m_hitInfo;
        internal MyPhysics.HitInfo? HitInfo
        {
            get
            {
                return m_hitInfo;
            }
        }

        public abstract MyCubeBlockDefinition HudBlockDefinition { get; }

        private static bool AdminSpectatorIsBuilding
        {
            get
            {
                return MyFakes.ENABLE_ADMIN_SPECTATOR_BUILDING && MySession.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator
                    && MyMultiplayer.Static != null && MyMultiplayer.Static.IsAdmin(MySession.LocalHumanPlayer.Id.SteamId) && MySession.Static != null && !MySession.Static.Battle;
            }
        }

        private static bool DeveloperSpectatorIsBuilding
        {
            get
            {
                return MySession.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator &&
                    (!MyFinalBuildConstants.IS_OFFICIAL || !MySession.Static.SurvivalMode || MyInput.Static.ENABLE_DEVELOPER_KEYS);
            }
        }

        public static bool SpectatorIsBuilding
        {
            get
            {
                return DeveloperSpectatorIsBuilding || AdminSpectatorIsBuilding;
            }
        }



        public static bool CameraControllerSpectator
        {
            get
            {
                var cameraController = MySession.GetCameraControllerEnum();
                return cameraController == MyCameraControllerEnum.Spectator || cameraController == MyCameraControllerEnum.SpectatorDelta;
            }
        }

        public static Vector3D IntersectionStart
        {
            get
            {
				var cameraController = MySession.GetCameraControllerEnum();
				return (cameraController == MyCameraControllerEnum.Entity
						|| cameraController == MyCameraControllerEnum.ThirdPersonSpectator) ? MySession.ControlledEntity.GetHeadMatrix(false).Translation : MySector.MainCamera.Position;
            }
        }

        public static Vector3D IntersectionDirection
        {
            get
            {
                return MySector.MainCamera.ForwardVector;
            }
        }

        public Vector3D FreePlacementTarget
        {
            get
            {
                return IntersectionStart + IntersectionDirection * IntersectionDistance;
            }
        }

        protected internal float IntersectionDistance = DEFAULT_BLOCK_BUILDING_DISTANCE;

        public abstract bool IsActivated { get; }
        public abstract void Activate();
        public abstract void Deactivate();

        protected virtual void ChooseGrid()
        {
            CurrentGrid = FindClosestGrid();
            m_invGridWorldMatrix = CurrentGrid != null ? Matrix.Invert(CurrentGrid.WorldMatrix) : Matrix.Identity;
        }

        internal virtual void ChoosePlacementObject()
        {
            MyCubeGrid grid;
            MyVoxelMap voxelMap;
            FindClosestPlacementObject(out grid, out voxelMap);

            CurrentGrid = grid;
            CurrentVoxelMap = voxelMap;

            Debug.Assert((CurrentGrid == null && CurrentVoxelMap == null) || (CurrentGrid != null && CurrentVoxelMap == null) || (CurrentGrid == null && CurrentVoxelMap != null));

            m_invGridWorldMatrix = CurrentGrid != null ? MatrixD.Invert(CurrentGrid.WorldMatrix) : MatrixD.Identity;
        }

        protected static void AddFastBuildModelWithSubparts(ref MatrixD matrix, List<MatrixD> matrices, List<string> models, MyCubeBlockDefinition blockDefinition)
        {
            if (string.IsNullOrEmpty(blockDefinition.Model))
                return;

            matrices.Add(matrix);
            models.Add(blockDefinition.Model);
            var data = new MyEntitySubpart.Data();

            MyCubeBlockDefinition subBlockDefinition;
            MatrixD subBlockMatrix;
            Vector3 dummyPosition;

            MyModel modelData = MyModels.GetModelOnlyData(blockDefinition.Model);
            foreach (var dummy in modelData.Dummies)
            {
                if (MyEntitySubpart.GetSubpartFromDummy(blockDefinition.Model, dummy.Key, dummy.Value, ref data)) 
                {
                    MatrixD mCopy = MatrixD.Multiply(data.InitialTransform, matrix);
                    matrices.Add(mCopy);
                    models.Add(data.File);
                }
                else if (MyFakes.ENABLE_SUBBLOCKS 
                    && MyCubeBlock.GetSubBlockDataFromDummy(blockDefinition, dummy.Key, dummy.Value, false, out subBlockDefinition, out subBlockMatrix, out dummyPosition))
                {
                    if (!string.IsNullOrEmpty(subBlockDefinition.Model)) 
                    {
                        // Repair subblock matrix to have int axes (because preview renderer does not allow such non integer rotation).
                        Vector3I forward = Vector3I.Round(Vector3.DominantAxisProjection(subBlockMatrix.Forward));
                        Vector3I invForward = Vector3I.One - Vector3I.Abs(forward);
                        Vector3I right = Vector3I.Round(Vector3.DominantAxisProjection((Vector3)subBlockMatrix.Right * invForward));
                        Vector3I up;
                        Vector3I.Cross(ref right, ref forward, out up);

                        subBlockMatrix.Forward = forward;
                        subBlockMatrix.Right = right;
                        subBlockMatrix.Up = up;

                        MatrixD mCopy = MatrixD.Multiply(subBlockMatrix, matrix);
                        matrices.Add(mCopy);
                        models.Add(subBlockDefinition.Model);
                    }
                }

            }
        }

        public MyCubeGrid FindClosestGrid()
        {
            LineD line = new LineD(IntersectionStart, IntersectionStart + IntersectionDirection * IntersectionDistance);

            m_tmpHitList.Clear();
            MyPhysics.CastRay(line.From, line.To, m_tmpHitList, MyPhysics.ObjectDetectionCollisionLayer);
            // Remove character hits.
            m_tmpHitList.RemoveAll(delegate(MyPhysics.HitInfo hit)
            {
                return (hit.HkHitInfo.Body.GetEntity() == MySession.ControlledEntity.Entity);
            });

            if (m_tmpHitList.Count == 0)
                return null;

            MyCubeGrid closestGrid = m_tmpHitList[0].HkHitInfo.Body.GetEntity() as MyCubeGrid;
            return closestGrid;
        }

        /// <summary>
        /// Finds closest object (grid or voxel map) for placement of blocks .
        /// </summary>
        public bool FindClosestPlacementObject(out MyCubeGrid closestGrid, out MyVoxelMap closestVoxelMap)
        {
            closestGrid = null;
            closestVoxelMap = null;

            if (MySession.ControlledEntity == null) return false;

            m_hitInfo = null;

            LineD line = new LineD(IntersectionStart, IntersectionStart + IntersectionDirection * IntersectionDistance);

            MyPhysics.CastRay(line.From, line.To, m_tmpHitList, MyPhysics.ObjectDetectionCollisionLayer);
            // Remove character hits.
            m_tmpHitList.RemoveAll(delegate(MyPhysics.HitInfo hit)
            {
                return (hit.HkHitInfo.Body.GetEntity() == MySession.ControlledEntity.Entity);
            });

            if (m_tmpHitList.Count == 0)
                return false;

            closestGrid = m_tmpHitList[0].HkHitInfo.Body.GetEntity() as MyCubeGrid;
            if (closestGrid != null)
                m_hitInfo = m_tmpHitList[0];

            if (MyFakes.ENABLE_BLOCK_PLACEMENT_ON_VOXEL)
            {
                closestVoxelMap = m_tmpHitList[0].HkHitInfo.Body.GetEntity() as MyVoxelMap;
                if (closestVoxelMap != null)
                    m_hitInfo = m_tmpHitList[0];
            }

            return closestGrid != null || closestVoxelMap != null;
        }

        protected Vector3I? IntersectCubes(MyCubeGrid grid, out double distance)
        {
            distance = float.MaxValue;

            var line = new LineD(IntersectionStart, IntersectionStart + IntersectionDirection * IntersectionDistance);
            Vector3I position = Vector3I.Zero;
            double dstSqr = double.MaxValue;

            if (grid.GetLineIntersectionExactGrid(ref line, ref position, ref dstSqr))
            {
                distance = Math.Sqrt(dstSqr);
                return position;
            }
            return null;
        }

        /// <summary>
        /// Calculates exact intersection point (in uniform grid coordinates) of eye ray with the given grid of all cubes.
        /// Returns position of intersected object in uniform grid coordinates
        /// </summary>
        protected Vector3D? IntersectExact(MyCubeGrid grid, ref MatrixD inverseGridWorldMatrix, out Vector3D intersection, out MySlimBlock intersectedBlock)
        {
            intersection = Vector3D.Zero;

            var line = new LineD(IntersectionStart, IntersectionStart + IntersectionDirection * IntersectionDistance);
            double distance;
            Vector3D? intersectedObjectPos = grid.GetLineIntersectionExactAll(ref line, out distance, out intersectedBlock);
            if (intersectedObjectPos != null)
            {
                Vector3D rayStart = Vector3D.Transform(IntersectionStart, inverseGridWorldMatrix);
                Vector3D rayDir = Vector3D.Normalize(Vector3D.TransformNormal(IntersectionDirection, inverseGridWorldMatrix));
                intersection = rayStart + distance * rayDir;
                intersection *= 1.0f / grid.GridSize;
            }

            return intersectedObjectPos;
        }

        /// <summary>
        /// Calculates exact intersection point (in uniform grid coordinates) from stored havok's hit info object obtained during FindClosest grid.
        /// Returns position of intersected object in uniform grid coordinates.
        /// </summary>
        protected Vector3D? GetIntersectedBlockData(ref MatrixD inverseGridWorldMatrix, out Vector3D intersection, out MySlimBlock intersectedBlock, out ushort? compoundBlockId)
        {
            Debug.Assert(m_hitInfo != null);
            Debug.Assert(m_hitInfo.Value.HkHitInfo.Body.GetEntity() == CurrentGrid);

            intersection = Vector3D.Zero;
            intersectedBlock = null;
            compoundBlockId = null;

            double distance = double.MaxValue;
            Vector3D? intersectedObjectPos = null;

            var line = new LineD(IntersectionStart, IntersectionStart + IntersectionDirection * IntersectionDistance);
            Vector3I position = Vector3I.Zero;
            if (!CurrentGrid.GetLineIntersectionExactGrid(ref line, ref position, ref distance, m_hitInfo.Value))
                return null;

            distance = Math.Sqrt(distance);
            intersectedObjectPos = position;

            intersectedBlock = CurrentGrid.GetCubeBlock(position);
            if (intersectedBlock == null)
                return null;

            // Compound block - get index of internal block for removing
            if (intersectedBlock.FatBlock is MyCompoundCubeBlock)
            {
                MyCompoundCubeBlock compoundBlock = intersectedBlock.FatBlock as MyCompoundCubeBlock;
                ushort? idInCompound = null;

                ushort blockId;
                MyIntersectionResultLineTriangleEx? triIntersection;

                if (compoundBlock.GetIntersectionWithLine(ref line, out triIntersection, out blockId))
                    idInCompound = blockId;
                else if (compoundBlock.GetBlocksCount() == 1) // If not intersecting with any internal block and there is only one then set the index to it
                    idInCompound = compoundBlock.GetBlockId(compoundBlock.GetBlocks()[0]);

                compoundBlockId = idInCompound;
            }

            Debug.Assert(intersectedObjectPos != null);
            Vector3D rayStart = Vector3D.Transform(IntersectionStart, inverseGridWorldMatrix);
            Vector3D rayDir = Vector3D.Normalize(Vector3D.TransformNormal(IntersectionDirection, inverseGridWorldMatrix));
            intersection = rayStart + distance * rayDir;
            intersection *= 1.0f / CurrentGrid.GridSize;

            return intersectedObjectPos;
        }

        protected void IntersectInflated(List<Vector3I> outHitPositions, MyCubeGrid grid)
        {
            float maxDist = 2000;
            var gridSizeInflate = new Vector3I(100, 100, 100);

            if (grid != null)
            {
                grid.RayCastCells(IntersectionStart, IntersectionStart + IntersectionDirection * maxDist, outHitPositions, gridSizeInflate);
            }
            else
            {
                float gridSize = MyDefinitionManager.Static.GetCubeSize(CurrentBlockDefinition.CubeSize);
                MyCubeGrid.RayCastStaticCells(IntersectionStart, IntersectionStart + IntersectionDirection * maxDist, outHitPositions, gridSize, gridSizeInflate);
            }
        }

        protected BoundingBoxD GetCubeBoundingBox(Vector3I cubePos)
        {
            var center = (Vector3D)(cubePos * CurrentGrid.GridSize);
            var bias = new Vector3(0.06f, 0.06f, 0.06f);
            return new BoundingBoxD(center - new Vector3D(CurrentGrid.GridSize / 2) - bias, center + new Vector3D(CurrentGrid.GridSize / 2) + bias);
        }

        protected bool GetCubeAddAndRemovePositions(Vector3I intersectedCube, bool placingSmallGridOnLargeStatic, out Vector3I addPos, out Vector3I addDir, out Vector3I removePos)
        {
            bool result = false;

            addPos = new Vector3I();
            addDir = new Vector3I();
            removePos = new Vector3I();

            MatrixD worldInv = MatrixD.Invert(CurrentGrid.WorldMatrix);

            Vector3D intersectionPos;
            addPos = intersectedCube;
            addDir = Vector3I.Forward;

            Vector3D rayStart = Vector3D.Transform(IntersectionStart, worldInv);
            Vector3D rayDir = Vector3D.Normalize(Vector3D.TransformNormal(IntersectionDirection, worldInv));

            RayD r = new RayD(rayStart, rayDir);

            for (int i = 0; i < 100; ++i) // Limit iterations to 100
            {
                var cubeBb = GetCubeBoundingBox(addPos);
                if (!placingSmallGridOnLargeStatic && cubeBb.Contains(rayStart) == ContainmentType.Contains)
                    break;

                double? dist = cubeBb.Intersects(r);
                if (!dist.HasValue) 
                    break;

                removePos = addPos;

                intersectionPos = rayStart + rayDir * dist.Value;
                Vector3 center = removePos * CurrentGrid.GridSize;
                Vector3I dirInt = Vector3I.Sign(Vector3.DominantAxisProjection(intersectionPos - center));

                addPos = removePos + dirInt;
                addDir = dirInt;
                result = true;

                if (!CurrentGrid.CubeExists(addPos)) 
                    break;
            }

            Debug.Assert(!result || addDir != Vector3I.Zero, "Direction vector cannot be zero");
            return result;
        }

        protected bool GetBlockAddPosition(float gridSize, bool placingSmallGridOnLargeStatic, out MySlimBlock intersectedBlock, out Vector3D intersectedBlockPos, out Vector3D intersectExactPos,
            out Vector3I addPositionBlock, out Vector3I addDirectionBlock, out ushort? compoundBlockId)
        {
            intersectedBlock = null;
            intersectedBlockPos = new Vector3D();
            intersectExactPos = new Vector3();
            addDirectionBlock = new Vector3I();
            addPositionBlock = new Vector3I();
            compoundBlockId = null;

            if (CurrentVoxelMap != null)
            {
                Vector3 hitInfoNormal = m_hitInfo.Value.HkHitInfo.Normal;
                Base6Directions.Direction closestDir = Base6Directions.GetClosestDirection(hitInfoNormal);
                Vector3I hitNormal = Base6Directions.GetIntVector(closestDir);

                double distance = IntersectionDistance * m_hitInfo.Value.HkHitInfo.HitFraction;

                Vector3D rayStart = IntersectionStart;
                Vector3D rayDir = Vector3D.Normalize(IntersectionDirection);
                Vector3D intersection = rayStart + distance * rayDir;

                // Get cube block placement position (add little threshold to hit normal direction to avoid wavy surfaces).
                addPositionBlock = MyCubeGrid.StaticGlobalGrid_WorldToUGInt(intersection + 0.1f * Vector3.Half * hitNormal * gridSize, gridSize, MyPerGameSettings.BuildingSettings.StaticGridAlignToCenter);
                addDirectionBlock = hitNormal;
                intersectedBlockPos = addPositionBlock - hitNormal;

                // Exact intersection in uniform grid coords.
                intersectExactPos = MyCubeGrid.StaticGlobalGrid_WorldToUG(intersection, gridSize, MyPerGameSettings.BuildingSettings.StaticGridAlignToCenter);
                // Project exact intersection to cube face of intersected block.
                intersectExactPos = ((Vector3.One - Vector3.Abs(hitNormal)) * intersectExactPos) + ((intersectedBlockPos + 0.5f * hitNormal) * Vector3.Abs(hitNormal));

                //Vector3 position = MyCubeGrid.StaticWorldGrid_UGToWorld(addPositionBlock);
                //Vector3 halfExtent = new Vector3(gridSize * 0.5f);
                //BoundingBox cubeBox = new BoundingBox(-halfExtent, halfExtent);

                //Matrix matrix = Matrix.CreateTranslation(position);
                //Vector4 blue = Color.Blue.ToVector4();
                //MySimpleObjectDraw.DrawTransparentBox(ref matrix, ref cubeBox, ref blue, MySimpleObjectRasterizer.Wireframe, 1, 0.04f);

                return true;
            }

            Vector3D? intersectedObjectPos = GetIntersectedBlockData(ref m_invGridWorldMatrix, out intersectExactPos, out intersectedBlock, out compoundBlockId);
            if (intersectedObjectPos == null)
                return false;

            intersectedBlockPos = intersectedObjectPos.Value;

            Vector3I removePos;
            if (!GetCubeAddAndRemovePositions(Vector3I.Round(intersectedBlockPos), placingSmallGridOnLargeStatic, out addPositionBlock, out addDirectionBlock, out removePos))
                return false;

            if (!placingSmallGridOnLargeStatic)
            {
                if (MyFakes.ENABLE_BLOCK_PLACING_ON_INTERSECTED_POSITION)
                {
                    Vector3I newRemovepos = Vector3I.Round(intersectedBlockPos);

                    if (newRemovepos != removePos)
                    {
                        if (m_hitInfo.HasValue)
                        {
                            Vector3 hitInfoNormal = m_hitInfo.Value.HkHitInfo.Normal;
                            Base6Directions.Direction closestDir = Base6Directions.GetClosestDirection(hitInfoNormal);
                            Vector3I hitNormal = Base6Directions.GetIntVector(closestDir);
                            addDirectionBlock = hitNormal;
                        }
                        removePos = newRemovepos;
                        addPositionBlock = removePos + addDirectionBlock;
                    }
                }
                else
                {
                    if (CurrentGrid.CubeExists(addPositionBlock))
                        return false;
                }
            }

            if (placingSmallGridOnLargeStatic)
                removePos = Vector3I.Round(intersectedBlockPos);

            intersectedBlockPos = removePos;
            intersectedBlock = CurrentGrid.GetCubeBlock(removePos);
            if (intersectedBlock == null)
            {
                Debug.Assert(false, "No intersected block");
                return false;
            }

            return true;
        }

        public static void ComputeSteps(Vector3I start, Vector3I end, Vector3I rotatedSize, out Vector3I stepDelta, out Vector3I counter, out int stepCount)
        {
            var offset = end - start;
            stepDelta = Vector3I.Sign(offset) * rotatedSize;
            counter = Vector3I.Abs(end - start) / rotatedSize + Vector3I.One;
            stepCount = counter.Size;
        }

       

    }
}
