﻿using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.ModAPI;
using Sandbox.Engine.Utils;
using Sandbox.Game.EntityComponents;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_CryoChamber))]
    public class MyCryoChamber : MyCockpit
    {
        private MatrixD m_characterDummy;
        private MatrixD m_cameraDummy;

        //Use this default if initialization fails
        private string m_overlayTextureName = "Textures\\GUI\\Screens\\cryopod_interior.dds";

        private MyPlayer.PlayerId? m_currentPlayerId;

        public override bool IsInFirstPersonView
        {
            get { return true; }
            set { }
        }

        private new MyCryoChamberDefinition BlockDefinition
        {
            get { return (MyCryoChamberDefinition)base.BlockDefinition; }
        }

        protected override MyStringId LeaveNotificationHintText { get { return MySpaceTexts.NotificationHintLeaveCryoChamber; } }

        public MyCryoChamber()
        {
            ControllerInfo.ControlAcquired += OnCryoChamberControlAcquired;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            m_cameraDummy = Matrix.Identity;
            m_characterDummy = Matrix.Identity;

            base.Init(objectBuilder, cubeGrid);

            var chamberOb = objectBuilder as MyObjectBuilder_CryoChamber;

            if (chamberOb.SteamId != null && chamberOb.SerialId != null)
            {
                m_currentPlayerId = new MyPlayer.PlayerId(chamberOb.SteamId.Value, chamberOb.SerialId.Value);
            }
            else
            {
                m_currentPlayerId = null;
            }

            var overlayTexture = BlockDefinition.OverlayTexture;
            if (!string.IsNullOrEmpty(overlayTexture))
            {
                m_overlayTextureName = overlayTexture;
            }

            var sinkComp = new MyResourceSinkComponent();
			sinkComp.Init(
                MyStringHash.GetOrCompute(BlockDefinition.ResourceSinkGroup),
                BlockDefinition.IdlePowerConsumption,
                this.CalculateRequiredPowerInput);
            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
	        ResourceSink = sinkComp;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        private float CalculateRequiredPowerInput()
        {
            return BlockDefinition.IdlePowerConsumption;
        }

        void PowerDistributor_PowerStateChaged(MyResourceStateEnum newState)
        {
            UpdateIsWorking();
            UpdateEmissivity();
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            UpdateEmissivity();
        }

        protected override void PostBaseInit()
        {
            base.PostBaseInit();

            TryGetDummies();
        }

        private void TryGetDummies()
        {
            if (Model != null)
            {
                MyModelDummy cameraDummy;
                Model.Dummies.TryGetValue("camera", out cameraDummy);
                if (cameraDummy != null)
                {
                    m_cameraDummy = MatrixD.Normalize(cameraDummy.Matrix);
                }

                MyModelDummy characterDummy;
                Model.Dummies.TryGetValue("character", out characterDummy);
                if (characterDummy != null)
                {
                    m_characterDummy = MatrixD.Normalize(characterDummy.Matrix);
                }
            }
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();

            TryGetDummies();
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            UpdateEmissivity();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var chamberOb = (MyObjectBuilder_CryoChamber)base.GetObjectBuilderCubeBlock(copy);

            if (m_currentPlayerId != null)
            {
                chamberOb.SteamId = m_currentPlayerId.Value.SteamId;
                chamberOb.SerialId = m_currentPlayerId.Value.SerialId;
            }

            return chamberOb;
        }

        protected override MySyncEntity OnCreateSync()
        {
            var sync = new MySyncCryoChamber(this);
            OnInitSync(sync);
            return sync;
        }

        protected override void PlacePilotInSeat(MyCharacter pilot)
        {
            pilot.EnableLights(false, false);

	        var jetpack = pilot.JetpackComp;
			if(jetpack != null)
				jetpack.TurnOnJetpack(false, false, false, false);

            pilot.Sit(true, MySession.LocalCharacter == pilot, false, BlockDefinition.CharacterAnimation);


            pilot.SuitBattery.ResourceSource.Enabled = true;

            pilot.PositionComp.SetWorldMatrix(m_characterDummy * WorldMatrix);
            UpdateEmissivity(true);
        }

        protected void OnCryoChamberControlAcquired(MyEntityController controller)
        {
            m_currentPlayerId = controller.Player.Id;
        }

        protected override void RemovePilotFromSeat(MyCharacter pilot)
        {
            m_currentPlayerId = null;
            UpdateEmissivity(false);
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            UpdateEmissivity();
        }

        public override UseActionResult CanUse(UseActionEnum actionEnum, IMyControllableEntity user)
        {
            if (IsWorking)
            {
                return base.CanUse(actionEnum, user);
            }
            else
            {
                return UseActionResult.Unpowered;
            }
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            UpdateEmissivity();
            ResourceSink.Update();
        }

        private void SetOverlay()
        {
            MyHudCameraOverlay.TextureName = m_overlayTextureName;
            MyHudCameraOverlay.Enabled = true;
        }

        private void UpdateEmissivity(bool isUsed)
        {
            UpdateIsWorking();

            if (IsWorking)
            {
                if (isUsed)
                {
                    MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Cyan, Color.White);
                }
                else
                {
                    MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
                }
            }
            else
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White);
            }

            if (MyFakes.ENABLE_OXYGEN_SOUNDS)
            {
                UpdateSound();
            }
        }

        private bool IsPowered()
        {
            if (ResourceSink == null || !ResourceSink.IsPowered) return false;
            return m_rechargeSocket != null && m_rechargeSocket.ResourceDistributor != null && m_rechargeSocket.ResourceDistributor.ResourceState != MyResourceStateEnum.NoPower;
        }

        protected override bool CheckIsWorking()
        {
            return base.CheckIsWorking() && IsPowered();
        }

        private void UpdateEmissivity()
        {
            UpdateEmissivity(Pilot != null);
        }

        public override MatrixD GetHeadMatrix(bool includeY, bool includeX = true, bool forceHeadAnim = false, bool forceHeadBone = false)
        {
            return m_cameraDummy * PositionComp.WorldMatrix;
        }

        public override MatrixD GetViewMatrix()
        {
            MatrixD headMatrix = GetHeadMatrix(false);
            MatrixD viewMatrix;

            MatrixD.Invert(ref headMatrix, out viewMatrix);

            return viewMatrix;
        }

        public override MyToolbarType ToolbarType
        {
            get
            {
                return MyToolbarType.None;
            }
        }

        protected override void ComponentStack_IsFunctionalChanged()
        {
            //Cache the pilot reference
            var pilot = m_pilot;
            var controller = ControllerInfo.Controller;

            base.ComponentStack_IsFunctionalChanged();

            //Only kill him if his player is no longer online
            if (!IsFunctional && pilot != null && controller == null)
            {
                if (MySession.Static.CreativeMode)
                {
                    pilot.Close();
                }
                else
                {
                    pilot.DoDamage(1000f, MyDamageType.Unknown, false);
                }
            }

            UpdateEmissivity();
        }

        public override void OnUnregisteredFromGridSystems()
        {
            var pilot = m_pilot;
            var controller = ControllerInfo.Controller;

            base.OnUnregisteredFromGridSystems();

            //Only kill him if his player is no longer online
            if (pilot != null && controller == null)
            {
                if (MySession.Static.CreativeMode)
                {
                    pilot.Close();
                }
                //Pilot is killed by base in survival
            }

            m_soundEmitter.StopSound(true);
        }

        private bool IsLocalCharacterInside()
        {
            return MySession.LocalCharacter != null && MySession.LocalCharacter == Pilot;
        }

        private void UpdateSound()
        {
            if (IsWorking)
            {
                if (IsLocalCharacterInside())
                {
                    if (m_soundEmitter.SoundId != BlockDefinition.InsideSound.SoundId)
                    {
                        m_soundEmitter.PlaySound(BlockDefinition.InsideSound, true);
                    }
                }
                else
                {
                    if (m_soundEmitter.SoundId != BlockDefinition.OutsideSound.SoundId)
                    {
                        m_soundEmitter.PlaySound(BlockDefinition.OutsideSound, true);
                    }
                }
            }
            else
            {
                m_soundEmitter.StopSound(true);
            }

            m_soundEmitter.Update();
        }
        

        public void CameraAttachedToChanged(IMyCameraController oldController, IMyCameraController newController)
        {
            if (oldController == this)
            {
                MyHudCameraOverlay.Enabled = false;
                MyRenderProxy.UpdateRenderObjectVisibility(Render.RenderObjectIDs[0], true, false);
            }
            else if (newController == this)
            {
                SetOverlay();
                MyRenderProxy.UpdateRenderObjectVisibility(Render.RenderObjectIDs[0], false, false);
            }
        }

        protected override void sync_UseSuccess(UseActionEnum actionEnum, IMyControllableEntity user)
        {
            if (user.Entity == MySession.LocalCharacter)
            {
                MySession.Static.CameraAttachedToChanged += CameraAttachedToChanged;
            }

            base.sync_UseSuccess(actionEnum, user);
        }

        protected override void OnControlledEntity_Used()
        {
            var pilot = m_pilot;
            base.OnControlledEntity_Used();

            if (pilot != null && pilot == MySession.LocalCharacter)
            {
                MySession.SetCameraController(MyCameraControllerEnum.Entity, pilot);
                MySession.Static.CameraAttachedToChanged -= CameraAttachedToChanged;
            }
        }

        protected override void UpdateCockpitModel()
        {
            //Do nothing
        }

        public bool TryToControlPilot(MyPlayer player)
        {
            if (Pilot == null)
            {
                return false;
            }

            if (player.Id != m_currentPlayerId)
            {
                return false;
            }

            (SyncObject as MySyncCryoChamber).SendControlPilotMsg(player);

            return true;
        }

        internal void OnPlayerLoaded()
        {
            MySession.Static.CameraAttachedToChanged += CameraAttachedToChanged;
        }
    }
}
