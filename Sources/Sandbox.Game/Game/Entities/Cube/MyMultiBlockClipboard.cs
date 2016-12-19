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
using Sandbox.Game.GameSystems.CoordinateSystem;
using Sandbox.Game.GUI;
using Sandbox.Game.World;
using Sandbox.Graphics;
using VRageMath;
using VRageRender;
using VRage.ObjectBuilders;
using VRage;
using VRage.Library.Utils;
using VRage.Game.Entity;
using VRage.Game;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents.Clipboard;
using VRage.Audio;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;
using VRage.Profiler;

namespace Sandbox.Game.Entities.Cube
{
    /// <summary>
    ///  Multiblock clipboard for building multiblocks. Can be used for building only (not copy/paste) because it uses definitions not real tile grid/block data.
    /// </summary>
    public class MyMultiBlockClipboard : MyGridClipboardAdvanced
    {
        private static List<Vector3D> m_tmpCollisionPoints = new List<Vector3D>();
        private static List<MyEntity> m_tmpNearEntities = new List<MyEntity>();

        protected override bool AnyCopiedGridIsStatic
        {
            get
            {
                return false;
            }
        }

        private MyMultiBlockDefinition m_multiBlockDefinition;

        public MySlimBlock RemoveBlock;
        public ushort? BlockIdInCompound;

        private Vector3I m_addPos;

        public HashSet<Tuple<MySlimBlock, ushort?>> RemoveBlocksInMultiBlock = new HashSet<Tuple<MySlimBlock, ushort?>>();
        private HashSet<Vector3I> m_tmpBlockPositionsSet = new HashSet<Vector3I>();
        private bool m_lastVoxelState = false;


        public MyMultiBlockClipboard(MyPlacementSettings settings, bool calculateVelocity = true) : base(settings, calculateVelocity)
        {
            m_useDynamicPreviews = false;
        }

        public override void Deactivate(bool afterPaste = false)
        {
            m_multiBlockDefinition = null;
            base.Deactivate(afterPaste: afterPaste);
        }

