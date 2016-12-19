using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.Game.SessionComponents.Clipboard;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRageMath;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;
using VRage.Network;
using VRage.Profiler;
using VRage.Sync;

namespace Sandbox.Game.Entities.Blocks
{
    public abstract partial class MyProjectorBase : MyFunctionalBlock
    {

        public new MyProjectorDefinition BlockDefinition
        {
            get { return (MyProjectorDefinition)base.BlockDefinition; }
        }

        private MyProjectorClipboard m_clipboard;
        private MyProjectorClipboard m_spawnClipboard;
        public MyProjectorClipboard Clipboard
        {
            get
            {
                return m_clipboard;
            }
        }
        
        protected Vector3I m_projectionOffset;
        protected Vector3I m_projectionRotation;

        public Vector3I ProjectionOffset { get { return m_projectionOffset; } }
        public Vector3I ProjectionRotation { get { return m_projectionRotation; } }
        public Quaternion ProjectionRotationQuaternion
        {
            get
            {
                Vector3 radians = ProjectionRotation * MathHelper.PiOver2;
                Quaternion rotation = Quaternion.CreateFromYawPitchRoll(radians.X, radians.Y, radians.Z);
                return rotation;
            }
        }

        private MySlimBlock m_hiddenBlock;

        private bool m_shouldUpdateProjection = false;
        private bool m_shouldUpdateTexts = false;

        private MyObjectBuilder_CubeGrid m_savedProjection;

        protected bool m_showOnlyBuildable = false;

        //Projector needs to wait some frames before it can ask if it is powered.
        private int m_frameCount = 0;
        private bool m_removeRequested = false;

        public MyProjectorBase()
            : base()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_keepProjection = SyncType.CreateAndAddProp<bool>();
            m_instantBuildingEnabled = SyncType.CreateAndAddProp<bool>();
            m_maxNumberOfProjections = SyncType.CreateAndAddProp<int>();
            m_maxNumberOfBlocksPerProjection = SyncType.CreateAndAddProp<int>();
            m_getOwnershipFromProjector = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            m_clipboard = new MyProjectorClipboard(this, MyClipboardComponent.ClipboardDefinition.PastingSettings);
            m_spawnClipboard = new MyProjectorClipboard(this, MyClipboardComponent.ClipboardDefinition.PastingSettings);

            m_keepProjection.Value = false;
            m_instantBuildingEnabled.Value = false;
            m_maxNumberOfProjections.Value = 0;
            m_maxNumberOfBlocksPerProjection.Value = 0;
            m_getOwnershipFromProjector.Value = false;

            m_instantBuildingEnabled.ValueChanged += m_instantBuildingEnabled_ValueChanged;
            m_maxNumberOfProjections.ValueChanged += m_maxNumberOfProjections_ValueChanged;
            m_maxNumberOfBlocksPerProjection.ValueChanged += m_maxNumberOfBlocksPerProjection_ValueChanged;
            m_getOwnershipFromProjector.ValueChanged += m_getOwnershipFromProjector_ValueChanged;
        }

        protected MyCubeGrid ProjectedGrid 
        {
            get
            {
                if (m_clipboard.PreviewGrids.Count != 0)
                    return m_clipboard.PreviewGrids[0];
                return null;
            }
        }
        //The actual grid builder is modified to have all blocks turned off
        private MyObjectBuilder_CubeGrid m_originalGridBuilder;

        protected const int MAX_NUMBER_OF_PROJECTIONS = 1000;
        protected const int MAX_NUMBER_OF_BLOCKS = 10000;
        private int m_projectionsRemaining = 0;

        private readonly Sync<bool> m_keepProjection;
        private readonly Sync<bool> m_instantBuildingEnabled;
        private readonly Sync<int> m_maxNumberOfProjections;
        private readonly Sync<int> m_maxNumberOfBlocksPerProjection;
        private readonly Sync<bool> m_getOwnershipFromProjector;

        #region Properties

        protected bool InstantBuildingEnabled
        {
            get { return m_instantBuildingEnabled; }
            set { m_instantBuildingEnabled.Value = value; }
        }

        protected int MaxNumberOfProjections
        {
            get { return m_maxNumberOfProjections; }
            set { m_maxNumberOfProjections.Value = value; }
        }

        protected int MaxNumberOfBlocksPerProjection
        {
            get { return m_maxNumberOfBlocksPerProjection; }
            set { m_maxNumberOfBlocksPerProjection.Value = value; }
        }

        protected bool GetOwnershipFromProjector
        {
            get { return m_getOwnershipFromProjector; }
            set { m_getOwnershipFromProjector.Value = value; }
        }

        protected bool KeepProjection
        {
            get { return m_keepProjection; }
            set { m_keepProjection.Value = value; }
        }

        public bool IsActivating { get; private set; }

        #endregion

        #region UI

        protected bool IsProjecting()
        {
            return m_clipboard.IsActive;
        }

        //This also updates the texts because there is no proper event that is called when the projector is showed.
        protected bool CanProject()
        {
            UpdateIsWorking();
            UpdateText();
            return IsWorking;
        }

        protected void OnOffsetsChanged()
        {
            m_shouldUpdateProjection = true;
            m_shouldUpdateTexts = true;
            SendNewOffset(m_projectionOffset, m_projectionRotation, m_showOnlyBuildable);

            //We need to remap because the after the movement, blocks that were already built can be built again
            SendRemap();
        }
        
        public void SelectBlueprint()
        {
            if (MyGuiScreenTerminal.IsOpen)
            {
                MyGuiScreenTerminal.Hide();
            }

            RemoveProjection(false);
            var blueprintScreen = new MyGuiBlueprintScreen(m_clipboard, true);
            blueprintScreen.Closed += OnBlueprintScreen_Closed;
            MyGuiSandbox.AddScreen(blueprintScreen);

        }

