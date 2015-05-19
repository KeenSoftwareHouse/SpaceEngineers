
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using VRageMath;
using System.Collections.Generic;
using Sandbox.Common;
using Sandbox.Game.World;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using System.Text;
using System;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;

using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.GameSystems;
using VRage;
using VRage.Utils;
using Sandbox.Game.Localization;
using VRage;
using VRage.Utils;
using VRage.Library.Utils;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.IO;
using VRage.FileSystem;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Projector))]
    class MyProjector : MyFunctionalBlock, IMyPowerConsumer, ModAPI.Ingame.IMyProjector
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

        public MyPowerReceiver PowerReceiver
        {
            get;
            protected set;
        }
        
        public MyProjector()
            : base()
        {
            m_clipboard = new MyProjectorClipboard(this);
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
        }

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

        private void OnOffsetsChanged()
        {
            m_shouldUpdateProjection = true;
            m_shouldUpdateTexts = true;
            SyncObject.SendNewOffset(m_projectionOffset, m_projectionRotation);

            //We need to remap because the after the movement, blocks that were already built can be built again
            SyncObject.SendRemap();
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

            if (block != null && block.DetectorPhysics != null && block.DetectorPhysics.Enabled)
            {
                block.DetectorPhysics.Enabled = false;
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

        //Projector needs to wait some frames before it can ask if it is powered.
        private int m_frameCount = 0;
        private bool m_removeRequested = false;
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

        private void HideIntersectedBlock()
        {
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

            SetRotation(m_projectionRotation);

            NeedsUpdate |= Common.MyEntityUpdateEnum.EACH_FRAME;
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
            return objectBuilder;
        }

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

        private void ResetRotation()
        {
            SetRotation(-m_projectionRotation);
        }

        private void SetRotation(Vector3I rotation)
        {
            m_clipboard.RotateAroundAxis(0, System.Math.Sign(rotation.X), true, System.Math.Abs(rotation.X * MathHelper.PiOver2));
            m_clipboard.RotateAroundAxis(1, System.Math.Sign(rotation.Y), true, System.Math.Abs(rotation.Y * MathHelper.PiOver2));
            m_clipboard.RotateAroundAxis(2, System.Math.Sign(rotation.Z), true, System.Math.Abs(rotation.Z * MathHelper.PiOver2));
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

        //Stats
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

        private List<MySlimBlock> m_visibleBlocks = new List<MySlimBlock>();
        private List<MySlimBlock> m_buildableBlocks = new List<MySlimBlock>();
        private List<MySlimBlock> m_hiddenBlocks = new List<MySlimBlock>();

        private void UpdateProjection()
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
                        m_visibleBlocks.Add(projectedBlock);
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
            if (!m_statsDirty)
            {
                return;
            }
            if (m_clipboard.IsActive)
            {
                UpdateStats();
            }
            m_statsDirty = false;

            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(BlockDefinition.RequiredPowerInput, DetailedInfo);

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
                DetailedInfo.Append("Build progress: " + (m_totalBlocks-m_remainingBlocks) + "/" + m_totalBlocks);
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

        private void ShowDebugNotification(string notificationText)
        {
            var debugNotification = new MyHudNotification(MySpaceTexts.CustomText, 5000, level: MyNotificationLevel.Important);
            debugNotification.SetTextFormatArguments("DEBUG: " + notificationText);
            MyHud.Notifications.Add(debugNotification);
        }

        private void ShowNotification(MyStringId textToDisplay)
        {
            var debugNotification = new MyHudNotification(textToDisplay, 5000, level: MyNotificationLevel.Important);
            MyHud.Notifications.Add(debugNotification);
        }

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
                quat, 0, owner, builder);

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
            projectorGrid.BuildBlock(cubeBlock.ColorMaskHSV, location, objectBuilder);
            HideCube(cubeBlock);
        }

        #region Sync
        internal void SetNewBlueprint(MyObjectBuilder_CubeGrid gridBuilder)
        {
            m_originalGridBuilder = gridBuilder;

            var clone = (MyObjectBuilder_CubeGrid)gridBuilder.Clone();
            MyEntities.RemapObjectBuilder(clone);
            m_clipboard.ProcessCubeGrid(clone);

            m_clipboard.SetGridFromBuilder(clone, Vector3.Zero, 0f);
            InitializeClipboard();
        }

        internal void SetNewOffset(Vector3I positionOffset, Vector3I rotationOffset)
        {
            m_clipboard.ResetGridOrientation();

            m_projectionOffset = positionOffset;
            m_projectionRotation = rotationOffset;

            SetRotation(m_projectionRotation);
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

            public void SendNewOffset(Vector3I positionOffset, Vector3I rotationOffset)
            {
                var msg = new OffsetMsg();
                msg.EntityId = m_projector.EntityId;
                msg.PositionOffset = positionOffset;
                msg.RotationOffset = rotationOffset;

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
                    projector.SetNewOffset(msg.PositionOffset, msg.RotationOffset);
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
        }
        #endregion

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

    }
}
