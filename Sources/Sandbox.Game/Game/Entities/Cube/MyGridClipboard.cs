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
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.GameSystems.CoordinateSystem;
using VRage.Audio;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Components;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;

namespace Sandbox.Game.Entities.Cube
{

    public class MyGridClipboard
    {
        struct GridCopy
        {
            MyObjectBuilder_CubeGrid Grid;
            Vector3 Offset;
            Quaternion Rotation;
        }

        [ThreadStatic]
        private static HashSet<IMyEntity> m_cacheEntitySet = new HashSet<IMyEntity>();

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
		protected const float m_maxDragDistance = 2E4f;
        protected Vector3 m_dragPointToPositionLocal;

        // Placement flags
        protected bool m_canBePlaced;
        protected virtual bool CanBePlaced
        {
            get
            {
                if (m_canBePlacedNeedsRefresh)
                    m_canBePlaced = TestPlacement();
                return m_canBePlaced;
            }
        }

        bool m_canBePlacedNeedsRefresh=true;//collision is only done once per X frames and therefore have to be done in the frame when we are pasting
        protected bool m_characterHasEnoughMaterials = false;
        public bool CharacterHasEnoughMaterials { get { return m_characterHasEnoughMaterials; } }

        protected MyPlacementSettings m_settings;

        // Raycasting
        private List<MyPhysics.HitInfo> m_raycastCollisionResults = new List<MyPhysics.HitInfo>();
        protected float m_closestHitDistSq = float.MaxValue;
        protected Vector3D m_hitPos = new Vector3(0.0f, 0.0f, 0.0f);
        protected Vector3 m_hitNormal = new Vector3(1.0f, 0.0f, 0.0f);
        protected IMyEntity m_hitEntity = null;

        protected bool m_visible = true;
        private bool m_allowSwitchCameraMode = true;

        protected bool m_useDynamicPreviews = false;

        protected Dictionary<string, int> m_blocksPerType = new Dictionary<string,int>();

        /// <summary>
        /// Grids that are around pasted grid. (In proximity, possible for merge)
        /// </summary>
        protected List<MyCubeGrid> m_touchingGrids = new List<MyCubeGrid>();

        private ParallelTasks.Task ActivationTask;
        private List<IMyEntity> m_resultIDs = new List<IMyEntity>();
        private bool m_isBeingAdded;

        protected delegate void UpdateAfterPasteCallback(List<MyObjectBuilder_CubeGrid> pastedBuilders);

        public event Action<MyGridClipboard, bool> Deactivated;

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
            protected set;
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


        public SnapMode SnapMode
        {
            get
            {
                if (m_previewGrids.Count == 0)
                    return SnapMode.Base6Directions;

                var gridSettings = m_settings.GetGridPlacementSettings(m_previewGrids[0].GridSizeEnum);
                return gridSettings.SnapMode;
            }
        }

        public bool EnablePreciseRotationWhenSnapped
        {
            get
            {
                if (m_previewGrids.Count == 0)
                    return false;

                var gridSettings = m_settings.GetGridPlacementSettings(m_previewGrids[0].GridSizeEnum);
                return gridSettings.EnablePreciseRotationWhenSnapped && EnableStationRotation;
            }
        }

        public bool OneAxisRotationMode
        {
            get
            {
                return IsSnapped && SnapMode == SnapMode.OneFreeAxis;
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
                //if (EnableStationRotation == true)
                //{
                //    return false;
                //}

                foreach (var grid in m_previewGrids)
                {
                    if (grid.IsStatic)
                        return true;
                }
                return false;
            }
        }

        bool m_enableStationRotation = false;
        public bool EnableStationRotation
        {
            get
            {
                return m_enableStationRotation && MyFakes.ENABLE_STATION_ROTATION;
            }

            set
            {

                if (m_enableStationRotation == value)
                    return;

                m_enableStationRotation = value;
                if (IsActive && m_enableStationRotation)
                {
                    AlignClipboardToGravity();
                    MyCoordinateSystem.Static.Visible = false;
                }
                else if (IsActive && !m_enableStationRotation)
                {
                    this.AlignRotationToCoordSys();
                    MyCoordinateSystem.Static.Visible = true;

                }
            }
        }

        public bool CreationMode
        {
            get;
            set;
        }

        public MyCubeSize CubeSize
        {
            get;
            set;
        }

        public bool IsStatic
        {
            get;
            set;
        }
        public bool ShowModdedBlocksWarning = true;

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

            if (grid.CubeBlocks.Count > 0)
            {
                MyDefinitionId firstBlock = grid.CubeBlocks[0].GetId();
                return MyDefinitionManager.Static.GetCubeBlockDefinition(firstBlock);
            }
            return null;
        }

        public virtual void ActivateNoAlign(Action callback = null)
        {
            if (!ActivationTask.IsComplete || m_isBeingAdded)
                return;

            m_isBeingAdded = true;
            ActivationTask = ParallelTasks.Parallel.Start(delegate() { ChangeClipboardPreview(true); }, delegate()
            {
                if (m_visible)
                {
                    foreach (var grid in m_previewGrids)
                    {
                        MyEntities.Add(grid);
                        DisablePhysicsRecursively(grid);
                    }
                    if (callback != null)
                        callback();

                    IsActive = true;
                }
                m_isBeingAdded = false;
            });
        }

        public virtual void Activate(Action callback = null)
        {
            if (!ActivationTask.IsComplete || m_isBeingAdded)
                return;
            MyHud.PushRotatingWheelVisible();
            m_isBeingAdded = true;
            ActivationTask = ParallelTasks.Parallel.Start(ActivateInternal, delegate()
            {
                if (m_visible)
                {
                    foreach (var entity in m_resultIDs)
                    {
                        VRage.ModAPI.IMyEntity foundEntity;
                        MyEntityIdentifier.TryGetEntity(entity.EntityId, out foundEntity);
                        if (foundEntity == null)
                            MyEntityIdentifier.AddEntityWithId(entity);
                        else
                            Debug.Fail("Two threads added the same entity");
                    }
                    m_resultIDs.Clear();
                    foreach (var grid in m_previewGrids)
                    {
                        MyEntities.Add(grid);
                        DisablePhysicsRecursively(grid);
                    }
                    if (callback != null)
                        callback();

                    IsActive = true;
                }
                m_isBeingAdded = false;
                MyHud.PopRotatingWheelVisible();
            });
        }

        private void ActivateInternal()
        {
            ChangeClipboardPreview(true);

            if (EnableStationRotation)
            {
                AlignClipboardToGravity();
            }

            if(!EnableStationRotation)
            {
                MyCoordinateSystem.Static.Visible = true;
                this.AlignRotationToCoordSys();
            }

            MyCoordinateSystem.OnCoordinateChange += OnCoordinateChange;
        }

