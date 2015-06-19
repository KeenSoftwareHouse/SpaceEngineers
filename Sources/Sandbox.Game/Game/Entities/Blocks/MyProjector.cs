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
using VRage;
using VRage.FileSystem;
using VRage.Utils;
using Sandbox.Game.Localization;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Projector))]
    public class MyProjector : MyFunctionalBlock, IMyPowerConsumer, ModAPI.Ingame.IMyProjector
    {
        public enum BuildCheckResult
        {
            OK,
            NotConnected,
            IntersectedWithGrid,
            IntersectedWithSomethingElse,
            AlreadyBuilt,
            NotFound,
        }

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
        
        private Vector3I m_projectionOffset;
        private Vector3I m_projectionRotation;

        private MySlimBlock m_hiddenBlock;

        private bool m_shouldUpdateProjection = false;
        private bool m_shouldUpdateTexts = false;

        private MyObjectBuilder_CubeGrid m_savedProjection;

        private new MySyncProjector SyncObject;

        private bool m_keepProjection = false;

        private bool m_showOnlyBuildable = false;

        //Projector needs to wait some frames before it can ask if it is powered.
        private int m_frameCount = 0;
        private bool m_removeRequested = false;

        public MyPowerReceiver PowerReceiver
        {
            get;
            protected set;
        }
        
        public MyProjector()
            : base()
        {
            m_clipboard = new MyProjectorClipboard(this);
            m_spawnClipboard = new MyProjectorClipboard(this);
        }

        private MyCubeGrid ProjectedGrid 
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

        private const int MAX_NUMBER_OF_PROJECTIONS = 1000;
        private const int MAX_NUMBER_OF_BLOCKS = 10000;
        private bool m_instantBuildingEnabled = false;
        private int m_maxNumberOfProjections = 0;
        private int m_maxNumberOfBlocksPerProjection = 0;
        private int m_projectionsRemaining = 0;

        static MyProjector()
        {
            if (!MyFakes.ENABLE_PROJECTOR_BLOCK)
            {
                return;
            }

            var blueprintBtn = new MyTerminalControlButton<MyProjector>("Blueprint", MySpaceTexts.Blueprints, MySpaceTexts.Blank, (p) => p.SelectBlueprint());
            blueprintBtn.Enabled = (b) => b.CanProject();
            blueprintBtn.SupportsMultipleBlocks = false;

            MyTerminalControlFactory.AddControl(blueprintBtn);

            var removeBtn = new MyTerminalControlButton<MyProjector>("Remove", MySpaceTexts.RemoveProjectionButton, MySpaceTexts.Blank, (p) => p.SyncObject.SendRemoveProjection());
            removeBtn.Enabled = (b) => b.IsProjecting();
            MyTerminalControlFactory.AddControl(removeBtn);

            var keepProjectionToggle = new MyTerminalControlCheckbox<MyProjector>("KeepProjection", MySpaceTexts.KeepProjectionToggle, MySpaceTexts.KeepProjectionTooltip);
            keepProjectionToggle.Getter = (x) => x.m_keepProjection;
            keepProjectionToggle.Setter = (x, v) =>
                {
                    x.SyncObject.SendNewKeepProjection(v);
                };
            keepProjectionToggle.EnableAction();
            keepProjectionToggle.Enabled = (b) => b.IsProjecting();
            MyTerminalControlFactory.AddControl(keepProjectionToggle);

            //ShowOnlyBuildable
            var showOnlyBuildableBlockToggle = new MyTerminalControlCheckbox<MyProjector>("ShowOnlyBuildable", MySpaceTexts.ShowOnlyBuildableBlockToggle, MySpaceTexts.ShowOnlyBuildableTooltip);
            showOnlyBuildableBlockToggle.Getter = (x) => x.m_showOnlyBuildable;
            showOnlyBuildableBlockToggle.Setter = (x, v) =>
            {
                x.m_showOnlyBuildable = v;
                x.OnOffsetsChanged();
            };
            showOnlyBuildableBlockToggle.Enabled = (b) => b.IsProjecting();
            MyTerminalControlFactory.AddControl(showOnlyBuildableBlockToggle);

            //Position
            var offsetX = new MyTerminalControlSlider<MyProjector>("X", MySpaceTexts.BlockPropertyTitle_ProjectionOffsetX, MySpaceTexts.Blank);
            offsetX.SetLimits(-50, 50);
            offsetX.DefaultValue = 0;
            offsetX.Getter = (x) => x.m_projectionOffset.X;
            offsetX.Setter = (x, v) =>
            {
                x.m_projectionOffset.X = Convert.ToInt32(v);
                x.OnOffsetsChanged();
            };
            offsetX.Writer = (x, result) => result.AppendInt32((int)(x.m_projectionOffset.X));
            offsetX.EnableActions(step: 0.01f);
            offsetX.Enabled = (x) => x.IsProjecting();
            MyTerminalControlFactory.AddControl(offsetX);

            var offsetY = new MyTerminalControlSlider<MyProjector>("Y", MySpaceTexts.BlockPropertyTitle_ProjectionOffsetY, MySpaceTexts.Blank);
            offsetY.SetLimits(-50, 50);
            offsetY.DefaultValue = 0;
            offsetY.Getter = (x) => x.m_projectionOffset.Y;
            offsetY.Setter = (x, v) =>
            {
                x.m_projectionOffset.Y = Convert.ToInt32(v);
                x.OnOffsetsChanged();
            };
            offsetY.Writer = (x, result) => result.AppendInt32((int)(x.m_projectionOffset.Y));
            offsetY.EnableActions(step: 0.01f);
            offsetY.Enabled = (x) => x.IsProjecting();
            MyTerminalControlFactory.AddControl(offsetY);

            var offsetZ = new MyTerminalControlSlider<MyProjector>("Z", MySpaceTexts.BlockPropertyTitle_ProjectionOffsetZ, MySpaceTexts.Blank);
            offsetZ.SetLimits(-50, 50);
            offsetZ.DefaultValue = 0;
            offsetZ.Getter = (x) => x.m_projectionOffset.Z;
            offsetZ.Setter = (x, v) =>
            {
                x.m_projectionOffset.Z = Convert.ToInt32(v);
                x.OnOffsetsChanged();
            };
            offsetZ.Writer = (x, result) => result.AppendInt32((int)(x.m_projectionOffset.Z));
            offsetZ.EnableActions(step: 0.01f);
            offsetZ.Enabled = (x) => x.IsProjecting();
            MyTerminalControlFactory.AddControl(offsetZ);

            //Rotation

            var rotationX = new MyTerminalControlSlider<MyProjector>("RotX", MySpaceTexts.BlockPropertyTitle_ProjectionRotationX, MySpaceTexts.Blank);
            rotationX.SetLimits(-2, 2);
            rotationX.DefaultValue = 0;
            rotationX.Getter = (x) => x.m_projectionRotation.X;
            rotationX.Setter = (x, v) =>
            {
                x.m_projectionRotation.X = Convert.ToInt32(v);
                x.OnOffsetsChanged();
            };
            rotationX.Writer = (x, result) => result.AppendInt32((int)x.m_projectionRotation.X * 90).Append("°");
            rotationX.EnableActions(step: 0.2f);
            rotationX.Enabled = (x) => x.IsProjecting();
            MyTerminalControlFactory.AddControl(rotationX);

            var rotationY = new MyTerminalControlSlider<MyProjector>("RotY", MySpaceTexts.BlockPropertyTitle_ProjectionRotationY, MySpaceTexts.Blank);
            rotationY.SetLimits(-2, 2);
            rotationY.DefaultValue = 0;
            rotationY.Getter = (x) => x.m_projectionRotation.Y;
            rotationY.Setter = (x, v) =>
            {
                x.m_projectionRotation.Y = Convert.ToInt32(v);
                x.OnOffsetsChanged();
            };
            rotationY.Writer = (x, result) => result.AppendInt32((int)x.m_projectionRotation.Y * 90).Append("°");
            rotationY.EnableActions(step: 0.2f);
            rotationY.Enabled = (x) => x.IsProjecting();
            MyTerminalControlFactory.AddControl(rotationY);

            var rotationZ = new MyTerminalControlSlider<MyProjector>("RotZ", MySpaceTexts.BlockPropertyTitle_ProjectionRotationZ, MySpaceTexts.Blank);
            rotationZ.SetLimits(-2, 2);
            rotationZ.DefaultValue = 0;
            rotationZ.Getter = (x) => x.m_projectionRotation.Z;
            rotationZ.Setter = (x, v) =>
            {
                x.m_projectionRotation.Z = Convert.ToInt32(v);
                x.OnOffsetsChanged();
            };
            rotationZ.Writer = (x, result) => result.AppendInt32((int)x.m_projectionRotation.Z * 90).Append("°");
            rotationZ.EnableActions(step: 0.2f);
            rotationZ.Enabled = (x) => x.IsProjecting();
            MyTerminalControlFactory.AddControl(rotationZ);

            var scenarioSettingsSeparator = new MyTerminalControlSeparator<MyProjector>();
            scenarioSettingsSeparator.Visible = (p) => p.ScenarioSettingsEnabled();
            MyTerminalControlFactory.AddControl(scenarioSettingsSeparator);

            var scenarioSettingsLabel = new MyTerminalControlLabel<MyProjector>("ScenarioLabel", MySpaceTexts.TerminalScenarioSettingsLabel);
            scenarioSettingsLabel.Visible = (p) => p.ScenarioSettingsEnabled();
            MyTerminalControlFactory.AddControl(scenarioSettingsLabel);

            var spawnProjectionButton = new MyTerminalControlButton<MyProjector>("SpawnProjection", MySpaceTexts.BlockPropertyTitle_ProjectionSpawn, MySpaceTexts.Blank, (p) => p.TrySpawnProjection());
            spawnProjectionButton.Visible = (p) => p.ScenarioSettingsEnabled();
            spawnProjectionButton.Enabled = (p) => p.CanSpawnProjection();
            spawnProjectionButton.EnableAction();
            MyTerminalControlFactory.AddControl(spawnProjectionButton);

            var instantBuildingCheckbox = new MyTerminalControlCheckbox<MyProjector>("InstantBuilding", MySpaceTexts.BlockPropertyTitle_Projector_InstantBuilding, MySpaceTexts.BlockPropertyTitle_Projector_InstantBuilding_Tooltip);
            instantBuildingCheckbox.Visible = (p) => p.ScenarioSettingsEnabled();
            instantBuildingCheckbox.Enabled = (p) => p.CanEnableInstantBuilding();
            instantBuildingCheckbox.Getter = (p) => p.m_instantBuildingEnabled;
            instantBuildingCheckbox.Setter = (p, v) => p.TrySetInstantBuilding(v);
            MyTerminalControlFactory.AddControl(instantBuildingCheckbox);

            var numberOfProjections = new MyTerminalControlSlider<MyProjector>("NumberOfProjections", MySpaceTexts.BlockPropertyTitle_Projector_NumberOfProjections, MySpaceTexts.BlockPropertyTitle_Projector_NumberOfProjections_Tooltip);
            numberOfProjections.Visible = (p) => p.ScenarioSettingsEnabled();
            numberOfProjections.Enabled = (p) => p.CanEditInstantBuildingSettings();
            numberOfProjections.Getter = (p) => p.m_maxNumberOfProjections;
            numberOfProjections.Setter = (p, v) => p.TryChangeNumberOfProjections(v);
            numberOfProjections.Writer = (p, s) =>
                {
                    if (p.m_maxNumberOfProjections == MAX_NUMBER_OF_PROJECTIONS)
                    {
                        s.AppendStringBuilder(MyTexts.Get(MySpaceTexts.ScreenTerminal_Infinite));
                    }
                    else
                    {
                        s.AppendInt32(p.m_maxNumberOfProjections);
                    }
                };
            numberOfProjections.SetLogLimits(1, MAX_NUMBER_OF_PROJECTIONS);
            MyTerminalControlFactory.AddControl(numberOfProjections);

            var numberOfBlocks = new MyTerminalControlSlider<MyProjector>("NumberOfBlocks", MySpaceTexts.BlockPropertyTitle_Projector_BlocksPerProjection, MySpaceTexts.BlockPropertyTitle_Projector_BlocksPerProjection_Tooltip);
            numberOfBlocks.Visible = (p) => p.ScenarioSettingsEnabled();
            numberOfBlocks.Enabled = (p) => p.CanEditInstantBuildingSettings();
            numberOfBlocks.Getter = (p) => p.m_maxNumberOfBlocksPerProjection;
            numberOfBlocks.Setter = (p, v) => p.TryChangeMaxNumberOfBlocksPerProjection(v);
            numberOfBlocks.Writer = (p, s) =>
                {
                    if (p.m_maxNumberOfBlocksPerProjection == MAX_NUMBER_OF_BLOCKS)
                    {
                        s.AppendStringBuilder(MyTexts.Get(MySpaceTexts.ScreenTerminal_Infinite));
                    }
                    else
                    {
                        s.AppendInt32(p.m_maxNumberOfBlocksPerProjection);
                    }
                };
            numberOfBlocks.SetLogLimits(1, MAX_NUMBER_OF_BLOCKS);
            MyTerminalControlFactory.AddControl(numberOfBlocks);
        }


        #region UI
        
        private bool IsProjecting()
        {
            return m_clipboard.IsActive;
        }

        //This also updates the texts because there is no proper event that is called when the projector is showed.
        private bool CanProject()
        {
            UpdateIsWorking();
            UpdateText();
            return IsWorking;
        }

        private void OnOffsetsChanged()
        {
            m_shouldUpdateProjection = true;
            m_shouldUpdateTexts = true;
            SyncObject.SendNewOffset(m_projectionOffset, m_projectionRotation, m_showOnlyBuildable);

            //We need to remap because the after the movement, blocks that were already built can be built again
            SyncObject.SendRemap();
        }

        private void SelectBlueprint()
        {
            if (MyGuiScreenTerminal.IsOpen)
            {
                MyGuiScreenTerminal.Hide();
            }

            RemoveProjection(false);
            var blueprintScreen = new MyGuiBlueprintScreen(m_clipboard);
            blueprintScreen.Closed += blueprintScreen_Closed;
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

        void blueprintScreen_Closed(MyGuiScreenBase source)
        {
            PowerReceiver.Update();
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

            m_originalGridBuilder = (MyObjectBuilder_CubeGrid)m_clipboard.CopiedGrids[largestGridIndex].Clone();

            m_clipboard.ProcessCubeGrid(m_clipboard.CopiedGrids[largestGridIndex]);

            MyEntities.RemapObjectBuilder(m_originalGridBuilder);
            SyncObject.SendNewBlueprint(m_originalGridBuilder);
        }

        private bool ScenarioSettingsEnabled()
        {
            return MySession.Static.Settings.ScenarioEditMode || MySession.Static.IsScenario;
        }

        private bool CanEditInstantBuildingSettings()
        {
            return CanEnableInstantBuilding() && m_instantBuildingEnabled;
        }

        private bool CanEnableInstantBuilding()
        {
            return MySession.Static.Settings.ScenarioEditMode;
        }

        private bool CanSpawnProjection()
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

        private void TrySetInstantBuilding(bool v)
        {
            if (CanEnableInstantBuilding())
            {
                SyncObject.SendNewInstantBuilding(v);
            }
        }

        private void TrySpawnProjection()
        {
            if (CanSpawnProjection())
            {
                SyncObject.SendSpawnProjection();
            }
        }

        private void TryChangeMaxNumberOfBlocksPerProjection(float v)
        {
            if (CanEditInstantBuildingSettings())
            {
                SyncObject.SendNewMaxNumberOfBlocks((int)Math.Round(v));
            }
        }

        private void TryChangeNumberOfProjections(float v)
        {
            if (CanEditInstantBuildingSettings())
            {
                SyncObject.SendNewMaxNumberOfProjections((int)Math.Round(v));
            }
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

        private void SetTransparency(MySlimBlock cubeBlock, float transparency)
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
            foreach (var subpart in renderEntity.Subparts)
            {
                subpart.Value.Render.Transparency = transparency;
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

            var character = MySession.LocalCharacter;
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
            base.Init(objectBuilder, cubeGrid);
            if (!MyFakes.ENABLE_PROJECTOR_BLOCK)
            {
                return;
            }

            var projectorBuilder = (MyObjectBuilder_Projector)objectBuilder;
            if (projectorBuilder.ProjectedGrid != null)
            {
                m_projectionOffset = projectorBuilder.ProjectionOffset;
                m_projectionRotation = projectorBuilder.ProjectionRotation;

                m_savedProjection = projectorBuilder.ProjectedGrid;
                m_keepProjection = projectorBuilder.KeepProjection;
            }

            m_showOnlyBuildable = projectorBuilder.ShowOnlyBuildable;
            m_instantBuildingEnabled = projectorBuilder.InstantBuildingEnabled;
            m_maxNumberOfProjections = projectorBuilder.MaxNumberOfProjections;
            m_maxNumberOfBlocksPerProjection = projectorBuilder.MaxNumberOfBlocks;
            m_projectionsRemaining = MathHelper.Clamp(projectorBuilder.ProjectionsRemaining, 0, m_maxNumberOfProjections);

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Utility,
                false,
                BlockDefinition.RequiredPowerInput,
                this.CalculateRequiredPowerInput);

            PowerReceiver.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            IsWorkingChanged += MyProjector_IsWorkingChanged;

            PowerReceiver.Update();
            m_statsDirty = true;
            UpdateText();
            
            SyncObject = new MySyncProjector(this);

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            CubeGrid.OnBlockAdded += previewGrid_OnBlockAdded;
            CubeGrid.OnBlockRemoved += previewGrid_OnBlockRemoved;
        
            CubeGrid.OnGridSplit += CubeGrid_OnGridSplit;
        }

        private void InitializeClipboard()
        {
            m_clipboard.ResetGridOrientation();
            m_shouldUpdateProjection = true;
            if (!m_clipboard.IsActive)
            {
                m_clipboard.Activate();
            }

            if (m_clipboard.PreviewGrids.Count != 0)
                ProjectedGrid.Projector = this;
            m_shouldUpdateProjection = true;
            m_shouldUpdateTexts = true;

            m_clipboard.ActuallyTestPlacement();

            SetRotation(m_clipboard, m_projectionRotation);

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_Projector objectBuilder = (MyObjectBuilder_Projector)base.GetObjectBuilderCubeBlock(copy);
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
        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            
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

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            PowerReceiver.Update();
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

        private void UpdateEmissivity()
        {
            UpdateIsWorking();
            if (IsWorking)
            {
                if (IsProjecting())
                {
                    MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Teal, Color.White);
                }
                else
                {
                    MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
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
                    DetailedInfo.Append("\nBlocks remaining:\n");

                    DetailedInfo.Append("Armor blocks: " + m_remainingArmorBlocks);

                    foreach (var entry in m_remainingBlocksPerType)
                    {
                        DetailedInfo.Append("\n");
                        DetailedInfo.Append(entry.Key.DisplayNameText + ": " + entry.Value);
                    }
                }

                RaisePropertiesChanged();
            }
        }

        void UpdateBaseText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
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
            if (m_originalGridBuilder != null && Sync.IsServer)
            {
                SyncObject.SendRemap();
            }
        }

        public override void OnRegisteredToGridSystems()
        {
            if (m_originalGridBuilder != null && Sync.IsServer)
            {
                SyncObject.SendRemap();
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
            if (!PowerReceiver.IsPowered && IsProjecting())
            {
                RequestRemoveProjection();
            }

            UpdateEmissivity();
        }

        private float CalculateRequiredPowerInput()
        {
            return BlockDefinition.RequiredPowerInput;
        }
        
        protected override bool CheckIsWorking()
        {
            if (PowerReceiver != null && !PowerReceiver.IsPowered)
            {
                return false;
            }
            return Enabled && base.CheckIsWorking();
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
            PowerReceiver.Update();
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
                m_shouldUpdateTexts = true;
            }
        }

        void previewGrid_OnBlockRemoved(MySlimBlock obj)
        {
            m_shouldUpdateProjection = true;
            m_shouldUpdateTexts = true;
        }


        internal void OnSpawnProjection()
        {
            if (Sync.IsServer && CanSpawnProjection())
            {
                var clone = (MyObjectBuilder_CubeGrid)m_originalGridBuilder.Clone();
                MyEntities.RemapObjectBuilder(clone);
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

        internal void OnConfirmSpawnProjection()
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

        internal void OnSetMaxNumberOfBlocks(int maxNumber)
        {
            m_maxNumberOfBlocksPerProjection = maxNumber;

            RaisePropertiesChanged();
        }

        internal void OnSetMaxNumberOfProjections(int maxNumber)
        {
            m_maxNumberOfProjections = maxNumber;
            m_projectionsRemaining = m_maxNumberOfProjections;

            RaisePropertiesChanged();
        }

        internal void OnSetInstantBuilding(bool enabled)
        {
            m_instantBuildingEnabled = enabled;
            m_shouldUpdateProjection = true;

            if (enabled)
            {
                m_projectionsRemaining = m_maxNumberOfProjections;
            }
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
            
            Matrix local;
            blockOrientation.GetMatrix(out local);
            var gridOrientation = (m_clipboard as MyGridClipboard).GetFirstGridOrientationMatrix();
            if (gridOrientation != Matrix.Identity)
            {
                var afterRotation = Matrix.Multiply(local, gridOrientation);
                blockOrientation = new MyBlockOrientation(ref afterRotation);
            }
            
            Quaternion blockOrientationQuat;
            blockOrientation.GetQuaternion(out blockOrientationQuat);

            Quaternion projQuat = Quaternion.Identity;
            Orientation.GetQuaternion(out projQuat);
            blockOrientationQuat = Quaternion.Multiply(projQuat, blockOrientationQuat);

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
            settings.Mode = MyGridPlacementSettings.SnapMode.OneFreeAxis;

            bool canBuild = true;
            if (checkHavokIntersections)
            {
                canBuild = MyCubeGrid.TestPlacementAreaCube(CubeGrid, ref settings, projectedMin, projectedMax, blockOrientation, projectedBlock.BlockDefinition, CubeGrid);
            }

            bool isConnected = MyCubeGrid.CheckConnectivity(this.CubeGrid, projectedBlock.BlockDefinition, ref blockOrientationQuat, ref blockPos);

            if (!canBuild)
            {
                return BuildCheckResult.IntersectedWithSomethingElse;
            }
            else
            {
                if (isConnected)
                {
                    if (CubeGrid.GetCubeBlock(blockPos) == null)
                    {
                        return BuildCheckResult.OK;
                    }
                    else
                    {
                        return BuildCheckResult.AlreadyBuilt;
                    }
                }
            }

            return BuildCheckResult.NotConnected;
        }

        public void Build(MySlimBlock cubeBlock, long owner, long builder)
        {
            Quaternion quat = Quaternion.Identity;
            var orientation = cubeBlock.Orientation;

            Matrix local;
            orientation.GetMatrix(out local);
            var gridOrientation = m_clipboard.GetFirstGridOrientationMatrix();
            if (gridOrientation != Matrix.Identity)
            {
                var afterRotation = Matrix.Multiply(local, gridOrientation);
                orientation = new MyBlockOrientation(ref afterRotation);
            }

            Quaternion projQuat = Quaternion.Identity;
            Orientation.GetQuaternion(out projQuat);
            orientation.GetQuaternion(out quat);
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


            MyCubeGrid.MyBlockLocation location = new MyCubeGrid.MyBlockLocation(cubeBlock.BlockDefinition.Id, projectedMin, projectedMax, pos,
                quat, 0, owner);

            MyObjectBuilder_CubeBlock objectBuilder = null;
            //Find original grid builder
            foreach (var blockBuilder in m_originalGridBuilder.CubeBlocks)
            {
                if (blockBuilder.Min == cubeMin && blockBuilder.GetId() == cubeBlock.BlockDefinition.Id)
                {
                    objectBuilder = (MyObjectBuilder_CubeBlock)blockBuilder.Clone();
                    objectBuilder.SetupForProjector();
                }
            }

            if (objectBuilder == null)
            {
                System.Diagnostics.Debug.Fail("Original object builder could not be found! (AlexFlorea)");
                objectBuilder = cubeBlock.GetObjectBuilder();
                location.EntityId = MyEntityIdentifier.AllocateId();
            }

            objectBuilder.ConstructionInventory = null;
            projectorGrid.BuildBlock(cubeBlock.ColorMaskHSV, location, objectBuilder, builder);
            HideCube(cubeBlock);
        }
        #endregion

        #region Sync
        internal void SetNewBlueprint(MyObjectBuilder_CubeGrid gridBuilder)
        {
            m_originalGridBuilder = gridBuilder;
            
            var clone = (MyObjectBuilder_CubeGrid)gridBuilder.Clone();
            m_clipboard.ProcessCubeGrid(clone);

            MyEntities.RemapObjectBuilder(clone);
            m_clipboard.ProcessCubeGrid(clone);

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

        [PreloadRequired]
        class MySyncProjector
        {
            MyProjector m_projector;
            //Cached because server doesn't send this back
            MyObjectBuilder_CubeGrid m_lastSentCubeGrid;

            [ProtoBuf.ProtoContract]
            [MessageIdAttribute(7600, SteamSDK.P2PMessageEnum.Reliable)]
            protected struct NewBlueprintMsg : IEntityMessage
            {
                [ProtoBuf.ProtoMember]
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                [ProtoBuf.ProtoMember]
                public MyObjectBuilder_CubeGrid ProjectedGrid;
            }

            [MessageIdAttribute(7601, SteamSDK.P2PMessageEnum.Reliable)]
            protected struct NewBlueprintAckMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }
            }

            [MessageIdAttribute(7602, SteamSDK.P2PMessageEnum.Reliable)]
            protected struct OffsetMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public Vector3I PositionOffset;
                public Vector3I RotationOffset;
                public Byte showOnlyBuildable;
            }

            [MessageIdAttribute(7603, SteamSDK.P2PMessageEnum.Reliable)]
            protected struct KeepProjectionMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public BoolBlit KeepProjection;
            }

            [MessageIdAttribute(7604, SteamSDK.P2PMessageEnum.Reliable)]
            protected struct RemoveProjectionMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }
            }

            [MessageIdAttribute(7605, SteamSDK.P2PMessageEnum.Reliable)]
            protected struct RemapRequestMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }
            }

            [MessageIdAttribute(7606, SteamSDK.P2PMessageEnum.Reliable)]
            protected struct RemapSeedMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public int Seed;
            }

            [MessageIdAttribute(7607, SteamSDK.P2PMessageEnum.Reliable)]
            protected struct SetInstantBuildingMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public BoolBlit Enabled;
            }

            [MessageIdAttribute(7608, SteamSDK.P2PMessageEnum.Reliable)]
            protected struct SetMaxNumberOfProjectionsMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public int MaxNumber;
            }

            [MessageIdAttribute(7609, SteamSDK.P2PMessageEnum.Reliable)]
            protected struct SetMaxNumberOfBlocksMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public int MaxNumber;
            }

            [MessageIdAttribute(7610, SteamSDK.P2PMessageEnum.Reliable)]
            protected struct SpawnProjectionMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }
            }

            [MessageIdAttribute(7611, SteamSDK.P2PMessageEnum.Reliable)]
            protected struct ConfirmSpawnProjectionMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }
            }


            static MySyncProjector()
            {
                MySyncLayer.RegisterMessage<NewBlueprintMsg>(OnNewBlueprintRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterMessage<NewBlueprintMsg>(OnNewBlueprintSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
                MySyncLayer.RegisterMessage<NewBlueprintAckMsg>(OnNewBlueprintAck, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
                MySyncLayer.RegisterMessage<OffsetMsg>(OnOffsetChangedRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterMessage<OffsetMsg>(OnOffsetChangedSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
                MySyncLayer.RegisterMessage<KeepProjectionMsg>(OnKeepProjectionChangedRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterMessage<KeepProjectionMsg>(OnKeepProjectionChangedSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
                MySyncLayer.RegisterMessage<RemoveProjectionMsg>(OnRemoveProjectionRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterMessage<RemoveProjectionMsg>(OnRemoveProjectionSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
                MySyncLayer.RegisterMessage<RemapRequestMsg>(OnRemapRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterMessage<RemapSeedMsg>(OnRemapSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

                MySyncLayer.RegisterMessage<SetInstantBuildingMsg>(OnSetInstantBuilding, MyMessagePermissions.Any);
                MySyncLayer.RegisterMessage<SetMaxNumberOfProjectionsMsg>(OnSetMaxNumberOfProjections, MyMessagePermissions.Any);
                MySyncLayer.RegisterMessage<SetMaxNumberOfBlocksMsg>(OnSetMaxNumberOfBlocks, MyMessagePermissions.Any);
                MySyncLayer.RegisterMessage<SpawnProjectionMsg>(OnSpawnProjection, MyMessagePermissions.ToServer);
                MySyncLayer.RegisterMessage<ConfirmSpawnProjectionMsg>(OnConfirmSpawnProjection, MyMessagePermissions.FromServer);
            }

            public MySyncProjector(MyProjector projector)
            {
                m_projector = projector;
            }

            public void SendNewBlueprint(MyObjectBuilder_CubeGrid projectedGrid)
            {
                var msg = new NewBlueprintMsg();
                msg.EntityId = m_projector.EntityId;
                msg.ProjectedGrid = projectedGrid;

                var ack = new NewBlueprintAckMsg();
                ack.EntityId = m_projector.EntityId;

                m_lastSentCubeGrid = projectedGrid;

                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                    OnNewBlueprintAck(ref ack, null);
                }
                else
                {
                    Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
                }
            }

            private static void OnNewBlueprintRequest(ref NewBlueprintMsg msg, MyNetworkClient sender)
            {
                //Send to all and self but one
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId, MyTransportMessageEnum.Success);
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Success);

                var ack = new NewBlueprintAckMsg();
                ack.EntityId = msg.EntityId;
                Sync.Layer.SendMessage(ref ack, sender.SteamUserId, MyTransportMessageEnum.Success);
            }

            private static void OnNewBlueprintSuccess(ref NewBlueprintMsg msg, MyNetworkClient sender)
            {
                MyEntity projectorEntity;
                MyEntities.TryGetEntityById(msg.EntityId, out projectorEntity);
                var projector = projectorEntity as MyProjector;
                if (projector != null)
                {
                    projector.SetNewBlueprint(msg.ProjectedGrid);
                }
            }

            private static void OnNewBlueprintAck(ref NewBlueprintAckMsg msg, MyNetworkClient sender)
            {
                MyEntity projectorEntity;
                MyEntities.TryGetEntityById(msg.EntityId, out projectorEntity);
                var projector = projectorEntity as MyProjector;
                if (projector != null)
                {
                    projector.SetNewBlueprint(projector.SyncObject.m_lastSentCubeGrid);
                }
            }

            public void SendNewOffset(Vector3I positionOffset, Vector3I rotationOffset, bool showOnlyBuildable)
            {
                var msg = new OffsetMsg();
                msg.EntityId = m_projector.EntityId;
                msg.PositionOffset = positionOffset;
                msg.RotationOffset = rotationOffset;
                msg.showOnlyBuildable = (byte)(showOnlyBuildable ? 1 : 0);
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
                }
                else
                {
                    Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
                }
            }

            private static void OnOffsetChangedRequest(ref OffsetMsg msg, MyNetworkClient sender)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }

            private static void OnOffsetChangedSuccess(ref OffsetMsg msg, MyNetworkClient sender)
            {
                MyEntity projectorEntity;
                MyEntities.TryGetEntityById(msg.EntityId, out projectorEntity);
                var projector = projectorEntity as MyProjector;
                if (projector != null)
                {
                    projector.SetNewOffset(msg.PositionOffset, msg.RotationOffset, msg.showOnlyBuildable == 1);
                    projector.m_shouldUpdateProjection = true;
                }
            }

            public void SendNewKeepProjection(bool keepProjection)
            {
                var msg = new KeepProjectionMsg();
                msg.EntityId = m_projector.EntityId;
                msg.KeepProjection = keepProjection;

                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
                }
                else
                {
                    Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
                }
            }

            private static void OnKeepProjectionChangedRequest(ref KeepProjectionMsg msg, MyNetworkClient sender)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }

            private static void OnKeepProjectionChangedSuccess(ref KeepProjectionMsg msg, MyNetworkClient sender)
            {
                MyEntity projectorEntity;
                MyEntities.TryGetEntityById(msg.EntityId, out projectorEntity);
                var projector = projectorEntity as MyProjector;
                if (projector != null)
                {
                    projector.m_keepProjection = msg.KeepProjection;
                }
            }

            public void SendRemoveProjection()
            {
                var msg = new RemoveProjectionMsg();
                msg.EntityId = m_projector.EntityId;

                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
                }
                else
                {
                    Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
                }
            }

            private static void OnRemoveProjectionRequest(ref RemoveProjectionMsg msg, MyNetworkClient sender)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }

            private static void OnRemoveProjectionSuccess(ref RemoveProjectionMsg msg, MyNetworkClient sender)
            {
                MyEntity projectorEntity;
                MyEntities.TryGetEntityById(msg.EntityId, out projectorEntity);
                var projector = projectorEntity as MyProjector;
                if (projector != null)
                {
                    projector.RemoveProjection(false);
                }
            }

            public void SendRemap()
            {
                var msg = new RemapRequestMsg();
                msg.EntityId = m_projector.EntityId;
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }

            private static void OnRemapRequest(ref RemapRequestMsg msg, MyNetworkClient sender)
            {
                MyEntity projectorEntity;
                MyEntities.TryGetEntityById(msg.EntityId, out projectorEntity);
                var projector = projectorEntity as MyProjector;
                if (projector != null)
                {
                    int randomSeed = MyRandom.Instance.CreateRandomSeed();
                    var seedMsg = new RemapSeedMsg();
                    seedMsg.EntityId = projector.EntityId;
                    seedMsg.Seed = randomSeed;

                    Sync.Layer.SendMessageToAllAndSelf(ref seedMsg, MyTransportMessageEnum.Success);
                }
            }

            private static void OnRemapSuccess(ref RemapSeedMsg msg, MyNetworkClient sender)
            {
                MyEntity projectorEntity;
                MyEntities.TryGetEntityById(msg.EntityId, out projectorEntity);
                var projector = projectorEntity as MyProjector;
                if (projector != null)
                {
                    projector.OnRemap(msg.Seed);
                }
            }


            public void SendNewInstantBuilding(bool instantBuilding)
            {
                var msg = new SetInstantBuildingMsg();
                msg.EntityId = m_projector.EntityId;
                msg.Enabled = instantBuilding;
                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }

            private static void OnSetInstantBuilding(ref SetInstantBuildingMsg msg, MyNetworkClient sender)
            {
                MyEntity projectorEntity;
                MyEntities.TryGetEntityById(msg.EntityId, out projectorEntity);
                var projector = projectorEntity as MyProjector;
                if (projector != null)
                {
                    projector.OnSetInstantBuilding(msg.Enabled);
                }
            }

            public void SendNewMaxNumberOfProjections(int maxNumber)
            {
                var msg = new SetMaxNumberOfProjectionsMsg();
                msg.MaxNumber = maxNumber;
                msg.EntityId = m_projector.EntityId;
                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }
            
            private static void OnSetMaxNumberOfProjections(ref SetMaxNumberOfProjectionsMsg msg, MyNetworkClient sender)
            {
                MyEntity projectorEntity;
                MyEntities.TryGetEntityById(msg.EntityId, out projectorEntity);
                var projector = projectorEntity as MyProjector;
                if (projector != null)
                {
                    projector.OnSetMaxNumberOfProjections(msg.MaxNumber);
                }
            }

            public void SendNewMaxNumberOfBlocks(int maxNumber)
            {
                var msg = new SetMaxNumberOfBlocksMsg();
                msg.MaxNumber = maxNumber;
                msg.EntityId = m_projector.EntityId;
                Sync.Layer.SendMessageToAllAndSelf(ref msg);
            }
            
            private static void OnSetMaxNumberOfBlocks(ref SetMaxNumberOfBlocksMsg msg, MyNetworkClient sender)
            {
                MyEntity projectorEntity;
                MyEntities.TryGetEntityById(msg.EntityId, out projectorEntity);
                var projector = projectorEntity as MyProjector;
                if (projector != null)
                {
                    projector.OnSetMaxNumberOfBlocks(msg.MaxNumber);
                }
            }

            public void SendSpawnProjection()
            {
                var msg = new SpawnProjectionMsg();
                msg.EntityId = m_projector.EntityId;
                Sync.Layer.SendMessageToServer(ref msg);
            }

            private static void OnSpawnProjection(ref SpawnProjectionMsg msg, MyNetworkClient sender)
            {
                MyEntity projectorEntity;
                MyEntities.TryGetEntityById(msg.EntityId, out projectorEntity);
                var projector = projectorEntity as MyProjector;
                if (projector != null)
                {
                    projector.OnSpawnProjection();
                }
            }

            public void SendConfirmSpawnProjection()
            {
                var msg = new ConfirmSpawnProjectionMsg();
                msg.EntityId = m_projector.EntityId;
                Sync.Layer.SendMessageToServer(ref msg);
            }

            private static void OnConfirmSpawnProjection(ref ConfirmSpawnProjectionMsg msg, MyNetworkClient sender)
            {
                MyEntity projectorEntity;
                MyEntities.TryGetEntityById(msg.EntityId, out projectorEntity);
                var projector = projectorEntity as MyProjector;
                if (projector != null)
                {
                    projector.OnConfirmSpawnProjection();
                }
            }
        }
        #endregion

        #region ModAPI
        int ModAPI.Ingame.IMyProjector.ProjectionOffsetX { get { return this.m_projectionOffset.X; } }
        int ModAPI.Ingame.IMyProjector.ProjectionOffsetY { get { return this.m_projectionOffset.Y; } }
        int ModAPI.Ingame.IMyProjector.ProjectionOffsetZ { get { return this.m_projectionOffset.Z; } }

        int ModAPI.Ingame.IMyProjector.ProjectionRotX { get { return this.m_projectionRotation.X*90; } }
        int ModAPI.Ingame.IMyProjector.ProjectionRotY { get { return this.m_projectionRotation.Y * 90; } }
        int ModAPI.Ingame.IMyProjector.ProjectionRotZ { get { return this.m_projectionRotation.Z* 90; } }

        int ModAPI.Ingame.IMyProjector.RemainingBlocks { get { return this.m_remainingBlocks; } }


        void ModAPI.Ingame.IMyProjector.LoadRandomBlueprint(string searchPattern)
        {
            string[] files = System.IO.Directory.GetFiles(Path.Combine(MyFileSystem.ContentPath, "Data", "Blueprints"), searchPattern);
            
            var index = MyRandom.Instance.Next() % files.Length;
            LoadBlueprint(files[index]);
        }
        void ModAPI.Ingame.IMyProjector.LoadBlueprint(string path)
        {
            LoadBlueprint(path);
        }

        private void LoadBlueprint(string path)
        {
            MyObjectBuilder_Definitions blueprint;
            blueprint = MyGuiBlueprintScreenBase.LoadPrefab(path);

            if (blueprint != null)
                MyGuiBlueprintScreen.CopyBlueprintPrefabToClipboard(blueprint, m_clipboard);
            blueprintScreen_Closed(null);
        }
        #endregion

    }
}
