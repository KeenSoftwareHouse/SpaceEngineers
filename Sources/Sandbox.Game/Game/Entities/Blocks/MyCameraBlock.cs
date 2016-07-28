using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.EntityComponents;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRage.Game.Components;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.Utils;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_CameraBlock))]
    public class MyCameraBlock : MyFunctionalBlock, IMyCameraController, IMyCameraBlock
    {
        public new MyCameraBlockDefinition BlockDefinition
        {
            get { return (MyCameraBlockDefinition)base.BlockDefinition; }
        }

        private const float MIN_FOV = 0.00001f;
        private const float MAX_FOV = 3.12413936f;

        private float m_fov;
        private float m_targetFov;

        public bool IsActive { get; private set; }

        public bool IsInFirstPersonView 
        { 
            get { return true; } 
            set { } 
        }
        public bool ForceFirstPersonCamera { get; set; }

        private static MyHudNotification m_hudNotification;
        private bool m_requestActivateAfterLoad = false;
        private IMyCameraController m_previousCameraController = null;

        readonly Sync<float> m_syncFov;

        public MyCameraBlock()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_syncFov = SyncType.CreateAndAddProp<float>();
#endif // XB1
            CreateTerminalControls();

            m_syncFov.ValueChanged += (x) => OnSyncFov();
        }

        static void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyCameraBlock>())
                return;

            var viewBtn = new MyTerminalControlButton<MyCameraBlock>("View", MySpaceTexts.BlockActionTitle_View, MySpaceTexts.Blank, (b) => b.RequestSetView());
            viewBtn.Enabled = (b) => b.CanUse();
            viewBtn.SupportsMultipleBlocks = false;
            var action = viewBtn.EnableAction(MyTerminalActionIcons.TOGGLE);
            if (action != null)
            {
                action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.ButtonPanel };
                action.ValidForGroups = false;
            }
            MyTerminalControlFactory.AddControl(viewBtn);

            var controlName = MyInput.Static.GetGameControl(MyControlsSpace.USE).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);
            m_hudNotification = new MyHudNotification(MySpaceTexts.NotificationHintPressToExitCamera);
            m_hudNotification.SetTextFormatArguments(controlName);
        }

        public bool CanUse()
        {
            return IsWorking && MyGridCameraSystem.CameraIsInRangeAndPlayerHasAccess(this);
        }

        public void RequestSetView()
        {
            if (!MyFakes.ENABLE_CAMERA_BLOCK)
            {
                return;
            }
            if (IsWorking)
            {
                MyHud.Notifications.Remove(m_hudNotification);
                MyHud.Notifications.Add(m_hudNotification);

                CubeGrid.GridSystems.CameraSystem.SetAsCurrent(this);
                SetView();
                if (MyGuiScreenTerminal.IsOpen)
                {
                    MyGuiScreenTerminal.Hide();
                }

            }
        }

        public void SetView()
        {
            if (!MyFakes.ENABLE_CAMERA_BLOCK)
            {
                return;
            }
            var block = MySession.Static.CameraController as MyCameraBlock;
            if (block != null)
            {
                var oldCamera = block;
                oldCamera.IsActive = false;
            }

            MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, this);

            SetFov(m_fov);

            IsActive = true;
        }

        private static void SetFov(float fov)
        {
            System.Diagnostics.Debug.Assert(fov >= MIN_FOV && fov <= MAX_FOV, "FOV for camera has invalid values");
            fov = MathHelper.Clamp(fov, MIN_FOV, MAX_FOV);
            
            MySector.MainCamera.FieldOfView = fov;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                MyStringHash.GetOrCompute(BlockDefinition.ResourceSinkGroup),
                BlockDefinition.RequiredPowerInput,
                CalculateRequiredPowerInput);

            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
            sinkComp.RequiredInputChanged += Receiver_RequiredInputChanged;

            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
            sinkComp.Update();

            var ob = objectBuilder as MyObjectBuilder_CameraBlock;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            IsWorkingChanged += MyCameraBlock_IsWorkingChanged;

            IsInFirstPersonView = true;

            if (ob.IsActive)
            {
                m_requestActivateAfterLoad = true;
                ob.IsActive = false;
            }

            OnChangeFov(ob.Fov);

            UpdateText();
        }

        void MyCameraBlock_IsWorkingChanged(MyCubeBlock obj)
        {
            CubeGrid.GridSystems.CameraSystem.CheckCurrentCameraStillValid();
            if (m_requestActivateAfterLoad && IsWorking)
            {
                m_requestActivateAfterLoad = false;
                RequestSetView();
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (MyFakes.ENABLE_CAMERA_BLOCK)
            {
                if (CubeGrid.GridSystems.CameraSystem.CurrentCamera == this)
                {
                    m_fov = VRageMath.MathHelper.Lerp(m_fov, m_targetFov, 0.5f);
                    SetFov(m_fov);
                }
            }
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
            if (MyFakes.ENABLE_CAMERA_BLOCK)
            {
                ResourceSink.Update();
            }
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity();
			ResourceSink.Update();
        }

        public void OnExitView()
        {
            IsActive = false;
            m_syncFov.Value =  m_fov;
        }

        protected override void OnEnabledChanged()
        {
			ResourceSink.Update();
            UpdateEmissivity();
            
            base.OnEnabledChanged();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_CameraBlock objectBuilder = (MyObjectBuilder_CameraBlock)base.GetObjectBuilderCubeBlock(copy);
            objectBuilder.IsActive = IsActive;
            objectBuilder.Fov = m_fov;

            return objectBuilder;
        }

        void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
        }
        
        void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            UpdateEmissivity();
            UpdateText();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        protected override bool CheckIsWorking()
        {
			return ResourceSink.IsPowered && base.CheckIsWorking();
        }

        private void UpdateEmissivity()
        {
            if (IsWorking)
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Red, Color.White);
            }
            else
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Black, Color.White);
            }
        }

        void Receiver_RequiredInputChanged(MyDefinitionId resourceTypeId, MyResourceSinkComponent receiver, float oldRequirement, float newRequirement)
        {
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
            RaisePropertiesChanged();
        }

        float CalculateRequiredPowerInput()
        {
            return BlockDefinition.RequiredPowerInput;
        }

        public override MatrixD GetViewMatrix()
        {
            var worldMatrix = WorldMatrix;
            worldMatrix.Translation += WorldMatrix.Forward * 0.2f;
            MatrixD viewMatrix;

            MatrixD.Invert(ref worldMatrix, out viewMatrix);
            
            return viewMatrix;
        }

        public void Rotate(Vector2 rotationIndicator, float rollIndicator)
        {
            //Do nothing
        }

        public void RotateStopped()
        {
            //Do nothing
        }

        public void OnAssumeControl(IMyCameraController previousCameraController)
        {
        }
        public void OnReleaseControl(IMyCameraController newCameraController)
        {
        }

        void IMyCameraController.ControlCamera(MyCamera currentCamera)
        {
            currentCamera.SetViewMatrix(GetViewMatrix());
        }

        void IMyCameraController.Rotate(Vector2 rotationIndicator, float rollIndicator)
        {
            Rotate(rotationIndicator, rollIndicator);
        }

        void IMyCameraController.RotateStopped()
        {
            RotateStopped();
        }

        void IMyCameraController.OnAssumeControl(IMyCameraController previousCameraController)
        {
            if (!(previousCameraController is MyCameraBlock))
                MyGridCameraSystem.PreviousNonCameraBlockController = previousCameraController;

            OnAssumeControl(previousCameraController);
        }

        void IMyCameraController.OnReleaseControl(IMyCameraController newCameraController)
        {
            OnReleaseControl(newCameraController);
        }

        bool IMyCameraController.IsInFirstPersonView
        {
            get
            {
                return IsInFirstPersonView;
            }
            set
            {
                IsInFirstPersonView = value;
            }
        }

        bool IMyCameraController.ForceFirstPersonCamera
        {
            get
            {
                return ForceFirstPersonCamera;
            }
            set
            {
                ForceFirstPersonCamera = value;
            }
        }

        bool IMyCameraController.HandleUse()
        {
            MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
            CubeGrid.GridSystems.CameraSystem.ResetCamera();

            if (MySession.Static.ControlledEntity is MyRemoteControl)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        bool IMyCameraController.HandlePickUp()
        {
            return false;
        }

        bool IMyCameraController.AllowCubeBuilding
        {
            get
            {
                return false;
            }
        }

        internal void ChangeZoom(int deltaZoom)
        {
            if (deltaZoom > 0)
            {
                m_targetFov -= 0.15f;
                if (m_targetFov < BlockDefinition.MinFov)
                {
                    m_targetFov = BlockDefinition.MinFov;
                }
            }
            else
            {
                m_targetFov += 0.15f;
                if (m_targetFov > BlockDefinition.MaxFov)
                {
                    m_targetFov = BlockDefinition.MaxFov;
                }
            }

            SetFov(m_fov);
        }

        internal void OnChangeFov(float newFov)
        {
            m_fov = newFov;
            if (m_fov > BlockDefinition.MaxFov)
            {
                m_fov = BlockDefinition.MaxFov;
            }
            m_targetFov = m_fov;
        }

        void OnSyncFov()
        {
            if (IsActive == false)
            {
                OnChangeFov(m_syncFov);
            }
        }
    }
}