        public Vector3 GetProjectionTranslationOffset()
        {
            return m_projectionOffset * m_clipboard.GridSize;
        }

        //Power failure might be only temporary (a few frames) during merging or splits
        private void RequestRemoveProjection()
        {
            m_removeRequested = true;
            m_frameCount = 0;
        }

        private void RemoveProjection(bool keepProjection)
        {
            m_hiddenBlock = null;
            m_clipboard.Deactivate();
            if (!keepProjection)
            {
                m_clipboard.Clear();
                m_originalGridBuilder = null;
            }

            UpdateEmissivity();
            m_statsDirty = true;
            UpdateText();

            //We call this to disable the controls
            RaisePropertiesChanged();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity();
        }

        private void ResetRotation()
        {
            SetRotation(m_clipboard, -m_projectionRotation);
        }

        private void SetRotation(MyGridClipboard clipboard, Vector3I rotation)
        {
            clipboard.RotateAroundAxis(0, System.Math.Sign(rotation.X), true, System.Math.Abs(rotation.X * MathHelper.PiOver2));
            clipboard.RotateAroundAxis(1, System.Math.Sign(rotation.Y), true, System.Math.Abs(rotation.Y * MathHelper.PiOver2));
            clipboard.RotateAroundAxis(2, System.Math.Sign(rotation.Z), true, System.Math.Abs(rotation.Z * MathHelper.PiOver2));
        }

        void OnBlueprintScreen_Closed(MyGuiScreenBase source)
        {
            ResourceSink.Update();
            UpdateIsWorking();
            if (m_clipboard.CopiedGrids.Count == 0 || !IsWorking)
            {
                RemoveProjection(false);
                return;
            }
            if (m_clipboard.GridSize != CubeGrid.GridSize)
            {
                RemoveProjection(false);
                ShowNotification(MySpaceTexts.NotificationProjectorGridSize);
                return;
            }
            if (m_clipboard.CopiedGrids.Count > 1)
            {
                ShowNotification(MySpaceTexts.NotificationProjectorMultipleGrids);
            }

            int largestGridIndex = -1;
            int largestGridBlockCount = -1;
            for (int i = 0; i < m_clipboard.CopiedGrids.Count; i++)
            {
                int currentGridBlockCount = m_clipboard.CopiedGrids[i].CubeBlocks.Count;
                if (currentGridBlockCount > largestGridBlockCount)
                {
                    largestGridBlockCount = currentGridBlockCount;
                    largestGridIndex = i;
                }
            }

            ParallelTasks.Parallel.Start(delegate()
            {
                m_originalGridBuilder = (MyObjectBuilder_CubeGrid)m_clipboard.CopiedGrids[largestGridIndex].Clone();
                m_clipboard.ProcessCubeGrid(m_clipboard.CopiedGrids[largestGridIndex]);
                MyEntities.RemapObjectBuilder(m_originalGridBuilder);
            }, 
            delegate()
            {
                SendNewBlueprint(m_originalGridBuilder);
            });
        }

        protected bool ScenarioSettingsEnabled()
        {
            return MySession.Static.Settings.ScenarioEditMode || MySession.Static.IsScenario;
        }

        protected bool CanEditInstantBuildingSettings()
        {
            return CanEnableInstantBuilding() && m_instantBuildingEnabled;
        }

        protected bool CanEnableInstantBuilding()
        {
            return MySession.Static.Settings.ScenarioEditMode;
        }

        protected bool CanSpawnProjection()
        {
            if (!m_instantBuildingEnabled)
            {
                return false;
            }

            if (ProjectedGrid == null)
            {
                return false;
            }

            if (m_maxNumberOfBlocksPerProjection < MAX_NUMBER_OF_BLOCKS && m_maxNumberOfBlocksPerProjection < ProjectedGrid.CubeBlocks.Count)
            {
                return false;
            }

            if (m_projectionsRemaining == 0)
            {
                return false;
            }

            if (!ScenarioSettingsEnabled())
            {
                return false;
            }

            return true;
        }

        protected void TrySetInstantBuilding(bool v)
        {
            if (CanEnableInstantBuilding())
                InstantBuildingEnabled = v;
        }

        protected void TrySetGetOwnership(bool v)
        {
            if (CanEnableInstantBuilding())
                GetOwnershipFromProjector = v;
        }

        protected void TrySpawnProjection()
        {
            if (CanSpawnProjection())
                SendSpawnProjection();
        }

        protected void TryChangeMaxNumberOfBlocksPerProjection(float v)
        {
            if (CanEditInstantBuildingSettings())
                MaxNumberOfProjections = (int)Math.Round(v);
        }

        protected void TryChangeNumberOfProjections(float v)
        {
            if (CanEditInstantBuildingSettings())
                MaxNumberOfBlocksPerProjection = (int)Math.Round(v);
        }
        
        #endregion

        #region Block Visibility
        private List<MySlimBlock> m_visibleBlocks = new List<MySlimBlock>();
        private List<MySlimBlock> m_buildableBlocks = new List<MySlimBlock>();
        private List<MySlimBlock> m_hiddenBlocks = new List<MySlimBlock>();

        public void ShowCube(MySlimBlock cubeBlock, bool canBuild)
        {
            ProfilerShort.Begin("SetTransparency");
            if (canBuild)
            {
                SetTransparency(cubeBlock, MyGridConstants.BUILDER_TRANSPARENCY);
            }
            else
            {
                SetTransparency(cubeBlock, MyGridConstants.PROJECTOR_TRANSPARENCY);
            }
            ProfilerShort.End();
        }

