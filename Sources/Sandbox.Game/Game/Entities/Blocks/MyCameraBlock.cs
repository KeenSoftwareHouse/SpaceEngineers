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
using Sandbox.ModAPI.Interfaces;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Voxels;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.Utils;
using VRage.Sync;
using VRageRender;
using VRageRender.Voxels;
using IMyCameraBlock = Sandbox.ModAPI.IMyCameraBlock;

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

        private int m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        private double m_availableScanRange;
        private double AvailableScanRange
        {
            get
            {
                if (this.IsWorking && EnableRaycast)
                {
                    m_availableScanRange = Math.Min(double.MaxValue, m_availableScanRange + (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime) * BlockDefinition.RaycastTimeMultiplier);
                    m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                }
                return m_availableScanRange;
            }
            set { m_availableScanRange = value; }
        }

        private float m_fov;
        private float m_targetFov;
        private RaycastInfo m_lastRay;

        private struct RaycastInfo
        {
            public Vector3D Start;
            public Vector3D End;
            public Vector3D? Hit;
            public double Distance;
        }

        public bool EnableRaycast { get; set; }

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

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyCameraBlock>())
                return;
            base.CreateTerminalControls();
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

            if (CubeGrid.GridSystems.CameraSystem.CurrentCamera == this)
            {
                m_fov = VRageMath.MathHelper.Lerp(m_fov, m_targetFov, 0.5f);
                SetFov(m_fov);
            }

            if (Math.Abs(m_fov - m_targetFov) < 0.01f)
            {
                NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
            }
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && m_lastRay.Distance!=0)
            {
                DrawDebug();
            }
        }
        
        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
        
            ResourceSink.Update();
        }

        /// <summary>
        /// Draws a frustum representing the valid scanning range, a line representing the last raycast, 
        /// and a sphere representing the last raycast hit.
        /// </summary>
        private void DrawDebug()
        {
            MyRenderProxy.DebugDrawLine3D(m_lastRay.Start, m_lastRay.End, Color.Orange, Color.Orange, false);
            if (m_lastRay.Hit.HasValue)
                MyRenderProxy.DebugDrawSphere(m_lastRay.Hit.Value, 1, Color.Orange, 1, false);

            double distance = m_lastRay.Distance / Math.Cos(MathHelper.ToRadians(BlockDefinition.RaycastConeLimit));

            //calculate the extremes of the scan area and draw the thing manually
            Vector3D[] corners = new Vector3D[4];

            var startPos = this.WorldMatrix.Translation;
            var forwardDir = this.WorldMatrix.Forward;

            float pitch = MathHelper.ToRadians(-BlockDefinition.RaycastConeLimit);
            float yaw = MathHelper.ToRadians(-BlockDefinition.RaycastConeLimit);
            var pitchMatrix = MatrixD.CreateFromAxisAngle(this.WorldMatrix.Right, pitch);
            var yawMatrix = MatrixD.CreateFromAxisAngle(this.WorldMatrix.Down, yaw);
            Vector3D direction;
            Vector3D intermediateDirection;
            Vector3D.RotateAndScale(ref forwardDir, ref pitchMatrix, out intermediateDirection);
            Vector3D.RotateAndScale(ref intermediateDirection, ref yawMatrix, out direction);

            corners[0] = startPos + direction * distance;

            pitch = MathHelper.ToRadians(-BlockDefinition.RaycastConeLimit);
            yaw = MathHelper.ToRadians(BlockDefinition.RaycastConeLimit);
            pitchMatrix = MatrixD.CreateFromAxisAngle(this.WorldMatrix.Right, pitch);
            yawMatrix = MatrixD.CreateFromAxisAngle(this.WorldMatrix.Down, yaw);
            Vector3D.RotateAndScale(ref forwardDir, ref pitchMatrix, out intermediateDirection);
            Vector3D.RotateAndScale(ref intermediateDirection, ref yawMatrix, out direction);

            corners[1] = startPos + direction * distance;

            pitch = MathHelper.ToRadians(BlockDefinition.RaycastConeLimit);
            yaw = MathHelper.ToRadians(BlockDefinition.RaycastConeLimit);
            pitchMatrix = MatrixD.CreateFromAxisAngle(this.WorldMatrix.Right, pitch);
            yawMatrix = MatrixD.CreateFromAxisAngle(this.WorldMatrix.Down, yaw);
            Vector3D.RotateAndScale(ref forwardDir, ref pitchMatrix, out intermediateDirection);
            Vector3D.RotateAndScale(ref intermediateDirection, ref yawMatrix, out direction);

            corners[2] = startPos + direction * distance;

            pitch = MathHelper.ToRadians(BlockDefinition.RaycastConeLimit);
            yaw = MathHelper.ToRadians(-BlockDefinition.RaycastConeLimit);
            pitchMatrix = MatrixD.CreateFromAxisAngle(this.WorldMatrix.Right, pitch);
            yawMatrix = MatrixD.CreateFromAxisAngle(this.WorldMatrix.Down, yaw);
            Vector3D.RotateAndScale(ref forwardDir, ref pitchMatrix, out intermediateDirection);
            Vector3D.RotateAndScale(ref intermediateDirection, ref yawMatrix, out direction);

            corners[3] = startPos + direction * distance;

            MyRenderProxy.DebugDrawLine3D(startPos, corners[0], Color.Blue, Color.Blue, false);
            MyRenderProxy.DebugDrawLine3D(startPos, corners[1], Color.Blue, Color.Blue, false);
            MyRenderProxy.DebugDrawLine3D(startPos, corners[2], Color.Blue, Color.Blue, false);
            MyRenderProxy.DebugDrawLine3D(startPos, corners[3], Color.Blue, Color.Blue, false);

            MyRenderProxy.DebugDrawLine3D(corners[0], corners[1], Color.Blue, Color.Blue, false);
            MyRenderProxy.DebugDrawLine3D(corners[1], corners[2], Color.Blue, Color.Blue, false);
            MyRenderProxy.DebugDrawLine3D(corners[2], corners[3], Color.Blue, Color.Blue, false);
            MyRenderProxy.DebugDrawLine3D(corners[3], corners[0], Color.Blue, Color.Blue, false);
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
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
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
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.RequiredInput, DetailedInfo);
            RaisePropertiesChanged();
        }

        float CalculateRequiredPowerInput()
        {
            if (!EnableRaycast)
                return BlockDefinition.RequiredPowerInput;
            return BlockDefinition.RequiredChargingInput;
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

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

            SetFov(m_fov);
        }

        internal void OnChangeFov(float newFov)
        {
            m_fov = newFov;
            if (m_fov > BlockDefinition.MaxFov)
            {
                m_fov = BlockDefinition.MaxFov;
            }

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

            m_targetFov = m_fov;
        }

        void OnSyncFov()
        {
            if (IsActive == false)
            {
                OnChangeFov(m_syncFov);
            }
        }

        private double m_angleLimitCosine = 0;
        
        /// <summary>
        /// Checks if the specified direction relative to the camera is within the valid scanning range
        /// </summary>
        /// <param name="directionNormalized"></param>
        /// <returns></returns>
        public bool CheckAngleLimits(Vector3D directionNormalized)
        {
            if (m_angleLimitCosine == 0)
                m_angleLimitCosine = Math.Cos(MathHelper.ToRadians(BlockDefinition.RaycastConeLimit));
            
            var projTargetFront = VectorProjection(directionNormalized, this.WorldMatrix.Forward);
            var projTargetLeft = VectorProjection(directionNormalized, this.WorldMatrix.Left);
            var projTargetUp = VectorProjection(directionNormalized, this.WorldMatrix.Up);
            var projTargetFrontLeft = projTargetFront + projTargetLeft;
            var projTargetFrontUp = projTargetFront + projTargetUp;
   
            var yawCheck = projTargetFrontLeft.Dot(this.WorldMatrix.Forward);
            var pitchCheck = projTargetFrontUp.Dot(this.WorldMatrix.Forward);

            return !(yawCheck < m_angleLimitCosine) && !(pitchCheck < m_angleLimitCosine);
        }

        private Vector3D VectorProjection(Vector3D a, Vector3D b)
        {
            return a.Dot(b) / b.LengthSquared() * b;
        }
        
        MyDetectedEntityInfo ModAPI.Ingame.IMyCameraBlock.Raycast(double distance, Vector3D targetDirection)
        {
            if(Vector3D.IsZero(targetDirection))
                throw new ArgumentOutOfRangeException("targetDirection", "Direction cannot be 0,0,0");

            targetDirection = Vector3D.TransformNormal(targetDirection, this.WorldMatrix);
            
            targetDirection.Normalize();

            if (CheckAngleLimits(targetDirection))
                return Raycast(distance, targetDirection);
            return new MyDetectedEntityInfo();
        }

        MyDetectedEntityInfo ModAPI.Ingame.IMyCameraBlock.Raycast(Vector3D targetPos)
        {
            Vector3D direction = Vector3D.Normalize(targetPos - this.WorldMatrix.Translation);
            
            if (CheckAngleLimits(direction))
                return Raycast(Vector3D.Distance(targetPos, this.WorldMatrix.Translation), direction);
            return new MyDetectedEntityInfo();
        }

        MyDetectedEntityInfo ModAPI.Ingame.IMyCameraBlock.Raycast(double distance, float pitch, float yaw)
        {
            if(pitch > BlockDefinition.RaycastConeLimit || yaw > BlockDefinition.RaycastConeLimit)
                return new MyDetectedEntityInfo();

            pitch = MathHelper.ToRadians(pitch);
            yaw = MathHelper.ToRadians(yaw);
            
            var forwardDir = this.WorldMatrix.Forward;
            var pitchMatrix = MatrixD.CreateFromAxisAngle(this.WorldMatrix.Right, pitch);
            //right hand rule!
            var yawMatrix = MatrixD.CreateFromAxisAngle(this.WorldMatrix.Down, yaw);
            Vector3D direction;
            Vector3D intermediateDirection;
            Vector3D.RotateAndScale(ref forwardDir, ref pitchMatrix, out intermediateDirection);
            Vector3D.RotateAndScale(ref intermediateDirection, ref yawMatrix, out direction);

            return Raycast(distance, direction);
        }

        public MyDetectedEntityInfo Raycast(double distance, Vector3D direction)
        {
            if (Vector3D.IsZero(direction))
                throw new ArgumentOutOfRangeException("direction", "Direction cannot be 0,0,0");

            //mods can disable raycast on a block by setting the distance limit to 0 (-1 means infinite)
            if (distance <= 0 || (BlockDefinition.RaycastDistanceLimit > -1 && distance > BlockDefinition.RaycastDistanceLimit))
                return new MyDetectedEntityInfo();

            if (AvailableScanRange < distance || !this.CheckIsWorking())
                return new MyDetectedEntityInfo();
            
            AvailableScanRange -= distance;
            
            var startPos = this.WorldMatrix.Translation;
            var targetPos = startPos + direction * distance;
            
            //try a physics raycast first
            //very accurate, but very slow
            List<MyPhysics.HitInfo> hits = new List<MyPhysics.HitInfo>();
            MyPhysics.CastRay(startPos, targetPos, hits);

            foreach (var hit in hits)
            {
                var entity = (MyEntity)hit.HkHitInfo.GetHitEntity();
                if (entity == this)
                    continue;

                m_lastRay = new RaycastInfo() { Distance = distance, Start = startPos, End = targetPos, Hit = hit.Position };
                return MyDetectedEntityInfoHelper.Create(entity, this.OwnerId, hit.Position);
            }
            
            //long-distance planet scanning
            //fastest way is to intersect planet bounding boxes then treat the planet as a sphere
            LineD line = new LineD(startPos, targetPos);
            var voxels = new List<MyLineSegmentOverlapResult<MyVoxelBase>>();
            MyGamePruningStructure.GetVoxelMapsOverlappingRay(ref line, voxels);

            foreach (var result in voxels)
            {
                var planet = result.Element as MyPlanet;
                if (planet == null)
                    continue;

                double distCenter = Vector3D.DistanceSquared(this.PositionComp.GetPosition(), planet.PositionComp.GetPosition());
                var gravComp = planet.Components.Get<MyGravityProviderComponent>();
                if (gravComp == null)
                    continue;

                if (!gravComp.IsPositionInRange(startPos) && distCenter > planet.MaximumRadius * planet.MaximumRadius)
                {
                    var boundingSphere = new BoundingSphereD(planet.PositionComp.GetPosition(), planet.MaximumRadius);
                    var rayd = new RayD(startPos, direction);
                    var intersection = boundingSphere.Intersects(rayd);

                    if (!intersection.HasValue)
                        continue;

                    if (distance < intersection.Value)
                        continue;

                    var hitPos = startPos + direction * intersection.Value;
                    m_lastRay = new RaycastInfo() {Distance = distance, Start = startPos, End = targetPos, Hit = hitPos};
                    return MyDetectedEntityInfoHelper.Create(result.Element, this.OwnerId, hitPos);
                }

                //if the camera is inside gravity, query voxel storage
                if (planet.RootVoxel.Storage == null)
                    continue;
                var start = Vector3D.Transform(line.From, planet.PositionComp.WorldMatrixInvScaled);
                start += planet.SizeInMetresHalf;
                var end = Vector3D.Transform(line.To, planet.PositionComp.WorldMatrixInvScaled);
                end += planet.SizeInMetresHalf;

                var voxRay = new LineD(start, end);

                double startOffset;
                double endOffset;
                if (!planet.RootVoxel.Storage.DataProvider.Intersect(ref voxRay, out startOffset, out endOffset))
                    continue;

                var from = voxRay.From;
                voxRay.From = from + voxRay.Direction * voxRay.Length * startOffset;
                voxRay.To = from + voxRay.Direction * voxRay.Length * endOffset;

                start = voxRay.From - planet.SizeInMetresHalf;
                start = Vector3D.Transform(start, planet.PositionComp.WorldMatrix);

                m_lastRay = new RaycastInfo() {Distance = distance, Start = startPos, End = targetPos, Hit = start};
                return MyDetectedEntityInfoHelper.Create(result.Element, this.OwnerId, start);
            }

            m_lastRay = new RaycastInfo() {Distance = distance, Start = startPos, End = targetPos, Hit = null};
            return new MyDetectedEntityInfo();
        }

        bool ModAPI.Ingame.IMyCameraBlock.CanScan(double distance)
        {
            if (BlockDefinition.RaycastDistanceLimit == -1)
                return distance <= AvailableScanRange;
            return distance <= AvailableScanRange && distance <= BlockDefinition.RaycastDistanceLimit;
        }
        
        double ModAPI.Ingame.IMyCameraBlock.AvailableScanRange
        {
            get { return AvailableScanRange; }
        }

        int ModAPI.Ingame.IMyCameraBlock.TimeUntilScan(double distance)
        {
            return (int)Math.Max((distance- AvailableScanRange) / BlockDefinition.RaycastTimeMultiplier , 0);
        }

        bool ModAPI.Ingame.IMyCameraBlock.EnableRaycast
        {
            get { return this.EnableRaycast; }
            set
            {
                this.EnableRaycast = value;
                this.ResourceSink.Update();
            }
        }

        float ModAPI.Ingame.IMyCameraBlock.RaycastConeLimit
        {
            get { return BlockDefinition.RaycastConeLimit; }
        }

        double ModAPI.Ingame.IMyCameraBlock.RaycastDistanceLimit
        {
            get { return BlockDefinition.RaycastDistanceLimit; }
        }
    }
}
