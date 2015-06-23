using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.GUI;
using Sandbox.Game.World;
using Sandbox.Graphics;
using VRageMath;
using VRageRender;
using VRage.ModAPI;
using VRage;

namespace Sandbox.Game.Entities.Cube
{
    class MyGridClipboard2 : MyGridClipboard
    {
        private static List<Vector3> m_tmpCollisionPoints = new List<Vector3>();

        private List<MyCubeGrid> m_touchingGrids = new List<MyCubeGrid>();

        protected bool m_dynamicBuildAllowed;

        protected override bool AnyCopiedGridIsStatic
        {
            get
            {
                //if (!MyCubeBuilder.Static.DynamicMode)
                //    return base.AnyCopiedGridIsStatic;
                return false;
            }
        }


        public MyGridClipboard2(MyPlacementSettings settings, bool calculateVelocity = true) : base(settings, calculateVelocity)
        {
            EnableGridChangeToDynamic = false;
            m_useDynamicPreviews = false;
            m_dragDistance = 0;
        }

        public override void Update()
        {
            if (!IsActive || !m_visible)
                return;

            UpdateHitEntity();

            if (!m_visible)
            {
                Hide();
                return;
            }

            Show();
            if (m_dragDistance == 0)
                SetupDragDistance();

            UpdatePastePosition();
            UpdateGridTransformations();
            if (!MyCubeBuilder.Static.DynamicMode)
                FixSnapTransformationBase6();

            if (m_calculateVelocity)
                m_objectVelocity = (m_pastePosition - m_pastePositionPrevious) / MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            m_canBePlaced = TestPlacement();

            TestBuildingMaterials();
            UpdatePreview();

            if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "FW: " + m_pasteDirForward.ToString(), Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 20.0f), "UP: " + m_pasteDirUp.ToString(), Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 40.0f), "AN: " + m_pasteOrientationAngle.ToString(), Color.Red, 1.0f);
            }
        }

        public override void Activate()
        {
            base.Activate();
            SetupDragDistance();
        }

        private void UpdateHitEntity()
        {
            Debug.Assert(m_raycastCollisionResults.Count == 0);

            MatrixD pasteMatrix = GetPasteMatrix();
            MyPhysics.CastRay(pasteMatrix.Translation, pasteMatrix.Translation + pasteMatrix.Forward * m_dragDistance, m_raycastCollisionResults);

            m_closestHitDistSq = float.MaxValue;
            m_hitPos = new Vector3(0.0f, 0.0f, 0.0f);
            m_hitNormal = new Vector3(1.0f, 0.0f, 0.0f);
            m_hitEntity = null;

            foreach (var hit in m_raycastCollisionResults)
            {
                if (hit.HkHitInfo.Body == null)
                    continue;
                MyPhysicsBody body = (MyPhysicsBody)hit.HkHitInfo.Body.UserObject;
                if (body == null)
                    continue;
                IMyEntity entity = body.Entity;
                if ((entity is MyVoxelMap) || (entity is MyCubeGrid && entity.EntityId != PreviewGrids[0].EntityId))
                {
                    if (PreviewGrids[0].GridSizeEnum == MyCubeSize.Large && (entity is MyCubeGrid) && (entity as MyCubeGrid).GridSizeEnum == MyCubeSize.Small)
                        continue;

                    float distSq = (float)(hit.Position - pasteMatrix.Translation).LengthSquared();
                    if (distSq < m_closestHitDistSq)
                    {
                        m_closestHitDistSq = distSq;
                        m_hitPos = hit.Position;
                        m_hitNormal = hit.HkHitInfo.Normal;
                        m_hitEntity = entity;
                    }
                }
            }

            m_raycastCollisionResults.Clear();
        }

        /// <summary>
        /// Converts the given grid to static with the world matrix. Instead of grid (which must have identity rotation for static grid) we transform blocks in the grid.
        /// </summary>
        /// <param name="originalGrid">grid to be transformed</param>
        /// <param name="worldMatrix">target world transform</param>
        private static void ConvertGridBuilderToStatic(MyObjectBuilder_CubeGrid originalGrid, MatrixD worldMatrix)
        {
            originalGrid.IsStatic = true;
            originalGrid.PositionAndOrientation = new MyPositionAndOrientation(originalGrid.PositionAndOrientation.Value.Position, Vector3.Forward, Vector3.Up);

            Vector3 fw = (Vector3)worldMatrix.Forward;
            Vector3 up = (Vector3)worldMatrix.Up;
            Base6Directions.Direction fwDir = Base6Directions.GetClosestDirection(fw);
            Base6Directions.Direction upDir = Base6Directions.GetClosestDirection(up);
            if (upDir == fwDir) 
                upDir = Base6Directions.GetPerpendicular(fwDir);
            MatrixI transform = new MatrixI(Vector3I.Zero, fwDir, upDir);

            // Blocks in static grid - must be recreated for static grid with different orientation and position
            foreach (var origBlock in originalGrid.CubeBlocks)
            {
                if (origBlock is MyObjectBuilder_CompoundCubeBlock)
                {
                    var origBlockCompound = origBlock as MyObjectBuilder_CompoundCubeBlock;
                    ConvertRotatedGridCompoundBlockToStatic(ref transform, origBlockCompound);
                    for (int i = 0; i < origBlockCompound.Blocks.Length; ++i)
                    {
                        var origBlockInCompound = origBlockCompound.Blocks[i];
                        ConvertRotatedGridBlockToStatic(ref transform, origBlockInCompound);
                    }
                }
                else
                {
                    ConvertRotatedGridBlockToStatic(ref transform, origBlock);
                }
            }
        }

        public override bool PasteGrid(IMyComponentInventory buildInventory = null, bool deactivate = true) 
        {
            if ((CopiedGrids.Count > 0) && !IsActive)
            {
                Activate();
                return true;
            }

            if (!m_canBePlaced)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                return false;
            }

            if (PreviewGrids.Count == 0)
                return false;

            bool result;

            bool isSnappedOnGrid = (m_hitEntity is MyCubeGrid) && IsSnapped && SnapMode == MyGridPlacementSettings.SnapMode.Base6Directions;
            bool placingDynamicGrid = !CopiedGrids[0].IsStatic && !isSnappedOnGrid;
            bool placingOnDynamicGrid = (m_hitEntity is MyCubeGrid) && !((MyCubeGrid)m_hitEntity).IsStatic && !MyCubeBuilder.Static.DynamicMode;

            if (MyCubeBuilder.Static.DynamicMode)
            {
                result = PasteGridsInDynamicMode(buildInventory, deactivate);
            }
            else if (placingDynamicGrid || placingOnDynamicGrid)
            {
                result = PasteGridInternal(buildInventory: buildInventory, deactivate: deactivate);
            }
            else
            {
                result = PasteGridsInStaticMode(buildInventory, deactivate);
            }

            return result;
        }

        private bool PasteGridsInDynamicMode(IMyComponentInventory buildInventory, bool deactivate)
        {
            bool result;
            // Remember static grid flag and set it to dynamic
            List<bool> gridStaticFlags = new List<bool>();
            foreach (var copiedGrid in CopiedGrids)
            {
                gridStaticFlags.Add(copiedGrid.IsStatic);

                copiedGrid.IsStatic = false;
            }

            result = PasteGridInternal(buildInventory: buildInventory, deactivate: deactivate);

            // Set static grid flag back
            for (int i = 0; i < CopiedGrids.Count; ++i)
                CopiedGrids[i].IsStatic = gridStaticFlags[i];
            return result;
        }

        private bool PasteGridsInStaticMode(IMyComponentInventory buildInventory, bool deactivate)
        {
            MatrixD firstGridMatrix = GetFirstGridOrientationMatrix();
            MatrixD inverseFirstGridMatrix = Matrix.Invert(firstGridMatrix);

            List<MatrixD> previewMatrices = new List<MatrixD>();
            foreach (var previewGrid in PreviewGrids)
                previewMatrices.Add(previewGrid.WorldMatrix);

            {
                // First grid is forced static
                MyObjectBuilder_CubeGrid originalCopiedGrid = CopiedGrids[0];
                MatrixD previewGridWorldMatrix = PreviewGrids[0].WorldMatrix;
                // Convert grid builder to static 
                ConvertGridBuilderToStatic(originalCopiedGrid, previewGridWorldMatrix);
                PreviewGrids[0].WorldMatrix = MatrixD.CreateTranslation(previewGridWorldMatrix.Translation);
            }

            for (int i = 1; i < CopiedGrids.Count; ++i)
            {
                if (CopiedGrids[i].IsStatic)
                {
                    MyObjectBuilder_CubeGrid originalCopiedGrid = CopiedGrids[i];
                    MatrixD previewGridWorldMatrix = PreviewGrids[i].WorldMatrix;
                    // Convert grid builder to static 
                    ConvertGridBuilderToStatic(originalCopiedGrid, previewGridWorldMatrix);
                    PreviewGrids[i].WorldMatrix = MatrixD.CreateTranslation(previewGridWorldMatrix.Translation);
                }
            }

            // All static grids has been reset to default rotation, builders will be set from paste.
            List<MyObjectBuilder_CubeGrid> pastedBuilders = new List<MyObjectBuilder_CubeGrid>();
            bool result = PasteGridInternal(buildInventory: buildInventory, deactivate: true, pastedBuilders: pastedBuilders, touchingGrids: m_touchingGrids,
                updateAfterPasteCallback: delegate(List<MyObjectBuilder_CubeGrid> pastedBuildersInCallback) 
                {
                    UpdateAfterPaste(deactivate, pastedBuildersInCallback);
                });

            if (result)
            {
                UpdateAfterPaste(deactivate, pastedBuilders);
            }

            return result;
        }

        private void UpdateAfterPaste(bool deactivate, List<MyObjectBuilder_CubeGrid> pastedBuilders)
        {
            if (CopiedGrids.Count == pastedBuilders.Count)
            {
                // Get builder positions from pasted builders and create new offsets
                m_copiedGridOffsets.Clear();
                for (int i = 0; i < CopiedGrids.Count; ++i)
                {
                    CopiedGrids[i].PositionAndOrientation = pastedBuilders[i].PositionAndOrientation;
                    m_copiedGridOffsets.Add((Vector3D)CopiedGrids[i].PositionAndOrientation.Value.Position - (Vector3D)CopiedGrids[0].PositionAndOrientation.Value.Position);
                }

                // Reset rotation
                m_pasteOrientationAngle = 0.0f;
                m_pasteDirForward = Vector3I.Forward;
                m_pasteDirUp = Vector3I.Up;

                if (!deactivate)
                    Activate();
            }
        }

        /// <summary>
        /// Converts the given block with the given matrix for static grid.
        /// </summary>
        private static void ConvertRotatedGridBlockToStatic(ref MatrixI transform, MyObjectBuilder_CubeBlock origBlock)
        {
            MyDefinitionId defId = new MyDefinitionId(origBlock.TypeId, origBlock.SubtypeName);
            MyCubeBlockDefinition blockDefinition;
            MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out blockDefinition);
            if (blockDefinition == null)
                return;

            // Orientation quaternion is not setup in origblock
            MyBlockOrientation origOrientation = origBlock.BlockOrientation;
            Vector3I origMin = origBlock.Min;
            Vector3I origMax;
            MySlimBlock.ComputeMax(blockDefinition, origOrientation, ref origMin, out origMax);

            Vector3I tMin;
            Vector3I tMax;
            Vector3I.Transform(ref origMin, ref transform, out tMin);
            Vector3I.Transform(ref origMax, ref transform, out tMax);
            Base6Directions.Direction forward = transform.GetDirection(origOrientation.Forward);
            Base6Directions.Direction up = transform.GetDirection(origOrientation.Up);

            // Write data
            MyBlockOrientation newBlockOrientation = new MyBlockOrientation(forward, up);
            Quaternion rotationQuat;
            newBlockOrientation.GetQuaternion(out rotationQuat);
            origBlock.Orientation = rotationQuat;
            origBlock.Min = Vector3I.Min(tMin, tMax);
        }

        /// <summary>
        /// Transforms given compound block with matrix for static grid. Rotation of block is not changed.
        /// </summary>
        private static void ConvertRotatedGridCompoundBlockToStatic(ref MatrixI transform, MyObjectBuilder_CompoundCubeBlock origBlock)
        {
            MyDefinitionId defId = new MyDefinitionId(origBlock.TypeId, origBlock.SubtypeName);
            MyCubeBlockDefinition blockDefinition;
            MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out blockDefinition);
            if (blockDefinition == null)
                return;

            // Orientation quaternion is not setup in origblock
            MyBlockOrientation origOrientation = origBlock.BlockOrientation;
            Vector3I origMin = origBlock.Min;
            Vector3I origMax;
            MySlimBlock.ComputeMax(blockDefinition, origOrientation, ref origMin, out origMax);

            Vector3I tMin;
            Vector3I tMax;
            Vector3I.Transform(ref origMin, ref transform, out tMin);
            Vector3I.Transform(ref origMax, ref transform, out tMax);

            // Write data
            origBlock.Min = Vector3I.Min(tMin, tMax);
        }

        protected new void UpdatePastePosition()
        {
            m_pastePositionPrevious = m_pastePosition;

            if (MyCubeBuilder.Static.DynamicMode)
            {
                m_visible = true;
                IsSnapped = false;

                // Cast shapes commented out - difficult to place larger grids (blueprints).
                //Vector3D? fixedPastePosition = GetFreeSpacePlacementPositionGridAabbs(true, out m_dynamicBuildAllowed);
                //if (fixedPastePosition.HasValue)
                //    m_pastePosition = fixedPastePosition.Value;
                //else
                    m_pastePosition = MyCubeBuilder.IntersectionStart + m_dragDistance * MyCubeBuilder.IntersectionDirection;

                Matrix firstGridOrientation = GetFirstGridOrientationMatrix();
                Vector3D worldRefPointOffset = Vector3.TransformNormal(m_dragPointToPositionLocal, firstGridOrientation);

                m_pastePosition += worldRefPointOffset;
            }
            else
            {
                m_visible = true;   

                if (!IsSnapped)
                {
                    m_pasteOrientationAngle = 0.0f;
                    m_pasteDirForward = Vector3I.Forward;
                    m_pasteDirUp = Vector3I.Up;
                }

                IsSnapped = true;

                MatrixD pasteMatrix = GetPasteMatrix();
                Vector3 dragVectorGlobal = pasteMatrix.Forward * m_dragDistance;
                var gridSettings = m_settings.GetGridPlacementSettings(PreviewGrids[0]);
                if (!TrySnapToSurface(gridSettings.Mode))
                {
                    m_pastePosition = pasteMatrix.Translation + dragVectorGlobal;

                    Matrix firstGridOrientation = GetFirstGridOrientationMatrix();
                    Vector3D worldRefPointOffset = Vector3.TransformNormal(m_dragPointToPositionLocal, firstGridOrientation);
                    m_pastePosition += worldRefPointOffset;

                    IsSnapped = true;
                }

                double gridSize = PreviewGrids[0].GridSize;
                if (m_settings.StaticGridAlignToCenter)
                    m_pastePosition = Vector3I.Round(m_pastePosition / gridSize) * gridSize;
                else
                    m_pastePosition = Vector3I.Round(m_pastePosition / gridSize + 0.5) * gridSize - 0.5 * gridSize;
            }
        }

        internal void SetDragDistance(float dragDistance)
        {
            m_dragDistance = dragDistance;
        }

        private double DistanceFromCharacterPlane(ref Vector3D point)
        {
            double dot = Vector3D.Dot(point - MyCubeBuilder.IntersectionStart, MyCubeBuilder.IntersectionDirection);
            return dot;
        }

        private static double? GetCurrentRayIntersection()
        {
            Vector3D position;
            Vector3 normal;
            HkRigidBody intersectedBody = MyPhysics.CastRay(MyCubeBuilder.IntersectionStart, MyCubeBuilder.IntersectionStart + 2000 * MyCubeBuilder.IntersectionDirection,
                out position, out normal, MyPhysics.CollisionLayerWithoutCharacter);
            if (intersectedBody != null)
            {
                Vector3D p = position - MyCubeBuilder.IntersectionStart;
                double dist = p.Length();
                return dist;
            }

            return null;
        }

        protected Vector3D? GetFreeSpacePlacementPosition(bool copyPaste, out bool buildAllowed)
        {
            Vector3D? freePlacementIntersectionPoint = null;
            buildAllowed = false;

            float gridSize = PreviewGrids[0].GridSize;

            double shortestDistance = double.MaxValue;
            double? currentRayInts = GetCurrentRayIntersection();
            if (currentRayInts.HasValue)
                shortestDistance = currentRayInts.Value;

            Vector3D worldRefPointOffset = Vector3D.Zero;
            if (copyPaste)
            {
                Matrix firstGridOrientation = GetFirstGridOrientationMatrix();
                worldRefPointOffset = Vector3.TransformNormal(m_dragPointToPositionLocal, firstGridOrientation);
            }

            Vector3D worldRefPoint = PreviewGrids[0].GridIntegerToWorld(Vector3I.Zero);

            Matrix blockWorlTransform;
            foreach (var block in PreviewGrids[0].GetBlocks())
            {
                Vector3 halfExt = block.BlockDefinition.Size * PreviewGrids[0].GridSize * 0.5f;
                Vector3 minLocal = block.Min * PreviewGrids[0].GridSize - Vector3.Half * PreviewGrids[0].GridSize;
                Vector3 maxLocal = block.Max * PreviewGrids[0].GridSize + Vector3.Half * PreviewGrids[0].GridSize;
                block.Orientation.GetMatrix(out blockWorlTransform);

                blockWorlTransform.Translation = 0.5f * (minLocal + maxLocal);

                blockWorlTransform = blockWorlTransform * PreviewGrids[0].WorldMatrix;
                Vector3D offset = blockWorlTransform.Translation + worldRefPointOffset - worldRefPoint;

                HkShape shape = new HkBoxShape(halfExt);

                Vector3D rayStart = MyCubeBuilder.IntersectionStart + offset;
                double castPlaneDistanceToRayStart = DistanceFromCharacterPlane(ref rayStart);
                rayStart -= castPlaneDistanceToRayStart * MyCubeBuilder.IntersectionDirection;

                Vector3D rayEnd = MyCubeBuilder.IntersectionStart + (m_dragDistance - castPlaneDistanceToRayStart) * MyCubeBuilder.IntersectionDirection + offset;
                MatrixD matrix = blockWorlTransform;
                matrix.Translation = rayStart;

                try
                {
                    float? dist = MyPhysics.CastShape(rayEnd, shape, ref matrix, MyPhysics.CollisionLayerWithoutCharacter);
                    if (dist.HasValue && dist.Value != 0f)
                    {
                        Vector3D intersectionPoint = rayStart + dist.Value * (rayEnd - rayStart);

                        const bool debugDraw = false;
                        if (debugDraw)
                        {
                            Color green = Color.Green;
                            BoundingBoxD localAABB = new BoundingBoxD(-halfExt, halfExt);
                            MatrixD drawMatrix = matrix;
                            drawMatrix.Translation = intersectionPoint;
                            MySimpleObjectDraw.DrawTransparentBox(ref drawMatrix, ref localAABB, ref green, MySimpleObjectRasterizer.Wireframe, 1, 0.04f);
                        }

                        double fixedDistance = DistanceFromCharacterPlane(ref intersectionPoint) - castPlaneDistanceToRayStart;
                        if (fixedDistance <= 0)
                        {
                            fixedDistance = 0;
                            shortestDistance = 0;
                            break;
                        }

                        if (fixedDistance < shortestDistance)
                            shortestDistance = fixedDistance;

                        buildAllowed = true;
                    }
                }
                finally
                {
                    shape.RemoveReference();
                }
            }

            float boxRadius = (float)PreviewGrids[0].PositionComp.WorldAABB.HalfExtents.Length();
            float dragDistance = 1.5f * boxRadius;
            if (shortestDistance < dragDistance)
            {
                shortestDistance = dragDistance;
                buildAllowed = false;
            }

            if (shortestDistance < m_dragDistance)
                freePlacementIntersectionPoint = MyCubeBuilder.IntersectionStart + shortestDistance * MyCubeBuilder.IntersectionDirection;

            return freePlacementIntersectionPoint;
        }

        /// <summary>
        /// Casts preview grids aabbs and get shortest distance. Returns shortest intersection or null.
        /// </summary>
        protected Vector3D? GetFreeSpacePlacementPositionGridAabbs(bool copyPaste, out bool buildAllowed)
        {
            Vector3D? freePlacementIntersectionPoint = null;
            buildAllowed = true;

            float gridSize = PreviewGrids[0].GridSize;

            double shortestDistance = double.MaxValue;
            double? currentRayInts = GetCurrentRayIntersection();
            if (currentRayInts.HasValue)
                shortestDistance = currentRayInts.Value;

            Vector3D worldRefPointOffset = Vector3D.Zero;
            if (copyPaste)
            {
                Matrix firstGridOrientation = GetFirstGridOrientationMatrix();
                worldRefPointOffset = Vector3.TransformNormal(m_dragPointToPositionLocal, firstGridOrientation);
            }

            Vector3D worldRefPoint = PreviewGrids[0].GridIntegerToWorld(Vector3I.Zero);

            foreach (var grid in PreviewGrids)
            {
                Vector3 halfExt = grid.PositionComp.LocalAABB.HalfExtents;
                Vector3 minLocal = grid.Min * grid.GridSize - Vector3.Half * grid.GridSize;
                Vector3 maxLocal = grid.Max * grid.GridSize + Vector3.Half * grid.GridSize;

                MatrixD gridWorlTransform = MatrixD.Identity;
                gridWorlTransform.Translation = 0.5f * (minLocal + maxLocal);
                gridWorlTransform = gridWorlTransform * grid.WorldMatrix;

                Vector3I size = grid.Max - grid.Min + Vector3I.One;
                Vector3 sizeOffset = Vector3I.Abs((size % 2) - Vector3I.One) * 0.5 * grid.GridSize;
                sizeOffset = Vector3.TransformNormal(sizeOffset, grid.WorldMatrix);
                Vector3D offset = gridWorlTransform.Translation + worldRefPointOffset - worldRefPoint /*- sizeOffset*/;// Vector3.Zero;// gridWorlTransform.Translation + worldRefPointOffset - worldRefPoint;

                HkShape shape = new HkBoxShape(halfExt);

                Vector3D rayStart = MyCubeBuilder.IntersectionStart + offset;
                double castPlaneDistanceToRayStart = DistanceFromCharacterPlane(ref rayStart);
                rayStart -= castPlaneDistanceToRayStart * MyCubeBuilder.IntersectionDirection;

                Vector3D rayEnd = MyCubeBuilder.IntersectionStart + (m_dragDistance - castPlaneDistanceToRayStart) * MyCubeBuilder.IntersectionDirection + offset;
                MatrixD matrix = gridWorlTransform;
                matrix.Translation = rayStart;

                try
                {
                    float? dist = MyPhysics.CastShape(rayEnd, shape, ref matrix, MyPhysics.CollisionLayerWithoutCharacter);
                    if (dist.HasValue && dist.Value != 0f)
                    {
                        Vector3D intersectionPoint = rayStart + dist.Value * (rayEnd - rayStart);

                        const bool debugDraw = true;
                        if (debugDraw)
                        {
                            Color green = Color.Green;
                            BoundingBoxD localAABB = new BoundingBoxD(-halfExt, halfExt);
                            localAABB.Inflate(0.03f);
                            MatrixD drawMatrix = matrix;
                            drawMatrix.Translation = intersectionPoint;
                            MySimpleObjectDraw.DrawTransparentBox(ref drawMatrix, ref localAABB, ref green, MySimpleObjectRasterizer.Wireframe, 1, 0.04f);
                        }

                        double fixedDistance = DistanceFromCharacterPlane(ref intersectionPoint) - castPlaneDistanceToRayStart;
                        if (fixedDistance <= 0)
                        {
                            fixedDistance = 0;
                            shortestDistance = 0;
                            buildAllowed = false;
                            break;
                        }

                        if (fixedDistance < shortestDistance)
                            shortestDistance = fixedDistance;
                    }
                    else
                    {
                        buildAllowed = false;
                    }
                }
                finally
                {
                    shape.RemoveReference();
                }

            }

            if (shortestDistance != 0 && shortestDistance < m_dragDistance)
                freePlacementIntersectionPoint = MyCubeBuilder.IntersectionStart + shortestDistance * MyCubeBuilder.IntersectionDirection;
            else
                buildAllowed = false;

            return freePlacementIntersectionPoint;
        }

        private bool TestPlacement()
        {
            bool retval = true;

            m_touchingGrids.Clear();

            for (int i = 0; i < PreviewGrids.Count; ++i)
            {
                var grid = PreviewGrids[i];

                m_touchingGrids.Add(null);

                if (MyCubeBuilder.Static.DynamicMode)
                {
                    if (!m_dynamicBuildAllowed)
                    {
                        var settings = m_settings.GetGridPlacementSettings(grid, false);

                        BoundingBoxD localAabb = (BoundingBoxD)grid.PositionComp.LocalAABB;
                        MatrixD worldMatrix = grid.WorldMatrix;

                        if (MyFakes.ENABLE_VOXEL_MAP_AABB_CORNER_TEST)
                            retval = retval && MyCubeGrid.TestPlacementVoxelMapOverlap(null, ref settings, ref localAabb, ref worldMatrix);

                        retval = retval && MyCubeGrid.TestPlacementArea(grid, false, ref settings, localAabb, true);

                        if (!retval)
                            break;

                        //foreach (var block in grid.GetBlocks())
                        //{
                        //    Vector3 minLocal = block.Min * PreviewGrids[i].GridSize - Vector3.Half * PreviewGrids[i].GridSize;
                        //    Vector3 maxLocal = block.Max * PreviewGrids[i].GridSize + Vector3.Half * PreviewGrids[i].GridSize;
                        //    BoundingBoxD aabbLocal = new BoundingBoxD(minLocal, maxLocal);
                        //    retval &= MyCubeGrid.TestPlacementArea(grid, false, ref settings, aabbLocal, true);
                        //    if (!retval)
                        //        break;
                        //}
                    }
                }
                else
                { //not dynamic building mode

                    if (i == 0 && m_hitEntity is MyCubeGrid && IsSnapped && SnapMode == MyGridPlacementSettings.SnapMode.Base6Directions)
                    {
                        var settings = grid.GridSizeEnum == MyCubeSize.Large ? MyPerGameSettings.BuildingSettings.LargeStaticGrid : MyPerGameSettings.BuildingSettings.SmallStaticGrid;

                        var hitGrid = m_hitEntity as MyCubeGrid;

                        if (hitGrid.GridSizeEnum == MyCubeSize.Small && grid.GridSizeEnum == MyCubeSize.Large)
                        {
                            retval = false;
                            break;
                        }

                        bool smallOnLargeGrid = hitGrid.GridSizeEnum == MyCubeSize.Large && grid.GridSizeEnum == MyCubeSize.Small;

                        if (MyFakes.ENABLE_STATIC_SMALL_GRID_ON_LARGE /*&& grid.IsStatic*/ && smallOnLargeGrid)
                        {
                            if (!hitGrid.IsStatic)
                            {
                                retval = false;
                                break;
                            }

                            foreach (var block in grid.CubeBlocks)
                            {
                                if (block.FatBlock is MyCompoundCubeBlock)
                                {
                                    MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                                    foreach (var blockInCompound in compoundBlock.GetBlocks())
                                    {
                                        retval = retval && TestBlockPlacement(blockInCompound, ref settings);
                                        if (!retval)
                                            break;
                                    }
                                }
                                else
                                {
                                    retval = retval && TestBlockPlacement(block, ref settings);
                                }

                                if (!retval)
                                    break;
                            }
                        }
                        else
                        {
                            retval = retval && TestGridPlacementOnGrid(grid, ref settings, hitGrid);
                        }
                    }
                    else
                    {
                        // Check with grid settings
                        {
                            MyCubeGrid touchingGrid = null;
                            var settings = i == 0 ? (grid.GridSizeEnum == MyCubeSize.Large ? MyPerGameSettings.BuildingSettings.LargeStaticGrid : MyPerGameSettings.BuildingSettings.SmallStaticGrid)
                                : MyPerGameSettings.BuildingSettings.GetGridPlacementSettings(grid);

                            if (grid.IsStatic)
                            {
                                foreach (var block in grid.CubeBlocks)
                                {
                                    if (block.FatBlock is MyCompoundCubeBlock)
                                    {
                                        MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                                        foreach (var blockInCompound in compoundBlock.GetBlocks())
                                        {
                                            MyCubeGrid touchingGridLocal = null;
                                            retval = retval && TestBlockPlacementNoAABBInflate(blockInCompound, ref settings, out touchingGridLocal);

                                            if (retval && touchingGridLocal != null && touchingGrid == null)
                                                touchingGrid = touchingGridLocal;

                                            if (!retval)
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        MyCubeGrid touchingGridLocal = null;
                                        retval = retval && TestBlockPlacementNoAABBInflate(block, ref settings, out touchingGridLocal);

                                        if (retval && touchingGridLocal != null && touchingGrid == null)
                                            touchingGrid = touchingGridLocal;
                                    }

                                    if (!retval)
                                        break;
                                }

                                if (retval && touchingGrid != null)
                                    m_touchingGrids[i] = touchingGrid;
                            }
                            else
                            {
                                foreach (var block in grid.CubeBlocks)
                                {
                                    Vector3 minLocal = block.Min * PreviewGrids[i].GridSize - Vector3.Half * PreviewGrids[i].GridSize;
                                    Vector3 maxLocal = block.Max * PreviewGrids[i].GridSize + Vector3.Half * PreviewGrids[i].GridSize;
                                    BoundingBoxD aabbLocal = new BoundingBoxD(minLocal, maxLocal);
                                    retval = retval && MyCubeGrid.TestPlacementArea(grid, grid.IsStatic, ref settings, aabbLocal, false);

                                    if (!retval)
                                        break;
                                }

                                m_touchingGrids[i] = null;
                            }
                        }

                        // Check connectivity with touching grid
                        if (retval && m_touchingGrids[i] != null)
                        {
                            var settings = grid.GridSizeEnum == MyCubeSize.Large ? MyPerGameSettings.BuildingSettings.LargeStaticGrid : MyPerGameSettings.BuildingSettings.SmallStaticGrid;
                            retval = retval && TestGridPlacementOnGrid(grid, ref settings, m_touchingGrids[i]);
                        }

                        // Check with paste settings only first grid
                        {
                            if (retval && i == 0)
                            {
                                bool smallStaticGrid = grid.GridSizeEnum == MyCubeSize.Small && grid.IsStatic;
                                if (smallStaticGrid || !grid.IsStatic)
                                {
                                    var settings = i == 0 ? m_settings.GetGridPlacementSettings(grid, false) : MyPerGameSettings.BuildingSettings.SmallStaticGrid;
                                    bool localRetVal = true;

                                    foreach (var block in grid.CubeBlocks)
                                    {
                                        Vector3 minLocal = block.Min * PreviewGrids[i].GridSize - Vector3.Half * PreviewGrids[i].GridSize;
                                        Vector3 maxLocal = block.Max * PreviewGrids[i].GridSize + Vector3.Half * PreviewGrids[i].GridSize;
                                        BoundingBoxD blockLocalAABB = new BoundingBoxD(minLocal, maxLocal);

                                        localRetVal = localRetVal && MyCubeGrid.TestPlacementArea(grid, false, ref settings, blockLocalAABB, false);
                                        if (!localRetVal)
                                            break;
                                    }

                                    retval &= !localRetVal;
                                }
                                else if (m_touchingGrids[i] == null)
                                {
                                    var settings = m_settings.GetGridPlacementSettings(grid, i == 0 ? true : grid.IsStatic);

                                    MyCubeGrid touchingGridLocal = null;

                                    bool localRetVal = false;
                                    foreach (var block in grid.CubeBlocks)
                                    {
                                        if (block.FatBlock is MyCompoundCubeBlock)
                                        {
                                            MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                                            foreach (var blockInCompound in compoundBlock.GetBlocks())
                                            {
                                                localRetVal |= TestBlockPlacementNoAABBInflate(blockInCompound, ref settings, out touchingGridLocal);
                                                if (localRetVal)
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            localRetVal |= TestBlockPlacementNoAABBInflate(block, ref settings, out touchingGridLocal);
                                        }

                                        if (localRetVal)
                                            break;
                                    }

                                    retval &= localRetVal;
                                }
                            }
                        }
                    }
                }

                BoundingBoxD aabb = (BoundingBoxD)grid.PositionComp.LocalAABB;
                MatrixD invGridWorlMatrix = grid.PositionComp.GetWorldMatrixNormalizedInv();

                // Character collisions.
                if (MySector.MainCamera != null)
                {
                    Vector3D cameraPos = Vector3D.Transform(MySector.MainCamera.Position, invGridWorlMatrix);
                    retval = retval && aabb.Contains(cameraPos) != ContainmentType.Contains;
                }

                if (retval)
                {
                    m_tmpCollisionPoints.Clear();
                    MyCubeBuilder.PrepareCharacterCollisionPoints(m_tmpCollisionPoints);
                    foreach (var pt in m_tmpCollisionPoints)
                    {
                        Vector3D ptLocal = Vector3D.Transform(pt, invGridWorlMatrix);
                        retval = retval && aabb.Contains(ptLocal) != ContainmentType.Contains;
                        if (!retval)
                            break;
                    }
                }

                if (!retval)
                    break;
            }

            return retval;
        }

        protected bool TestGridPlacementOnGrid(MyCubeGrid previewGrid, ref MyGridPlacementSettings settings, MyCubeGrid hitGrid)
        {
            bool retval = true;

            Vector3I gridOffset = hitGrid.WorldToGridInteger(m_pastePosition);
            MatrixI transform = hitGrid.CalculateMergeTransform(previewGrid, gridOffset);

            if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 60.0f), "First grid offset: " + gridOffset.ToString(), Color.Red, 1.0f);
            retval = retval && hitGrid.GridSizeEnum == previewGrid.GridSizeEnum && hitGrid.CanMergeCubes(previewGrid, gridOffset);
            retval = retval && MyCubeGrid.CheckMergeConnectivity(hitGrid, previewGrid, gridOffset);

            // Check if any block connects to hit grid
            if (retval)
            {
                bool connected = false;

                foreach (var block in previewGrid.CubeBlocks)
                {
                    if (block.FatBlock is MyCompoundCubeBlock)
                    {
                        MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                        foreach (var blockInCompound in compoundBlock.GetBlocks())
                        {
                            connected |= CheckConnectivityOnGrid(blockInCompound, ref transform, ref settings, hitGrid);
                            if (connected)
                                break;
                        }
                    }
                    else
                    {
                        connected |= CheckConnectivityOnGrid(block, ref transform, ref settings, hitGrid);
                    }

                    if (connected)
                        break;
                }

                retval &= connected;
            }

            if (retval)
            {
                foreach (var block in previewGrid.CubeBlocks)
                {
                    if (block.FatBlock is MyCompoundCubeBlock)
                    {
                        MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                        foreach (var blockInCompound in compoundBlock.GetBlocks())
                        {
                            retval = retval && TestBlockPlacementOnGrid(blockInCompound, ref transform, ref settings, hitGrid);
                            if (!retval)
                                break;
                        }
                    }
                    else
                    {
                        retval = retval && TestBlockPlacementOnGrid(block, ref transform, ref settings, hitGrid);
                    }

                    if (!retval)
                        break;
                }
            }

            return retval;
        }

        protected static bool CheckConnectivityOnGrid(MySlimBlock block, ref MatrixI transform, ref MyGridPlacementSettings settings, MyCubeGrid hitGrid)
        {
            Vector3I position;
            Vector3I.Transform(ref block.Position, ref transform, out position);

            Vector3I forward = Base6Directions.GetIntVector(transform.GetDirection(block.Orientation.Forward));
            Vector3I up = Base6Directions.GetIntVector(transform.GetDirection(block.Orientation.Up));
            MyBlockOrientation blockOrientation = new MyBlockOrientation(Base6Directions.GetDirection(forward), Base6Directions.GetDirection(up));
            Quaternion rotation;
            blockOrientation.GetQuaternion(out rotation);

            return MyCubeGrid.CheckConnectivity(hitGrid, block.BlockDefinition, ref rotation, ref position);
        }

        protected static bool TestBlockPlacementOnGrid(MySlimBlock block, ref MatrixI transform, ref MyGridPlacementSettings settings, MyCubeGrid hitGrid)
        {
            Vector3I positionMin;
            Vector3I.Transform(ref block.Min, ref transform, out positionMin);
            Vector3I positionMax;
            Vector3I.Transform(ref block.Max, ref transform, out positionMax);
            Vector3I min = Vector3I.Min(positionMin, positionMax);
            Vector3I max = Vector3I.Max(positionMin, positionMax);

            Vector3I forward = Base6Directions.GetIntVector(transform.GetDirection(block.Orientation.Forward));
            Vector3I up = Base6Directions.GetIntVector(transform.GetDirection(block.Orientation.Up));

            MyBlockOrientation blockOrientation = new MyBlockOrientation(Base6Directions.GetDirection(forward), Base6Directions.GetDirection(up));

            if (!hitGrid.CanAddCubes(min, max, blockOrientation, block.BlockDefinition))
                return false;

            return MyCubeGrid.TestPlacementAreaCube(hitGrid, ref settings, min, max, blockOrientation, block.BlockDefinition, ignoredEntity: hitGrid);
        }

        protected static bool TestBlockPlacement(MySlimBlock block, ref MyGridPlacementSettings settings)
        {
            return MyCubeGrid.TestPlacementAreaCube(block.CubeGrid, ref settings, block.Min, block.Max, block.Orientation, block.BlockDefinition, ignoredEntity: block.CubeGrid);
        }

        protected static bool TestBlockPlacement(MySlimBlock block, ref MyGridPlacementSettings settings, out MyCubeGrid touchingGrid) 
        {
            return MyCubeGrid.TestPlacementAreaCube(block.CubeGrid, ref settings, block.Min, block.Max, block.Orientation, block.BlockDefinition, out touchingGrid, ignoredEntity: block.CubeGrid);
        }

        protected static bool TestBlockPlacementNoAABBInflate(MySlimBlock block, ref MyGridPlacementSettings settings, out MyCubeGrid touchingGrid)
        {
            return MyCubeGrid.TestPlacementAreaCubeNoAABBInflate(block.CubeGrid, ref settings, block.Min, block.Max, block.Orientation, block.BlockDefinition, out touchingGrid, ignoredEntity: block.CubeGrid);
        }

        protected static bool TestBlockPlacementArea(MySlimBlock block, ref MyGridPlacementSettings settings, bool dynamicMode)
        {
            var localAabb = BoundingBoxD.CreateInvalid();
            localAabb.Include(block.Min * block.CubeGrid.GridSize - block.CubeGrid.GridSize / 2);
            localAabb.Include(block.Max * block.CubeGrid.GridSize + block.CubeGrid.GridSize / 2);
            return MyCubeGrid.TestBlockPlacementArea(block.BlockDefinition, block.Orientation, block.CubeGrid.WorldMatrix, ref settings, localAabb, dynamicMode, ignoredEntity: block.CubeGrid);
        }

        private void UpdatePreview()
        {
            if (PreviewGrids == null)
                return;

            if (m_visible == false || HasPreviewBBox == false)
                return;

            // Switch off shadows - does not work
            //foreach (var grid in PreviewGrids)
            //{
            //    grid.Render.CastShadows = false;
            //    foreach (var block in grid.GetBlocks())
            //    {
            //        if (block.FatBlock != null)
            //        {
            //            block.FatBlock.Render.CastShadows = false;
            //        }
            //    }
            //}

            string lineMaterial = m_canBePlaced ? "GizmoDrawLine" : "GizmoDrawLineRed";
            Color white = Color.White;

            foreach (var grid in PreviewGrids)
            {
                BoundingBoxD localAABB = (BoundingBoxD)grid.PositionComp.LocalAABB;
                MatrixD drawMatrix = grid.PositionComp.WorldMatrix;
                MySimpleObjectDraw.DrawTransparentBox(ref drawMatrix, ref localAABB, ref white, MySimpleObjectRasterizer.Wireframe, 1, 0.04f, lineMaterial: lineMaterial);

                //foreach (var block in grid.GetBlocks())
                //{
                //    if (block.FatBlock != null)
                //        MyEntities.EnableEntityBoundingBoxDraw(block.FatBlock, true, color, lineWidth: 0.04f);
                //}
            }
        }

        internal void DynamicModeChanged()
        {
            if (MyCubeBuilder.Static.DynamicMode)
            {
                SetupDragDistance();
            }
        }

        protected virtual void SetupDragDistance()
        {
            if (!IsActive)
                return;

            if (PreviewGrids.Count > 0)
            {
                double? currentRayInts = GetCurrentRayIntersection();
                if (currentRayInts.HasValue && m_dragDistance > currentRayInts.Value)
                    m_dragDistance = (float)currentRayInts.Value;

                float boxRadius = (float)PreviewGrids[0].PositionComp.WorldAABB.HalfExtents.Length();
                float dragDistance = 2.5f * boxRadius;
                if (m_dragDistance < dragDistance)
                    m_dragDistance = dragDistance;
            }
            else
            {
                m_dragDistance = 0;
            }
        }

        public override void MoveEntityCloser()
        {
            base.MoveEntityCloser();
            if (m_dragDistance < MyCubeBuilder.MIN_BLOCK_BUILDING_DISTANCE)
                m_dragDistance = MyCubeBuilder.MIN_BLOCK_BUILDING_DISTANCE;
        }
    }
}
