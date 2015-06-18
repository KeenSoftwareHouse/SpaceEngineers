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
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GUI;
using Sandbox.Game.World;
using Sandbox.Graphics;
using VRageMath;
using VRageRender;
using VRage.ObjectBuilders;
using VRage;

namespace Sandbox.Game.Entities.Cube
{
    /// <summary>
    ///  Multiblock clipboard for building multiblocks. Can be used for building only (not copy/paste) because it uses definitions not real tile grid/block data.
    /// </summary>
    class MyMultiBlockClipboard : MyGridClipboard2
    {
        private static List<Vector3> m_tmpCollisionPoints = new List<Vector3>();

        protected override bool AnyCopiedGridIsStatic
        {
            get
            {
                return false;
            }
        }

        public MySlimBlock RemoveBlock;
        public ushort? BlockIdInCompound;

        private Vector3I m_addPos;


        public MyMultiBlockClipboard(MyPlacementSettings settings, bool calculateVelocity = true) : base(settings, calculateVelocity)
        {
            EnableGridChangeToDynamic = false;
            m_useDynamicPreviews = false;
        }

        public override void Update()
        {
            if (!IsActive)
                return;

            UpdateHitEntity();

            if (!m_visible)
            {
                Hide();
                return;
            }

            Show();

            if (PreviewGrids.Count == 0)
                return;

            if (m_dragDistance == 0)
                SetupDragDistance();
            if (m_dragDistance > MyCubeBuilder.MAX_BLOCK_BUILDING_DISTANCE)
                m_dragDistance = MyCubeBuilder.MAX_BLOCK_BUILDING_DISTANCE;

            UpdatePastePosition();
            UpdateGridTransformations();
            FixSnapTransformationBase6();

            if (m_calculateVelocity)
                m_objectVelocity = (m_pastePosition - m_pastePositionPrevious) / MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            m_canBePlaced = TestPlacement();

            if (!m_visible)
            {
                Hide();
                return;
            }

            TestBuildingMaterials();
            UpdatePreview();

            if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "FW: " + m_pasteDirForward.ToString(), Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 20.0f), "UP: " + m_pasteDirUp.ToString(), Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 40.0f), "AN: " + m_pasteOrientationAngle.ToString(), Color.Red, 1.0f);
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

            bool placingOnDynamicGrid = RemoveBlock != null && !RemoveBlock.CubeGrid.IsStatic;

            if (MyCubeBuilder.Static.DynamicMode || placingOnDynamicGrid)
            {
                result = PasteGridsInDynamicMode(buildInventory, deactivate);
            }
            else
            {
                result = PasteGridsInStaticMode(buildInventory, deactivate);
            }

            return result;
        }

        private static MyObjectBuilder_CubeGrid ConvertGridBuilderToStatic(MyObjectBuilder_CubeGrid originalGrid, MatrixD worldMatrix)
        {
            var gridBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            gridBuilder.EntityId = originalGrid.EntityId;
            gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(originalGrid.PositionAndOrientation.Value.Position, Vector3.Forward, Vector3.Up);
            gridBuilder.GridSizeEnum = originalGrid.GridSizeEnum;
            gridBuilder.IsStatic = true;
            gridBuilder.PersistentFlags |= MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;

            // Blocks in static grid - must be recreated for static grid with different orientation and position
            foreach (var origBlock in originalGrid.CubeBlocks)
            {
                if (origBlock is MyObjectBuilder_CompoundCubeBlock)
                {
                    var origBlockCompound = origBlock as MyObjectBuilder_CompoundCubeBlock;
                    var blockBuilderCompound = ConvertDynamicGridBlockToStatic(ref worldMatrix, origBlock) as MyObjectBuilder_CompoundCubeBlock;
                    Debug.Assert(blockBuilderCompound != null);
                    if (blockBuilderCompound == null)
                        continue;

                    blockBuilderCompound.Blocks = new MyObjectBuilder_CubeBlock[origBlockCompound.Blocks.Length];

                    for (int i = 0; i < origBlockCompound.Blocks.Length; ++i)
                    {
                        var origBlockInCompound = origBlockCompound.Blocks[i];
                        var blockBuilder = ConvertDynamicGridBlockToStatic(ref worldMatrix, origBlockInCompound);
                        if (blockBuilder == null)
                            continue;

                        blockBuilderCompound.Blocks[i] = blockBuilder;
                    }
                    gridBuilder.CubeBlocks.Add(blockBuilderCompound);
                }
                else
                {
                    var blockBuilder = ConvertDynamicGridBlockToStatic(ref worldMatrix, origBlock);
                    if (blockBuilder == null)
                        continue;
                    gridBuilder.CubeBlocks.Add(blockBuilder);
                }
            }

            return gridBuilder;
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
            // Paste generates grid from builder and use matrix from preview
            List<MyObjectBuilder_CubeGrid> copiedGridsOrig = new List<MyObjectBuilder_CubeGrid>();
            List<MatrixD> previewGridsWorldMatrices = new List<MatrixD>();

            {
                // First grid is forced static
                MyObjectBuilder_CubeGrid originalCopiedGrid = CopiedGrids[0];
                copiedGridsOrig.Add(originalCopiedGrid);
                MatrixD previewGridWorldMatrix = PreviewGrids[0].WorldMatrix;
                // Convert grid builder to static 
                var gridBuilder = ConvertGridBuilderToStatic(originalCopiedGrid, previewGridWorldMatrix);
                // Set it to copied grids
                CopiedGrids[0] = gridBuilder;

                previewGridsWorldMatrices.Add(previewGridWorldMatrix);
                PreviewGrids[0].WorldMatrix = MatrixD.CreateTranslation(previewGridWorldMatrix.Translation);
            }


            for (int i = 1; i < CopiedGrids.Count; ++i)
            {
                MyObjectBuilder_CubeGrid originalCopiedGrid = CopiedGrids[i];
                copiedGridsOrig.Add(originalCopiedGrid);
                MatrixD previewGridWorldMatrix = PreviewGrids[i].WorldMatrix;
                previewGridsWorldMatrices.Add(previewGridWorldMatrix);

                if (CopiedGrids[i].IsStatic)
                {
                    // Convert grid builder to static 
                    var gridBuilder = ConvertGridBuilderToStatic(originalCopiedGrid, previewGridWorldMatrix);
                    // Set it to copied grids
                    CopiedGrids[i] = gridBuilder;

                    PreviewGrids[i].WorldMatrix = MatrixD.CreateTranslation(previewGridWorldMatrix.Translation);
                }
            }

            Debug.Assert(CopiedGrids.Count == copiedGridsOrig.Count);
            Debug.Assert(CopiedGrids.Count == previewGridsWorldMatrices.Count);

            bool result = PasteGridInternal(buildInventory: buildInventory, deactivate: deactivate);

            // Set original grids back
            CopiedGrids.Clear();
            CopiedGrids.AddList(copiedGridsOrig);

            for (int i = 0; i < PreviewGrids.Count; ++i)
                PreviewGrids[i].WorldMatrix = previewGridsWorldMatrices[i];

            return result;
        }

        private static MyObjectBuilder_CubeBlock ConvertDynamicGridBlockToStatic(ref MatrixD worldMatrix, MyObjectBuilder_CubeBlock origBlock)
        {
            MyDefinitionId defId = new MyDefinitionId(origBlock.TypeId, origBlock.SubtypeName);
            MyCubeBlockDefinition blockDefinition;
            MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out blockDefinition);
            if (blockDefinition == null)
                return null;

            var blockBuilder = MyObjectBuilderSerializer.CreateNewObject(defId) as MyObjectBuilder_CubeBlock;
            blockBuilder.EntityId = origBlock.EntityId;
            // Orientation quaternion is not setup in origblock
            MyBlockOrientation orientation = origBlock.BlockOrientation;
            Quaternion rotationQuat;
            orientation.GetQuaternion(out rotationQuat);
            Matrix origRotationMatrix = Matrix.CreateFromQuaternion(rotationQuat);
            Matrix rotationMatrix = origRotationMatrix * worldMatrix;
            blockBuilder.Orientation = Quaternion.CreateFromRotationMatrix(rotationMatrix);

            Vector3I origSizeRotated = Vector3I.Abs(Vector3I.Round(Vector3.TransformNormal((Vector3)blockDefinition.Size, origRotationMatrix)));
            Vector3I origMin = origBlock.Min;
            Vector3I origMax = origBlock.Min + origSizeRotated - Vector3I.One;

            Vector3I minXForm = Vector3I.Round(Vector3.TransformNormal((Vector3)origMin, worldMatrix));
            Vector3I maxXForm = Vector3I.Round(Vector3.TransformNormal((Vector3)origMax, worldMatrix));

            blockBuilder.Min = Vector3I.Min(minXForm, maxXForm);
            return blockBuilder;
        }


        protected new void UpdatePastePosition()
        {
            m_pastePositionPrevious = m_pastePosition;

            if (MyCubeBuilder.Static.DynamicMode)
            {
                m_visible = true;
                IsSnapped = false;

                Vector3D? fixedPastePosition = GetFreeSpacePlacementPosition(false, out m_dynamicBuildAllowed);
                if (fixedPastePosition.HasValue)
                    m_pastePosition = fixedPastePosition.Value;
                else
                    m_pastePosition = MyCubeBuilder.Static.FreePlacementTarget;
            }
            else if (RemoveBlock != null) 
            {
                m_pastePosition = Vector3D.Transform(m_addPos * RemoveBlock.CubeGrid.GridSize, RemoveBlock.CubeGrid.WorldMatrix);

                if (!IsSnapped && RemoveBlock.CubeGrid.IsStatic)
                {
                    m_pasteOrientationAngle = 0.0f;
                    m_pasteDirForward = Vector3I.Forward;
                    m_pasteDirUp = Vector3I.Up;
                }

                IsSnapped = true;
            }
            else if (MyFakes.ENABLE_BLOCK_PLACEMENT_ON_VOXEL && m_hitEntity is MyVoxelMap) 
            {
                float gridSize = MyDefinitionManager.Static.GetCubeSize(CopiedGrids[0].GridSizeEnum);
                m_pastePosition = m_addPos * gridSize;
                if (!MyPerGameSettings.BuildingSettings.StaticGridAlignToCenter)
                    m_pastePosition -= 0.5 * gridSize;

                if (!IsSnapped)
                {
                    m_pasteOrientationAngle = 0.0f;
                    m_pasteDirForward = Vector3I.Forward;
                    m_pasteDirUp = Vector3I.Up;
                }

                IsSnapped = true;
            }
        }

        public override void MoveEntityFurther()
        {
            if (MyCubeBuilder.Static.DynamicMode)
            {
                base.MoveEntityFurther();
                if (m_dragDistance > MyCubeBuilder.MAX_BLOCK_BUILDING_DISTANCE)
                    m_dragDistance = MyCubeBuilder.MAX_BLOCK_BUILDING_DISTANCE;
            }
        }

        public override void MoveEntityCloser()
        {
            if (MyCubeBuilder.Static.DynamicMode)
            {
                base.MoveEntityCloser();
                if (m_dragDistance < MyCubeBuilder.MIN_BLOCK_BUILDING_DISTANCE)
                    m_dragDistance = MyCubeBuilder.MIN_BLOCK_BUILDING_DISTANCE;
            }
        }

        private void UpdateHitEntity()
        {
            m_closestHitDistSq = float.MaxValue;
            m_hitPos = new Vector3(0.0f, 0.0f, 0.0f);
            m_hitNormal = new Vector3(1.0f, 0.0f, 0.0f);
            m_hitEntity = null;
            m_addPos = Vector3I.Zero;
            RemoveBlock = null;
            BlockIdInCompound = null;
            m_dynamicBuildAllowed = false;

            if (MyFakes.ENABLE_BATTLE_SYSTEM && MySession.Static.Battle)
            {
                m_visible = false;
                return;
            }

            if (MyCubeBuilder.Static.DynamicMode)
            {
                m_visible = true;
                return;
            }

            MatrixD pasteMatrix = GetPasteMatrix();

            Vector3? addPosSmallOnLarge;
            Vector3I addDir;
            Vector3I removePos;

            if (MyCubeBuilder.Static.CurrentGrid == null && MyCubeBuilder.Static.CurrentVoxelMap == null) 
            {
                MyCubeBuilder.Static.ChoosePlacementObject();
            }

            if (MyCubeBuilder.Static.HitInfo.HasValue) 
            {
                float gridSize = MyDefinitionManager.Static.GetCubeSize(CopiedGrids[0].GridSizeEnum);
                MyCubeGrid hitGrid = MyCubeBuilder.Static.HitInfo.Value.HkHitInfo.Body.GetEntity() as MyCubeGrid;
                bool placingSmallGridOnLargeStatic = hitGrid != null && hitGrid.IsStatic && hitGrid.GridSizeEnum == MyCubeSize.Large && CopiedGrids[0].GridSizeEnum == MyCubeSize.Small && MyFakes.ENABLE_STATIC_SMALL_GRID_ON_LARGE;

                bool add = MyCubeBuilder.Static.GetAddAndRemovePositions(gridSize, placingSmallGridOnLargeStatic, out m_addPos, out addPosSmallOnLarge, out addDir, out removePos, out RemoveBlock, out BlockIdInCompound);
                if (add) 
                {
                    if (RemoveBlock != null) 
                    {
                        m_hitPos = MyCubeBuilder.Static.HitInfo.Value.Position;
                        m_closestHitDistSq = (float)(m_hitPos - pasteMatrix.Translation).LengthSquared();
                        m_hitNormal = addDir;
                        m_hitEntity = RemoveBlock.CubeGrid;

                        double cubeSizeTarget = MyDefinitionManager.Static.GetCubeSize(RemoveBlock.CubeGrid.GridSizeEnum);
                        double cubeSizeCopied = MyDefinitionManager.Static.GetCubeSize(CopiedGrids[0].GridSizeEnum);
                        if ((cubeSizeTarget / cubeSizeCopied) < 1)
                            RemoveBlock = null;

                        m_visible = RemoveBlock != null;
                    }
                    else if (MyFakes.ENABLE_BLOCK_PLACEMENT_ON_VOXEL && MyCubeBuilder.Static.HitInfo.Value.HkHitInfo.Body.GetEntity() is MyVoxelMap)
                    {
                        m_hitPos = MyCubeBuilder.Static.HitInfo.Value.Position;
                        m_closestHitDistSq = (float)(m_hitPos - pasteMatrix.Translation).LengthSquared();
                        m_hitNormal = addDir;
                        m_hitEntity = MyCubeBuilder.Static.HitInfo.Value.HkHitInfo.Body.GetEntity() as MyVoxelMap;

                        m_visible = true;
                    }
                    else 
                    {
                        m_visible = false;
                    }
                }
                else 
                {
                    m_visible = false;
                }
            }
            else 
            {
                m_visible = false;
            }
        }

        private new void FixSnapTransformationBase6()
        {
            Debug.Assert(CopiedGrids.Count > 0);
            if (CopiedGrids.Count == 0)
                return;

            var hitGrid = m_hitEntity as MyCubeGrid;
            if (hitGrid == null) 
                return;

            // Fix rotation of the first pasted grid
            Matrix hitGridRotation = hitGrid.WorldMatrix.GetOrientation();
            Matrix firstRotation = PreviewGrids[0].WorldMatrix.GetOrientation();
            Matrix newFirstRotation = Matrix.AlignRotationToAxes(ref firstRotation, ref hitGridRotation);
            Matrix rotationDelta = Matrix.Invert(firstRotation) * newFirstRotation;

            foreach (var grid in PreviewGrids)
            {
                Matrix rotation = grid.WorldMatrix.GetOrientation();
                rotation = rotation * rotationDelta;
                Matrix rotationInv = Matrix.Invert(rotation);

                Vector3D position = m_pastePosition;

                MatrixD newWorld = MatrixD.CreateWorld(position, rotation.Forward, rotation.Up);
                Debug.Assert(newWorld.GetOrientation().IsRotation());
                grid.PositionComp.SetWorldMatrix(newWorld);
            }

            bool smallOnLargeGrid = hitGrid.GridSizeEnum == MyCubeSize.Large && PreviewGrids[0].GridSizeEnum == MyCubeSize.Small;

            if (smallOnLargeGrid)
            {
                Vector3 pasteOffset = TransformLargeGridHitCoordToSmallGrid(m_hitPos, hitGrid.PositionComp.WorldMatrixNormalizedInv, hitGrid.GridSize);
                m_pastePosition = hitGrid.GridIntegerToWorld(pasteOffset);
            }
            else
            {
                // Find a collision-free position for the first paste grid along the raycast normal
                Vector3I collisionTestStep = Vector3I.Round(m_hitNormal);
                Vector3I pasteOffset = hitGrid.WorldToGridInteger(m_pastePosition);
                Vector3I previewGridMin = PreviewGrids[0].Min;
                Vector3I previewGridMax = PreviewGrids[0].Max;
                Vector3I previewGridSize = previewGridMax - previewGridMin + Vector3I.One;
                Vector3D previewGridSizeInWorld = Vector3D.TransformNormal((Vector3D)previewGridSize, PreviewGrids[0].WorldMatrix);
                Vector3I previewGridSizeInHitGrid = Vector3I.Abs(Vector3I.Round(Vector3D.TransformNormal(previewGridSizeInWorld, hitGrid.PositionComp.WorldMatrixNormalizedInv)));

                int attemptsCount = Math.Abs(Vector3I.Dot(ref collisionTestStep, ref previewGridSizeInHitGrid));
                Debug.Assert(attemptsCount > 0);
                int i;

                for (i = 0; i < attemptsCount; ++i)
                {
                    if (hitGrid.CanMergeCubes(PreviewGrids[0], pasteOffset))
                        break;
                    pasteOffset += collisionTestStep;
                }

                if (i == attemptsCount)
                {
                    pasteOffset = hitGrid.WorldToGridInteger(m_pastePosition);
                }

                m_pastePosition = hitGrid.GridIntegerToWorld(pasteOffset);
            }

            // Move all the grids according to the collision-free position of the first one
            for (int i = 0; i < PreviewGrids.Count; ++i)
            {
                var grid = PreviewGrids[i];
                MatrixD matrix = grid.WorldMatrix;
                matrix.Translation = m_pastePosition + Vector3.Transform(m_copiedGridOffsets[i], rotationDelta);
                grid.PositionComp.SetWorldMatrix(matrix);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
                MyRenderProxy.DebugDrawLine3D(m_hitPos, m_hitPos + m_hitNormal, Color.Red, Color.Green, false);
        }

        private bool TestPlacement()
        {
            bool retval = true;

            for (int i = 0; i < PreviewGrids.Count; ++i)
            {
                var grid = PreviewGrids[i];
                var settings = m_settings.GetGridPlacementSettings(grid);

                if (MySession.Static.SurvivalMode && !MyCubeBuilder.SpectatorIsBuilding)
                {
                    if (i == 0 && MyCubeBuilder.CameraControllerSpectator)
                    {
                        m_visible = false;
                        return false;
                    }

                    if (i == 0 && !MyCubeBuilder.Static.DynamicMode)
                    {
                        MatrixD invMatrix = grid.PositionComp.WorldMatrixNormalizedInv;
                        if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref invMatrix, (BoundingBoxD)grid.PositionComp.LocalAABB, grid.GridSize, MyCubeBuilder.Static.IntersectionDistance))
                        {
                            m_visible = false;
                            return false;
                        }
                    }

                    /*if (!MySession.Static.SimpleSurvival && MySession.ControlledEntity is MyCharacter)
                    {
                        foreach (var block in grid.GetBlocks())
                        {
                            retval &= (MySession.ControlledEntity as MyCharacter).CanStartConstruction(block.BlockDefinition);
                            if (!retval)
                                break;
                        }
                    }

                    if (i == 0 && MySession.Static.SimpleSurvival)
                    {
                        retval = retval && MyCubeBuilder.Static.CanBuildBlockSurvivalTime();
                    }*/

                    if (!retval)
                        return false;
                }

                if (MyCubeBuilder.Static.DynamicMode) 
                {
                    if (!m_dynamicBuildAllowed)
                    {
                        var settingsLocal = grid.GridSizeEnum == MyCubeSize.Large ? MyPerGameSettings.PastingSettings.LargeGrid : MyPerGameSettings.PastingSettings.SmallGrid;

                        foreach (var block in grid.GetBlocks())
                        {
                            Vector3 minLocal = block.Min * PreviewGrids[i].GridSize - Vector3.Half * PreviewGrids[i].GridSize;
                            Vector3 maxLocal = block.Max * PreviewGrids[i].GridSize + Vector3.Half * PreviewGrids[i].GridSize;
                            BoundingBoxD aabbLocal = new BoundingBoxD(minLocal, maxLocal);
                            retval = retval && MyCubeGrid.TestPlacementArea(grid, grid.IsStatic, ref settingsLocal, aabbLocal, true);
                            if (!retval)
                                break;
                        }
                    }
                }
                else if (i == 0 && m_hitEntity is MyCubeGrid && IsSnapped && SnapMode == MyGridPlacementSettings.SnapMode.Base6Directions)
                {
                    var hitGrid = m_hitEntity as MyCubeGrid;

                    bool smallOnLargeGrid = hitGrid.GridSizeEnum == MyCubeSize.Large && grid.GridSizeEnum == MyCubeSize.Small;

                    if (smallOnLargeGrid)
                    {
                        retval = retval && MyCubeGrid.TestPlacementArea(grid, ref settings, (BoundingBoxD)grid.PositionComp.LocalAABB, false/*, hitGrid*/);
                    }
                    else
                    {
                        retval = retval && TestGridPlacementOnGrid(grid, ref settings, hitGrid);
                    }
                }
                else if (i == 0 && m_hitEntity is MyVoxelMap)
                {
                    foreach (var block in grid.CubeBlocks)
                    {
                        if (block.FatBlock is MyCompoundCubeBlock)
                        {
                            MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                            foreach (var blockInCompound in compoundBlock.GetBlocks())
                            {
                                retval = retval && TestBlockPlacementArea(blockInCompound, ref settings, false);
                                if (!retval)
                                    break;
                            }
                        }
                        else
                        {
                            retval = retval && TestBlockPlacementArea(block, ref settings, false);
                        }

                        if (!retval)
                            break;
                    }
                }
                else
                {
                    var settingsLocal = m_settings.GetGridPlacementSettings(grid, grid.IsStatic && !MyCubeBuilder.Static.DynamicMode);
                    retval = retval && MyCubeGrid.TestPlacementArea(grid, grid.IsStatic, ref settingsLocal, (BoundingBoxD)grid.PositionComp.LocalAABB, false);
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
            }

            return retval;
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

            if (RemoveBlock != null)
            {
                Vector4 red = new Vector4(Color.Red.ToVector3() * 0.8f, 1);
                MyCubeBuilder.DrawSemiTransparentBox(RemoveBlock.CubeGrid, RemoveBlock, red, lineMaterial: "GizmoDrawLineRed");
            }
        }

        protected override void SetupDragDistance()
        {
            if (!IsActive)
                return;

            base.SetupDragDistance();

            if (MySession.Static.SurvivalMode)
                m_dragDistance = MyCubeBuilder.Static.IntersectionDistance;
        }


    }
}
