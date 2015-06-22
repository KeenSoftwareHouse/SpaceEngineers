using System.Text;
using Havok;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;

using VRageMath;
using System.Diagnostics;
using System;
using Sandbox.Game.World;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Engine.Multiplayer;
using SteamSDK;
using System.Reflection;
using Sandbox.Common;
using Sandbox.Game.Components;
using Sandbox.Definitions;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Entities.Blocks;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.ModAPI;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities;
using Sandbox.Game;
using Sandbox;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Terminal.Controls;

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_MedicalRoom))]
    public class MyMedicalRoom : MyFunctionalBlock, IMyRechargeSocketOwner, IMyPowerConsumer, IMyMedicalRoom, IMyOxygenConsumer
    {
        #region Sync class
        [PreloadRequired]
        class SyncClass
        {
            private MyMedicalRoom m_block;

            [MessageIdAttribute(2482, P2PMessageEnum.Reliable)]
            protected struct UseMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public long UsedByEntityId;
                public UseActionEnum ActionEnum;

                public override string ToString()
                {
                    return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
                }
            }

            static SyncClass()
            {
                MySyncLayer.RegisterMessage<UseMsg>(UseRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterMessage<UseMsg>(UseSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            }

            public SyncClass(MyMedicalRoom block)
            {
                m_block = block;
            }

            public void RequestUse(UseActionEnum actionEnum, MyCharacter user)
            {
                var msg = new UseMsg();

                msg.EntityId = m_block.EntityId;
                msg.UsedByEntityId = user.EntityId;
                msg.ActionEnum = actionEnum;

                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);

            }

            private static void UseRequest(ref UseMsg msg, MyNetworkClient sender)
            {
                MyCharacter user;
                MyMedicalRoom medicalRoom;
                MyEntities.TryGetEntityById(msg.EntityId, out medicalRoom);
                MyEntities.TryGetEntityById(msg.UsedByEntityId, out user);
                if (user != null && medicalRoom != null)
                {
                    Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
                }
            }

            private static void UseSuccess(ref UseMsg msg, MyNetworkClient sender)
            {
                MyMedicalRoom medicalRoom;
                MyCharacter user;
                MyEntities.TryGetEntityById(msg.EntityId, out medicalRoom);
                MyEntities.TryGetEntityById(msg.UsedByEntityId, out user);
                if (medicalRoom != null && user != null)
                {
                    medicalRoom.UseInternal(msg.ActionEnum, user);
                }
            }
        }
        #endregion

        private MySoundPair m_idleSound;
        private MySoundPair m_progressSound;

        private MyRechargeSocket m_rechargeSocket;
        private MyCharacter m_user;
        private int m_lastTimeUsed;

        private MyEntity3DSoundEmitter m_idleSoundEmitter;
        private MyEntity3DSoundEmitter m_progressSoundEmitter;
        private float m_requiredPowerInput;
        private new SyncClass SyncObject;

        protected bool m_takeSpawneeOwnership = false;

        //obsolete, use IDModule
        private ulong SteamUserId { get; set; }

        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get
            {
                return m_conveyorEndpoint;
            }
        }
        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
        }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        static MyMedicalRoom()
        {
            //terminal:
            var label = new MyTerminalControlLabel<MyMedicalRoom>("ScenarioControls", MySpaceTexts.TerminalScenarioSettingsLabel);
            var ownershipCheckbox = new MyTerminalControlCheckbox<MyMedicalRoom>("TakeOwnership", MySpaceTexts.MedicalRoom_ownershipAssignmentLabel, MySpaceTexts.MedicalRoom_ownershipAssignmentTooltip);
            ownershipCheckbox.Getter = (x) => x.m_takeSpawneeOwnership;
            ownershipCheckbox.Setter = (x, val) =>
            {
                x.m_takeSpawneeOwnership = val;
            };
            ownershipCheckbox.Enabled = (x) => MySession.Static.Settings.ScenarioEditMode;
            MyTerminalControlFactory.AddControl(label);
            MyTerminalControlFactory.AddControl(ownershipCheckbox);

        }

        public MyMedicalRoom()
        {
            m_idleSoundEmitter = new MyEntity3DSoundEmitter(this);
            m_progressSoundEmitter = new MyEntity3DSoundEmitter(this);

            m_progressSoundEmitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.ShouldPlay2D].Add((Func<bool>) (() => MySession.ControlledEntity != null && m_user == MySession.ControlledEntity.Entity));
            if (MySession.Static != null && MyFakes.ENABLE_NEW_SOUNDS && MySession.Static.Settings.RealisticSound)
            {
                m_progressSoundEmitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.CanHear].Add((Func<bool>) (() => MySession.ControlledEntity != null && m_user == MySession.ControlledEntity.Entity));
            }

            m_requiredPowerInput = MyEnergyConstants.MAX_REQUIRED_POWER_MEDICAL_ROOM/10f;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            if (BlockDefinition is MyMedicalRoomDefinition)
            {
                var def = (MyMedicalRoomDefinition)BlockDefinition;
                m_idleSound = new MySoundPair(def.IdleSound);
                m_progressSound = new MySoundPair(def.ProgressSound);
            }
            else
            {
                m_idleSound = new MySoundPair("BlockMedical");
                m_progressSound = new MySoundPair("BlockMedicalProgress");
            }

            m_rechargeSocket = new MyRechargeSocket();

            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            SteamUserId = (objectBuilder as MyObjectBuilder_MedicalRoom).SteamUserId;

            if (SteamUserId != 0) //backward compatibility
            {
                MyPlayer controller = Sync.Players.TryGetPlayerById(new MyPlayer.PlayerId(SteamUserId));
                if (controller != null)
                {
                    IDModule.Owner = controller.Identity.IdentityId;
                    IDModule.ShareMode = MyOwnershipShareModeEnum.Faction;
                }
            }
            SteamUserId = 0;

            m_takeSpawneeOwnership = (objectBuilder as MyObjectBuilder_MedicalRoom).TakeOwnership;

            SyncObject = new SyncClass(this);

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            InitializeConveyorEndpoint();

            PowerReceiver = new MyPowerReceiver(
                group: MyConsumerGroupEnum.Utility,
                isAdaptible: false,
                maxRequiredInput: MyEnergyConstants.MAX_REQUIRED_POWER_MEDICAL_ROOM,
                requiredInputFunc: () => (Enabled && IsFunctional) ? PowerReceiver.MaxRequiredInput : 0f);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.Update();
            AddDebugRenderComponent(new MyDebugRenderComponentDrawPowerReciever(PowerReceiver, this));
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        protected override void OnStopWorking()
        {
            StopIdleSound();
        }

        protected override void OnStartWorking()
        {
            StartIdleSound();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_MedicalRoom)base.GetObjectBuilderCubeBlock(copy);
            builder.SteamUserId = SteamUserId;
            builder.IdleSound = m_idleSound.ToString();
            builder.ProgressSound = m_progressSound.ToString();
            builder.TakeOwnership = m_takeSpawneeOwnership;
            return builder;
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            m_idleSoundEmitter.Update();
            m_progressSoundEmitter.Update();
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();

            if (m_user == null)
                return;

            if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeUsed < MyEnergyConstants.RECHARGE_TIMEOUT)
                return;

            StopProgressLoopSound();

            m_rechargeSocket.Unplug();
            m_requiredPowerInput = MyEnergyConstants.MAX_REQUIRED_POWER_MEDICAL_ROOM / 10f;
            m_user = null;
        }


        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();

            if (CubeGrid.GridSystems.OxygenSystem != null)
            {
                CubeGrid.GridSystems.OxygenSystem.RegisterOxygenBlock(this);
            }
        }

        public override void OnUnregisteredFromGridSystems()
        {
            base.OnUnregisteredFromGridSystems();

            if (CubeGrid.GridSystems.OxygenSystem != null)
            {
                CubeGrid.GridSystems.OxygenSystem.UnregisterOxygenBlock(this);
            }
        }

        public void Use(UseActionEnum actionEnum, MyCharacter user)
        {
            var relation = GetUserRelationToOwner(user.ControllerInfo.Controller.Player.Identity.IdentityId);
            if (relation != MyRelationsBetweenPlayerAndBlock.Owner && relation != MyRelationsBetweenPlayerAndBlock.FactionShare)
            {
                if (user.ControllerInfo.Controller.Player == MySession.LocalHumanPlayer)
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.AccessDenied);
                }
                return;
            }

            if(actionEnum == UseActionEnum.OpenTerminal)
            {
                MyGuiScreenTerminal.Show(MyTerminalPageEnum.ControlPanel, user, this);
            }
            else if (actionEnum == UseActionEnum.Manipulate)
            {
                if (m_user != null && m_user != user)
                    return;
                SyncObject.RequestUse(actionEnum, user);
            }
        }

        MyRechargeSocket IMyRechargeSocketOwner.RechargeSocket
        {
            get { return m_rechargeSocket; }
        }

        protected override void Closing()
        {
            StopIdleSound();
            StopProgressLoopSound();
            if (m_user != null)
                m_rechargeSocket.Unplug();
            m_user = null;
            base.Closing();
        }

        private void StopIdleSound()
        {
            m_idleSoundEmitter.StopSound(false);
        }

        private void StartIdleSound()
        {
            m_idleSoundEmitter.PlaySingleSound(m_idleSound, true);
        }

        private void StartProgressLoopSound()
        {
            m_progressSoundEmitter.PlaySingleSound(m_progressSound, true);
        }

        public void StopProgressLoopSound()
        {
            m_progressSoundEmitter.StopSound(false);
        }

        private void UseInternal(UseActionEnum actionEnum, MyCharacter user)
        {
            if (!IsWorking)
                return;
            if (m_user == null)
            {
                m_user = user;
                m_rechargeSocket.PlugIn(m_user.SuitBattery);
                StartProgressLoopSound();
            }
            m_lastTimeUsed = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            if (IsWorking)
            {
                m_user.AddHealth(0.075f);
            }
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
            PowerReceiver.Update();
        }

        public static Matrix GetSafePlaceRelative()
        {
            //return -WorldMatrix.Up + WorldMatrix.Right;
            return Matrix.CreateTranslation(1, -1, 0);
        }

        public bool HasSpawnPosition()
        {
             MyModelDummy dummy;
             return Model.Dummies.TryGetValue("dummy detector_respawn", out dummy);
        }

        public MatrixD GetSpawnPosition()
        {
            MatrixD dummyLocal = MatrixD.Identity;
            MyModelDummy dummy;

            if (Model.Dummies.TryGetValue("dummy detector_respawn", out dummy))
            {
                dummyLocal = dummy.Matrix;
            }

            MatrixD worldMatrix = MatrixD.Multiply(dummyLocal, WorldMatrix);
            return worldMatrix;
        }
         
        public float GetOxygenLevel()
        {
            if (!MySession.Static.Settings.EnableOxygen)
            {
                return 0f;
            }

            var oxygenBlock = CubeGrid.GridSystems.OxygenSystem.GetOxygenBlock(WorldMatrix.Translation);

            if (oxygenBlock.Room == null || !oxygenBlock.Room.IsPressurized)
            {
                return 0f;
            }

            return oxygenBlock.OxygenLevel(CubeGrid.GridSize);
        }

        bool IMyOxygenBlock.IsWorking()
        {
            return IsWorking && m_user != null;
        }

        int IMyOxygenConsumer.GetPriority()
        {
            return 0;
        }

        float IMyOxygenConsumer.ConsumptionNeed(float deltaTime)
        {
            if (m_user == null)
            {
                return 0f;
            }

            return Math.Min(MyOxygenConstants.OXYGEN_REGEN_PER_SECOND, m_user.SuitOxygenAmountMissing) * deltaTime;
        }

        void IMyOxygenConsumer.Consume(float amount)
        {
            if (amount <= 0f || m_user == null)
            {
                return;
            }

            m_user.SuitOxygenAmount += amount;
        }

        public void TryTakeSpawneeOwnership(MyPlayer player)
        {
            if (MySession.Static.IsScenario && m_takeSpawneeOwnership && Sync.IsServer && OwnerId == 0)
                ChangeBlockOwnerRequest(player.Identity.IdentityId, MyOwnershipShareModeEnum.None);
        }
    }
}