        private bool m_isAligning = false;
        private int m_lastFrameAligned = 0;
        private void OnCoordinateChange()
        {
            if (MyCoordinateSystem.Static.LocalCoordExist && AnyCopiedGridIsStatic)
            {
                this.EnableStationRotation = false;
                MyCoordinateSystem.Static.Visible = true;
            }
            if (!MyCoordinateSystem.Static.LocalCoordExist)
            {
                this.EnableStationRotation = true;
                MyCoordinateSystem.Static.Visible = false;
            }
            else
            {
                //GK: Do this in order for CoordinateSystem to be redrawn when entering again in this coordinate
                this.EnableStationRotation = false;
                MyCoordinateSystem.Static.Visible = true;
            }

            if (!m_enableStationRotation && IsActive && !m_isAligning)
            {
                m_isAligning = true;
                int currentFrameCount = MyFpsManager.GetSessionTotalFrames();
                if (currentFrameCount - m_lastFrameAligned >= 12)
                {
                this.AlignRotationToCoordSys();
                    m_lastFrameAligned = currentFrameCount;
                }
                m_isAligning = false;
            }
        }

        public virtual void Deactivate(bool afterPaste = false)
        {
            CreationMode = false;
            bool wasActive = IsActive;
            ChangeClipboardPreview(false);
            IsActive = false;

            var handler = Deactivated;
            if (wasActive && handler != null)
            {
                handler(this, afterPaste);
            }

            MyCoordinateSystem.Static.Visible = false;
            MyCoordinateSystem.Static.ResetSelection();
            MyCoordinateSystem.OnCoordinateChange -= OnCoordinateChange;

        }

        public void Hide()
        {
            if (MyFakes.ENABLE_VR_BUILDING)
                ShowPreview(false);
            else
                ChangeClipboardPreview(false);
        }

        public void Show()
        {
            if (IsActive)
            {
                if (m_previewGrids.Count == 0)
                    ChangeClipboardPreview(true);

                if (MyFakes.ENABLE_VR_BUILDING)
                    ShowPreview(true);
            }
        }

        protected void ShowPreview(bool show)
        {
            if (PreviewGrids.Count == 0)
                return;

            if (PreviewGrids[0].Render.Visible == show)
                return;

            foreach (var previewGrid in PreviewGrids)
            {
                previewGrid.Render.Visible = show;

                foreach (var block in previewGrid.GetBlocks())
                {
                    var compound = block.FatBlock as MyCompoundCubeBlock;
                    if (compound != null)
                    {
                        compound.Render.UpdateRenderObject(show);

                        foreach (var blockInCompound in compound.GetBlocks())
                            if (blockInCompound.FatBlock != null)
                                blockInCompound.FatBlock.Render.UpdateRenderObject(show);
                    }
                    else
                    {
                        if (block.FatBlock != null)
                            block.FatBlock.Render.UpdateRenderObject(show);
                    }
                }
            }
        }

        public void ClearClipboard()
        {
            if (IsActive)
                Deactivate();
            m_copiedGrids.Clear();
            m_copiedGridOffsets.Clear();
        }

        public void CopyGroup(MyCubeGrid gridInGroup, GridLinkTypeEnum groupType)
        {
            if (gridInGroup == null)
                return;
            m_copiedGrids.Clear();
            m_copiedGridOffsets.Clear();

            if (MyFakes.ENABLE_COPY_GROUP && MyFakes.ENABLE_LARGE_STATIC_GROUP_COPY_FIRST)
            {
                // Find large static grid, large grid or small static grid if present as first group.
                var group = MyCubeGridGroups.Static.GetGroups(groupType).GetGroupNodes(gridInGroup);

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

                foreach (var node in group)
                {
                    if (largeGrid == null && node.GridSizeEnum == MyCubeSize.Large)
                        largeGrid = node;

                    if (staticLargeGrid == null && node.GridSizeEnum == MyCubeSize.Large && node.IsStatic)
                        staticLargeGrid = node;

                    if (smallStaticGrid == null && node.GridSizeEnum == MyCubeSize.Small && node.IsStatic)
                        smallStaticGrid = node;
                }

                MyCubeGrid firstGrid = staticLargeGrid != null ? staticLargeGrid : null;
                firstGrid = firstGrid != null ? firstGrid : (largeGrid != null ? largeGrid : null);
                firstGrid = firstGrid != null ? firstGrid : (smallStaticGrid != null ? smallStaticGrid : null);
                firstGrid = firstGrid != null ? firstGrid : gridInGroup;

                group = MyCubeGridGroups.Static.GetGroups(groupType).GetGroupNodes(firstGrid);

                CopyGridInternal(firstGrid);

                foreach (var node in group)
                {
                    if (node != firstGrid)
                        CopyGridInternal(node);
                }
            }
            else
            {
                CopyGridInternal(gridInGroup);

                if (MyFakes.ENABLE_COPY_GROUP)
                {
                    var group = MyCubeGridGroups.Static.GetGroups(groupType).GetGroupNodes(gridInGroup);
                    foreach (var node in group)
                    {
                        if (node != gridInGroup)
                            CopyGridInternal(node);
                    }
                }
            }

        }

        public void CutGrid(MyCubeGrid grid)
        {
            if (grid == null)
                return;

            CopyGrid(grid);

            grid.SendGridCloseRequest();
        }

        public void CopyGrid(MyCubeGrid grid)
        {
            if (grid == null)
                return;
            m_copiedGrids.Clear();
            m_copiedGridOffsets.Clear();
            CopyGridInternal(grid);
        }

        public void CutGroup(MyCubeGrid grid, GridLinkTypeEnum groupType)
        {
            if (grid == null)
                return;

            CopyGroup(grid, groupType);

            if (MyFakes.ENABLE_COPY_GROUP)
            {
                var group = MyCubeGridGroups.Static.GetGroups(groupType).GetGroupNodes(grid);
                foreach (var node in group)
                {
                    node.SendGridCloseRequest();
                }
            }
            else
            {
                grid.SendGridCloseRequest();
            }
        }

        private void CopyGridInternal(MyCubeGrid toCopy)
        {
            if (MySession.Static.CameraController.Equals(toCopy))
            {
                MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator, null, (Vector3D)toCopy.PositionComp.GetPosition());
            }

            var gridBuilder = (MyObjectBuilder_CubeGrid)toCopy.GetObjectBuilder(true);
            m_copiedGrids.Add(gridBuilder);

            RemovePilots(gridBuilder);
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

        public virtual bool PasteGrid(MyInventoryBase buildInventory = null, bool deactivate = true)
        {
            return PasteGridInternal(buildInventory, deactivate, touchingGrids: m_touchingGrids);
        }

