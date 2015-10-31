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
using Sandbox.ModAPI.Ingame;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Game.EntityComponents;
using VRage;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_JumpDrive))]
    public class MyJumpDrive : MyFunctionalBlock, IMyJumpDrive
    {
        private float m_storedPower = 0;
        private IMyGps m_selectedGps = null;
        private IMyGps m_jumpTarget = null;
        // From 0 to 100 percent
        private float m_jumpDistanceRatio = 100.0f;
        
        private int? m_storedJumpTarget = null;
        private float m_timeRemaining = 0.0f;

        private bool m_isRecharging = false;
        public bool IsJumping = false;
        private static readonly MyGuiControlListbox m_gpsGuiControl;

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
        static MyJumpDrive()
        {
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
            recharging.Setter = (x, v) => x.SetRecharging(v);
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
                    x.SetJumpDistanceRatio(v);
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

            var selectBtn = new MyTerminalControlButton<MyJumpDrive>("SelectBtn", MySpaceTexts.SelectBlueprint, MySpaceTexts.Blank, (x) => x.SelectTarget());
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
                SyncObject.SendTargetSelected(m_selectedGps.Hash);
            }
        }

        private void OnTargetSelected(int gpsHash)
        {
            m_jumpTarget = MySession.Static.Gpss.GetGps(gpsHash);
            RaisePropertiesChangedJumpDrive();
        }

        private bool CanRemove()
        {
            return m_jumpTarget != null;
        }

        private void RemoveSelected()
        {
            if (CanRemove())
            {
                SyncObject.SendTargetRemoved();
            }
        }

        private void OnTargetRemoved()
        {
            m_jumpTarget = null;
            RaisePropertiesChangedJumpDrive();
        }

        private void SetRecharging(bool recharging)
        {
            SyncObject.SendRecharging(recharging);
        }

        void OnRechargingSet(bool recharging)
        {
            m_isRecharging = recharging;
            RaisePropertiesChangedJumpDrive();
        }

        private void SetJumpDistanceRatio(float jumpDistanceRatio)
        {
            SyncObject.SendJumpDistanceRatio(jumpDistanceRatio);
        }

        private void OnJumpDistanceRatioSet(float jumpDistanceRatio)
        {
            m_jumpDistanceRatio = jumpDistanceRatio;
        }

        private void RequestJump()
        {
            if (CanJump)
            {
                if (MySession.LocalCharacter != null)
                {
                    var shipController = MySession.LocalCharacter.Parent as MyShipController;
                    if (shipController == null && MySession.ControlledEntity != null)
                    {
                        shipController = MySession.ControlledEntity.Entity as MyShipController;
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
            MySession.Static.Gpss.GetGpsList(MySession.LocalPlayerId, gpsList);
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
                RaisePropertiesChangedJumpDrive();
            }
        }
        #endregion

        #region Init
        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            base.Init(objectBuilder, cubeGrid);

			var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                BlockDefinition.ResourceSinkGroup,
                BlockDefinition.RequiredPowerInput,
                ComputeRequiredPower);
	        ResourceSink = sinkComp;

            var jumpDriveBuilder = objectBuilder as MyObjectBuilder_JumpDrive;

            NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.BEFORE_NEXT_FRAME | VRage.ModAPI.MyEntityUpdateEnum.EACH_FRAME | VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;

            m_storedPower = jumpDriveBuilder.StoredPower;
            if (m_storedPower >= BlockDefinition.PowerNeededForJump)
            {
                m_storedPower = BlockDefinition.PowerNeededForJump;
            }

            m_storedJumpTarget = jumpDriveBuilder.JumpTarget;
            if (jumpDriveBuilder.JumpTarget != null)
            {
                m_jumpTarget = MySession.Static.Gpss.GetGps(jumpDriveBuilder.JumpTarget.Value);
            }

            m_jumpDistanceRatio = jumpDriveBuilder.JumpRatio;
            m_isRecharging = jumpDriveBuilder.Recharging;

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
					StorePower(100f * MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS, ResourceSink.CurrentInput);
                    if (Sync.IsServer)
                    {
                        if (IsFull)
                        {
                            SyncObject.SendStoredPowerReliable(m_storedPower);
                        }
                        else
                        {
                            SyncObject.SendStoredPower(m_storedPower);
                        }
                    }
                }
            }

            UpdateEmissivity();
            UpdateText();
        }

        void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(BlockDefinition.RequiredPowerInput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxStoredPower));
            MyValueFormatter.AppendWorkHoursInBestUnit(BlockDefinition.PowerNeededForJump, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
			MyValueFormatter.AppendWorkInBestUnit(ResourceSink.CurrentInput, DetailedInfo);
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
            RaisePropertiesChangedJumpDrive();
        }

        private void RaisePropertiesChangedJumpDrive()
        {
            int gpsFirstVisibleRow = m_gpsGuiControl != null ? m_gpsGuiControl.FirstVisibleRow : 0;
            RaisePropertiesChanged();
            if (m_gpsGuiControl != null && gpsFirstVisibleRow < m_gpsGuiControl.Items.Count)
            {
                m_gpsGuiControl.FirstVisibleRow = gpsFirstVisibleRow;
            }
        }
        #endregion

        #region Power

        private float ComputeRequiredPower()
        {
            if (IsFunctional && IsWorking)
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

            m_storedPower += increment;

            deltaTime /= 1000f;

            if (m_storedPower > BlockDefinition.PowerNeededForJump)
            {
                m_storedPower = BlockDefinition.PowerNeededForJump;
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

            m_storedPower = filledRatio * BlockDefinition.PowerNeededForJump;
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

        private void UpdateEmissivity()
        {
            if (IsFunctional && IsWorking)
            {
                if (IsFull)
                {
                    SetEmissive(Color.Cyan, 1.0f, 1.0f);
                }
                else if (!m_isRecharging)
                {
                    SetEmissive(Color.Yellow, m_storedPower / BlockDefinition.PowerNeededForJump, 1.0f);
                }
				else if (ResourceSink.CurrentInput > 0f)
                {
                    SetEmissive(Color.Green, m_storedPower / BlockDefinition.PowerNeededForJump, 1.0f);
                }
                else
                {
                    SetEmissive(Color.Red, m_storedPower / BlockDefinition.PowerNeededForJump, 0.0f);
                }
            }
            else
            {
                SetEmissive(Color.Red, 1.0f, 0.0f);
            }
        }

        private static string[] m_emissiveNames = { "Emissive0", "Emissive1", "Emissive2", "Emissive3" };

        private Color m_prevColor = Color.White;
        private int m_prevFillCount = -1;

        private void SetEmissive(Color color, float fill, float emissivity)
        {
            int fillCount = (int)(fill * m_emissiveNames.Length);

            if (Render.RenderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED && (color != m_prevColor || fillCount != m_prevFillCount))
            {
                for (int i = 0; i < m_emissiveNames.Length; i++)
                {
                    if (i <= fillCount)
                    {
                        VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, m_emissiveNames[i], color, emissivity);
                    }
                    else
                    {
                        VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, m_emissiveNames[i], Color.Black, 0);
                    }
                }

                VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, "Emissive4", color, emissivity);

                m_prevColor = color;
                m_prevFillCount = fillCount;
            }
        }
        #endregion

        #region Sync
        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncJumpDrive(this);
        }

        internal new MySyncJumpDrive SyncObject
        {
            get
            {
                return (MySyncJumpDrive)base.SyncObject;
            }
        }

        [PreloadRequired]
        internal class MySyncJumpDrive : MySyncEntity
        {
            [MessageIdAttribute(8400, P2PMessageEnum.Reliable)]
            protected struct SelectTargetMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public int GpsHash;
            }

            [MessageIdAttribute(8401, P2PMessageEnum.Reliable)]
            protected struct RemoveTargetMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }
            }

            [MessageIdAttribute(8402, P2PMessageEnum.Reliable)]
            protected struct SetJumpDistanceRatioMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public float JumpDistancRatio;
            }

            [MessageIdAttribute(8403, P2PMessageEnum.Unreliable)]
            protected struct UpdateStoredPowerMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public float StoredPower;
            }
            
            [MessageIdAttribute(8404, P2PMessageEnum.Reliable)]
            protected struct UpdateStoredPowerReliableMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public float StoredPower;
            }

            [MessageIdAttribute(8405, P2PMessageEnum.Reliable)]
            protected struct SetRechargingMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public BoolBlit Recharging;
            }

            private MyJumpDrive m_jumpDrive;

            static MySyncJumpDrive()
            {
                MySyncLayer.RegisterEntityMessage<MySyncJumpDrive, SelectTargetMsg>(OnTargetSelected, MyMessagePermissions.ToServer|MyMessagePermissions.FromServer|MyMessagePermissions.ToSelf);
                MySyncLayer.RegisterEntityMessage<MySyncJumpDrive, RemoveTargetMsg>(OnTargetRemoved, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
                MySyncLayer.RegisterEntityMessage<MySyncJumpDrive, SetJumpDistanceRatioMsg>(OnJumpDistanceRatioSet, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
                MySyncLayer.RegisterEntityMessage<MySyncJumpDrive, UpdateStoredPowerMsg>(OnUpdateStoredPower, MyMessagePermissions.FromServer);
                MySyncLayer.RegisterEntityMessage<MySyncJumpDrive, UpdateStoredPowerReliableMsg>(OnUpdateStoredPowerReliable, MyMessagePermissions.FromServer);
                MySyncLayer.RegisterEntityMessage<MySyncJumpDrive, SetRechargingMsg>(OnSetRecharging, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
            }

            public MySyncJumpDrive(MyJumpDrive jumpDrive)
                : base(jumpDrive)
            {
                m_jumpDrive = jumpDrive;
            }

            public void SendTargetSelected(int gpsHash)
            {
                var msg = new SelectTargetMsg();
                msg.EntityId = m_jumpDrive.EntityId;
                msg.GpsHash = gpsHash;

                Sync.Layer.SendMessageToServerAndSelf(ref msg);
            }

            private static void OnTargetSelected(MySyncJumpDrive syncObject, ref SelectTargetMsg msg, MyNetworkClient sender)
            {
                syncObject.m_jumpDrive.OnTargetSelected(msg.GpsHash);
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                }
            }

            public void SendTargetRemoved()
            {
                var msg = new RemoveTargetMsg();
                msg.EntityId = m_jumpDrive.EntityId;

                Sync.Layer.SendMessageToServerAndSelf(ref msg);
            }

            private static void OnTargetRemoved(MySyncJumpDrive syncObject, ref RemoveTargetMsg msg, MyNetworkClient sender)
            {
                syncObject.m_jumpDrive.OnTargetRemoved();
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                }
            }

            public void SendJumpDistanceRatio(float jumpDistanceRatio)
            {
                var msg = new SetJumpDistanceRatioMsg();
                msg.EntityId = m_jumpDrive.EntityId;
                msg.JumpDistancRatio = jumpDistanceRatio;

                Sync.Layer.SendMessageToServerAndSelf(ref msg);
            }

            private static void OnJumpDistanceRatioSet(MySyncJumpDrive syncObject, ref SetJumpDistanceRatioMsg msg, MyNetworkClient sender)
            {
                syncObject.m_jumpDrive.OnJumpDistanceRatioSet(msg.JumpDistancRatio);
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                }
            }

            public void SendStoredPower(float storedPower)
            {
                var msg = new UpdateStoredPowerMsg();
                msg.EntityId = m_jumpDrive.EntityId;
                msg.StoredPower = storedPower;

                Sync.Layer.SendMessageToServerAndSelf(ref msg);
            }

            private static void OnUpdateStoredPower(MySyncJumpDrive syncObject, ref UpdateStoredPowerMsg msg, MyNetworkClient sender)
            {
                syncObject.m_jumpDrive.m_storedPower = msg.StoredPower;
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                }
            }

            public void SendStoredPowerReliable(float storedPower)
            {
                var msg = new UpdateStoredPowerReliableMsg();
                msg.EntityId = m_jumpDrive.EntityId;
                msg.StoredPower = storedPower;

                Sync.Layer.SendMessageToServer(ref msg);
            }

            private static void OnUpdateStoredPowerReliable(MySyncJumpDrive syncObject, ref UpdateStoredPowerReliableMsg msg, MyNetworkClient sender)
            {
                syncObject.m_jumpDrive.m_storedPower = msg.StoredPower;
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                }
            }

            public void SendRecharging(bool recharging)
            {
                var msg = new SetRechargingMsg();
                msg.EntityId = m_jumpDrive.EntityId;
                msg.Recharging = recharging;

                Sync.Layer.SendMessageToServer(ref msg);
            }

            private static void OnSetRecharging(MySyncJumpDrive syncObject, ref SetRechargingMsg msg, MyNetworkClient sender)
            {
                syncObject.m_jumpDrive.OnRechargingSet(msg.Recharging);
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                }
            }
        }
        #endregion
    }
}
