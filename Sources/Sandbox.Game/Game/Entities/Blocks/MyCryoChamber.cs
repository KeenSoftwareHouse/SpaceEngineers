using Sandbox.Common.ObjectBuilders;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_CryoChamber))]
    class MyCryoChamber : MyCockpit, IMyPowerConsumer
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

        public MyPowerReceiver PowerReceiver
        {
            get;
            protected set;
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

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Utility,
                false,
                BlockDefinition.IdlePowerConsumption,
                this.CalculateRequiredPowerInput);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;

            NeedsUpdate |= Common.MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        private float CalculateRequiredPowerInput()
        {
            return BlockDefinition.IdlePowerConsumption;
        }

        void PowerDistributor_PowerStateChaged(MyPowerStateEnum newState)
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

            m_rechargeSocket.PowerDistributor.PowerStateChaged += PowerDistributor_PowerStateChaged;
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
            pilot.EnableJetpack(false, false, false, false);
            pilot.Sit(true, MySession.LocalCharacter == pilot, false, BlockDefinition.CharacterAnimation);

            pilot.SuitBattery.Enabled = true;

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
            PowerReceiver.Update();
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
        }

        private bool IsPowered()
        {
            if (PowerReceiver == null || !PowerReceiver.IsPowered) return false;
            return m_rechargeSocket != null && m_rechargeSocket.PowerDistributor != null && m_rechargeSocket.PowerDistributor.PowerState != MyPowerStateEnum.NoPower;
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