        public void HideCube(MySlimBlock cubeBlock)
        {
            SetTransparency(cubeBlock, 1f);
        }

        protected virtual void SetTransparency(MySlimBlock cubeBlock, float transparency)
        {
            //This is intended. It signals to the shader to render it in a special way.
            transparency = -transparency;

            if (cubeBlock.Dithering == transparency && cubeBlock.CubeGrid.Render.Transparency == transparency)
            {
                return;
            }
            cubeBlock.CubeGrid.Render.Transparency = transparency;
            cubeBlock.Dithering = transparency;
            cubeBlock.UpdateVisual();

            var block = cubeBlock.FatBlock;
            if (block != null)
            {
                SetTransparencyForSubparts(block, transparency);
            }

            if (block != null && block.UseObjectsComponent != null && block.UseObjectsComponent.DetectorPhysics != null)
            {
                block.UseObjectsComponent.DetectorPhysics.Enabled = false;
            }
        }

        private void SetTransparencyForSubparts(MyEntity renderEntity, float transparency)
        {
            renderEntity.Render.CastShadows = false;

            if (renderEntity.Subparts == null)
                return;

            foreach (var subpart in renderEntity.Subparts)
            {
                subpart.Value.Render.Transparency = transparency;
                subpart.Value.Render.CastShadows = false;
                subpart.Value.Render.RemoveRenderObjects();
                subpart.Value.Render.AddRenderObjects();

                SetTransparencyForSubparts(subpart.Value, transparency);
            }
        }

        private void HideIntersectedBlock()
        {
            if (m_instantBuildingEnabled)
            {
                return;
            }

            var character = MySession.Static.LocalCharacter;
            if (character == null)
            {
                return;
            }

            Vector3D position = character.GetHeadMatrix(true).Translation;
            if (ProjectedGrid == null) return;

            Vector3I gridPosition = ProjectedGrid.WorldToGridInteger(position);
            MySlimBlock cubeBlock = ProjectedGrid.GetCubeBlock(gridPosition);
            if (cubeBlock != null)
            {
                if (Math.Abs(cubeBlock.Dithering) < 1.0f)
                {
                    if (m_hiddenBlock != cubeBlock)
                    {
                        if (m_hiddenBlock != null)
                        {
                            ShowCube(m_hiddenBlock, CanBuild(m_hiddenBlock));
                        }
                        HideCube(cubeBlock);
                        m_hiddenBlock = cubeBlock;
                    }
                }
            }
            else
            {
                if (m_hiddenBlock != null)
                {
                    ShowCube(m_hiddenBlock, CanBuild(m_hiddenBlock));
                    m_hiddenBlock = null;
                }
            }
        }
        #endregion

