using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Game.EntityComponents;
using VRage;
using VRage.Utils;
using VRageMath;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Sync;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_JumpDrive))]
    public class MyJumpDrive : MyFunctionalBlock, IMyJumpDrive
    {
        private readonly Sync<float> m_storedPower;
        private IMyGps m_selectedGps = null;
        private IMyGps m_jumpTarget = null;
        // From 0 to 100 percent
        private readonly Sync<int?> m_targetSync;
        private readonly Sync<float> m_jumpDistanceRatio;
        
        private int? m_storedJumpTarget = null;
        private float m_timeRemaining = 0.0f;

        private readonly  Sync<bool> m_isRecharging;
        public bool IsJumping = false;
        private static MyGuiControlListbox m_gpsGuiControl;

        public new MyJumpDriveDefinition BlockDefinition
        {
            get { return (MyJumpDriveDefinition)base.BlockDefinition; }
        }

        public bool CanJump
        {
            get
            {
                if (IsWorking && IsFunctional && IsFull)
                {
                    return true;
                }
                return false;
            }
        }

        public bool CanJumpAndHasAccess(long userId)
        {
            if (!CanJump)
            {
                return false;
            }

            var relationship = IDModule.GetUserRelationToOwner(userId);
            return relationship.IsFriendly();
        }

        public bool IsFull
        {
            get
            {
                return m_storedPower >= BlockDefinition.PowerNeededForJump;
            }
        }

        #region UI
        public MyJumpDrive()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_storedPower = SyncType.CreateAndAddProp<float>();
            m_targetSync = SyncType.CreateAndAddProp<int?>();
            m_jumpDistanceRatio = SyncType.CreateAndAddProp<float>();
            m_isRecharging = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();

            m_isRecharging.ValueChanged += x => RaisePropertiesChanged();   //GR: Maybe not needed since called every 100 frames either way
            m_targetSync.ValueChanged += x => TargetChanged();
            m_storedPower.ValidateNever();
        }

        void TargetChanged()
        {
            if (m_targetSync.Value.HasValue)
            {
                m_jumpTarget = MySession.Static.Gpss.GetGps(m_targetSync.Value.Value);
            }
            else
            { 
                 m_jumpTarget = null;
            }
            RaisePropertiesChanged();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyJumpDrive>())
                return;
            base.CreateTerminalControls();
            var jumpButton = new MyTerminalControlButton<MyJumpDrive>("Jump", MySpaceTexts.BlockActionTitle_Jump, MySpaceTexts.Blank, (x) => x.RequestJump());
            jumpButton.Enabled = (x) => x.CanJump;
            jumpButton.SupportsMultipleBlocks = false;
            // Can only be called from toolbar of cockpit
            jumpButton.Visible = (x) => false;
            var action = jumpButton.EnableAction(MyTerminalActionIcons.TOGGLE);
            if (action != null)
            {
                action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.ButtonPanel, MyToolbarType.Character, MyToolbarType.Seat };
                action.ValidForGroups = false;
            }
            MyTerminalControlFactory.AddControl(jumpButton);

            var recharging = new MyTerminalControlOnOffSwitch<MyJumpDrive>("Recharge", MySpaceTexts.BlockPropertyTitle_Recharge, MySpaceTexts.Blank);
            recharging.Getter = (x) => x.m_isRecharging;
            recharging.Setter = (x, v) => x.m_isRecharging.Value = v;
            recharging.EnableToggleAction();
            recharging.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(recharging);

            var maxDistanceSlider = new MyTerminalControlSlider<MyJumpDrive>("JumpDistance", MySpaceTexts.BlockPropertyTitle_JumpDistance, MySpaceTexts.Blank);
            maxDistanceSlider.SetLimits(0f, 100f);
            maxDistanceSlider.DefaultValue = 100f;
            maxDistanceSlider.Enabled = (x) => x.m_jumpTarget == null;
            maxDistanceSlider.Getter = (x) => x.m_jumpDistanceRatio;
            maxDistanceSlider.Setter = (x, v) =>
                {
                    x.m_jumpDistanceRatio.Value = v;
                };
            maxDistanceSlider.Writer = (x, v) =>
                {
                    v.AppendFormatedDecimal((x.m_jumpDistanceRatio / 100f).ToString("P0") + " (", (float)x.ComputeMaxDistance() / 1000f, 0, " km").Append(")");
                };
            maxDistanceSlider.EnableActions(0.01f);
            MyTerminalControlFactory.AddControl(maxDistanceSlider);


            var selectedTarget = new MyTerminalControlListbox<MyJumpDrive>("SelectedTarget", MySpaceTexts.BlockPropertyTitle_DestinationGPS, MySpaceTexts.Blank, false, 1);
            selectedTarget.ListContent = (x, list1, list2) => x.FillSelectedTarget(list1, list2);
            MyTerminalControlFactory.AddControl(selectedTarget);

            var removeBtn = new MyTerminalControlButton<MyJumpDrive>("RemoveBtn", MySpaceTexts.RemoveProjectionButton, MySpaceTexts.Blank, (x) => x.RemoveSelected());
            removeBtn.Enabled = (x) => x.CanRemove();
            MyTerminalControlFactory.AddControl(removeBtn);

            var selectBtn = new MyTerminalControlButton<MyJumpDrive>("SelectBtn", MyCommonTexts.SelectBlueprint, MySpaceTexts.Blank, (x) => x.SelectTarget());
            selectBtn.Enabled = (x) => x.CanSelect();
            MyTerminalControlFactory.AddControl(selectBtn);

            var gpsList = new MyTerminalControlListbox<MyJumpDrive>("GpsList", MySpaceTexts.BlockPropertyTitle_GpsLocations, MySpaceTexts.Blank, true);
            gpsList.ListContent = (x, list1, list2) => x.FillGpsList(list1, list2);
            gpsList.ItemSelected = (x, y) => x.SelectGps(y);
            MyTerminalControlFactory.AddControl(gpsList);

            if (!MySandboxGame.IsDedicated)
            {
                m_gpsGuiControl = (MyGuiControlListbox)((MyGuiControlBlockProperty)gpsList.GetGuiControl()).PropertyControl;
            }
        }

        private bool CanSelect()
        {
            return m_selectedGps != null;
        }

        private void SelectTarget()
        {
            if (CanSelect())
            {
                m_targetSync.Value = m_selectedGps.Hash;
            }
        }

        private bool CanRemove()
        {
            return m_jumpTarget != null;
        }

        private void RemoveSelected()
        {
            if (CanRemove())
            {
               m_targetSync.Value = null;
            }
        }

        private void RequestJump()
        {
            if (CanJump)
            {
                if (MySession.Static.LocalCharacter != null)
                {
                    var shipController = MySession.Static.LocalCharacter.Parent as MyShipController;
                    if (shipController == null && MySession.Static.ControlledEntity != null)
                    {
                        shipController = MySession.Static.ControlledEntity.Entity as MyShipController;
                    }


                    if (shipController != null && (shipController.IsMainCockpit || !CubeGrid.HasMainCockpit()))
                    {
                        if (m_jumpTarget != null)
                        {
                            CubeGrid.GridSystems.JumpSystem.RequestJump(m_jumpTarget.Name, m_jumpTarget.Coords, shipController.OwnerId);
                        }
                        else
                        {
                            Vector3 localForward = Base6Directions.GetVector(shipController.Orientation.Forward);
                            Vector3D forward = Vector3D.Transform(localForward, shipController.CubeGrid.WorldMatrix.GetOrientation());

                            forward.Normalize();

                            Vector3D jumpCoords = CubeGrid.WorldMatrix.Translation + forward * ComputeMaxDistance();
                            CubeGrid.GridSystems.JumpSystem.RequestJump("Blind Jump", jumpCoords, shipController.OwnerId);
                        }
                    }
                }
            }
            else if (!IsJumping && !IsFull)
            {
                var notification = new MyHudNotification(MySpaceTexts.NotificationJumpDriveNotFullyCharged, 1500);
                notification.SetTextFormatArguments((m_storedPower / BlockDefinition.PowerNeededForJump).ToString("P"));
                MyHud.Notifications.Add(notification);
            }
        }

        private double ComputeMaxDistance()
        {
            double maxDistance = CubeGrid.GridSystems.JumpSystem.GetMaxJumpDistance(IDModule.Owner);
            if (maxDistance < MyGridJumpDriveSystem.MIN_JUMP_DISTANCE)
            {
                return MyGridJumpDriveSystem.MIN_JUMP_DISTANCE;
            }

            return MyGridJumpDriveSystem.MIN_JUMP_DISTANCE + 1f + (maxDistance - MyGridJumpDriveSystem.MIN_JUMP_DISTANCE) * (m_jumpDistanceRatio / 100.0f);
        }

        private void FillGpsList(ICollection<MyGuiControlListbox.Item> gpsItemList, ICollection<MyGuiControlListbox.Item> selectedGpsItemList)
        {
            List<IMyGps> gpsList = new List<IMyGps>();
            MySession.Static.Gpss.GetGpsList(MySession.Static.LocalPlayerId, gpsList);
            foreach (var gps in gpsList)
            {
                var item = new MyGuiControlListbox.Item(text: new StringBuilder(gps.Name), userData: gps);
                gpsItemList.Add(item);

                if (m_selectedGps == gps)
                {
                    selectedGpsItemList.Add(item);
                }
            }
        }

        private void FillSelectedTarget(ICollection<MyGuiControlListbox.Item> selectedTargetList, ICollection<MyGuiControlListbox.Item> emptyList)
        {
            if (m_jumpTarget != null)
            {
                selectedTargetList.Add(new MyGuiControlListbox.Item(text: new StringBuilder(m_jumpTarget.Name), toolTip: MyTexts.GetString(MySpaceTexts.BlockActionTooltip_SelectedJumpTarget), userData: m_jumpTarget));
            }
            else
            {
                selectedTargetList.Add(new MyGuiControlListbox.Item(text: new StringBuilder("Blind Jump"), toolTip: MyTexts.GetString(MySpaceTexts.BlockActionTooltip_SelectedJumpTarget), userData: null));
            }
        }

        private void SelectGps(List<MyGuiControlListbox.Item> selection)
        {
            if (selection.Count > 0)
            {
                m_selectedGps = (IMyGps)selection[0].UserData;
                RaisePropertiesChanged();
            }
        }
        #endregion

        #region Init
        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                BlockDefinition.ResourceSinkGroup,
                BlockDefinition.RequiredPowerInput,
                ComputeRequiredPower);
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);

            var ob = objectBuilder as MyObjectBuilder_JumpDrive;

            NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.BEFORE_NEXT_FRAME | VRage.ModAPI.MyEntityUpdateEnum.EACH_FRAME | VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;

            m_storedPower.Value = Math.Min(ob.StoredPower,BlockDefinition.PowerNeededForJump);

            m_storedJumpTarget = ob.JumpTarget;
            if (ob.JumpTarget != null)
            {
                m_jumpTarget = MySession.Static.Gpss.GetGps(ob.JumpTarget.Value);
            }
 
            m_jumpDistanceRatio.Value = ob.JumpRatio;
            m_isRecharging.Value = ob.Recharging;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            IsWorkingChanged += MyJumpDrive_IsWorkingChanged;

			ResourceSink.Update();
            UpdateEmissivity();
        }

        private void MyJumpDrive_IsWorkingChanged(MyCubeBlock obj)
        {
            CheckForAbort();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            CheckForAbort();
        }

        private void CheckForAbort()
        {
            if (Sync.IsServer)
            {
                if (IsJumping && (!IsWorking || !IsFunctional))
                {
                    IsJumping = false;
                    CubeGrid.GridSystems.JumpSystem.RequestAbort();
                }
            }
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var jumpDriveBuilder = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_JumpDrive;
            
            jumpDriveBuilder.StoredPower = m_storedPower;
            if (m_jumpTarget != null)
            {
                jumpDriveBuilder.JumpTarget = m_jumpTarget.Hash;
            }
            jumpDriveBuilder.JumpRatio = m_jumpDistanceRatio;
            jumpDriveBuilder.Recharging = m_isRecharging;

            return jumpDriveBuilder;
        }

        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();
            CubeGrid.GridSystems.JumpSystem.RegisterJumpDrive(this);
        }

        public override void OnUnregisteredFromGridSystems()
        {
            base.OnUnregisteredFromGridSystems();
            CubeGrid.GridSystems.JumpSystem.AbortJump();
            CubeGrid.GridSystems.JumpSystem.UnregisterJumpDrive(this);
        }

        #endregion

        #region Update
        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (m_storedJumpTarget != null)
            {
                m_jumpTarget = MySession.Static.Gpss.GetGps(m_storedJumpTarget.Value);
                if (m_jumpTarget != null)
                {
                    m_targetSync.Value = m_jumpTarget.Hash;
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

			ResourceSink.Update();
        }
        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            if (IsFunctional)
            {
                if (!IsFull && m_isRecharging)
                {
                    StorePower(100f * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS, ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId));
                }
            }

            UpdateEmissivity();
            UpdateText();
        }

        void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(BlockDefinition.RequiredPowerInput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxStoredPower));
            MyValueFormatter.AppendWorkHoursInBestUnit(BlockDefinition.PowerNeededForJump, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId), DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_StoredPower));
            MyValueFormatter.AppendWorkHoursInBestUnit(m_storedPower, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_RechargedIn));
            MyValueFormatter.AppendTimeInBestUnit(m_timeRemaining, DetailedInfo);
            DetailedInfo.Append("\n");
            int maxDistance = (int)(CubeGrid.GridSystems.JumpSystem.GetMaxJumpDistance(OwnerId) / 1000);
            DetailedInfo.Append("Max jump distance: ");
            DetailedInfo.Append(maxDistance).Append(" km");
            if (m_jumpTarget != null)
            {
                DetailedInfo.Append("\n");
                double distance = (m_jumpTarget.Coords - CubeGrid.WorldMatrix.Translation).Length();
                float ratio = Math.Min(1.0f, (float)(maxDistance / distance));
                DetailedInfo.Append("Current jump: " + (ratio * 100f).ToString("F2") + "%");
            }
            RaisePropertiesChanged();
        }

        #endregion

        #region Power

        private float ComputeRequiredPower()
        {
            if (IsFunctional && IsWorking && m_isRecharging)
            {
                if (IsFull)
                {
                    return 0f;
                }
                else
                {
                    return BlockDefinition.RequiredPowerInput;
                }
            }
            else
            {
                return 0f;
            }
        }


        private void StorePower(float deltaTime, float input)
        {
            float inputPowerPerMillisecond = input / (60 * 60 * 1000);
            float increment = (deltaTime * inputPowerPerMillisecond) * 0.80f;

            if (MySession.Static.CreativeMode && !MyFinalBuildConstants.IS_OFFICIAL)
            {
                increment *= 1000.0f;
            }

            m_storedPower.Value += increment;

            deltaTime /= 1000f;

            if (m_storedPower > BlockDefinition.PowerNeededForJump)
            {
                m_storedPower.Value = BlockDefinition.PowerNeededForJump;
            }
            if (increment > 0f)
            {
                m_timeRemaining = (BlockDefinition.PowerNeededForJump - m_storedPower) * deltaTime / increment;
            }
            else
            {
                m_timeRemaining = 0f;
            }
        }

        public void SetStoredPower(float filledRatio)
        {
            Debug.Assert(filledRatio >= 0f);
            Debug.Assert(filledRatio <= 1f);

            if (filledRatio < 0f) filledRatio = 0f;
            if (filledRatio >= 1f)
            {
                filledRatio = 1f;
            }

            m_storedPower.Value = filledRatio * BlockDefinition.PowerNeededForJump;
            UpdateEmissivity();
        }
        #endregion

        #region Emissivity
        public override void OnModelChange()
        {
            base.OnModelChange();
            m_prevFillCount = -1;
            UpdateEmissivity();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity(true);
        }

        private void UpdateEmissivity(bool force = false)
        {
            if (IsFunctional && IsWorking)
            {
                if (IsFull)
                {
                    SetEmissive(Color.Cyan, 1.0f, 1.0f, force);
                }
                else if (!m_isRecharging)
                {
                    SetEmissive(Color.Yellow, m_storedPower / BlockDefinition.PowerNeededForJump, 1.0f, force);
                }
                else if (ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) > 0f)
                {
                    SetEmissive(Color.Green, m_storedPower / BlockDefinition.PowerNeededForJump, 1.0f, force);
                }
                else
                {
                    SetEmissive(Color.Red, m_storedPower / BlockDefinition.PowerNeededForJump, 0.0f, force);
                }
            }
            else
            {
                SetEmissive(Color.Red, 1.0f, 0.0f, force);
            }
        }

        private static string[] m_emissiveNames = { "Emissive0", "Emissive1", "Emissive2", "Emissive3" };

        private Color m_prevColor = Color.White;
        private int m_prevFillCount = -1;

        private void SetEmissive(Color color, float fill, float emissivity, bool force)
        {
            int fillCount = (int)(fill * m_emissiveNames.Length);

            if (force || Render.RenderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED && (color != m_prevColor || fillCount != m_prevFillCount))
            {
                for (int i = 0; i < m_emissiveNames.Length; i++)
                {
                    if (i <= fillCount)
                    {
                        UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], m_emissiveNames[i], color, emissivity);
                    }
                    else
                    {
                        UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], m_emissiveNames[i], Color.Black, 0);
                    }
                }

                UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], "Emissive", color, emissivity);

                m_prevColor = color;
                m_prevFillCount = fillCount;
            }
        }
        #endregion
    }
}