        protected bool PasteGridInternal(MyInventoryBase buildInventory, bool deactivate, List<MyObjectBuilder_CubeGrid> pastedBuilders = null, List<MyCubeGrid> touchingGrids = null,
            UpdateAfterPasteCallback updateAfterPasteCallback = null, bool multiBlock = false)
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

            if (!IsWithinWorldLimits())
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                MyHud.Notifications.Add(MyNotificationSingletons.ShipOverLimits);
                return false;
            }

            if (m_previewGrids.Count == 0)
                return false;

            foreach (var grid in m_copiedGrids)
            {
                foreach (var block in grid.CubeBlocks)
                {
                    block.BuiltBy = MySession.Static.LocalPlayerId;
                }
            }

            bool missingBlockDefinitions = false;
            if (ShowModdedBlocksWarning)
            {
                missingBlockDefinitions = !CheckPastedBlocks();
            }

            if (missingBlockDefinitions)
            {
                AllowSwitchCameraMode = false;
                var messageBox = MyGuiSandbox.CreateMessageBox(
                    styleEnum: MyMessageBoxStyleEnum.Info,
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextDoYouWantToPasteGridWithMissingBlocks),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionWarning),
                    callback: (result) =>
                    {
                        if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                        {
                            PasteInternal(buildInventory, missingBlockDefinitions, deactivate, pastedBuilders, updateAfterPasteCallback: updateAfterPasteCallback, multiBlock: multiBlock);
                        }
                        AllowSwitchCameraMode = true;
                    });
                MyGuiSandbox.AddScreen(messageBox);
                return false;
            }

            if (!MySession.Static.IsScripter && !CheckPastedScripts())
                MyHud.Notifications.Add(MyNotificationSingletons.BlueprintScriptsRemoved);

            return PasteInternal(buildInventory, missingBlockDefinitions, deactivate, pastedBuilders: pastedBuilders, touchingGrids: touchingGrids, 
                updateAfterPasteCallback: updateAfterPasteCallback, multiBlock: multiBlock);
        }

        private bool PasteInternal(MyInventoryBase buildInventory, bool missingDefinitions, bool deactivate, List<MyObjectBuilder_CubeGrid> pastedBuilders = null, List<MyCubeGrid> touchingGrids = null,
            UpdateAfterPasteCallback updateAfterPasteCallback = null, bool multiBlock = false)
        {
            var copiedLocalGrids = new List<MyObjectBuilder_CubeGrid>();
            foreach (var gridCopy in m_copiedGrids)
            {
                copiedLocalGrids.Add((MyObjectBuilder_CubeGrid)gridCopy.Clone());
            }

            var grid = copiedLocalGrids[0];
            bool isMergeNeeded = IsSnapped && SnapMode == SnapMode.Base6Directions && m_hitEntity is MyCubeGrid && grid != null && ((MyCubeGrid)m_hitEntity).GridSizeEnum == grid.GridSizeEnum;

            if (isMergeNeeded && !IsMergeWithinWorldLimits())
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                MyHud.Notifications.Add(MyNotificationSingletons.ShipOverLimits);
                return false;
            }

            MyGuiAudio.PlaySound(MyGuiSounds.HudPlaceBlock);

            MyCubeGrid hitGrid = null;
            if(isMergeNeeded)
            {
                hitGrid = m_hitEntity as MyCubeGrid;
            }

            isMergeNeeded |= touchingGrids != null && touchingGrids.Count > 0;

            if(hitGrid == null && touchingGrids != null && touchingGrids.Count > 0)
            {
                hitGrid = touchingGrids[0];
            }

            int i = 0;
            foreach (var gridBuilder in copiedLocalGrids)
            {
                gridBuilder.CreatePhysics = true;
                gridBuilder.EnableSmallToLargeConnections = true;

                gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(m_previewGrids[i].WorldMatrix);
                gridBuilder.PositionAndOrientation.Value.Orientation.Normalize();
                i++;
            }

            long inventoryOwnerId = 0;

            if (buildInventory != null)
            {
                if (MyFakes.ENABLE_MEDIEVAL_INVENTORY)
                {
                    inventoryOwnerId = buildInventory.Entity.EntityId;
                }
                else if( buildInventory is MyInventory)
                {
                    inventoryOwnerId = (buildInventory as MyInventory).Owner.EntityId;
                }
            }

            bool isAdmin = MySession.Static.CreativeToolsEnabled(Sync.MyId);

            if (isMergeNeeded && hitGrid != null)
            {
                hitGrid.PasteBlocksToGrid(copiedLocalGrids, inventoryOwnerId, multiBlock, isAdmin);
            }
            //TODO: GZA - This is connected with creational clipboards used to create new grids in space. Should be removed later.
            else if(CreationMode)
            {
                MyMultiplayer.RaiseStaticEvent(s => MyCubeGrid.TryCreateGrid_Implementation, CubeSize, IsStatic, copiedLocalGrids[0].PositionAndOrientation.Value, inventoryOwnerId, isAdmin);
                CreationMode = false;
            }
            else if (MySession.Static.CreativeMode || MySession.Static.HasCreativeRights)
            {
                bool anyGridInGround = false;
                bool anyGridIsSmall = false;

                foreach (var prevGrid in m_previewGrids)
                {
                    anyGridIsSmall |= prevGrid.GridSizeEnum == MyCubeSize.Small;

                    var settings = m_settings.GetGridPlacementSettings(prevGrid.GridSizeEnum, prevGrid.IsStatic);
                    anyGridInGround |= MyCubeGrid.IsAabbInsideVoxel(prevGrid.PositionComp.WorldMatrix, (BoundingBoxD)prevGrid.PositionComp.LocalAABB, settings);
                }

                bool smallInMedieval = false;
                if (MyPerGameSettings.Game == GameEnum.ME_GAME)
                {
                    MyCubeGrid hitEntGrid = m_hitEntity as MyCubeGrid;
                    if (hitEntGrid != null)
                        smallInMedieval = anyGridIsSmall && (hitEntGrid.GridSizeEnum == MyCubeSize.Large);
                }

                foreach (var gridOb in copiedLocalGrids)
                {
                    gridOb.IsStatic = smallInMedieval || anyGridInGround || (MySession.Static.EnableConvertToStation && gridOb.IsStatic);                    
                }
                if (!MySandboxGame.IsDedicated)
                {
                    MyHud.PushRotatingWheelVisible();
                }
                MyMultiplayer.RaiseStaticEvent(s => MyCubeGrid.TryPasteGrid_Implementation, copiedLocalGrids, missingDefinitions, inventoryOwnerId, m_objectVelocity, multiBlock, isAdmin);
            }

            if (deactivate)
            {
                Deactivate(afterPaste: true);
            }

            if (updateAfterPasteCallback != null)
            {
                updateAfterPasteCallback(pastedBuilders);
            }
           
            return true;
        }

        /// <summary>
        /// Determines whether the placed grid still fits within block limits set by server
        /// </summary>
        private bool IsWithinWorldLimits()
        {
            if (!MySession.Static.EnableBlockLimits) return true;

            bool withinLimit = true;
            int sizeSum = 0;
            foreach (var grid in PreviewGrids)
            {
                if (MySession.Static.MaxGridSize != 0)
                {
                    withinLimit &= grid.BlocksCount <= MySession.Static.MaxGridSize;
                }
                sizeSum += grid.BlocksCount;
            }
            var identity = MySession.Static.Players.TryGetIdentity(MySession.Static.LocalPlayerId);
            int typeBuilt;
            foreach (var limit in MySession.Static.BlockTypeLimits)
            {
                if (m_blocksPerType.ContainsKey(limit.Key))
                {
                    withinLimit &= (identity.BlockTypeBuilt.TryGetValue(limit.Key, out typeBuilt) ? typeBuilt : 0) + m_blocksPerType[limit.Key] <= MySession.Static.GetBlockTypeLimit(limit.Key);
                }
            }
            withinLimit &= MySession.Static.MaxBlocksPerPlayer == 0 || identity == null || sizeSum + identity.BlocksBuilt <= MySession.Static.MaxBlocksPerPlayer + identity.BlockLimitModifier;
            return withinLimit;
        }

        /// <summary>
        /// Determines whether the placed grid still fits within block limits set by server after it merges with a nearby grid
        /// </summary>
        private bool IsMergeWithinWorldLimits()
        {
            return MySession.Static.MaxGridSize == 0 || (m_hitEntity as MyCubeGrid).BlocksCount + PreviewGrids[0].BlocksCount <= MySession.Static.MaxGridSize;
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

        /// <summary>
        /// Checks the pasted object builder for scripts inside programmable blocks
        /// </summary>
        /// <returns></returns>
        protected bool CheckPastedScripts()
        {
            if (MySession.Static.IsScripter)
                return true;

            foreach (var gridBuilder in m_copiedGrids)
            {
                foreach (var block in gridBuilder.CubeBlocks)
                {
                    var programmable = block as MyObjectBuilder_MyProgrammableBlock;
                    if (programmable == null)
                        continue;

                    if (programmable.Program != null)
                        return false;
                }
            }

            return true;
        }

        public void SetGridFromBuilder(MyObjectBuilder_CubeGrid grid, Vector3 dragPointDelta, float dragVectorLength)
        {

            m_copiedGrids.Clear();
            m_copiedGridOffsets.Clear();

            m_dragPointToPositionLocal = dragPointDelta;
            m_dragDistance = dragVectorLength;
            var transform = grid.PositionAndOrientation ?? MyPositionAndOrientation.Default;
            m_pasteDirUp = transform.Up;
            m_pasteDirForward = transform.Forward;

            SetGridFromBuilderInternal(grid, Vector3.Zero);
        }

        public void SetGridFromBuilders(MyObjectBuilder_CubeGrid[] grids, Vector3 dragPointDelta, float dragVectorLength)
        {
            ShowModdedBlocksWarning = true;
            if (IsActive)
            {
                Deactivate();
            }

            m_copiedGrids.Clear();
            m_copiedGridOffsets.Clear();

            if (grids.Length == 0) return;

            MatrixD pasteMatrix = GetPasteMatrix();
            m_dragPointToPositionLocal = dragPointDelta;
            m_dragDistance = dragVectorLength;
            var transform = grids[0].PositionAndOrientation ?? MyPositionAndOrientation.Default;
            m_pasteDirUp = transform.Up;
            m_pasteDirForward = transform.Forward;

            SetGridFromBuilderInternal(grids[0], Vector3.Zero);

            MatrixD invMatrix = grids[0].PositionAndOrientation.HasValue ? grids[0].PositionAndOrientation.Value.GetMatrix() : MatrixD.Identity;
            invMatrix = MatrixD.Invert(invMatrix);
            for (int i = 1; i < grids.Length; ++i)
            {
                Vector3D offset = grids[i].PositionAndOrientation.HasValue ? (Vector3D)grids[i].PositionAndOrientation.Value.Position : Vector3D.Zero;
                offset = Vector3D.Transform(offset, invMatrix);
                SetGridFromBuilderInternal(grids[i], offset);
            }

            //Activate();
        }

        private void SetGridFromBuilderInternal(MyObjectBuilder_CubeGrid grid, Vector3 offset)
        {
            BeforeCreateGrid(grid);

            m_copiedGrids.Add(grid);
            m_copiedGridOffsets.Add(offset);
            RemovePilots(grid);
        }

        protected void BeforeCreateGrid(MyObjectBuilder_CubeGrid grid)
        {
            Debug.Assert(grid.CubeBlocks.Count > 0, "The grid does not contain any blocks");

            foreach (var block in grid.CubeBlocks)
            {
                var defId = block.GetId();
                MyCubeBlockDefinition blockDef = null;
                MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out blockDef);
                if (blockDef == null) continue;

                MyCubeBuilder.BuildComponent.BeforeCreateBlock(blockDef, GetClipboardBuilder(), block, buildAsAdmin: MySession.Static.CreativeToolsEnabled(Sync.MyId));
            }
        }


        protected virtual void ChangeClipboardPreview(bool visible)
        {
            foreach (var grid in m_previewGrids)
            {
                MyEntities.EnableEntityBoundingBoxDraw(grid, false);
                grid.Close();
            }

            m_visible = false;
            m_previewGrids.Clear();
            m_buildComponents.Clear();

            if (m_copiedGrids.Count == 0 || !visible)
                return;

            CalculateItemRequirements(m_copiedGrids, m_buildComponents);

            MyEntities.RemapObjectBuilderCollection(m_copiedGrids);

            Vector3D firstGridPosition = Vector3D.Zero;

            bool first = true;

            m_blocksPerType.Clear();

            MyEntityIdentifier.LazyInitPerThreadStorage(2048);

            foreach (var gridBuilder in m_copiedGrids)
            {
                bool savedIsStatic = gridBuilder.IsStatic;
                if (m_useDynamicPreviews)
                    gridBuilder.IsStatic = false;

                gridBuilder.CreatePhysics = false;
                gridBuilder.EnableSmallToLargeConnections = false;
                foreach (var blockBuilder in gridBuilder.CubeBlocks)
                {
                    blockBuilder.BuiltBy = 0;
                    MyDefinitionId id = new MyDefinitionId(blockBuilder.TypeId, blockBuilder.SubtypeId);
                    MyCubeBlockDefinition definition;
                    if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(id, out definition))
                    {
                        string name = definition.BlockPairName;
                        if (m_blocksPerType.ContainsKey(name))
                            m_blocksPerType[name]++;
                        else
                            m_blocksPerType.Add(name, 1);
                    }
                }

                if (gridBuilder.PositionAndOrientation.HasValue)
                {
                    //reset position from prefab (it can be outside the world)
                    MyPositionAndOrientation position = gridBuilder.PositionAndOrientation.Value;

                    if (first)
                    {
                        first = false;
                        firstGridPosition = position.Position;
                    }

                    position.Position -= firstGridPosition;
                    gridBuilder.PositionAndOrientation = position;
                }

                var previewGrid = MyEntities.CreateFromObjectBuilder(gridBuilder, false) as MyCubeGrid;
                MakeTransparent(previewGrid);
                previewGrid.UpdateDirty();

                gridBuilder.IsStatic = savedIsStatic;

                if (previewGrid == null)
                {
                    ChangeClipboardPreview(false);
                    return;// Not enough memory to create preview grid or there was some error.
                }

                previewGrid.DebugCreatedBy = DebugCreatedBy.Clipboard;
                if (previewGrid.CubeBlocks.Count == 0)
                {
                    m_copiedGrids.Remove(gridBuilder);
                    ChangeClipboardPreview(false);
                    return;
                }

                previewGrid.IsPreview = true;
                previewGrid.Save = false;
                m_previewGrids.Add(previewGrid);
                previewGrid.OnClose += previewGrid_OnClose;
                previewGrid.UpdateDirty();
            }
            m_resultIDs.Clear();
            MyEntityIdentifier.GetPerThreadEntities(m_resultIDs);
            MyEntityIdentifier.ClearPerThreadEntities();
            //IsActive = visible;
            m_visible = visible;
        }

        void previewGrid_OnClose(MyEntity obj)
        {
            m_previewGrids.Remove(obj as MyCubeGrid);
            if (m_previewGrids.Count == 0)
            {
                //TODO: show some notification that the paste failed
                // Deactivation commented out because during clipboard moving grid can be hidden (it is closed, see Hide) and deactivation is not wanted.
                //Deactivate();
            }
        }

        public static void CalculateItemRequirements(List<MyObjectBuilder_CubeGrid> blocksToBuild, MyComponentList buildComponents)
        {
            buildComponents.Clear();
            foreach (var grid in blocksToBuild)
            {
                foreach (var block in grid.CubeBlocks)
                {
                    var compound = block as MyObjectBuilder_CompoundCubeBlock;
                    if (compound != null)
                    {
                        foreach (var subblock in compound.Blocks)
                        {
                            AddSingleBlockRequirements(subblock, buildComponents);
                        }
                    }
                    else
                    {
                        AddSingleBlockRequirements(block, buildComponents);
                    }
                }
            }
        }

        public static void CalculateItemRequirements(MyObjectBuilder_CubeGrid[] blocksToBuild, MyComponentList buildComponents)
        {
            buildComponents.Clear();
            foreach (var grid in blocksToBuild)
            {
                foreach (var block in grid.CubeBlocks)
                {
                    var compound = block as MyObjectBuilder_CompoundCubeBlock;
                    if (compound != null)
                    {
                        foreach (var subblock in compound.Blocks)
                        {
                            AddSingleBlockRequirements(subblock, buildComponents);
                        }
                    }
                    else
                    {
                        AddSingleBlockRequirements(block, buildComponents);
                    }
                }
            }
        }


        static private void AddSingleBlockRequirements(MyObjectBuilder_CubeBlock block, MyComponentList buildComponents)
        {
            MyComponentStack.GetMountedComponents(buildComponents, block);
            if (block.ConstructionStockpile != null)
                foreach (var item in block.ConstructionStockpile.Items)
                {
                    if (item.PhysicalContent != null)
                        buildComponents.AddMaterial(item.PhysicalContent.GetId(), item.Amount, addToDisplayList: false);
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
            grid.Render.CastShadows = false;

            if (m_cacheEntitySet == null)
                m_cacheEntitySet = new HashSet<IMyEntity>();

            grid.Hierarchy.GetChildrenRecursive(m_cacheEntitySet);

            foreach (var child in m_cacheEntitySet)
            {
                child.Render.Transparency = Transparency;
                child.Render.CastShadows = false;
            }
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

            if (IsSnapped && SnapMode == SnapMode.Base6Directions)
            {
                FixSnapTransformationBase6();
            }

            if (m_calculateVelocity)
                m_objectVelocity = (m_pastePosition - m_pastePositionPrevious) / VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            if (0 == MyFpsManager.GetSessionTotalFrames() % 11)
                m_canBePlaced = TestPlacement();
            else
                m_canBePlacedNeedsRefresh = true;

            if (MySession.Static.CreativeToolsEnabled(Sync.MyId))
            {
                m_characterHasEnoughMaterials = true;              
            }
            else
            {
                TestBuildingMaterials();
            }
            UpdatePreviewBBox();

            if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "FW: " + m_pasteDirForward.ToString(), Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 20.0f), "UP: " + m_pasteDirUp.ToString(), Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 40.0f), "AN: " + m_pasteOrientationAngle.ToString(), Color.Red, 1.0f);
            }
        }

        protected bool UpdateHitEntity(bool canPasteLargeOnSmall = true)
        {
            Debug.Assert(m_raycastCollisionResults.Count == 0);

            m_closestHitDistSq = float.MaxValue;
            m_hitPos = new Vector3(0.0f, 0.0f, 0.0f);
            m_hitNormal = new Vector3(1.0f, 0.0f, 0.0f);
            m_hitEntity = null;

            MatrixD pasteMatrix = GetPasteMatrix();

            if (MyFakes.ENABLE_VR_BUILDING && MyCubeBuilder.PlacementProvider != null)
            {
                if (MyCubeBuilder.PlacementProvider.HitInfo == null)
                    return false;

                m_hitEntity = (MyEntity)MyCubeBuilder.PlacementProvider.ClosestGrid ?? (MyEntity)MyCubeBuilder.PlacementProvider.ClosestVoxelMap;
                m_hitPos = MyCubeBuilder.PlacementProvider.HitInfo.Value.Position;
                m_hitNormal = MyCubeBuilder.PlacementProvider.HitInfo.Value.HkHitInfo.Normal;
                m_hitNormal = Base6Directions.GetIntVector(Base6Directions.GetClosestDirection(Vector3.TransformNormal(m_hitNormal, m_hitEntity.PositionComp.WorldMatrixNormalizedInv)));
                m_hitNormal = Vector3.TransformNormal(m_hitNormal, m_hitEntity.PositionComp.WorldMatrix);
                m_closestHitDistSq = (float)(m_hitPos - pasteMatrix.Translation).LengthSquared();

                return true;
            }

            MyPhysics.CastRay(pasteMatrix.Translation, pasteMatrix.Translation + pasteMatrix.Forward * m_dragDistance, m_raycastCollisionResults, MyPhysics.CollisionLayers.DefaultCollisionLayer);

            foreach (var hit in m_raycastCollisionResults)
            {
                if (hit.HkHitInfo.Body == null)
                    continue;

                var entity = hit.HkHitInfo.GetHitEntity();
                if (entity == null)
                    continue;

                MyCubeGrid grid = entity as MyCubeGrid;

                if (!canPasteLargeOnSmall && m_previewGrids[0].GridSizeEnum == MyCubeSize.Large && grid != null && grid.GridSizeEnum == MyCubeSize.Small)
                    continue;

                if ((entity is MyVoxelBase) || (grid != null && grid.EntityId != m_previewGrids[0].EntityId))
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

            return true;
        }

        protected virtual void TestBuildingMaterials()
        {
            m_characterHasEnoughMaterials = EntityCanPaste(GetClipboardBuilder());
        }

        protected virtual MyEntity GetClipboardBuilder()
        {
            return MySession.Static.LocalCharacter;
        }

        public virtual bool EntityCanPaste(MyEntity pastingEntity)
        {
            if (m_copiedGrids.Count < 1) return false;
            if (MySession.Static.CreativeToolsEnabled(Sync.MyId))
            {
                return true;
            }
            MyCubeBuilder.BuildComponent.GetGridSpawnMaterials(m_copiedGrids[0]);
            return MyCubeBuilder.BuildComponent.HasBuildingMaterials(pastingEntity);
        }

        protected virtual bool TestPlacement()
        {
            m_canBePlacedNeedsRefresh = false;
            if(MyFakes.DISABLE_CLIPBOARD_PLACEMENT_TEST)
                return true;

            bool retval = true;
            for (int i = 0; i < m_previewGrids.Count; ++i)
            {
                var grid = m_previewGrids[i];
                if (i == 0 && m_hitEntity is MyCubeGrid && IsSnapped && SnapMode == SnapMode.Base6Directions)
                {
                    var hitGrid = m_hitEntity as MyCubeGrid;

                    bool smallOnLargeGrid = hitGrid.GridSizeEnum == MyCubeSize.Large && grid.GridSizeEnum == MyCubeSize.Small;
                    var settings = m_settings.GetGridPlacementSettings(grid.GridSizeEnum, grid.IsStatic);

                    if (MyFakes.ENABLE_STATIC_SMALL_GRID_ON_LARGE && grid.IsStatic && smallOnLargeGrid)
                    {
                        retval &= MyCubeGrid.TestPlacementArea(grid, grid.IsStatic, ref settings, (BoundingBoxD)grid.PositionComp.LocalAABB, false, hitGrid);
                    }
                    else
                    {
                        Vector3I gridOffset = hitGrid.WorldToGridInteger(m_pastePosition);
                        if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
                            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 60.0f), "First grid offset: " + gridOffset.ToString(), Color.Red, 1.0f);
                        retval &= hitGrid.GridSizeEnum == grid.GridSizeEnum && hitGrid.CanMergeCubes(grid, gridOffset);
                        retval &= MyCubeGrid.CheckMergeConnectivity(hitGrid, grid, gridOffset);

                        retval &= MyCubeGrid.TestPlacementArea(grid, grid.IsStatic, ref settings, (BoundingBoxD)grid.PositionComp.LocalAABB, false, hitGrid);
                    }
                }
                else
                {
                    var settings = m_settings.GetGridPlacementSettings(grid.GridSizeEnum, grid.IsStatic);
                    this.GetTouchingGrids(grid, settings);
                    retval &= MyCubeGrid.TestPlacementArea(grid, grid.IsStatic, ref settings, (BoundingBoxD)grid.PositionComp.LocalAABB, false);
                }

                if (!retval)
                    return false;
            }

            return retval;
        }

        /// <summary>
        /// Gets grids that are touching given grid. It's used for diciding if grids should be merged on later stage.
        /// </summary>
        /// <param name="grid">Grid to be tested.</param>
        /// <param name="settings">Settings used for the test.</param>
        private void GetTouchingGrids(MyCubeGrid grid, MyGridPlacementSettings settings)
        {
            m_touchingGrids.Clear();

            foreach (var block in grid.CubeBlocks)
            {
                if (block.FatBlock is MyCompoundCubeBlock)
                {
                    bool isTouching = false;
                    MyCompoundCubeBlock compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                    foreach (var blockInCompound in compoundBlock.GetBlocks())
                    {
                        MyCubeGrid touchingGridLocal = null;
                        MyCubeGrid.TestPlacementAreaCubeNoAABBInflate(blockInCompound.CubeGrid, ref settings, blockInCompound.Min, blockInCompound.Max, blockInCompound.Orientation,
                            blockInCompound.BlockDefinition, out touchingGridLocal, blockInCompound.CubeGrid);

                        if (touchingGridLocal == null) 
                            continue;

                        m_touchingGrids.Add(touchingGridLocal);
                        isTouching = true;
                        break;
                    }

                    if (isTouching)
                        break;
                }
                else
                {
                    MyCubeGrid touchingGridLocal = null;
                    MyCubeGrid.TestPlacementAreaCubeNoAABBInflate(block.CubeGrid, ref settings, block.Min, block.Max, block.Orientation,
                        block.BlockDefinition, out touchingGridLocal, block.CubeGrid);
                    
                    if (touchingGridLocal == null) 
                        continue;
                    
                    m_touchingGrids.Add(touchingGridLocal);
                    break;
                }
            }
        }

        protected virtual void UpdateGridTransformations()
        {
            Debug.Assert(m_copiedGrids.Count != 0);
            if (m_copiedGrids.Count == 0)
                return;

            Matrix originalOrientation = GetFirstGridOrientationMatrix();
            var invRotation = Matrix.Invert(m_copiedGrids[0].PositionAndOrientation.Value.GetMatrix()).GetOrientation();
            Matrix orientationDelta = invRotation * originalOrientation; // matrix from original orientation to new orientation

            for (int i = 0; i < m_previewGrids.Count; i++)
            {
                if (i > m_copiedGrids.Count - 1)
                    break;

                if(!m_copiedGrids[i].PositionAndOrientation.HasValue)
                    continue;
                
                MatrixD worldMatrix2 = m_copiedGrids[i].PositionAndOrientation.Value.GetMatrix(); //get original rotation and position
                var offset = worldMatrix2.Translation - m_copiedGrids[0].PositionAndOrientation.Value.Position; //calculate offset to first pasted grid
                //if (!AnyCopiedGridIsStatic)
                {
                    m_copiedGridOffsets[i] = Vector3.TransformNormal(offset, orientationDelta); // Transform the offset to new orientation
                    worldMatrix2 = worldMatrix2 * orientationDelta; //correct rotation
                }
                Vector3D translation = m_pastePosition + m_copiedGridOffsets[i];// m_copiedGridOffsets[i]; //correct position

                worldMatrix2.Translation = Vector3.Zero;
                worldMatrix2 = MatrixD.Orthogonalize(worldMatrix2);
                worldMatrix2.Translation = translation;

                m_previewGrids[i].PositionComp.SetWorldMatrix(worldMatrix2);// Set the corrected position
            }
        }

        protected virtual void UpdatePastePosition()
        {
            Debug.Assert(m_previewGrids.Count > 0, "m_previewGrids is empty (MyGridClipboard - UpdatePastePosition)");
            if (m_previewGrids.Count == 0) return;
            m_pastePositionPrevious = m_pastePosition;

            // Current position of the placed entity is either simple translation or
            // it can be calculated by raycast, if we want to snap to surfaces
            MatrixD pasteMatrix = GetPasteMatrix();
            Vector3 dragVectorGlobal = pasteMatrix.Forward * m_dragDistance;

            var gridSettings = m_settings.GetGridPlacementSettings(m_previewGrids[0].GridSizeEnum);
            if (!TrySnapToSurface(gridSettings.SnapMode))
            {
                m_pastePosition = pasteMatrix.Translation + dragVectorGlobal;
                Matrix firstGridOrientation = GetFirstGridOrientationMatrix();
                m_pastePosition += Vector3.TransformNormal(m_dragPointToPositionLocal, firstGridOrientation);
            }


            double gridSize = m_previewGrids[0].GridSize;
            MyCoordinateSystem.CoordSystemData localCoordData = MyCoordinateSystem.Static.SnapWorldPosToClosestGrid(ref m_pastePosition, gridSize, m_settings.StaticGridAlignToCenter);
            if (MyCoordinateSystem.Static.LocalCoordExist && !EnableStationRotation)
            {
                m_pastePosition = localCoordData.SnappedTransform.Position;
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
            {
                MyRenderProxy.DebugDrawSphere(pasteMatrix.Translation + dragVectorGlobal, 0.15f, Color.Pink.ToVector3(), 1.0f, false);
                MyRenderProxy.DebugDrawSphere(m_pastePosition, 0.15f, Color.Pink.ToVector3(), 1.0f, false);
            }
            
        }

        /// <summary>
        /// Used to update the color of a new ship/station block when the player switches it
        /// </summary>
        public virtual void UpdateColor(Vector3 newHSV)
        {
            for (int i = 0; i < m_previewGrids.Count; ++i)
            {
                foreach (var block in m_previewGrids[i].CubeBlocks)
                {
                    if (block.ColorMaskHSV != newHSV)
                    {
                        block.ColorMaskHSV = newHSV;
                        block.UpdateVisual();
                    }
                }
            }
        }

        protected static MatrixD GetPasteMatrix()
        {
            if (MySession.Static.ControlledEntity != null &&
                (MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Entity || MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator))
            {
                return MySession.Static.ControlledEntity.GetHeadMatrix(true);
            }
            else
            {
                return MySector.MainCamera.WorldMatrix;
            }
        }

        public virtual Matrix GetFirstGridOrientationMatrix()
        {
            return Matrix.CreateWorld(Vector3.Zero, m_pasteDirForward, m_pasteDirUp);// * Matrix.CreateFromAxisAngle(m_pasteDirUp, m_pasteOrientationAngle);
        }

        public void AlignClipboardToGravity()
        {
            if (PreviewGrids.Count > 0)
            {
                Vector3 gravity = GameSystems.MyGravityProviderSystem.CalculateNaturalGravityInPoint(PreviewGrids[0].WorldMatrix.Translation);
                if (gravity.LengthSquared() < Double.Epsilon && MyPerGameSettings.Game == GameEnum.ME_GAME)
                {
                    gravity = Vector3.Down;
                }

                AlignClipboardToGravity(gravity);
            }
        }

        public void AlignClipboardToGravity(Vector3 gravity)
        {
            if (PreviewGrids.Count > 0 && gravity.LengthSquared() > 0.0001f)
            {
                gravity.Normalize();

                //Vector3 gridLeft = PreviewGrids[0].WorldMatrix.Left;
                Vector3 forward = Vector3D.Reject(m_pasteDirForward, gravity);//Vector3.Cross(gravity, gridLeft);

                m_pasteDirForward = forward;
                m_pasteDirUp = -gravity;
                //m_pasteOrientationAngle = 0f;
            }
        }

        protected void AlignRotationToCoordSys()
        {
            if (m_previewGrids.Count <= 0)
                return;
            double gridSize = m_previewGrids[0].GridSize;
            MyCoordinateSystem.CoordSystemData localCoordData = MyCoordinateSystem.Static.SnapWorldPosToClosestGrid(ref m_pastePosition, gridSize, m_settings.StaticGridAlignToCenter);

            m_pastePosition = localCoordData.SnappedTransform.Position;
            m_pasteDirForward = localCoordData.SnappedTransform.Rotation.Forward;
            m_pasteDirUp = localCoordData.SnappedTransform.Rotation.Up;
            m_pasteOrientationAngle = 0.0f;
        }

        protected bool TrySnapToSurface(SnapMode snapMode)
        {
            if (m_closestHitDistSq < float.MaxValue)
            {
                Vector3 newDragPointPosition = m_hitPos;

                //bool isAnyStatic = AnyCopiedGridIsStatic;
                //if (isAnyStatic)
                //{
                //    m_pasteDirForward = Vector3.Forward;
                //    m_pasteDirUp = Vector3.Up;
                //}
                //else if (m_hitNormal.Length() > 0.5)
                //{
                if (m_hitNormal.Length() > 0.5) 
                { 
                    //if (snapMode == MyGridPlacementSettings.SnapMode.OneFreeAxis)
                    //{
                    //    m_pasteDirUp = m_hitNormal;

                    //    float dotUp = m_pasteDirUp.Dot(m_hitEntity.WorldMatrix.Up);
                    //    float dotFwd = m_pasteDirUp.Dot(m_hitEntity.WorldMatrix.Forward);
                    //    float dotRt = m_pasteDirUp.Dot(m_hitEntity.WorldMatrix.Right);
                    //    if (Math.Abs(dotUp) > Math.Abs(dotFwd))
                    //    {
                    //        if (Math.Abs(dotUp) > Math.Abs(dotRt))
                    //        {
                    //            m_pasteDirForward = Vector3.Normalize(m_hitEntity.WorldMatrix.Right - (dotRt * m_pasteDirUp));
                    //        }
                    //        else
                    //        {
                    //            m_pasteDirForward = Vector3.Normalize(m_hitEntity.WorldMatrix.Forward - (dotFwd * m_pasteDirUp));
                    //        }
                    //    }
                    //    else
                    //    {
                    //        if (Math.Abs(dotFwd) > Math.Abs(dotRt))
                    //        {
                    //            m_pasteDirForward = Vector3.Normalize(m_hitEntity.WorldMatrix.Up - (dotUp * m_pasteDirUp));
                    //        }
                    //        else
                    //        {
                    //            m_pasteDirForward = Vector3.Normalize(m_hitEntity.WorldMatrix.Forward - (dotFwd * m_pasteDirUp));
                    //        }
                    //    }
                    //}
                    //else if (snapMode == MyGridPlacementSettings.SnapMode.Base6Directions)
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
            if (m_canBePlaced)
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
            // PARODY
            hitGridRotation = Matrix.Normalize(hitGridRotation);
            // PARODY
            firstRotation = Matrix.Normalize(firstRotation);
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
                Vector3 pasteOffset = MyCubeBuilder.TransformLargeGridHitCoordToSmallGrid(m_pastePosition, hitGrid.PositionComp.WorldMatrixNormalizedInv, hitGrid.GridSize);
                m_pastePosition = hitGrid.GridIntegerToWorld(pasteOffset);
                if (MyFakes.ENABLE_VR_BUILDING) // Move pasted grid to aabb edge
                {
                    Vector3 normal = Vector3I.Round(Vector3.TransformNormal(m_hitNormal, hitGrid.PositionComp.WorldMatrixNormalizedInv));
                    Vector3 normalStepLocalInHitGrid = normal * (m_previewGrids[0].GridSize / hitGrid.GridSize);
                    Vector3 normalStepLocalInPreviewGrid = Vector3I.Round(Vector3D.TransformNormal(Vector3D.TransformNormal(normal, hitGrid.WorldMatrix),
                        m_previewGrids[0].PositionComp.WorldMatrixNormalizedInv)); 
                    var localAABB = m_previewGrids[0].PositionComp.LocalAABB;
                    localAABB.Min /= m_previewGrids[0].GridSize;
                    localAABB.Max /= m_previewGrids[0].GridSize;
                    Vector3 offsetOrigin = m_dragPointToPositionLocal / m_previewGrids[0].GridSize;
                    Vector3 offsetLocalInPreviewGrid = Vector3.Zero;
                    Vector3 offsetLocalInHitGrid = Vector3.Zero;
                    BoundingBox cubeBox = new BoundingBox(-Vector3.Half, Vector3.Half);
                    cubeBox.Inflate(-0.05f);
                    cubeBox.Translate(-offsetOrigin + offsetLocalInPreviewGrid - normalStepLocalInPreviewGrid);
                    while (localAABB.Contains(cubeBox) != ContainmentType.Disjoint)
                    {
                        offsetLocalInPreviewGrid -= normalStepLocalInPreviewGrid;
                        offsetLocalInHitGrid -= normalStepLocalInHitGrid;
                        cubeBox.Translate(-normalStepLocalInPreviewGrid);
                    }

                    m_pastePosition = hitGrid.GridIntegerToWorld(pasteOffset - offsetLocalInHitGrid);
                }
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
            MyCubeGrid grid = PreviewGrids.Count > 0 ? PreviewGrids[0] : null;

            if (grid != null)
            {
                Vector3I gridSize = grid.Max - grid.Min + new Vector3I(1, 1, 1);
                BoundingBoxD worldBox = new BoundingBoxD(-gridSize * grid.GridSize * 0.5f, gridSize * grid.GridSize * 0.5f);

                MatrixD mat = grid.WorldMatrix;
                Vector3D positionToDragPointGlobal = Vector3D.TransformNormal(-m_dragPointToPositionLocal, mat);
                mat.Translation = mat.Translation + positionToDragPointGlobal;

                hints.CalculateRotationHints(mat, worldBox, !MyHud.MinimalHud && !MyHud.CutsceneHud && MySandboxGame.Config.RotationHints, isRotating, OneAxisRotationMode);
            }
        }

        private void RemovePilots(MyObjectBuilder_CubeGrid grid)
        {
            foreach (var block in grid.CubeBlocks)
            {
                MyObjectBuilder_Cockpit cockpit = block as MyObjectBuilder_Cockpit;
                if (cockpit != null)
                {
                    cockpit.ClearPilotAndAutopilot();
                    // Remove also from Hierarchy component
                    if(cockpit.ComponentContainer != null && cockpit.ComponentContainer.Components != null)
                    {
                        foreach (var componentData in cockpit.ComponentContainer.Components)
                        {
                            if (componentData.TypeId == typeof(MyHierarchyComponentBase).Name)
                            {
                                var hierarchy = (MyObjectBuilder_HierarchyComponentBase)componentData.Component;
                                hierarchy.Children.RemoveAll(x => x is MyObjectBuilder_Character);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    MyObjectBuilder_LandingGear landingGear = block as MyObjectBuilder_LandingGear;
                    if (landingGear != null)
                    {
                        landingGear.IsLocked = false;
                        landingGear.MasterToSlave = null;
                        landingGear.AttachedEntityId = null;
                        landingGear.LockMode = LandingGearMode.Unlocked;
                    }
                }
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

        public void HideGridWhenColliding(List<Vector3D> collisionTestPoints)
        {
            if (m_previewGrids.Count == 0) return;
            bool visible = true;
            foreach (var point in collisionTestPoints)
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
            //GR: Issue with render not all blocks are hidden (Cubeblocks are not hidden). Put visible to false to reproduce
            foreach (var grid in m_previewGrids)
                grid.Render.Visible = visible;
        }

        #region Pasting transform control
        public void RotateAroundAxis(int axisIndex, int sign, bool newlyPressed, float angleDelta)
        {

            bool isSnapped = !EnableStationRotation || IsSnapped;

            if (isSnapped /*&& SnapMode == SnapMode.Base6Directions */&& EnablePreciseRotationWhenSnapped == false)
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

            //if (IsSnapped && SnapMode == SnapMode.Base6Directions)
            //{
                ApplyOrientationAngle();
            //}
            }

        private void AnglePlus(float angle)
        {
            //if (AnyCopiedGridIsStatic) return;
            m_pasteOrientationAngle += angle;
            if (m_pasteOrientationAngle >= (float)Math.PI * 2.0f)
            {
                m_pasteOrientationAngle -= (float)Math.PI * 2.0f;
            }
        }

        private void AngleMinus(float angle)
        {
            //if (AnyCopiedGridIsStatic) return;
            m_pasteOrientationAngle -= angle;
            if (m_pasteOrientationAngle < 0.0f)
            {
                m_pasteOrientationAngle += (float)Math.PI * 2.0f;
            }
        }

        private void UpPlus(float angle)
        {
            if (/*AnyCopiedGridIsStatic ||*/ OneAxisRotationMode) return;

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
            if (/*AnyCopiedGridIsStatic ||*/ OneAxisRotationMode) return;

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
			var newDragDistance = m_dragDistance * 1.1f;
            m_dragDistance = MathHelper.Clamp(newDragDistance, m_dragDistance, m_maxDragDistance);
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