        #region Init
        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                BlockDefinition.ResourceSinkGroup,
                BlockDefinition.RequiredPowerInput,
                CalculateRequiredPowerInput);
           
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);
            if (!MyFakes.ENABLE_PROJECTOR_BLOCK)
            {
                return;
            }

            var projectorBuilder = (MyObjectBuilder_ProjectorBase)objectBuilder;
            if (projectorBuilder.ProjectedGrid != null)
            {
                m_projectionOffset = projectorBuilder.ProjectionOffset;
                m_projectionRotation = projectorBuilder.ProjectionRotation;

                m_savedProjection = projectorBuilder.ProjectedGrid;
                KeepProjection = projectorBuilder.KeepProjection;
            }

            m_showOnlyBuildable = projectorBuilder.ShowOnlyBuildable;
            InstantBuildingEnabled = projectorBuilder.InstantBuildingEnabled;
            MaxNumberOfProjections = projectorBuilder.MaxNumberOfProjections;
            MaxNumberOfBlocksPerProjection = projectorBuilder.MaxNumberOfBlocks;
            GetOwnershipFromProjector = projectorBuilder.GetOwnershipFromProjector;

            m_projectionsRemaining = MathHelper.Clamp(projectorBuilder.ProjectionsRemaining, 0, m_maxNumberOfProjections);

			
            IsWorkingChanged += MyProjector_IsWorkingChanged;
            sinkComp.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
			ResourceSink.Update();
            m_statsDirty = true;
            UpdateText();

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            CubeGrid.OnBlockAdded += previewGrid_OnBlockAdded;
            CubeGrid.OnBlockRemoved += previewGrid_OnBlockRemoved;
        
            CubeGrid.OnGridSplit += CubeGrid_OnGridSplit;
        }

        private void InitializeClipboard()
        {
            m_clipboard.ResetGridOrientation();
            m_shouldUpdateProjection = true;
            if (!m_clipboard.IsActive && !IsActivating)
            {
                IsActivating = true;
                m_clipboard.Activate(delegate()
                {
                    if (m_clipboard.PreviewGrids.Count != 0)
                        ProjectedGrid.Projector = this;
                    m_shouldUpdateProjection = true;
                    m_shouldUpdateTexts = true;

                    m_clipboard.ActuallyTestPlacement();

                    SetRotation(m_clipboard, m_projectionRotation);

                    NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
                    IsActivating = false;
                });
            }
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_ProjectorBase objectBuilder = (MyObjectBuilder_ProjectorBase)base.GetObjectBuilderCubeBlock(copy);
            if (m_clipboard != null && m_clipboard.CopiedGrids != null && m_clipboard.CopiedGrids.Count > 0 && m_originalGridBuilder != null)
            {
                if (copy)
                {
                    var clone = (MyObjectBuilder_CubeGrid)m_originalGridBuilder.Clone();
                    MyEntities.RemapObjectBuilder(clone);
                    objectBuilder.ProjectedGrid = clone;
                }
                else
                {
                    objectBuilder.ProjectedGrid = m_originalGridBuilder;
                }
                objectBuilder.ProjectionOffset = m_projectionOffset;
                objectBuilder.ProjectionRotation = m_projectionRotation;
                objectBuilder.KeepProjection = m_keepProjection;
            }
            else
            {
                if (objectBuilder.ProjectedGrid == null && m_savedProjection != null && CubeGrid.Projector == null)
                {
                    objectBuilder.ProjectedGrid = m_savedProjection;
                    objectBuilder.ProjectionOffset = m_projectionOffset;
                    objectBuilder.ProjectionRotation = m_projectionRotation;
                    objectBuilder.KeepProjection = m_keepProjection;
                }
                else
                {
                    objectBuilder.ProjectedGrid = null;
                }
            }
            
            objectBuilder.ShowOnlyBuildable = m_showOnlyBuildable;
            objectBuilder.InstantBuildingEnabled = m_instantBuildingEnabled;
            objectBuilder.MaxNumberOfProjections = m_maxNumberOfProjections;
            objectBuilder.MaxNumberOfBlocks = m_maxNumberOfBlocksPerProjection;
            objectBuilder.ProjectionsRemaining = m_projectionsRemaining;
            objectBuilder.GetOwnershipFromProjector = m_getOwnershipFromProjector;

            return objectBuilder;
        }
        #endregion

        #region Stats
        int m_remainingBlocks = 0;
        int m_totalBlocks = 0;
        Dictionary<MyCubeBlockDefinition, int> m_remainingBlocksPerType = new Dictionary<MyCubeBlockDefinition, int>();
        int m_remainingArmorBlocks = 0;
        int m_buildableBlocksCount = 0;
        bool m_statsDirty = false;

        private void UpdateStats()
        {
            ProfilerShort.Begin("Updating stats");
            m_totalBlocks = ProjectedGrid.CubeBlocks.Count;

            m_remainingArmorBlocks = 0;
            m_remainingBlocksPerType.Clear();

            foreach (var projectedBlock in ProjectedGrid.CubeBlocks)
            {
                Vector3 worldPosition = ProjectedGrid.GridIntegerToWorld(projectedBlock.Position);
                Vector3I realPosition = CubeGrid.WorldToGridInteger(worldPosition);
                var realBlock = CubeGrid.GetCubeBlock(realPosition);
                if (realBlock == null || projectedBlock.BlockDefinition.Id != realBlock.BlockDefinition.Id)
                {
                    if (projectedBlock.FatBlock == null)
                    {
                        m_remainingArmorBlocks++;
                    }
                    else
                    {
                        if (!m_remainingBlocksPerType.ContainsKey(projectedBlock.BlockDefinition))
                        {
                            m_remainingBlocksPerType.Add(projectedBlock.BlockDefinition, 1);
                        }
                        else
                        {
                            m_remainingBlocksPerType[projectedBlock.BlockDefinition]++;
                        }
                    }
                }
            }
            ProfilerShort.End();
        }
        #endregion

        #region Update & Events
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

			ResourceSink.Update();
            if (m_removeRequested)
            {
                m_frameCount++;
                if (m_frameCount > 10)
                {
                    UpdateIsWorking();
                    if (!IsWorking && IsProjecting())
                    {
                        RemoveProjection(true);
                    }
                    m_frameCount = 0;
                    m_removeRequested = false;
                }
            }


            if (m_clipboard.IsActive)
            {
                m_clipboard.Update();

                if (!MySandboxGame.IsDedicated)
                {
                    HideIntersectedBlock();
                }

                if (m_shouldUpdateProjection)
                {
                    UpdateProjection();
                    m_shouldUpdateProjection = false;
                }
            }
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            if (m_clipboard.IsActive && m_instantBuildingEnabled)
            {
                m_clipboard.ActuallyTestPlacement();
            }
        }

        private void UpdateProjection()
        {
            if (m_instantBuildingEnabled)
            {
                if (ProjectedGrid != null)
                {
                    foreach (var projectedBlock in ProjectedGrid.CubeBlocks)
                    {
                        ShowCube(projectedBlock, true);
                    }

                    m_clipboard.HasPreviewBBox = true;
                }
            }
            else
            {
                m_hiddenBlock = null;
                if (m_clipboard.PreviewGrids.Count == 0) return;

                m_remainingBlocks = ProjectedGrid.CubeBlocks.Count;

                ProjectedGrid.Render.Transparency = 0f;

                m_buildableBlocksCount = 0;

                m_visibleBlocks.Clear();
                m_buildableBlocks.Clear();
                m_hiddenBlocks.Clear();

                ProfilerShort.Begin("Update cube visibility");
                foreach (var projectedBlock in ProjectedGrid.CubeBlocks)
                {
                    Vector3 worldPosition = ProjectedGrid.GridIntegerToWorld(projectedBlock.Position);
                    Vector3I realPosition = CubeGrid.WorldToGridInteger(worldPosition);
                    var realBlock = CubeGrid.GetCubeBlock(realPosition);
                    if (realBlock != null && projectedBlock.BlockDefinition.Id == realBlock.BlockDefinition.Id)
                    {
                        m_hiddenBlocks.Add(projectedBlock);
                        m_remainingBlocks--;
                    }
                    else
                    {
                        bool canBuild = CanBuild(projectedBlock);
                        if (canBuild)
                        {
                            m_buildableBlocks.Add(projectedBlock);
                            m_buildableBlocksCount++;
                        }
                        else
                        {
                            if (m_showOnlyBuildable)
                            {
                                m_hiddenBlocks.Add(projectedBlock);
                            }
                            else
                            {
                                m_visibleBlocks.Add(projectedBlock);
                            }
                        }
                    }
                }

                foreach (var block in m_visibleBlocks)
                {
                    ShowCube(block, false);
                }
                foreach (var block in m_buildableBlocks)
                {
                    ShowCube(block, true);
                }
                foreach (var block in m_hiddenBlocks)
                {
                    HideCube(block);
                }
                ProfilerShort.End();


                if (m_remainingBlocks == 0 && !m_keepProjection)
                {
                    RemoveProjection(m_keepProjection);
                }
                else
                {
                    UpdateEmissivity();
                }

                m_statsDirty = true;
                if (m_shouldUpdateTexts)
                {
                    UpdateText();
                    m_shouldUpdateTexts = false;
                }

                m_clipboard.HasPreviewBBox = false;
            }
        }

        protected override void OnStopWorking()
        {
            UpdateEmissivity();
            base.OnStopWorking();
        }

        protected override void OnStartWorking()
        {
            UpdateEmissivity();
            base.OnStartWorking();
        }

        private void UpdateEmissivity()
        {
            UpdateIsWorking();
            if (IsWorking)
            {
                if (IsProjecting())
                {
                    MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Teal, Color.White);
                    if (m_soundEmitter != null && (m_soundEmitter.SoundId != BlockDefinition.PrimarySound.Arcade && m_soundEmitter.SoundId != BlockDefinition.PrimarySound.Realistic))
                    {
                        m_soundEmitter.StopSound(false);
                        m_soundEmitter.PlaySound(BlockDefinition.PrimarySound);
                    }
                }
                else
                {
                    MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
                    if (m_soundEmitter != null && (m_soundEmitter.SoundId != BlockDefinition.IdleSound.Arcade && m_soundEmitter.SoundId != BlockDefinition.IdleSound.Realistic))
                    {
                        m_soundEmitter.StopSound(false);
                        m_soundEmitter.PlaySound(BlockDefinition.IdleSound);
                    }
                }
            }
            else
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Red, Color.White);
            }
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        void UpdateText()
        {
            if (m_instantBuildingEnabled)
            {
                UpdateBaseText();
                if (m_clipboard.IsActive && ProjectedGrid != null)
                {
                    if (m_maxNumberOfBlocksPerProjection < MAX_NUMBER_OF_BLOCKS)
                    {
                        DetailedInfo.Append("\n");
                        DetailedInfo.Append("Ship blocks: " + (ProjectedGrid.BlocksCount) + "/" + m_maxNumberOfBlocksPerProjection);
                    }
                    if (m_maxNumberOfProjections < MAX_NUMBER_OF_PROJECTIONS)
                    {
                        DetailedInfo.Append("\n");
                        DetailedInfo.Append("Projections remaining: " + (m_projectionsRemaining) + "/" + m_maxNumberOfProjections);
                    }
                }
            }
            else
            {
                if (!m_statsDirty)
                {
                    return;
                }
                if (m_clipboard.IsActive)
                {
                    UpdateStats();
                }
                m_statsDirty = false;

                UpdateBaseText();

                if (m_clipboard.IsActive)
                {
                    DetailedInfo.Append("\n");
                    if (m_buildableBlocksCount > 0)
                    {
                        DetailedInfo.Append("\n");
                    }
                    else
                    {
                        DetailedInfo.Append("WARNING! Projection out of bounds!\n");
                    }
                    DetailedInfo.Append("Build progress: " + (m_totalBlocks - m_remainingBlocks) + "/" + m_totalBlocks);
                    if (m_remainingArmorBlocks > 0 || m_remainingBlocksPerType.Count != 0)
                    {
                        DetailedInfo.Append("\nBlocks remaining:\n");

                        DetailedInfo.Append("Armor blocks: " + m_remainingArmorBlocks);

                        foreach (var entry in m_remainingBlocksPerType)
                        {
                            DetailedInfo.Append("\n");
                            DetailedInfo.Append(entry.Key.DisplayNameText + ": " + entry.Value);
                        }
                    }
                    else
                    {
                        DetailedInfo.Append("\nComplete!");
                    }
                } 

                RaisePropertiesChanged();
            }
        }

        void UpdateBaseText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(BlockDefinition.RequiredPowerInput, DetailedInfo);
        }

        private void ShowNotification(MyStringId textToDisplay)
        {
            var debugNotification = new MyHudNotification(textToDisplay, 5000, level: MyNotificationLevel.Important);
            MyHud.Notifications.Add(debugNotification);
        }
        
        protected override void Closing()
        {
            base.Closing();

            CubeGrid.OnBlockAdded -= previewGrid_OnBlockAdded;
            CubeGrid.OnBlockRemoved -= previewGrid_OnBlockRemoved;

            if (m_clipboard.IsActive)
            {
                RemoveProjection(false);
            }
        }

        void CubeGrid_OnGridSplit(MyCubeGrid grid1, MyCubeGrid grid2)
        {
            if (m_originalGridBuilder != null && Sync.IsServer && MarkedForClose == false && Closed  == false)
            {
               SendRemap();
            }
        }

        public override void OnRegisteredToGridSystems()
        {
            if (m_originalGridBuilder != null && Sync.IsServer)
            {
                SendRemap();
            }
        }

        private void OnRemap(int seed)
        {
            if (m_originalGridBuilder != null)
            {
                using (MyRandom.Instance.PushSeed(seed))
                {
                    MyEntities.RemapObjectBuilder(m_originalGridBuilder);
                }
            }
        }

        private void PowerReceiver_IsPoweredChanged()
        {
            if (!ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && IsProjecting())
            {
                RequestRemoveProjection();
            }

            UpdateEmissivity();
        }

        private float CalculateRequiredPowerInput()
        {
            return BlockDefinition.RequiredPowerInput;
        }

        void MyProjector_IsWorkingChanged(MyCubeBlock obj)
        {
            if (!IsWorking && IsProjecting())
            {
                RequestRemoveProjection();
            }
            else
            {
             if (IsWorking && !IsProjecting())
                {
                    if (m_clipboard.HasCopiedGrids())
                    {
                        InitializeClipboard();
                    }
                }

                UpdateEmissivity();
            }
        }

        void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
            UpdateEmissivity();
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            //Only create projections from real projectors
            if (CubeGrid.Physics != null && m_savedProjection != null)
            {
                var clone = (MyObjectBuilder_CubeGrid)m_savedProjection.Clone();
                MyEntities.RemapObjectBuilder(clone);
                m_clipboard.ProcessCubeGrid(clone);

                m_clipboard.SetGridFromBuilder(clone, Vector3.Zero, 0f);
                m_originalGridBuilder = m_savedProjection;
                m_savedProjection = null;
                InitializeClipboard();
                
                //This will just issue the request
                //It will only remove it only if conditions are not met a few frames later
                RequestRemoveProjection();
            }
            UpdateEmissivity();
        }

        void previewGrid_OnBlockAdded(MySlimBlock obj)
        {
            m_shouldUpdateProjection = true;
            m_shouldUpdateTexts = true; // Text should always be updated, not only when terminal block is added, armor blocks etc also count!

            //Update groups
            if (m_originalGridBuilder == null || !IsProjecting())
            {
                return;
            }

            Vector3I transformed = ProjectedGrid.WorldToGridInteger(CubeGrid.GridIntegerToWorld(obj.Position));

            var terminalBlock = obj.FatBlock as MyTerminalBlock;
            if (terminalBlock != null)
            {
                foreach (var groupBuilder in m_originalGridBuilder.BlockGroups)
                {
                    foreach (var block in groupBuilder.Blocks)
                    {
                        if (transformed == block)
                        {
                            //Search if group already exits and add the terminal block to it
                            bool found = false;
                            for (int i = 0; i < CubeGrid.BlockGroups.Count; i++)
                            {
                                var group = CubeGrid.BlockGroups[i];
                                if (group.Name.ToString() == groupBuilder.Name)
                                {
                                    if (!group.Blocks.Contains(terminalBlock))
                                    {
                                        MyBlockGroup newGroup = new MyBlockGroup(CubeGrid);
                                        newGroup.Name = group.Name;
                                        newGroup.Blocks.Add(terminalBlock);
                                        newGroup.Blocks.AddList(group.Blocks);

                                        CubeGrid.RemoveGroup(group);

                                        CubeGrid.AddGroup(newGroup);
                                    }
                                    found = true;
                                    break;
                                }
                            }

                            //Group was not found
                            if (!found)
                            {
                                MyBlockGroup newGroup = new MyBlockGroup(CubeGrid);
                                newGroup.Name = new StringBuilder(groupBuilder.Name);
                                newGroup.Blocks.Add(terminalBlock);
                                CubeGrid.AddGroup(newGroup);
                            }
                        }
                    }
                }
            }
        }

        void previewGrid_OnBlockRemoved(MySlimBlock obj)
        {
            m_shouldUpdateProjection = true;
            m_shouldUpdateTexts = true;
        }


        [Event, Reliable, Server]
        private void OnSpawnProjection()
        {
            if (CanSpawnProjection())
            {
                var clone = (MyObjectBuilder_CubeGrid)m_originalGridBuilder.Clone();
                MyEntities.RemapObjectBuilder(clone);

                if (m_getOwnershipFromProjector)
                {
                    foreach (var block in clone.CubeBlocks)
                    {
                        block.Owner = OwnerId;
                        block.ShareMode = IDModule.ShareMode;
                    }
                }

                m_spawnClipboard.SetGridFromBuilder(clone, Vector3.Zero, 0f);

                m_spawnClipboard.ResetGridOrientation();
                if (!m_spawnClipboard.IsActive)
                {
                    m_spawnClipboard.Activate();
                }
                SetRotation(m_spawnClipboard, m_projectionRotation);
                m_spawnClipboard.Update();
                if (m_spawnClipboard.ActuallyTestPlacement() && m_spawnClipboard.PasteGrid())
                {
                    OnConfirmSpawnProjection();
                }
                    
                m_spawnClipboard.Deactivate();
                m_spawnClipboard.Clear();
            }
        }

        [Event, Reliable, Server]
        private void OnConfirmSpawnProjection()
        {
            if (m_maxNumberOfProjections < MAX_NUMBER_OF_PROJECTIONS)
            {
                Debug.Assert(m_projectionsRemaining > 0);
                m_projectionsRemaining--;

            }
            if (!m_keepProjection)
            {
                RemoveProjection(false);
            }

            UpdateText();
            RaisePropertiesChanged();
        }

        void m_instantBuildingEnabled_ValueChanged(SyncBase obj)
        {
            m_shouldUpdateProjection = true;
            if (m_instantBuildingEnabled)
                m_projectionsRemaining = m_maxNumberOfProjections;

            RaisePropertiesChanged();
        }

        void m_maxNumberOfProjections_ValueChanged(SyncBase obj)
        {
            m_projectionsRemaining = m_maxNumberOfProjections;
            RaisePropertiesChanged();
        }

        void m_maxNumberOfBlocksPerProjection_ValueChanged(SyncBase obj)
        {
            RaisePropertiesChanged();
        }

        void m_getOwnershipFromProjector_ValueChanged(SyncBase obj)
        {
            RaisePropertiesChanged();
        }

        #endregion

        #region Building
        private bool CanBuild(MySlimBlock cubeBlock)
        {
            ProfilerShort.Begin("CheckBuild");
            var canBuild = CanBuild(cubeBlock, false);
            ProfilerShort.End();
            return canBuild == BuildCheckResult.OK;
        }

        public BuildCheckResult CanBuild(MySlimBlock projectedBlock, bool checkHavokIntersections)
        {
            MyBlockOrientation blockOrientation = projectedBlock.Orientation;
            
            //GR: For rotation take into account:
            //the projected block orientation
            Quaternion blockOrientationQuat;
            blockOrientation.GetQuaternion(out blockOrientationQuat);

            //GR: The projector block orientation (which is relative to the Cubegrid orientation)
            Quaternion projQuat = Quaternion.Identity;
            Orientation.GetQuaternion(out projQuat);

            //AB: The orienation settings of the projector 
            //Take into account order of multiplication to review!
            blockOrientationQuat = Quaternion.Multiply(Quaternion.Multiply(projQuat, ProjectionRotationQuaternion), blockOrientationQuat);

            Vector3I projectedMin = CubeGrid.WorldToGridInteger(projectedBlock.CubeGrid.GridIntegerToWorld(projectedBlock.Min));
            Vector3I projectedMax = CubeGrid.WorldToGridInteger(projectedBlock.CubeGrid.GridIntegerToWorld(projectedBlock.Max));
            Vector3I blockPos = CubeGrid.WorldToGridInteger(projectedBlock.CubeGrid.GridIntegerToWorld(projectedBlock.Position));

            Vector3I min = new Vector3I(Math.Min(projectedMin.X, projectedMax.X), Math.Min(projectedMin.Y, projectedMax.Y), Math.Min(projectedMin.Z, projectedMax.Z));
            Vector3I max = new Vector3I(Math.Max(projectedMin.X, projectedMax.X), Math.Max(projectedMin.Y, projectedMax.Y), Math.Max(projectedMin.Z, projectedMax.Z));

            projectedMin = min;
            projectedMax = max;

            if (!CubeGrid.CanAddCubes(projectedMin, projectedMax))
            {
                return BuildCheckResult.IntersectedWithGrid;
            }

            MyGridPlacementSettings settings = new MyGridPlacementSettings();
            settings.SnapMode = SnapMode.OneFreeAxis;

			var mountPoints = projectedBlock.BlockDefinition.GetBuildProgressModelMountPoints(1.0f);
			bool isConnected = MyCubeGrid.CheckConnectivity(this.CubeGrid, projectedBlock.BlockDefinition, mountPoints,
															ref blockOrientationQuat, ref blockPos);
            if (isConnected)
            {
                if (CubeGrid.GetCubeBlock(blockPos) == null)
                {
                    if (checkHavokIntersections)
                    {
                        if (MyCubeGrid.TestPlacementAreaCube(CubeGrid, ref settings, projectedMin, projectedMax, blockOrientation, projectedBlock.BlockDefinition, CubeGrid))
                        {
                            return BuildCheckResult.OK;
                        }
                        else
                        {
                            return BuildCheckResult.IntersectedWithSomethingElse;
                        }
                    }
                    else
                    {
                        return BuildCheckResult.OK;
                    }
                }
                else
                {
                    return BuildCheckResult.AlreadyBuilt;
                }
            }
            else
            {
                return BuildCheckResult.NotConnected;
            }
        }

        public void Build(MySlimBlock cubeBlock, long owner, long builder, bool requestInstant = true, long builtBy = 0)
        {
            Quaternion quat = Quaternion.Identity;
            var orientation = cubeBlock.Orientation;

            Quaternion projQuat = Quaternion.Identity;
            Orientation.GetQuaternion(out projQuat);
            orientation.GetQuaternion(out quat);
            quat = Quaternion.Multiply(ProjectionRotationQuaternion, quat);
            quat = Quaternion.Multiply(projQuat, quat);

            var projectorGrid = CubeGrid;
            var projectedGrid = cubeBlock.CubeGrid;

            Vector3I cubeMin = cubeBlock.FatBlock != null ? cubeBlock.FatBlock.Min : cubeBlock.Position;
            Vector3I cubeMax = cubeBlock.FatBlock != null ? cubeBlock.FatBlock.Max : cubeBlock.Position;

            Vector3I min = projectorGrid.WorldToGridInteger(projectedGrid.GridIntegerToWorld(cubeMin));
            Vector3I max = projectorGrid.WorldToGridInteger(projectedGrid.GridIntegerToWorld(cubeMax));
            Vector3I pos = projectorGrid.WorldToGridInteger(projectedGrid.GridIntegerToWorld(cubeBlock.Position));

            Vector3I projectedMin = new Vector3I(Math.Min(min.X, max.X), Math.Min(min.Y, max.Y), Math.Min(min.Z, max.Z));
            Vector3I projectedMax = new Vector3I(Math.Max(min.X, max.X), Math.Max(min.Y, max.Y), Math.Max(min.Z, max.Z));


            MyCubeGrid.MyBlockLocation location = new MyCubeGrid.MyBlockLocation(cubeBlock.BlockDefinition.Id, projectedMin, projectedMax, pos, quat, 0, owner);

            MyObjectBuilder_CubeBlock objectBuilder = null;

            if (m_originalGridBuilder != null)
            {
                //Find original grid builder
                foreach (var blockBuilder in m_originalGridBuilder.CubeBlocks)
                {
                    if ((Vector3I)blockBuilder.Min == cubeMin && blockBuilder.GetId() == cubeBlock.BlockDefinition.Id)
                    {
                        objectBuilder = (MyObjectBuilder_CubeBlock)blockBuilder.Clone();
                        objectBuilder.SetupForProjector();
                    }
                }
            }

            if (objectBuilder == null)
            {
                //Original object builder not found because projector was destroyed
                //System.Diagnostics.Debug.Fail("Original object builder could not be found! (AlexFlorea)");
                objectBuilder = cubeBlock.GetObjectBuilder();
                location.EntityId = MyEntityIdentifier.AllocateId();
            }

            objectBuilder.ConstructionInventory = null;
            objectBuilder.BuiltBy = builtBy;
            bool buildInstant = requestInstant && MySession.Static.CreativeToolsEnabled(Sync.MyId);
            MyMultiplayer.RaiseEvent(projectorGrid, x => x.BuildBlockRequest, cubeBlock.ColorMaskHSV.PackHSVToUint(), location, objectBuilder, builder, buildInstant, owner);
            HideCube(cubeBlock);
        }
        #endregion

        #region Sync
        internal void SetNewBlueprint(MyObjectBuilder_CubeGrid gridBuilder)
        {
            m_originalGridBuilder = gridBuilder;

            var clone = m_originalGridBuilder;//(MyObjectBuilder_CubeGrid)gridBuilder.Clone();

            //MyEntities.RemapObjectBuilder(clone);
            //m_clipboard.ProcessCubeGrid(clone);

            m_clipboard.SetGridFromBuilder(clone, Vector3.Zero, 0f);

            if (m_instantBuildingEnabled)
            {
                ResetRotation();
                var boundingBox = clone.CalculateBoundingBox();
                // Add 1 to get the center out of the bounds and another 1 for a gap
                m_projectionOffset.Y = Math.Abs((int)(boundingBox.Min.Y / MyDefinitionManager.Static.GetCubeSize(clone.GridSizeEnum))) + 2;
            }

            InitializeClipboard();
        }

        internal void SetNewOffset(Vector3I positionOffset, Vector3I rotationOffset, bool onlyCanBuildBlock)
        {
            m_clipboard.ResetGridOrientation();

            m_projectionOffset = positionOffset;
            m_projectionRotation = rotationOffset;
            m_showOnlyBuildable = onlyCanBuildBlock;

            SetRotation(m_clipboard, m_projectionRotation);
        }

        private void SendNewBlueprint(MyObjectBuilder_CubeGrid projectedGrid)
        {
            SetNewBlueprint(projectedGrid);
            MyMultiplayer.RaiseEvent(this, x => x.OnNewBlueprintSuccess, projectedGrid);
        }

        [Event, Reliable, Server, BroadcastExcept]
        private void OnNewBlueprintSuccess(MyObjectBuilder_CubeGrid projectedGrid)
        {
            if (MyEventContext.Current.IsLocallyInvoked == false)
            {
                if (!MySession.Static.IsUserScripter(MyEventContext.Current.Sender.Value))
                {
                    if (RemoveScriptsFromProjection(ref projectedGrid))
                        MyMultiplayer.RaiseEvent(this, x => x.ShowScriptRemoveMessage, MyEventContext.Current.Sender);
                }
                SetNewBlueprint(projectedGrid);
            }
        }

        private bool RemoveScriptsFromProjection(ref MyObjectBuilder_CubeGrid grid)
        {
            bool found = false;
            foreach (var block in grid.CubeBlocks)
            {
                var programmable = block as MyObjectBuilder_MyProgrammableBlock;
                if (programmable == null)
                    continue;

                if (programmable.Program != null)
                {
                    programmable.Program = null;
                    found = true;
                }
            }
            return found;
        }

        [Event, Reliable, Client]
        private void ShowScriptRemoveMessage()
        {
            MyHud.Notifications.Add(new MyHudNotification(MySpaceTexts.Notification_BlueprintScriptRemoved, 5000, MyFontEnum.Red));
        }

        public void SendNewOffset(Vector3I positionOffset, Vector3I rotationOffset, bool showOnlyBuildable)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnOffsetChangedSuccess, positionOffset, rotationOffset, showOnlyBuildable);
        }


        [Event, Reliable, Server, Broadcast]
        private void OnOffsetChangedSuccess(Vector3I positionOffset, Vector3I rotationOffset, bool showOnlyBuildable)
        {   
            SetNewOffset(positionOffset, rotationOffset, showOnlyBuildable);
            m_shouldUpdateProjection = true;
        }

        public void SendRemoveProjection()
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnRemoveProjectionRequest);
        }

        [Event, Reliable, Server, Broadcast]
        private void OnRemoveProjectionRequest()
        {
            RemoveProjection(false);
        }

        private void SendRemap()
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnRemapRequest);
        }

        [Event, Reliable, Server]
        private void OnRemapRequest()
        {
            int randomSeed = MyRandom.Instance.CreateRandomSeed();
            OnRemap(randomSeed);
            MyMultiplayer.RaiseEvent(this, x => x.OnRemapSuccess, randomSeed);
        }

        [Event, Reliable, Broadcast]
        private void OnRemapSuccess(int seed)
        {
            OnRemap(seed);
        }

        private void SendSpawnProjection()
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnSpawnProjection);
        }

        private void SendConfirmSpawnProjection()
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnConfirmSpawnProjection);
        }

        #endregion
    }
}