        public override void Update()
        {
            if (!IsActive)
                return;

            UpdateHitEntity();

            if (!m_visible)
            {
                ShowPreview(false);
                return;
            }

            if (PreviewGrids.Count == 0)
                return;

            if (m_dragDistance == 0)
                SetupDragDistance();
            if (m_dragDistance > MyCubeBuilder.CubeBuilderDefinition.MaxBlockBuildingDistance)
                m_dragDistance = MyCubeBuilder.CubeBuilderDefinition.MaxBlockBuildingDistance;

            UpdatePastePosition();
            UpdateGridTransformations();
            FixSnapTransformationBase6();

            if (m_calculateVelocity)
                m_objectVelocity = (m_pastePosition - m_pastePositionPrevious) / VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            m_canBePlaced = TestPlacement();

            if (!m_visible)
            {
                ShowPreview(false);
                return;
            }

            ShowPreview(true);

            TestBuildingMaterials();
            m_canBePlaced &= CharacterHasEnoughMaterials;

            UpdatePreview();

            if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "FW: " + m_pasteDirForward.ToString(), Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 20.0f), "UP: " + m_pasteDirUp.ToString(), Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 40.0f), "AN: " + m_pasteOrientationAngle.ToString(), Color.Red, 1.0f);
            }
        }

        public override bool PasteGrid(MyInventoryBase buildInventory = null, bool deactivate = true) 
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

        public override bool EntityCanPaste(MyEntity pastingEntity)
        {
            if (CopiedGrids.Count < 1) 
                return false;

            if (MySession.Static.CreativeToolsEnabled(Sync.MyId))
                return true;

            MyCubeBuilder.BuildComponent.GetMultiBlockPlacementMaterials(m_multiBlockDefinition);
            return MyCubeBuilder.BuildComponent.HasBuildingMaterials(pastingEntity);
        }

        private bool PasteGridsInDynamicMode(MyInventoryBase buildInventory, bool deactivate)
        {
            bool result;
            // Remember static grid flag and set it to dynamic
            List<bool> gridStaticFlags = new List<bool>();
            foreach (var copiedGrid in CopiedGrids)
            {
                gridStaticFlags.Add(copiedGrid.IsStatic);
                copiedGrid.IsStatic = false;
                BeforeCreateGrid(copiedGrid);
            }

            result = PasteGridInternal(buildInventory: buildInventory, deactivate: deactivate, multiBlock: true);

            // Set static grid flag back
            for (int i = 0; i < CopiedGrids.Count; ++i)
                CopiedGrids[i].IsStatic = gridStaticFlags[i];
            return result;
        }

        private bool PasteGridsInStaticMode(MyInventoryBase buildInventory, bool deactivate)
        {
            // Paste generates grid from builder and use matrix from preview
            List<MyObjectBuilder_CubeGrid> copiedGridsOrig = new List<MyObjectBuilder_CubeGrid>();
            List<MatrixD> previewGridsWorldMatrices = new List<MatrixD>();

            {
                // First grid is forced static
                MyObjectBuilder_CubeGrid originalCopiedGrid = CopiedGrids[0];
                BeforeCreateGrid(originalCopiedGrid);
                copiedGridsOrig.Add(originalCopiedGrid);
                MatrixD previewGridWorldMatrix = PreviewGrids[0].WorldMatrix;
                // Convert grid builder to static 
                var gridBuilder = MyCubeBuilder.ConvertGridBuilderToStatic(originalCopiedGrid, previewGridWorldMatrix);
                // Set it to copied grids
                CopiedGrids[0] = gridBuilder;

                previewGridsWorldMatrices.Add(previewGridWorldMatrix);
                //PreviewGrids[0].WorldMatrix = MatrixD.CreateTranslation(previewGridWorldMatrix.Translation);
            }


            for (int i = 1; i < CopiedGrids.Count; ++i)
            {
                MyObjectBuilder_CubeGrid originalCopiedGrid = CopiedGrids[i];
                BeforeCreateGrid(originalCopiedGrid);
                copiedGridsOrig.Add(originalCopiedGrid);
                MatrixD previewGridWorldMatrix = PreviewGrids[i].WorldMatrix;
                previewGridsWorldMatrices.Add(previewGridWorldMatrix);

                if (CopiedGrids[i].IsStatic)
                {
                    // Convert grid builder to static 
                    var gridBuilder = MyCubeBuilder.ConvertGridBuilderToStatic(originalCopiedGrid, previewGridWorldMatrix);
                    // Set it to copied grids
                    CopiedGrids[i] = gridBuilder;

                    //PreviewGrids[i].WorldMatrix = MatrixD.CreateTranslation(previewGridWorldMatrix.Translation);
                }
            }

            Debug.Assert(CopiedGrids.Count == copiedGridsOrig.Count);
            Debug.Assert(CopiedGrids.Count == previewGridsWorldMatrices.Count);

            bool result = PasteGridInternal(buildInventory: buildInventory, deactivate: deactivate, multiBlock: true, touchingGrids: m_touchingGrids);

            // Set original grids back
            CopiedGrids.Clear();
            CopiedGrids.AddList(copiedGridsOrig);

            for (int i = 0; i < PreviewGrids.Count; ++i)
                PreviewGrids[i].WorldMatrix = previewGridsWorldMatrices[i];

            return result;
        }

        protected override void UpdatePastePosition()
        {
            m_pastePositionPrevious = m_pastePosition;


            if (MyCubeBuilder.Static.HitInfo.HasValue)
                m_pastePosition = MyCubeBuilder.Static.HitInfo.Value.Position;
            else
                m_pastePosition = MyCubeBuilder.Static.FreePlacementTarget;
            
            double gridSize = MyDefinitionManager.Static.GetCubeSize(CopiedGrids[0].GridSizeEnum);
            MyCoordinateSystem.CoordSystemData localCoordData = MyCoordinateSystem.Static.SnapWorldPosToClosestGrid(ref m_pastePosition, gridSize, m_settings.StaticGridAlignToCenter);

            EnableStationRotation = MyCubeBuilder.Static.DynamicMode;

            if (MyCubeBuilder.Static.DynamicMode)
            {
                this.AlignClipboardToGravity();
                
                m_visible = true;
                IsSnapped = false;
                m_lastVoxelState = false;
            }
            else if (RemoveBlock != null) 
            {
                m_pastePosition = Vector3D.Transform(m_addPos * RemoveBlock.CubeGrid.GridSize, RemoveBlock.CubeGrid.WorldMatrix);

                if (!IsSnapped && RemoveBlock.CubeGrid.IsStatic)
                {
                    m_pasteOrientationAngle = 0.0f;
                    m_pasteDirForward = RemoveBlock.CubeGrid.WorldMatrix.Forward;
                    m_pasteDirUp = RemoveBlock.CubeGrid.WorldMatrix.Up;
                }

                IsSnapped = true;
                m_lastVoxelState = false;
            }
            else if (MyFakes.ENABLE_BLOCK_PLACEMENT_ON_VOXEL && m_hitEntity is MyVoxelBase) 
            {

                if (MyCoordinateSystem.Static.LocalCoordExist)
                {
                    m_pastePosition = localCoordData.SnappedTransform.Position;
                    if(!m_lastVoxelState)
                        this.AlignRotationToCoordSys();
                }

                IsSnapped = true;
                m_lastVoxelState = true;
            }
        }

        public override void MoveEntityFurther()
        {
            if (MyCubeBuilder.Static.DynamicMode)
            {
                base.MoveEntityFurther();
                if (m_dragDistance > MyCubeBuilder.CubeBuilderDefinition.MaxBlockBuildingDistance)
                    m_dragDistance = MyCubeBuilder.CubeBuilderDefinition.MaxBlockBuildingDistance;
            }
        }

        public override void MoveEntityCloser()
        {
            if (MyCubeBuilder.Static.DynamicMode)
            {
                base.MoveEntityCloser();
                if (m_dragDistance < MyCubeBuilder.CubeBuilderDefinition.MinBlockBuildingDistance)
                    m_dragDistance = MyCubeBuilder.CubeBuilderDefinition.MinBlockBuildingDistance;
            }
        }

        protected override void ChangeClipboardPreview(bool visible)
        {
            base.ChangeClipboardPreview(visible);

            if (!visible)
                return;

            if (MySession.Static.SurvivalMode)
            {
                // Modify integrity of preview grid blocks to full integrity.
                foreach (var grid in PreviewGrids)
                {
                    foreach (var block in grid.GetBlocks())
                    {
                        var compound = block.FatBlock as MyCompoundCubeBlock;
                        if (compound != null)
                        {
                            foreach (var blockInCompound in compound.GetBlocks())
                                SetBlockToFullIntegrity(blockInCompound);
                        }
                        else
                        {
                            SetBlockToFullIntegrity(block);
                        }
                    }
                }
            }
        }

        private static void SetBlockToFullIntegrity(MySlimBlock block)
        {
            var oldRatio = block.ComponentStack.BuildRatio;
            block.ComponentStack.SetIntegrity(block.ComponentStack.MaxIntegrity, block.ComponentStack.MaxIntegrity);
            if (block.BlockDefinition.ModelChangeIsNeeded(oldRatio, block.ComponentStack.BuildRatio))
                block.UpdateVisual();
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
            RemoveBlocksInMultiBlock.Clear();
            m_dynamicBuildAllowed = false;
            m_visible = false;
            m_canBePlaced = false;

            if (MyCubeBuilder.Static.DynamicMode)
            {
                m_visible = true;
                return;
            }

            MatrixD pasteMatrix = GetPasteMatrix();

            Vector3? addPosSmallOnLarge;
            Vector3I addDir;
            Vector3I removePos;

            if (MyCubeBuilder.Static.CurrentGrid == null && MyCubeBuilder.Static.CurrentVoxelBase == null) 
            {
                MyCubeBuilder.Static.ChooseHitObject();
            }

            if (MyCubeBuilder.Static.HitInfo.HasValue) 
            {
                float gridSize = MyDefinitionManager.Static.GetCubeSize(CopiedGrids[0].GridSizeEnum);
                MyCubeGrid hitGrid = MyCubeBuilder.Static.HitInfo.Value.HkHitInfo.GetHitEntity() as MyCubeGrid;
                bool placingSmallGridOnLargeStatic = hitGrid != null && hitGrid.IsStatic && hitGrid.GridSizeEnum == MyCubeSize.Large && CopiedGrids[0].GridSizeEnum == MyCubeSize.Small && MyFakes.ENABLE_STATIC_SMALL_GRID_ON_LARGE;

                bool add = MyCubeBuilder.Static.GetAddAndRemovePositions(gridSize, placingSmallGridOnLargeStatic, out m_addPos, out addPosSmallOnLarge, out addDir, 
                    out removePos, out RemoveBlock, out BlockIdInCompound, RemoveBlocksInMultiBlock);
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
                    else if (MyFakes.ENABLE_BLOCK_PLACEMENT_ON_VOXEL && MyCubeBuilder.Static.HitInfo.Value.HkHitInfo.GetHitEntity() is MyVoxelBase)
                    {
                        m_hitPos = MyCubeBuilder.Static.HitInfo.Value.Position;
                        m_closestHitDistSq = (float)(m_hitPos - pasteMatrix.Translation).LengthSquared();
                        m_hitNormal = addDir;
                        m_hitEntity = MyCubeBuilder.Static.HitInfo.Value.HkHitInfo.GetHitEntity() as MyVoxelBase;

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
            Matrix rotationDelta = GetRotationDeltaMatrixToHitGrid(hitGrid);

            foreach (var grid in PreviewGrids)
            {
                Matrix rotation = grid.WorldMatrix.GetOrientation();
                rotation = rotation * rotationDelta;

                Vector3D position = m_pastePosition;

                MatrixD newWorld = MatrixD.CreateWorld(position, rotation.Forward, rotation.Up);
                Debug.Assert(newWorld.GetOrientation().IsRotation());
                grid.PositionComp.SetWorldMatrix(newWorld);
            }

            bool smallOnLargeGrid = hitGrid.GridSizeEnum == MyCubeSize.Large && PreviewGrids[0].GridSizeEnum == MyCubeSize.Small;

            if (smallOnLargeGrid)
            {
                Vector3 pasteOffset = MyCubeBuilder.TransformLargeGridHitCoordToSmallGrid(m_hitPos, hitGrid.PositionComp.WorldMatrixNormalizedInv, hitGrid.GridSize);
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

        public Matrix GetRotationDeltaMatrixToHitGrid(MyCubeGrid hitGrid)
        {
            // Fix rotation of the first pasted grid
            Matrix hitGridRotation = hitGrid.WorldMatrix.GetOrientation();
            Matrix firstRotation = PreviewGrids[0].WorldMatrix.GetOrientation();
            Matrix newFirstRotation = Matrix.AlignRotationToAxes(ref firstRotation, ref hitGridRotation);
            Matrix rotationDelta = Matrix.Invert(firstRotation) * newFirstRotation;
            return rotationDelta;
        }

        private new bool TestPlacement()
        {
            bool retval = true;

            m_touchingGrids.Clear();

            for (int i = 0; i < PreviewGrids.Count; ++i)
            {
                var grid = PreviewGrids[i];
                var settings = m_settings.GetGridPlacementSettings(grid.GridSizeEnum);

                m_touchingGrids.Add(null);

                if (MySession.Static.SurvivalMode && !MyCubeBuilder.SpectatorIsBuilding && !MySession.Static.CreativeToolsEnabled(Sync.MyId))
                {
                    if (i == 0 && MyCubeBuilder.CameraControllerSpectator)
                    {
                        m_visible = false;
                        return false;
                    }

                    if (i == 0 && !MyCubeBuilder.Static.DynamicMode)
                    {
                        MatrixD invMatrix = grid.PositionComp.WorldMatrixNormalizedInv;
                        if (!MyCubeBuilderGizmo.DefaultGizmoCloseEnough(ref invMatrix, (BoundingBoxD)grid.PositionComp.LocalAABB, grid.GridSize, MyCubeBuilder.IntersectionDistance))
                        {
                            m_visible = false;
                            return false;
                        }
                    }

                    /*if (!MySession.Static.SimpleSurvival && MySession.Static.ControlledEntity is MyCharacter)
                    {
                        foreach (var block in grid.GetBlocks())
                        {
                            retval &= (MySession.Static.ControlledEntity as MyCharacter).CanStartConstruction(block.BlockDefinition);
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
                   // if (!m_dynamicBuildAllowed)
                  //  {
                        var settingsLocal = grid.GridSizeEnum == MyCubeSize.Large ? m_settings.LargeGrid :
                                                                                    m_settings.SmallGrid;
                        bool anyBlockVoxelHit = false;
                        foreach (var block in grid.GetBlocks())
                        {
                            Vector3 minLocal = block.Min * PreviewGrids[i].GridSize - Vector3.Half * PreviewGrids[i].GridSize;
                            Vector3 maxLocal = block.Max * PreviewGrids[i].GridSize + Vector3.Half * PreviewGrids[i].GridSize;
                            BoundingBoxD aabbLocal = new BoundingBoxD(minLocal, maxLocal);
                            if (!anyBlockVoxelHit)
                                anyBlockVoxelHit = TestVoxelPlacement(block, ref settings, true);
                            retval = retval && MyCubeGrid.TestPlacementArea(grid, grid.IsStatic, ref settingsLocal, aabbLocal, true, testVoxel: false);
                            if (!retval)
                                break;
                        }

                        retval = retval && anyBlockVoxelHit;
                   // }
                    }
                else if (i == 0 && m_hitEntity is MyCubeGrid && IsSnapped /* && SnapMode == SnapMode.Base6Directions*/)
                {
                    var hitGrid = m_hitEntity as MyCubeGrid;
                    var settingsLocal = m_settings.GetGridPlacementSettings(hitGrid.GridSizeEnum, hitGrid.IsStatic);

                    bool smallOnLargeGrid = hitGrid.GridSizeEnum == MyCubeSize.Large && grid.GridSizeEnum == MyCubeSize.Small;

                    if (smallOnLargeGrid)
                    {
                        retval = retval && MyCubeGrid.TestPlacementArea(grid, ref settings, (BoundingBoxD)grid.PositionComp.LocalAABB, false);
                    }
                    else
                    {
                        retval = retval && TestGridPlacementOnGrid(grid, ref settingsLocal, hitGrid);
                    }

                    m_touchingGrids.Clear();
                    m_touchingGrids.Add(hitGrid);
                }
                else if (i == 0 && m_hitEntity is MyVoxelMap)
                {
                    bool anyBlockVoxelHit = false;
                    foreach (var block in grid.CubeBlocks)
                    {
                        if (block.FatBlock is MyCompoundCubeBlock)
                        {
                            MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                            foreach (var blockInCompound in compoundBlock.GetBlocks())
                            {
                                if(!anyBlockVoxelHit)
                                    anyBlockVoxelHit = TestVoxelPlacement(blockInCompound, ref settings, false);
                                retval = retval && TestBlockPlacementArea(blockInCompound, ref settings, false, false);
                                if (!retval)
                                    break;
                            }
                        }
                        else
                        {
                            if (!anyBlockVoxelHit)
                                anyBlockVoxelHit = TestVoxelPlacement(block, ref settings, false);
                            retval = retval && TestBlockPlacementArea(block, ref settings, false, false);
                        }

                        if (!retval)
                            break;
                    }

                    retval = retval && anyBlockVoxelHit;

                    Debug.Assert(i == 0);
                    m_touchingGrids[i] = DetectTouchingGrid();
                }
                else
                {
                    var settingsLocal = m_settings.GetGridPlacementSettings(grid.GridSizeEnum, grid.IsStatic && !MyCubeBuilder.Static.DynamicMode);
                    retval = retval && MyCubeGrid.TestPlacementArea(grid, grid.IsStatic, ref settingsLocal, (BoundingBoxD)grid.PositionComp.LocalAABB, false);
                }

                BoundingBoxD aabb = (BoundingBoxD)grid.PositionComp.LocalAABB;
                MatrixD invGridWorlMatrix = grid.PositionComp.WorldMatrixNormalizedInv;

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

        /// <summary>
        /// Detects a grid where multiblock can be merged.
        /// </summary>
        private MyCubeGrid DetectTouchingGrid()
        {
            if (PreviewGrids == null || PreviewGrids.Count == 0)
                return null;

            foreach (var block in PreviewGrids[0].CubeBlocks)
            {
                MyCubeGrid touchingGrid = DetectTouchingGrid(block);
                if (touchingGrid != null)
                    return touchingGrid;
            }

            return null;
        }

        private MyCubeGrid DetectTouchingGrid(MySlimBlock block)
        {
            if (MyCubeBuilder.Static.DynamicMode)
                return null;

            if (block == null)
                return null;

            if (block.FatBlock is MyCompoundCubeBlock)
            {
                foreach (var blockInCompound in (block.FatBlock as MyCompoundCubeBlock).GetBlocks())
                {
                    MyCubeGrid touchingGrid = DetectTouchingGrid(blockInCompound);
                    if (touchingGrid != null)
                        return touchingGrid;
                }

                return null;
            }

            ProfilerShort.Begin("MultiBlockClipboard: DetectMerge");

            float gridSize = block.CubeGrid.GridSize;
            BoundingBoxD aabb;
            block.GetWorldBoundingBox(out aabb);
            // Inflate by half cube, so it will intersect for sure when there's anything
            aabb.Inflate(gridSize / 2);

            m_tmpNearEntities.Clear();
            MyEntities.GetElementsInBox(ref aabb, m_tmpNearEntities);

            var mountPoints = block.BlockDefinition.GetBuildProgressModelMountPoints(block.BuildLevelRatio);

            try
            {
                for (int i = 0; i < m_tmpNearEntities.Count; i++)
                {
                    var grid = m_tmpNearEntities[i] as MyCubeGrid;
                    if (grid != null && grid != block.CubeGrid && grid.Physics != null && grid.Physics.Enabled && grid.IsStatic && grid.GridSizeEnum == block.CubeGrid.GridSizeEnum)
                    {
                        Vector3I gridOffset = grid.WorldToGridInteger(m_pastePosition);
                        if (!grid.CanMergeCubes(block.CubeGrid, gridOffset))
                            continue;

                        MatrixI transform = grid.CalculateMergeTransform(block.CubeGrid, gridOffset);
                        Base6Directions.Direction forward = transform.GetDirection(block.Orientation.Forward);
                        Base6Directions.Direction up = transform.GetDirection(block.Orientation.Up);
                        MyBlockOrientation newOrientation = new MyBlockOrientation(forward, up);
                        Quaternion newRotation;
                        newOrientation.GetQuaternion(out newRotation);
                        Vector3I newPosition = Vector3I.Transform(block.Position, transform);

                        if (!MyCubeGrid.CheckConnectivity(grid, block.BlockDefinition, mountPoints, ref newRotation, ref newPosition))
                            continue;

                        return grid;
                    }
                }
            }
            finally
            {
                m_tmpNearEntities.Clear();
                ProfilerShort.End();
            }

            return null;
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

            Vector4 red = new Vector4(Color.Red.ToVector3() * 0.8f, 1);

            if (RemoveBlocksInMultiBlock.Count > 0)
            {
                m_tmpBlockPositionsSet.Clear();

                MyCubeBuilder.GetAllBlocksPositions(RemoveBlocksInMultiBlock, m_tmpBlockPositionsSet);

                foreach (var position in m_tmpBlockPositionsSet)
                    MyCubeBuilder.DrawSemiTransparentBox(position, position, RemoveBlock.CubeGrid, red, lineMaterial: "GizmoDrawLineRed");

                m_tmpBlockPositionsSet.Clear();
            }
            else if (RemoveBlock != null)
            {
                MyCubeBuilder.DrawSemiTransparentBox(RemoveBlock.CubeGrid, RemoveBlock, red, lineMaterial: "GizmoDrawLineRed");
            }
        }

        protected override void SetupDragDistance()
        {
            if (!IsActive)
                return;

            base.SetupDragDistance();

            if (MySession.Static.SurvivalMode && !MySession.Static.CreativeToolsEnabled(Sync.MyId))
                m_dragDistance = MyCubeBuilder.IntersectionDistance;
        }

        public void SetGridFromBuilder(MyMultiBlockDefinition multiBlockDefinition, MyObjectBuilder_CubeGrid grid, Vector3 dragPointDelta, float dragVectorLength)
        {
            Debug.Assert(multiBlockDefinition != null);
            ChangeClipboardPreview(false);
            m_multiBlockDefinition = multiBlockDefinition;

            SetGridFromBuilder(grid, dragPointDelta, dragVectorLength);
            ChangeClipboardPreview(true);
        }

        public static void TakeMaterialsFromBuilder(List<MyObjectBuilder_CubeGrid> blocksToBuild, MyEntity builder)
        {
            Debug.Assert(blocksToBuild.Count == 1);
            if (blocksToBuild.Count == 0)
                return;

            // Search for multiblock definition.
            var firstBlock = blocksToBuild[0].CubeBlocks.FirstOrDefault();
            Debug.Assert(firstBlock != null);
            if (firstBlock == null )
                return;

            MyDefinitionId multiBlockDefId;
            var compound = firstBlock as MyObjectBuilder_CompoundCubeBlock;
            if (compound != null)
            {
                Debug.Assert(compound.Blocks != null && compound.Blocks.Length > 0 && compound.Blocks[0].MultiBlockDefinition != null);
                if (compound.Blocks == null || compound.Blocks.Length == 0 || compound.Blocks[0].MultiBlockDefinition == null)
                    return;

                multiBlockDefId = compound.Blocks[0].MultiBlockDefinition.Value;
            }
            else
            {
                Debug.Assert(firstBlock.MultiBlockDefinition != null);
                if (firstBlock.MultiBlockDefinition == null)
                    return;

                multiBlockDefId = firstBlock.MultiBlockDefinition.Value;
            }

            MyMultiBlockDefinition multiBlockDefinition = MyDefinitionManager.Static.TryGetMultiBlockDefinition(multiBlockDefId);
            Debug.Assert(multiBlockDefinition != null);
            if (multiBlockDefinition == null)
                return;

            MyCubeBuilder.BuildComponent.GetMultiBlockPlacementMaterials(multiBlockDefinition);
            MyCubeBuilder.BuildComponent.AfterSuccessfulBuild(builder, instantBuild: false);
        }
    }
}
