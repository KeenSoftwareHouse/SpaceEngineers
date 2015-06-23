using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.GUI;
using VRageRender;
using Sandbox.Graphics.GUI;

using VRage;
using Sandbox.Game.Localization;
using Sandbox.Graphics;
using VRage.ObjectBuilders;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Cube
{

    public struct MyPlacementSettings
    {
        public MyGridPlacementSettings SmallGrid;
        public MyGridPlacementSettings SmallStaticGrid;
        public MyGridPlacementSettings LargeGrid;
        public MyGridPlacementSettings LargeStaticGrid;

        /// <summary>
        /// Align static grids to corners (false) or centers (true).
        /// You should always set to corners in new games. Center alignment is only for backwards compatibility so that
        /// static grids are correctly aligned with already existing saves.
        /// </summary>
        public bool StaticGridAlignToCenter;

        internal MyGridPlacementSettings GetGridPlacementSettings(MyCubeGrid grid)
        {
            return GetGridPlacementSettings(grid, grid.IsStatic);
        }

        internal MyGridPlacementSettings GetGridPlacementSettings(MyCubeGrid grid, bool isStatic)
        {
            switch (grid.GridSizeEnum)
            {
                case MyCubeSize.Large: return (isStatic) ? LargeStaticGrid : LargeGrid;
                case MyCubeSize.Small: return (isStatic) ? SmallStaticGrid : SmallGrid;

                default:
                    Debug.Fail("Invalid branch.");
                    return LargeGrid;
            }
        }

    }

    public struct MyGridPlacementSettings
    {
        public SnapMode Mode;
        public float SearchHalfExtentsDeltaRatio;
        public float SearchHalfExtentsDeltaAbsolute;
        public GroundPenetration Penetration;

        /// <summary>
        /// When min. allowed penetration is not met, block may still be placed if it is touching static grid and this property is true.
        /// </summary>
        public bool CanAnchorToStaticGrid;

        public struct GroundPenetration
        {
            public float MinAllowed;
            public float MaxAllowed;
            public PenetrationUnitEnum Unit;
        }

        public enum PenetrationUnitEnum
        {
            Ratio,
            Absolute,
        }

        public enum SnapMode
        {
            Base6Directions,
            OneFreeAxis,
        }

        public bool EnablePreciseRotationWhenSnapped;
    }

    public class MyGridClipboard
    {
        struct GridCopy
        {
            MyObjectBuilder_CubeGrid Grid;
            Vector3 Offset;
            Quaternion Rotation;
        }

        private static List<HkRigidBody> m_cacheRigidBodyList = new List<HkRigidBody>();
        private static HashSet<IMyEntity> m_cacheEntitySet = new HashSet<IMyEntity>();
        private static List<MyObjectBuilder_EntityBase> m_tmpPastedBuilders = new List<MyObjectBuilder_EntityBase>();

        private List<MyObjectBuilder_CubeGrid> m_copiedGrids = new List<MyObjectBuilder_CubeGrid>();
        protected List<Vector3> m_copiedGridOffsets = new List<Vector3>();
        private List<MyCubeGrid> m_previewGrids = new List<MyCubeGrid>();

        private MyComponentList m_buildComponents = new MyComponentList();

        // Paste position
        protected Vector3D m_pastePosition;
        protected Vector3D m_pastePositionPrevious;

        // Paste velocity
        protected bool m_calculateVelocity = true;
        protected Vector3 m_objectVelocity = Vector3.Zero;

        // Paste orientation
        protected float m_pasteOrientationAngle = 0.0f;
        protected Vector3 m_pasteDirUp = new Vector3(1.0f, 0.0f, 0.0f);
        protected Vector3 m_pasteDirForward = new Vector3(0.0f, 1.0f, 0.0f);

        // Copy position
        protected float m_dragDistance;
        protected Vector3 m_dragPointToPositionLocal;

        // Placement flags
        protected bool m_canBePlaced;
        protected virtual bool CanBePlaced
        {
            get
            {
                return m_canBePlaced;
            }
        }
        protected bool m_characterHasEnoughMaterials = false;
        public bool CharacterHasEnoughMaterials { get { return m_characterHasEnoughMaterials; } }

        protected MyPlacementSettings m_settings;

        // Raycasting
        protected List<MyPhysics.HitInfo> m_raycastCollisionResults = new List<MyPhysics.HitInfo>();
        protected float m_closestHitDistSq = float.MaxValue;
        protected Vector3D m_hitPos = new Vector3(0.0f, 0.0f, 0.0f);
        protected Vector3 m_hitNormal = new Vector3(1.0f, 0.0f, 0.0f);
        protected IMyEntity m_hitEntity = null;

        protected bool m_visible = true;
        private bool m_allowSwitchCameraMode = true;

        protected bool m_useDynamicPreviews = false;

        protected delegate void UpdateAfterPasteCallback(List<MyObjectBuilder_CubeGrid> pastedBuilders);


        public virtual bool HasPreviewBBox
        {
            get
            {
                return true;
            }
            set
            {

            }
        }

        public bool IsActive
        {
            get;
            private set;
        }

        public bool AllowSwitchCameraMode
        {
            get { return m_allowSwitchCameraMode; }
            private set { m_allowSwitchCameraMode = value; }
        }

        public bool IsSnapped
        {
            get;
            protected set;
        }

        public List<MyObjectBuilder_CubeGrid> CopiedGrids
        {
            get { return m_copiedGrids;  }
        }

        public MyGridPlacementSettings.SnapMode SnapMode
        {
            get
            {
                if (m_previewGrids.Count == 0)
                    return MyGridPlacementSettings.SnapMode.Base6Directions;

                var gridSettings = m_settings.GetGridPlacementSettings(m_previewGrids[0]);
                return gridSettings.Mode;
            }
        }

        public bool EnablePreciseRotationWhenSnapped
        {
            get
            {
                if (m_previewGrids.Count == 0)
                    return false;

                var gridSettings = m_settings.GetGridPlacementSettings(m_previewGrids[0]);
                return gridSettings.EnablePreciseRotationWhenSnapped && EnableStationRotation;
            }
        }

        public bool OneAxisRotationMode
        {
            get
            {
                return IsSnapped && SnapMode == MyGridPlacementSettings.SnapMode.OneFreeAxis;
            }
        }

        public List<MyCubeGrid> PreviewGrids
        {
            get { return m_previewGrids; }
        }

        protected virtual bool AnyCopiedGridIsStatic
        {
            get
            {
                if (EnableStationRotation == true)
                {
                    return false;
                }

                foreach (var grid in m_previewGrids)
                {
                    if (grid.IsStatic)
                        return true;
                }
                return false;
            }
        }

        protected bool EnableGridChangeToDynamic = MyFakes.ENABLE_GRID_CLIPBOARD_CHANGE_TO_DYNAMIC;
        private bool m_gridChangeToDynamicDisabled;

        bool m_enableStationRotation = false;
        public bool EnableStationRotation
        {
            get
            {
                return m_enableStationRotation && MyFakes.ENABLE_STATION_ROTATION;
            }

            set
            {
                m_enableStationRotation = value;
            }
        }


        public MyGridClipboard(MyPlacementSettings settings, bool calculateVelocity = true)
        {
            m_calculateVelocity = calculateVelocity;
            m_settings = settings;
        }

        public MyCubeBlockDefinition GetFirstBlockDefinition(MyObjectBuilder_CubeGrid grid = null)
        {
            if (grid == null)
                if (m_copiedGrids.Count > 0)
                    grid = m_copiedGrids[0];
                else
                    return null;

            if (grid.CubeBlocks.Count() > 0)
            {
                MyDefinitionId firstBlock = grid.CubeBlocks[0].GetId();
                return MyDefinitionManager.Static.GetCubeBlockDefinition(firstBlock);
            }
            return null;
        }

        public virtual void Activate()
        {
            ChangeClipboardPreview(true);
            IsActive = true;
        }

        public virtual void Deactivate()
        {
            ChangeClipboardPreview(false);
            IsActive = false;
        }

        public void Hide()
        {
            ChangeClipboardPreview(false);
        }

        public void Show()
        {
            if (IsActive && m_previewGrids.Count == 0)
                ChangeClipboardPreview(true);
        }

        public void CopyGroup(MyCubeGrid gridInGroup)
        {
            if (gridInGroup == null)
                return;
            m_copiedGrids.Clear();
            m_copiedGridOffsets.Clear();

            if (MyFakes.ENABLE_COPY_GROUP && MyFakes.ENABLE_LARGE_STATIC_GROUP_COPY_FIRST)
            {
                // Find large static grid, large grid or small static grid if present as first group.
                var group = MyCubeGridGroups.Static.Logical.GetGroup(gridInGroup);

                MyCubeGrid staticLargeGrid = null;
                MyCubeGrid largeGrid = null;
                MyCubeGrid smallStaticGrid = null;
                if (gridInGroup.GridSizeEnum == MyCubeSize.Large)
                {
                    largeGrid = gridInGroup;
                    if (gridInGroup.IsStatic)
                        staticLargeGrid = gridInGroup;
                }
                else if (gridInGroup.GridSizeEnum == MyCubeSize.Small && gridInGroup.IsStatic)
                    smallStaticGrid = gridInGroup;

                foreach (var node in group.Nodes)
                {
                    if (largeGrid == null && node.NodeData.GridSizeEnum == MyCubeSize.Large)
                        largeGrid = node.NodeData;

                    if (staticLargeGrid == null && node.NodeData.GridSizeEnum == MyCubeSize.Large && node.NodeData.IsStatic)
                        staticLargeGrid = node.NodeData;

                    if (smallStaticGrid == null && node.NodeData.GridSizeEnum == MyCubeSize.Small && node.NodeData.IsStatic)
                        smallStaticGrid = node.NodeData;
                }

                MyCubeGrid firstGrid = staticLargeGrid != null ? staticLargeGrid : null;
                firstGrid = firstGrid != null ? firstGrid : (largeGrid != null ? largeGrid : null);
                firstGrid = firstGrid != null ? firstGrid : (smallStaticGrid != null ? smallStaticGrid : null);
                firstGrid = firstGrid != null ? firstGrid : gridInGroup;

                group = MyCubeGridGroups.Static.Logical.GetGroup(firstGrid);

                CopyGridInternal(firstGrid);

                foreach (var node in group.Nodes)
                {
                    if (node.NodeData != firstGrid)
                        CopyGridInternal(node.NodeData);
                }
            }
            else
            {
                CopyGridInternal(gridInGroup);

                if (MyFakes.ENABLE_COPY_GROUP)
                {
                    var group = MyCubeGridGroups.Static.Logical.GetGroup(gridInGroup);
                    foreach (var node in group.Nodes)
                    {
                        if (node.NodeData != gridInGroup)
                            CopyGridInternal(node.NodeData);
                    }
                }
            }

            Activate();
        }

        public void CutGrid(MyCubeGrid grid)
        {
            if (grid == null)
                return;

            CopyGrid(grid);

            foreach (var block in grid.GetBlocks())
            {
                var cockpit = block.FatBlock as MyCockpit;
                if (cockpit != null && cockpit.Pilot != null)
                    cockpit.Use();
            }

            grid.SyncObject.SendCloseRequest();
            Deactivate();
        }

        public void CopyGrid(MyCubeGrid grid)
        {
            if (grid == null)
                return;
            m_copiedGrids.Clear();
            m_copiedGridOffsets.Clear();
            CopyGridInternal(grid);
            Activate();
        }

        public void CutGroup(MyCubeGrid grid)
        {
            if (grid == null)
                return;

            CopyGroup(grid);

            if (MyFakes.ENABLE_COPY_GROUP)
            {
                var group = MyCubeGridGroups.Static.Logical.GetGroup(grid);
                foreach (var node in group.Nodes)
                {
                    foreach (var block in node.NodeData.GetBlocks())
                    {
                        var cockpit = block.FatBlock as MyCockpit;
                        if (cockpit != null && cockpit.Pilot != null)
                            cockpit.Use();
                    }
                    node.NodeData.SyncObject.SendCloseRequest();
                }
            }
            else
            {
                foreach (var block in grid.GetBlocks())
                {
                    var cockpit = block.FatBlock as MyCockpit;
                    if (cockpit != null && cockpit.Pilot != null)
                        cockpit.Use();
                }
                grid.SyncObject.SendCloseRequest();
            }
            Deactivate();
        }

        private void CopyGridInternal(MyCubeGrid toCopy)
        {
            if (MySession.Static.CameraController.Equals(toCopy))
            {
                MySession.SetCameraController(MyCameraControllerEnum.Spectator, null, (Vector3D)toCopy.PositionComp.GetPosition());
            }
            m_copiedGrids.Add((MyObjectBuilder_CubeGrid)toCopy.GetObjectBuilder(true));
            RemovePilots(m_copiedGrids.Last());
            if (m_copiedGrids.Count == 1)
            {
                MatrixD pasteMatrix = GetPasteMatrix();

                Vector3I? draggedCube = toCopy.RayCastBlocks(pasteMatrix.Translation, pasteMatrix.Translation + pasteMatrix.Forward * 1000.0f);
                Vector3D dragPointGlobal = draggedCube.HasValue ? toCopy.GridIntegerToWorld(draggedCube.Value) : toCopy.WorldMatrix.Translation;

                m_dragPointToPositionLocal = Vector3D.TransformNormal(toCopy.PositionComp.GetPosition() - dragPointGlobal, toCopy.PositionComp.WorldMatrixNormalizedInv);
                m_dragDistance = (float)(dragPointGlobal - pasteMatrix.Translation).Length();

                m_pasteDirUp = toCopy.WorldMatrix.Up;
                m_pasteDirForward = toCopy.WorldMatrix.Forward;
                m_pasteOrientationAngle = 0.0f;
            }
            m_copiedGridOffsets.Add(toCopy.WorldMatrix.Translation - m_copiedGrids[0].PositionAndOrientation.Value.Position);
        }

        public virtual bool PasteGrid(IMyComponentInventory buildInventory = null, bool deactivate = true)
        {
            return PasteGridInternal(buildInventory, deactivate);
        }

        protected bool PasteGridInternal(IMyComponentInventory buildInventory, bool deactivate, List<MyObjectBuilder_CubeGrid> pastedBuilders = null, List<MyCubeGrid> touchingGrids = null,
            UpdateAfterPasteCallback updateAfterPasteCallback = null)
        {
            if (m_copiedGrids.Count == 0)
                return false;

            if ((m_copiedGrids.Count > 0) && !IsActive)
            {
                Activate();
                return true;
            }

            if (!CanBePlaced)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                return false;
            }

            if (m_previewGrids.Count == 0)
                return false;

            bool missingBlockDefinitions = !CheckPastedBlocks();

            if (missingBlockDefinitions)
            {
                AllowSwitchCameraMode = false;
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextDoYouWantToPasteGridWithMissingBlocks),
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionWarning),
                    callback: (result) =>
                    {
                        if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                        {
                            PasteInternal(buildInventory, missingBlockDefinitions, deactivate, pastedBuilders, updateAfterPasteCallback: updateAfterPasteCallback);
                        }
                        AllowSwitchCameraMode = true;
                    });
                MyGuiSandbox.AddScreen(messageBox);
                return false;
            }

            return PasteInternal(buildInventory, missingBlockDefinitions, deactivate, pastedBuilders: pastedBuilders, touchingGrids: touchingGrids, updateAfterPasteCallback: updateAfterPasteCallback);
        }

        private bool IsForcedDynamic()
        {
            bool forceDynamicGrid = EnableGridChangeToDynamic
                && !(m_hitEntity != null && ((m_hitEntity is MyVoxelMap) || ((m_hitEntity is MyCubeGrid) && ((MyCubeGrid)m_hitEntity).IsStatic)));
            return forceDynamicGrid;

        }

        private bool PasteInternal(IMyComponentInventory buildInventory, bool missingDefinitions, bool deactivate, List<MyObjectBuilder_CubeGrid> pastedBuilders = null, List<MyCubeGrid> touchingGrids = null,
            UpdateAfterPasteCallback updateAfterPasteCallback = null)
        {
            MyGuiAudio.PlaySound(MyGuiSounds.HudPlaceBlock);

            MyEntities.RemapObjectBuilderCollection(m_copiedGrids);

            m_tmpPastedBuilders.Clear();
            m_tmpPastedBuilders.Capacity = m_copiedGrids.Count;
            MyCubeGrid firstPastedGrid = null;

            bool forceDynamicGrid = IsForcedDynamic() && !m_gridChangeToDynamicDisabled;

            int i = 0;
            bool retVal = false;
            List<MyCubeGrid> pastedGrids = new List<MyCubeGrid>();

            foreach (var gridBuilder in m_copiedGrids)
               {
                gridBuilder.CreatePhysics = true;
                gridBuilder.EnableSmallToLargeConnections = true;
                bool savedStaticFlag = gridBuilder.IsStatic;

                if (forceDynamicGrid)
                {
                    gridBuilder.IsStatic = false;
                }

                var previousPos = gridBuilder.PositionAndOrientation;
                gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(m_previewGrids[i].WorldMatrix);

                var pastedGrid = MyEntities.CreateFromObjectBuilder(gridBuilder) as MyCubeGrid;

                if (pastedGrid == null)
                {
                    retVal = true;
                    continue;
                }

                if (MySession.Static.EnableStationVoxelSupport && pastedGrid.IsStatic)
                {
                    pastedGrid.TestDynamic = true;
                }

                //pastedGrid.PositionComp.SetPosition(MySector.MainCamera.Position);
                MyEntities.Add(pastedGrid);
                if (i == 0) firstPastedGrid = pastedGrid;

               
                if (missingDefinitions)
                    pastedGrid.DetectDisconnectsAfterFrame();

                //pastedGrid.PositionComp.SetWorldMatrix(m_previewGrids[i].WorldMatrix);
                i++;

                if (!pastedGrid.IsStatic && (!MyFakes.ENABLE_BATTLE_SYSTEM || !MySession.Static.Battle))
                    pastedGrid.Physics.LinearVelocity = m_objectVelocity;

                if (!pastedGrid.IsStatic && MySession.ControlledEntity != null && MySession.ControlledEntity.Entity.Physics != null && m_calculateVelocity
                    && (!MyFakes.ENABLE_BATTLE_SYSTEM || !MySession.Static.Battle))
                {
                    pastedGrid.Physics.AngularVelocity = MySession.ControlledEntity.Entity.Physics.AngularVelocity;
                }

                pastedGrids.Add(pastedGrid);

                gridBuilder.IsStatic = savedStaticFlag;

                retVal = true;
            }

            //Because blocks fills SubBlocks in this method..
            //TODO: Create LoadPhase2
            MyEntities.UpdateOnceBeforeFrame();

            foreach (var pastedGrid in pastedGrids)
            {
                var builder = pastedGrid.GetObjectBuilder();
                m_tmpPastedBuilders.Add(builder);

                if (pastedBuilders != null)
                    pastedBuilders.Add((MyObjectBuilder_CubeGrid)builder);
            }

            if (IsSnapped && SnapMode == MyGridPlacementSettings.SnapMode.Base6Directions && m_hitEntity is MyCubeGrid && firstPastedGrid != null && ((MyCubeGrid)m_hitEntity).GridSizeEnum == firstPastedGrid.GridSizeEnum)
            {
                var hitGrid = m_hitEntity as MyCubeGrid;

                MatrixI mergingTransform = hitGrid.CalculateMergeTransform(firstPastedGrid, hitGrid.WorldToGridInteger(firstPastedGrid.PositionComp.GetPosition()));
                MySyncCreate.RequestMergingCopyPaste(m_tmpPastedBuilders, m_hitEntity.EntityId, mergingTransform);
            }
            else if (touchingGrids != null && touchingGrids.Count > 0)
            {
                // Currently only first grid is supported for merging.
                MyCubeGrid touchingGrid = touchingGrids[0];

                if (touchingGrid != null)
                {
                    MatrixI mergingTransform = touchingGrid.CalculateMergeTransform(firstPastedGrid, touchingGrid.WorldToGridInteger(firstPastedGrid.PositionComp.GetPosition()));
                    MySyncCreate.RequestMergingCopyPaste(m_tmpPastedBuilders, touchingGrid.EntityId, mergingTransform);
                }
                else
                {
                    //MySyncCreate.RequestEntitiesCreate(m_tmpPastedBuilders);
                    MySyncCreate.SendEntitiesCreated(m_tmpPastedBuilders);
                }
            }
            else
            {
                // CH:TODO: This would probably be safer if it was requested from the server as well
                MySyncCreate.SendEntitiesCreated(m_tmpPastedBuilders);
            }

            // CH:TODO: Use only items for grids that were really added to not screw with players
            if (buildInventory != null)
            {
                foreach (var item in m_buildComponents.TotalMaterials)
                {
                    buildInventory.RemoveItemsOfType(item.Value, item.Key);
                }
            }

            if (deactivate)
                Deactivate();

            if (retVal && updateAfterPasteCallback != null)
            {
                updateAfterPasteCallback(pastedBuilders);
            }

            return retVal;
        }

        /// <summary>
        /// Checks the pasted object builder for non-existent blocks (e.g. copying from world with a cube block mod to a world without it)
        /// </summary>
        /// <returns>True when the grid can be pasted</returns>
        protected bool CheckPastedBlocks()
        {
            MyCubeBlockDefinition cbDef;
            foreach (var gridBuilder in m_copiedGrids)
            {
                foreach (var block in gridBuilder.CubeBlocks)
                {
                    MyDefinitionId id = new MyDefinitionId(block.TypeId, block.SubtypeId);
                    if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(id, out cbDef) == false)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void SetGridFromBuilder(MyObjectBuilder_CubeGrid grid, Vector3 dragPointDelta, float dragVectorLength)
        {
            if (IsActive)
            {
                Deactivate();
            }

            m_copiedGrids.Clear();
            m_copiedGridOffsets.Clear();

            MatrixD pasteMatrix = GetPasteMatrix();
            m_dragPointToPositionLocal = dragPointDelta;
            m_dragDistance = dragVectorLength;
            var transform = grid.PositionAndOrientation ?? MyPositionAndOrientation.Default;
            m_pasteDirUp = transform.Up;
            m_pasteDirForward = transform.Forward;

            SetGridFromBuilderInternal(grid, Vector3.Zero);

            Activate();
        }

        public void SetGridFromBuilders(MyObjectBuilder_CubeGrid[] grids, Vector3 dragPointDelta, float dragVectorLength)
        {
            if (IsActive)
            {
                Deactivate();
            }

            m_copiedGrids.Clear();
            m_copiedGridOffsets.Clear();

            if (grids.Count() == 0) return;

            MatrixD pasteMatrix = GetPasteMatrix();
            m_dragPointToPositionLocal = dragPointDelta;
            m_dragDistance = dragVectorLength;
            var transform = grids[0].PositionAndOrientation ?? MyPositionAndOrientation.Default;
            m_pasteDirUp = transform.Up;
            m_pasteDirForward = transform.Forward;

            SetGridFromBuilderInternal(grids[0], Vector3.Zero);

            MatrixD invMatrix = grids[0].PositionAndOrientation.HasValue ? grids[0].PositionAndOrientation.Value.GetMatrix() : MatrixD.Identity;
            invMatrix = MatrixD.Invert(invMatrix);
            for (int i = 1; i < grids.Count(); ++i)
            {
                Vector3D offset = grids[i].PositionAndOrientation.HasValue ? (Vector3D)grids[i].PositionAndOrientation.Value.Position : Vector3D.Zero;
                offset = Vector3D.Transform(offset, invMatrix);
                SetGridFromBuilderInternal(grids[i], offset);
            }

            Activate();
        }

        private void SetGridFromBuilderInternal(MyObjectBuilder_CubeGrid grid, Vector3 offset)
        {
            Debug.Assert(grid.CubeBlocks.Count() > 0, "The grid does not contain any blocks");

            foreach (var block in grid.CubeBlocks)
            {
                var defId = block.GetId();
                MyCubeBlockDefinition blockDef = null;
                MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out blockDef);
                if (blockDef == null) continue;
                
                MyCubeBuilder.BuildComponent.BeforeCreateBlock(blockDef, GetClipboardBuilder(), block);
            }

            m_copiedGrids.Add(grid);
            m_copiedGridOffsets.Add(offset);
            RemovePilots(grid);
        }

        private void ChangeClipboardPreview(bool visible)
        {
            if (m_copiedGrids.Count == 0 || !visible)
            {
                foreach(var grid in m_previewGrids)
                {
                    MyEntities.EnableEntityBoundingBoxDraw(grid, false);
                    grid.Close();
                }
                m_previewGrids.Clear();
                m_visible = false;
                m_buildComponents.Clear();
                return;
            }

            CalculateItemRequirements();

            MyEntities.RemapObjectBuilderCollection(m_copiedGrids);

            foreach (var gridBuilder in m_copiedGrids)
            {
                bool savedIsStatic = gridBuilder.IsStatic;
                if (m_useDynamicPreviews)
                    gridBuilder.IsStatic = false;

                gridBuilder.CreatePhysics = false;
                gridBuilder.EnableSmallToLargeConnections = false;
                var previewGrid = MyEntities.CreateFromObjectBuilder(gridBuilder) as MyCubeGrid;

                gridBuilder.IsStatic = savedIsStatic;

                if (previewGrid == null)
                {
                    ChangeClipboardPreview(false);
                    return;// Not enough memory to create preview grid or there was some error.
                }

                if(previewGrid.CubeBlocks.Count == 0)
                {
                    m_copiedGrids.Remove(gridBuilder);
                    ChangeClipboardPreview(false);
                    return;
                }

                //reset position from prefab (it can be outside the world)
                previewGrid.PositionComp.SetPosition(MySector.MainCamera.Position);
                MakeTransparent(previewGrid);
                IsActive = visible;
                m_visible = visible;
                MyEntities.Add(previewGrid);

                previewGrid.Save = false;
                DisablePhysicsRecursively(previewGrid);
                m_previewGrids.Add(previewGrid);
            }
        }

        private void CalculateItemRequirements()
        {
            m_buildComponents.Clear();
            foreach (var grid in m_copiedGrids)
            {
                foreach (var block in grid.CubeBlocks)
                {
                    var compound = block as MyObjectBuilder_CompoundCubeBlock;
                    if (compound != null)
                    {
                        foreach (var subblock in compound.Blocks)
                        {
                            AddSingleBlockRequirements(subblock);
                        }
                    }
                    else
                    {
                        AddSingleBlockRequirements(block);
                    }
                }
            }
        }

        private void AddSingleBlockRequirements(MyObjectBuilder_CubeBlock block)
        {
            MyComponentStack.GetMountedComponents(m_buildComponents, block);
            if (block.ConstructionStockpile != null)
                foreach (var item in block.ConstructionStockpile.Items)
                {
                    m_buildComponents.AddMaterial(item.PhysicalContent.GetId(), item.Amount, addToDisplayList: false);
                }
        }

        protected virtual float Transparency
        {
            get
            {
                return MyGridConstants.BUILDER_TRANSPARENCY;
            }
        }

        private void MakeTransparent(MyCubeGrid grid)
        {
            grid.Render.Transparency = Transparency;

            grid.Hierarchy.GetChildrenRecursive(m_cacheEntitySet);
            foreach (var child in m_cacheEntitySet)
                child.Render.Transparency = Transparency;
            m_cacheEntitySet.Clear();
        }

        private void DisablePhysicsRecursively(MyEntity entity)
        {
            if (entity.Physics != null && entity.Physics.Enabled)
                entity.Physics.Enabled = false;

            var block = entity as MyCubeBlock;
            if (block != null && block.UseObjectsComponent.DetectorPhysics != null && block.UseObjectsComponent.DetectorPhysics.Enabled)
                block.UseObjectsComponent.DetectorPhysics.Enabled = false;

            if (block != null)
                block.NeedsUpdate = MyEntityUpdateEnum.NONE;

            foreach (var child in entity.Hierarchy.Children)
                DisablePhysicsRecursively(child.Container.Entity as MyEntity);
        }

        public virtual void Update()
        {
            if (!IsActive || !m_visible)
                return;

            UpdateHitEntity();
            UpdatePastePosition();
            UpdateGridTransformations();

            if (IsSnapped && SnapMode == MyGridPlacementSettings.SnapMode.Base6Directions)
            {
                FixSnapTransformationBase6();
            }

            if (m_calculateVelocity)
                m_objectVelocity = (m_pastePosition - m_pastePositionPrevious) / MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            m_canBePlaced = TestPlacement();

            TestBuildingMaterials();
            UpdatePreviewBBox();

            if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "FW: " + m_pasteDirForward.ToString(), Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 20.0f), "UP: " + m_pasteDirUp.ToString(), Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 40.0f), "AN: " + m_pasteOrientationAngle.ToString(), Color.Red, 1.0f);
            }
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
                if ((entity is MyVoxelMap) || (entity is MyCubeGrid && entity.EntityId != m_previewGrids[0].EntityId))
                {
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

        protected virtual void TestBuildingMaterials()
        {
            m_characterHasEnoughMaterials = EntityCanPaste(GetClipboardBuilder());
        }

        protected virtual MyEntity GetClipboardBuilder()
        {
            return MySession.LocalCharacter;
        }

        public bool EntityCanPaste(MyEntity pastingEntity)
        {
            if (m_copiedGrids.Count < 1) return false;
            MyCubeBuilder.BuildComponent.GetGridSpawnMaterials(m_copiedGrids[0]);
            return MyCubeBuilder.BuildComponent.HasBuildingMaterials(pastingEntity);
        }

        protected virtual bool TestPlacement()
        {
            bool forceDynamicGrid = IsForcedDynamic();
            m_gridChangeToDynamicDisabled = false;

            bool retval = true;
            for (int i = 0; i < m_previewGrids.Count; ++i)
            {
                var grid = m_previewGrids[i];
                if (i == 0 && m_hitEntity is MyCubeGrid && IsSnapped && SnapMode == MyGridPlacementSettings.SnapMode.Base6Directions)
                {
                    var hitGrid = m_hitEntity as MyCubeGrid;

                    bool smallOnLargeGrid = hitGrid.GridSizeEnum == MyCubeSize.Large && grid.GridSizeEnum == MyCubeSize.Small;
                    var settings = m_settings.GetGridPlacementSettings(grid, forceDynamicGrid ? false : grid.IsStatic);

                    // To medieval guys from Cestmir: This made me sweat a bit during the latest update. Please consult it with me if you want to uncomment :-)
                    if (MyFakes.ENABLE_STATIC_SMALL_GRID_ON_LARGE && grid.IsStatic && smallOnLargeGrid)
                    {
                        retval &= MyCubeGrid.TestPlacementArea(grid, forceDynamicGrid ? false : grid.IsStatic, ref settings, (BoundingBoxD)grid.PositionComp.LocalAABB, false, hitGrid);
                    }
                    else
                    {
                        Vector3I gridOffset = hitGrid.WorldToGridInteger(m_pastePosition);
                        if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
                            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 60.0f), "First grid offset: " + gridOffset.ToString(), Color.Red, 1.0f);
                        retval &= hitGrid.GridSizeEnum == grid.GridSizeEnum && hitGrid.CanMergeCubes(grid, gridOffset);
                        retval &= MyCubeGrid.CheckMergeConnectivity(hitGrid, grid, gridOffset);

                        retval &= MyCubeGrid.TestPlacementArea(grid, forceDynamicGrid ? false : grid.IsStatic, ref settings, (BoundingBoxD)grid.PositionComp.LocalAABB, false, hitGrid);
                    }
                }
                else
                {
                    var settings = m_settings.GetGridPlacementSettings(grid, forceDynamicGrid ? false : grid.IsStatic);
                    retval &= MyCubeGrid.TestPlacementAreaWithEntities(grid, forceDynamicGrid ? false : grid.IsStatic, ref settings, (BoundingBoxD)grid.PositionComp.LocalAABB, false);
                }

                if (grid.IsStatic && forceDynamicGrid)
                {
                    if (!retval)
                    {
                        m_gridChangeToDynamicDisabled = true;
                        retval = true;
                    }
                }

                if (!retval)
                    return false;
            }

            return retval;
        }

        protected virtual void UpdateGridTransformations()
        {
            Matrix originalOrientation = GetFirstGridOrientationMatrix();
            var invRotation = Matrix.Invert(m_copiedGrids[0].PositionAndOrientation.Value.GetMatrix()).GetOrientation();
            Matrix orientationDelta = invRotation * originalOrientation; // matrix from original orientation to new orientation

            for (int i = 0; i < m_previewGrids.Count; i++)
            {
                MatrixD worldMatrix2 = m_copiedGrids[i].PositionAndOrientation.Value.GetMatrix(); //get original rotation and position
                var offset = worldMatrix2.Translation - m_copiedGrids[0].PositionAndOrientation.Value.Position; //calculate offset to first pasted grid
                m_copiedGridOffsets[i] = Vector3.TransformNormal(offset, orientationDelta); // Transform the offset to new orientation
                if (!AnyCopiedGridIsStatic)
                    worldMatrix2 = worldMatrix2 * orientationDelta; //correct rotation
                Vector3D translation = m_pastePosition + m_copiedGridOffsets[i]; //correct position

                worldMatrix2.Translation = Vector3.Zero;
                worldMatrix2 = MatrixD.Orthogonalize(worldMatrix2);
                worldMatrix2.Translation = translation;

                m_previewGrids[i].PositionComp.SetWorldMatrix(worldMatrix2);// Set the corrected position
            }
        }

        protected virtual void UpdatePastePosition()
        {
            m_pastePositionPrevious = m_pastePosition;

            // Current position of the placed entity is either simple translation or
            // it can be calculated by raycast, if we want to snap to surfaces
            MatrixD pasteMatrix = GetPasteMatrix();
            Vector3 dragVectorGlobal = pasteMatrix.Forward * m_dragDistance;

            var gridSettings = m_settings.GetGridPlacementSettings(m_previewGrids[0]);
            if (!TrySnapToSurface(gridSettings.Mode))
            {
                m_pastePosition = pasteMatrix.Translation + dragVectorGlobal;
                Matrix firstGridOrientation = GetFirstGridOrientationMatrix();
                m_pastePosition += Vector3.TransformNormal(m_dragPointToPositionLocal, firstGridOrientation);
            }

            if (AnyCopiedGridIsStatic)
            {
                double gridSize = m_previewGrids[0].GridSize;
                if (m_settings.StaticGridAlignToCenter)
                {
                    m_pastePosition = Vector3I.Round(m_pastePosition / gridSize) * gridSize;
                }
                else
                {
                    m_pastePosition = Vector3I.Round(m_pastePosition / gridSize + 0.5) * gridSize - 0.5 * gridSize;
                }

                m_pasteDirForward = Vector3.Forward;
                m_pasteDirUp = Vector3.Up;
                m_pasteOrientationAngle = 0.0f;
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
            {
                MyRenderProxy.DebugDrawSphere(pasteMatrix.Translation + dragVectorGlobal, 0.15f, Color.Pink.ToVector3(), 1.0f, false);
                MyRenderProxy.DebugDrawSphere(m_pastePosition, 0.15f, Color.Pink.ToVector3(), 1.0f, false);
            }
        }

        protected static MatrixD GetPasteMatrix()
        {
            if (MySession.ControlledEntity != null &&
                (MySession.GetCameraControllerEnum() == MyCameraControllerEnum.Entity || MySession.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator))
            {
                return MySession.ControlledEntity.GetHeadMatrix(true);
            }
            else
            {
                return MySector.MainCamera.WorldMatrix;
            }
        }

        public virtual Matrix GetFirstGridOrientationMatrix()
        {
            return Matrix.CreateWorld(Vector3.Zero, m_pasteDirForward, m_pasteDirUp) * Matrix.CreateFromAxisAngle(m_pasteDirUp, m_pasteOrientationAngle);
        }

        protected bool TrySnapToSurface(MyGridPlacementSettings.SnapMode snapMode)
        {
            if (m_closestHitDistSq < float.MaxValue)
            {
                Vector3 newDragPointPosition = m_hitPos;

                bool isAnyStatic = AnyCopiedGridIsStatic;
                if (isAnyStatic)
                {
                    m_pasteDirForward = Vector3.Forward;
                    m_pasteDirUp = Vector3.Up;
                }
                else if (m_hitNormal.Length() > 0.5)
                {
                    if (snapMode == MyGridPlacementSettings.SnapMode.OneFreeAxis)
                    {
                        m_pasteDirUp = m_hitNormal;

                        float dotUp = m_pasteDirUp.Dot(m_hitEntity.WorldMatrix.Up);
                        float dotFwd = m_pasteDirUp.Dot(m_hitEntity.WorldMatrix.Forward);
                        float dotRt = m_pasteDirUp.Dot(m_hitEntity.WorldMatrix.Right);
                        if (Math.Abs(dotUp) > Math.Abs(dotFwd))
                        {
                            if (Math.Abs(dotUp) > Math.Abs(dotRt))
                            {
                                m_pasteDirForward = Vector3.Normalize(m_hitEntity.WorldMatrix.Right - (dotRt * m_pasteDirUp));
                            }
                            else
                            {
                                m_pasteDirForward = Vector3.Normalize(m_hitEntity.WorldMatrix.Forward - (dotFwd * m_pasteDirUp));
                            }
                        }
                        else
                        {
                            if (Math.Abs(dotFwd) > Math.Abs(dotRt))
                            {
                                m_pasteDirForward = Vector3.Normalize(m_hitEntity.WorldMatrix.Up - (dotUp * m_pasteDirUp));
                            }
                            else
                            {
                                m_pasteDirForward = Vector3.Normalize(m_hitEntity.WorldMatrix.Forward - (dotFwd * m_pasteDirUp));
                            }
                        }
                    }
                    else if (snapMode == MyGridPlacementSettings.SnapMode.Base6Directions)
                    {
                        var hitGrid = m_hitEntity as MyCubeGrid;
                        if (hitGrid != null)
                        {
                            Matrix hitGridRotation = hitGrid.WorldMatrix.GetOrientation();
                            Matrix firstRotation = GetFirstGridOrientationMatrix();
                            Matrix newFirstRotation = Matrix.AlignRotationToAxes(ref firstRotation, ref hitGridRotation);
                            Matrix rotationDelta = Matrix.Invert(firstRotation) * newFirstRotation;

                            m_pasteDirForward = newFirstRotation.Forward;
                            m_pasteDirUp = newFirstRotation.Up;
                            m_pasteOrientationAngle = 0.0f;
                        }
                    }
                }

                Matrix newOrientation = GetFirstGridOrientationMatrix();
                Vector3 globalCenterDelta = Vector3.TransformNormal(m_dragPointToPositionLocal, newOrientation);

                m_pastePosition = newDragPointPosition + globalCenterDelta;

                if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
                {
                    MyRenderProxy.DebugDrawSphere(newDragPointPosition, 0.08f, Color.Red.ToVector3(), 1.0f, false);
                    MyRenderProxy.DebugDrawSphere(m_pastePosition, 0.08f, Color.Red.ToVector3(), 1.0f, false);
                }

                IsSnapped = true;
                return true;
            }
            IsSnapped = false;
            return false;
        }

        private void UpdatePreviewBBox()
        {
            if (m_previewGrids == null)
                return;

            if (m_visible == false || HasPreviewBBox == false)
            {
                foreach(var grid in m_previewGrids)
                    MyEntities.EnableEntityBoundingBoxDraw(grid, false);
                return;
            }

            //Vector4 color = new Vector4(Color.Red.ToVector3() * 0.8f, 1);
            Vector4 color = new Vector4(Color.White.ToVector3(), 1);
            string lineMaterial = "GizmoDrawLineRed";
            if (CanBePlaced)
            {
                if (m_characterHasEnoughMaterials)
                {
                    lineMaterial = "GizmoDrawLine";
                }
                else
                    color = Color.Gray.ToVector4();
            }

            // Draw a little inflated bounding box
            var inflation = new Vector3(0.1f);
            foreach(var grid in m_previewGrids)
                MyEntities.EnableEntityBoundingBoxDraw(grid, true, color, lineWidth: 0.04f, inflateAmount: inflation, lineMaterial: lineMaterial);
        }

        /// <summary>
        /// Converts hit coordinates to large grid coordinates but for small cubes. Allows placement of small grids to large grids.
        /// Returns coordinates of small grid (in large grid coordinates) which touches large grid in the hit position.
        /// </summary>
        protected static Vector3 TransformLargeGridHitCoordToSmallGrid(Vector3 coords, Matrix worldMatrixNormalizedInv, float gridSize)
        {
            Vector3 localCoords = Vector3.Transform(coords, worldMatrixNormalizedInv);
            localCoords /= gridSize;
            // We have 10 small cubes in large one.
            localCoords *= 10f;
            Vector3I sign = Vector3I.Sign(localCoords);
            // Center of small cube has offset 0.05
            localCoords -= 0.5f * sign;
            localCoords = sign * Vector3I.Round(Vector3.Abs(localCoords));
            localCoords += 0.5f * sign;
            localCoords /= 10f;
            return localCoords;
        }

        protected void FixSnapTransformationBase6()
        {
            Debug.Assert(m_copiedGrids.Count != 0);
            if (m_copiedGrids.Count == 0)
                return;

            var pasteMatrix = GetPasteMatrix();
            var hitGrid = m_hitEntity as MyCubeGrid;

            if (hitGrid == null)
                return;
            
            // Fix rotation of the first pasted grid
            Matrix hitGridRotation = hitGrid.WorldMatrix.GetOrientation();
            Matrix firstRotation = m_previewGrids[0].WorldMatrix.GetOrientation();
            Matrix newFirstRotation = Matrix.AlignRotationToAxes(ref firstRotation, ref hitGridRotation);
            Matrix rotationDelta = Matrix.Invert(firstRotation) * newFirstRotation;

            // Fix transformations of all the pasted grids
            int gridIndex = 0;
            foreach (var grid in m_previewGrids)
            {
                Matrix rotation = grid.WorldMatrix.GetOrientation();
                rotation = rotation * rotationDelta;
                Matrix rotationInv = Matrix.Invert(rotation);

                Vector3D position = m_pastePosition;

                MatrixD newWorld = MatrixD.CreateWorld(position, rotation.Forward, rotation.Up);
                Debug.Assert(newWorld.GetOrientation().IsRotation());
                grid.PositionComp.SetWorldMatrix(newWorld);
            }


            bool smallOnLargeGrid = hitGrid.GridSizeEnum == MyCubeSize.Large && m_previewGrids[0].GridSizeEnum == MyCubeSize.Small;

            if (smallOnLargeGrid)
            {
                Vector3 pasteOffset = TransformLargeGridHitCoordToSmallGrid(m_pastePosition, hitGrid.PositionComp.WorldMatrixNormalizedInv, hitGrid.GridSize);
                m_pastePosition = hitGrid.GridIntegerToWorld(pasteOffset);
            }
            else
            {
                // Find a collision-free position for the first paste grid along the raycast normal
                Vector3I collisionTestStep = Vector3I.Round(Vector3.TransformNormal(m_hitNormal, hitGrid.PositionComp.WorldMatrixNormalizedInv));
                Vector3I pasteOffset = hitGrid.WorldToGridInteger(m_pastePosition);

                int i;
                for (i = 0; i < 100; ++i) // CH:TODO: Fix the step limit
                {
                    if (hitGrid.CanMergeCubes(m_previewGrids[0], pasteOffset))
                        break;
                    pasteOffset += collisionTestStep;
                }

                if (i == 0)
                {
                    for (i = 0; i < 100; ++i) // CH:TODO: Fix the step limit
                    {
                        pasteOffset -= collisionTestStep;
                        if (!hitGrid.CanMergeCubes(m_previewGrids[0], pasteOffset))
                            break;
                    }
                    pasteOffset += collisionTestStep;
                }

                if (i == 100)
                {
                    pasteOffset = hitGrid.WorldToGridInteger(m_pastePosition);
                }

                m_pastePosition = hitGrid.GridIntegerToWorld(pasteOffset);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
                MyRenderProxy.DebugDrawLine3D(m_hitPos, m_hitPos + m_hitNormal, Color.Red, Color.Green, false);

            // Move all the grids according to the collision-free position of the first one
            gridIndex = 0;
            foreach (var grid in m_previewGrids)
            {
                MatrixD matrix = grid.WorldMatrix;
                matrix.Translation = m_pastePosition + Vector3.Transform(m_copiedGridOffsets[gridIndex++], rotationDelta);
                grid.PositionComp.SetWorldMatrix(matrix);
            }
        }

        public void DrawHud()
        {
            if (m_previewGrids == null) return;

            MyCubeBlockDefinition firstBlockDefinition = GetFirstBlockDefinition(m_copiedGrids[0]);
            MyHud.BlockInfo.LoadDefinition(firstBlockDefinition);
            MyHud.BlockInfo.Visible = true;
        }

        public void CalculateRotationHints(MyBlockBuilderRotationHints hints, bool isRotating)
        {
            MyEntity entity = PreviewGrids.Count > 0 ? PreviewGrids[0] : null;
            if (entity != null)
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                if (grid != null && (!grid.IsStatic || EnableStationRotation))
                {
                    Vector3I gridSize = grid.Max - grid.Min + new Vector3I(1, 1, 1);
                    BoundingBoxD worldBox = new BoundingBoxD(-gridSize * grid.GridSize * 0.5f, gridSize * grid.GridSize * 0.5f);

                    MatrixD mat = entity.WorldMatrix;
                    Vector3D positionToDragPointGlobal = Vector3D.TransformNormal(-m_dragPointToPositionLocal, mat);
                    mat.Translation = mat.Translation + positionToDragPointGlobal;

                    hints.CalculateRotationHints(mat, worldBox, !MyHud.MinimalHud && MySandboxGame.Config.RotationHints && MyFakes.ENABLE_ROTATION_HINTS, isRotating, OneAxisRotationMode);
                }
            }
        }

        private void RemovePilots(MyObjectBuilder_CubeGrid grid)
        {
            foreach (var block in grid.CubeBlocks)
            {
                MyObjectBuilder_Cockpit cockpit = block as MyObjectBuilder_Cockpit;
                if (cockpit != null)
                    cockpit.ClearPilotAndAutopilot();
            }
        }

        public bool HasCopiedGrids()
        {
            return m_copiedGrids.Count > 0;
        }

        public string CopiedGridsName
        {
            get
            {
                if (HasCopiedGrids())
                {
                    return m_copiedGrids[0].DisplayName;
                }

                return null;
            }
        }

        public void SaveClipboardAsPrefab(string name = null, string path = null)
        {
            if (m_copiedGrids.Count == 0)
            {
                return;
            }

            name = name ?? MyWorldGenerator.GetPrefabTypeName(m_copiedGrids[0]) + "_" + MyUtils.GetRandomInt(1000000, 9999999);
            if (path == null)
            {
                MyPrefabManager.SavePrefab(name, m_copiedGrids);
            }
            else
            {
                MyPrefabManager.SavePrefabToPath(name, path, m_copiedGrids);
            }
            MyHud.Notifications.Add(new MyHudNotificationDebug("Prefab saved: " + path ?? name, 10000));
        }

        public void HideWhenColliding(List<Vector3> m_collisionTestPoints)
        {
            if (m_previewGrids.Count == 0) return;
            bool visible = true;
            foreach (var point in m_collisionTestPoints)
            {
                foreach (var grid in m_previewGrids)
                {
                    var localPoint = Vector3.Transform(point, grid.PositionComp.WorldMatrixNormalizedInv);
                    if (grid.PositionComp.LocalAABB.Contains(localPoint) == ContainmentType.Contains)
                    {
                        visible = false;
                        break;
                    }
                }
                if (!visible)
                    break;
            }
            foreach (var grid in m_previewGrids)
                grid.Render.Visible = visible;
        }

        #region Pasting transform control
        public void RotateAroundAxis(int axisIndex, int sign, bool newlyPressed, float angleDelta)
        {
            if (IsSnapped && SnapMode == MyGridPlacementSettings.SnapMode.Base6Directions && EnablePreciseRotationWhenSnapped == false)
            {
                if (!newlyPressed) return;

                angleDelta = MathHelper.PiOver2;
            }

            switch (axisIndex)
            {
                case 0:
                    if (sign < 0)
                        UpMinus(angleDelta);
                    else
                        UpPlus(angleDelta);
                    break;

                case 1:
                    if (sign < 0)
                        AngleMinus(angleDelta);
                    else
                        AnglePlus(angleDelta);
                    break;

                case 2:
                    if (sign < 0)
                        RightPlus(angleDelta);
                    else
                        RightMinus(angleDelta);
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }

            if (IsSnapped && SnapMode == MyGridPlacementSettings.SnapMode.Base6Directions)
            {
                ApplyOrientationAngle();
            }
        }

        private void AnglePlus(float angle)
        {
            if (AnyCopiedGridIsStatic) return;
            m_pasteOrientationAngle += angle;
            if (m_pasteOrientationAngle >= (float)Math.PI * 2.0f)
            {
                m_pasteOrientationAngle -= (float)Math.PI * 2.0f;
            }
        }

        private void AngleMinus(float angle)
        {
            if (AnyCopiedGridIsStatic) return;
            m_pasteOrientationAngle -= angle;
            if (m_pasteOrientationAngle < 0.0f)
            {
                m_pasteOrientationAngle += (float)Math.PI * 2.0f;
            }
        }

        private void UpPlus(float angle)
        {
            if (AnyCopiedGridIsStatic || OneAxisRotationMode) return;

            ApplyOrientationAngle();
            Vector3 right = Vector3.Cross(m_pasteDirForward, m_pasteDirUp);
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            Vector3 up = m_pasteDirUp * cos - m_pasteDirForward * sin;
            m_pasteDirForward = m_pasteDirUp * sin + m_pasteDirForward * cos;
            m_pasteDirUp = up;
        }

        private void UpMinus(float angle)
        {
            UpPlus(-angle);
        }

        private void RightPlus(float angle)
        {
            if (AnyCopiedGridIsStatic || OneAxisRotationMode) return;

            ApplyOrientationAngle();
            Vector3 right = Vector3.Cross(m_pasteDirForward, m_pasteDirUp);
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            m_pasteDirUp = m_pasteDirUp * cos + right * sin;
        }

        private void RightMinus(float angle)
        {
            RightPlus(-angle);
        }

        public virtual void MoveEntityFurther()
        {
            m_dragDistance *= 1.1f;
        }

        public virtual void MoveEntityCloser()
        {
            m_dragDistance /= 1.1f;
        }

        private void ApplyOrientationAngle()
        {
            m_pasteDirForward = Vector3.Normalize(m_pasteDirForward);
            m_pasteDirUp = Vector3.Normalize(m_pasteDirUp);

            Vector3 right = Vector3.Cross(m_pasteDirForward, m_pasteDirUp);
            float cos = (float)Math.Cos(m_pasteOrientationAngle);
            float sin = (float)Math.Sin(m_pasteOrientationAngle);
            m_pasteDirForward = m_pasteDirForward * cos - right * sin;
            m_pasteOrientationAngle = 0.0f;
        }

        #endregion

    }
}
