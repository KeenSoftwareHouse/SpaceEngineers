using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity.UseObject;
using VRage.Game.ModAPI;
using VRage.Import;
using VRage.ModAPI;
using VRage.Network;
using VRage.Utils;
using VRageMath;
using VRageRender.Import;

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_MedicalRoom))]
    public partial class MyMedicalRoom : MyFunctionalBlock, IMyRechargeSocketOwner, IMyGasBlock, IMyMedicalRoom
    {
        private bool m_healingAllowed;
        private bool m_refuelAllowed;
        private bool m_suitChangeAllowed;
        private bool m_customWardrobesEnabled;
        private bool m_forceSuitChangeOnRespawn;
        private bool m_spawnWithoutOxygenEnabled;

        private HashSet<string> m_customWardrobeNames = new HashSet<string>();
        private string m_respawnSuitName = null;

        private MySoundPair m_idleSound;
        private MySoundPair m_progressSound;

        private MyRechargeSocket m_rechargeSocket;
        private MyCharacter m_user;
        private int m_lastTimeUsed;

        private readonly MyEntity3DSoundEmitter m_idleSoundEmitter;
        private readonly MyEntity3DSoundEmitter m_progressSoundEmitter;

        protected bool m_takeSpawneeOwnership = false;
        protected bool m_setFactionToSpawnee = false;
        public bool SetFactionToSpawnee { get { return m_setFactionToSpawnee; } }

	    private MyResourceSinkComponent m_sinkComponent;
	    public MyResourceSinkComponent SinkComp
	    {
			get { return m_sinkComponent; }
			set { if(Components.Contains(typeof(MyResourceSinkComponent))) Components.Remove<MyResourceSinkComponent>(); Components.Add<MyResourceSinkComponent>(value); m_sinkComponent = value; }
	    }

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
            return SinkComp.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }

        /// <summary>
        /// Disabling prevents healing characters.
        /// </summary>
        public bool HealingAllowed { set { m_healingAllowed = value; } get { return m_healingAllowed; } }
        /// <summary>
        /// Disabling prevents refueling suits.
        /// </summary>
        public bool RefuelAllowed { set { m_refuelAllowed = value; } get { return m_refuelAllowed; } }
        /// <summary>
        /// Disable to remove respawn component from medical room.
        /// </summary>
        public bool RespawnAllowed
        {
            set
            {
                if (value)
                {
                    if (Components.Get<MyRespawnComponent>() == null)
                        Components.Add<MyRespawnComponent>(new MyRespawnComponent());
                }
                else
                {
                    Components.Remove<MyRespawnComponent>();
                }
            }
            get
            {
                return Components.Get<MyRespawnComponent>() != null;
            }
        }
        /// <summary>
        /// Disable to prevent players from changing their suits.
        /// </summary>
        public bool SuitChangeAllowed { set { m_suitChangeAllowed = value; } get { return m_suitChangeAllowed; } }
        /// <summary>
        /// If set to true CustomWardrobeNames are used instead of all definitions when instantiating WardrobeScreen.
        /// </summary>
        public bool CustomWardrobesEnabled { set { m_customWardrobesEnabled = value; } get { return m_customWardrobesEnabled; } }
        /// <summary>
        /// Used when CustomWardrobes are enabled.
        /// </summary>
        public HashSet<string> CustomWardrobeNames { set { m_customWardrobeNames = value; } get { return m_customWardrobeNames; } }
        /// <summary>
        /// Use when you want to force suit change on respawn. Wont turn to true if RespawnSuitName is null.
        /// </summary>
        public bool ForceSuitChangeOnRespawn
        {
            set
            {
                if (value && m_respawnSuitName != null)
                    m_forceSuitChangeOnRespawn = value;
            }
            get { return m_forceSuitChangeOnRespawn; }
        }
        /// <summary>
        /// Name of suit into which would player be forced upon respawn.
        /// </summary>
        public string RespawnSuitName { set { m_respawnSuitName = value; } get { return m_respawnSuitName; } }
        /// <summary>
        /// Players wont be able to spawn in rooms that are not pressurised.
        /// </summary>
        public bool SpawnWithoutOxygenEnabled { set { m_spawnWithoutOxygenEnabled = value; } get { return m_spawnWithoutOxygenEnabled; } }

        public MyMedicalRoom()
        {
            CreateTerminalControls();

            m_idleSoundEmitter = new MyEntity3DSoundEmitter(this, true);
            m_progressSoundEmitter = new MyEntity3DSoundEmitter(this, true);

            m_progressSoundEmitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.ShouldPlay2D].Add((Func<bool>)(() => MySession.Static.ControlledEntity != null && m_user == MySession.Static.ControlledEntity.Entity));
            if (MySession.Static != null && MyFakes.ENABLE_NEW_SOUNDS && MySession.Static.Settings.RealisticSound)
            {
                m_progressSoundEmitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.CanHear].Add((Func<bool>)(() => MySession.Static.ControlledEntity != null && m_user == MySession.Static.ControlledEntity.Entity));
            }
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyMedicalRoom>())
                return;
            base.CreateTerminalControls();
            //terminal:
            var label = new MyTerminalControlLabel<MyMedicalRoom>(MySpaceTexts.TerminalScenarioSettingsLabel);
            var ownershipCheckbox = new MyTerminalControlCheckbox<MyMedicalRoom>("TakeOwnership", MySpaceTexts.MedicalRoom_ownershipAssignmentLabel, MySpaceTexts.MedicalRoom_ownershipAssignmentTooltip);
            ownershipCheckbox.Getter = (x) => x.m_takeSpawneeOwnership;
            ownershipCheckbox.Setter = (x, val) =>
            {
                x.m_takeSpawneeOwnership = val;
            };
            ownershipCheckbox.Enabled = (x) => MySession.Static.Settings.ScenarioEditMode;
            MyTerminalControlFactory.AddControl(label);
            MyTerminalControlFactory.AddControl(ownershipCheckbox);

            var factionCheckbox = new MyTerminalControlCheckbox<MyMedicalRoom>("SetFaction", MySpaceTexts.MedicalRoom_factionAssignmentLabel, MySpaceTexts.MedicalRoom_factionAssignmentTooltip);
            factionCheckbox.Getter = (x) => x.m_setFactionToSpawnee;
            factionCheckbox.Setter = (x, val) =>
            {
                x.m_setFactionToSpawnee = val;
            };
            factionCheckbox.Enabled = (x) => MySession.Static.Settings.ScenarioEditMode;
            MyTerminalControlFactory.AddControl(factionCheckbox);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            var medicalRoomDefinition = BlockDefinition as MyMedicalRoomDefinition;
            MyStringHash resourceSinkGroup;
            if (medicalRoomDefinition != null)
            {
                m_idleSound = new MySoundPair(medicalRoomDefinition.IdleSound);
                m_progressSound = new MySoundPair(medicalRoomDefinition.ProgressSound);
                resourceSinkGroup = MyStringHash.GetOrCompute(medicalRoomDefinition.ResourceSinkGroup);
            }
            else
            {
                m_idleSound = new MySoundPair("BlockMedical");
                m_progressSound = new MySoundPair("BlockMedicalProgress");
                resourceSinkGroup = MyStringHash.GetOrCompute("Utility");
            }

            SinkComp = new MyResourceSinkComponent();
            SinkComp.Init(
                resourceSinkGroup,
                MyEnergyConstants.MAX_REQUIRED_POWER_MEDICAL_ROOM,
                () => (Enabled && IsFunctional) ? SinkComp.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0f);
            SinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;

            base.Init(objectBuilder, cubeGrid);
	         
            m_rechargeSocket = new MyRechargeSocket();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            SteamUserId = (objectBuilder as MyObjectBuilder_MedicalRoom).SteamUserId;

            if (SteamUserId != 0) //backward compatibility
            {
                MyPlayer controller = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(SteamUserId));
                if (controller != null)
                {
                    IDModule.Owner = controller.Identity.IdentityId;
                    IDModule.ShareMode = MyOwnershipShareModeEnum.Faction;
                }
            }
            SteamUserId = 0;

            m_takeSpawneeOwnership = (objectBuilder as MyObjectBuilder_MedicalRoom).TakeOwnership;
            m_setFactionToSpawnee = (objectBuilder as MyObjectBuilder_MedicalRoom).SetFaction;
       
            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            InitializeConveyorEndpoint();
            SinkComp.Update();
			
            AddDebugRenderComponent(new MyDebugRenderComponentDrawPowerReciever(SinkComp, this));

            if (this.CubeGrid.CreatePhysics)
                Components.Add<MyRespawnComponent>(new MyRespawnComponent());

            m_healingAllowed                = medicalRoomDefinition.HealingAllowed;
            m_refuelAllowed                 = medicalRoomDefinition.RefuelAllowed;
            m_suitChangeAllowed             = medicalRoomDefinition.SuitChangeAllowed;
            m_customWardrobesEnabled        = medicalRoomDefinition.CustomWardrobesEnabled;
            m_forceSuitChangeOnRespawn      = medicalRoomDefinition.ForceSuitChangeOnRespawn;
            m_customWardrobeNames           = medicalRoomDefinition.CustomWardrobeNames;
            m_respawnSuitName               = medicalRoomDefinition.RespawnSuitName;
            m_spawnWithoutOxygenEnabled     = medicalRoomDefinition.SpawnWithoutOxygenEnabled;
            RespawnAllowed                  = medicalRoomDefinition.RespawnAllowed;
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
			SinkComp.Update();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_MedicalRoom)base.GetObjectBuilderCubeBlock(copy);
            builder.SteamUserId = SteamUserId;
            builder.IdleSound = m_idleSound.ToString();
            builder.ProgressSound = m_progressSound.ToString();
            builder.TakeOwnership = m_takeSpawneeOwnership;
            builder.SetFaction = m_setFactionToSpawnee;

            return builder;
        }

        public override void UpdateSoundEmitters()
        {
            base.UpdateSoundEmitters();
            if(m_idleSoundEmitter != null)
                m_idleSoundEmitter.Update();
            if(m_progressSoundEmitter != null)
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

            if (m_refuelAllowed)
            {
                m_rechargeSocket.Unplug();
                m_user.SuitBattery.ResourceSink.TemporaryConnectedEntity = null;
            }
            m_user = null;
        }

        public void Use(UseActionEnum actionEnum, MyCharacter user)
        {
            var relation = GetUserRelationToOwner(user.ControllerInfo.Controller.Player.Identity.IdentityId);
            var player = MyPlayer.GetPlayerFromCharacter(user);
            if (!(relation.IsFriendly() || (player != null && MySession.Static.IsUserSpaceMaster(player.Client.SteamUserId))))
            {
                if (user.ControllerInfo.Controller.Player == MySession.Static.LocalHumanPlayer)
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

                MyMultiplayer.RaiseEvent(this, x => x.RequestUse, actionEnum, user.EntityId);
            }
        }

        [Event,Reliable,Server,Broadcast]
        void RequestUse(UseActionEnum actionEnum, long userId)
        {
            MyCharacter character;
            MyEntities.TryGetEntityById<MyCharacter>(userId, out character);
            if (character != null)
            {
                UseInternal(actionEnum, character);
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
            if (m_user != null && m_refuelAllowed)
            {
                m_rechargeSocket.Unplug();
                m_user.SuitBattery.ResourceSink.TemporaryConnectedEntity = null;
            }
            m_user = null;
            base.Closing();
        }

        private void StopIdleSound()
        {
            m_idleSoundEmitter.StopSound(false);
        }

        private void StartIdleSound()
        {
            m_idleSoundEmitter.PlaySound(m_idleSound, true);
        }

        private void StartProgressLoopSound()
        {
            m_progressSoundEmitter.PlaySound(m_progressSound, true);
        }

        public void StopProgressLoopSound()
        {
            m_progressSoundEmitter.StopSound(false);
        }

        private void UseInternal(UseActionEnum actionEnum, MyCharacter user)
        {
            bool shouldPlay = false;

            if (!IsWorking)
                return;
            if (m_user == null)
            {
                m_user = user;
                if (m_refuelAllowed)
                {
                    m_user.SuitBattery.ResourceSink.TemporaryConnectedEntity = this;
                    m_rechargeSocket.PlugIn(m_user.SuitBattery.ResourceSink);
                    shouldPlay = true;
                }
            }
            m_lastTimeUsed = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            if (m_user.StatComp != null && m_healingAllowed)
            {
                m_user.StatComp.DoAction("MedRoomHeal");
                shouldPlay = true;
            }

            if (shouldPlay && !m_progressSoundEmitter.IsPlaying) StartProgressLoopSound();
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
			SinkComp.Update();
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

            MatrixD worldMatrix = MatrixD.Multiply(MatrixD.CreateTranslation(dummyLocal.Translation), WorldMatrix);
            return worldMatrix;
        }

        public float GetOxygenLevel()
        {
            if (!MySession.Static.Settings.EnableOxygen)
            {
                return 0f;
            }

            if (CubeGrid.GridSystems.GasSystem == null)
                return 0;

            var oxygenBlock = CubeGrid.GridSystems.GasSystem.GetOxygenBlock(WorldMatrix.Translation);

            if (oxygenBlock.Room == null || !oxygenBlock.Room.IsPressurized)
            {
                return 0f;
            }

            return oxygenBlock.OxygenLevel(CubeGrid.GridSize);
        }

        bool IMyGasBlock.IsWorking()
        {
            return IsWorking && m_user != null;
        }

        public void TrySetFaction(MyPlayer player)
        {
            if (MySession.Static.IsScenario && m_setFactionToSpawnee && Sync.IsServer && OwnerId != 0)
            {
                //if (null != MySession.Static.Factions.TryGetPlayerFaction(player.Identity.IdentityId))
                //    return;
                IMyFaction faction = MySession.Static.Factions.TryGetPlayerFaction(this.OwnerId);
                if (faction == null)
                    return;
                MyFactionCollection.SendJoinRequest(faction.FactionId, player.Identity.IdentityId);
                if (!faction.AutoAcceptMember)
                    MyFactionCollection.AcceptJoin(faction.FactionId, player.Identity.IdentityId);
            }
        }

        public void TryTakeSpawneeOwnership(MyPlayer player)
        {
            if (MySession.Static.IsScenario && m_takeSpawneeOwnership && Sync.IsServer && OwnerId == 0)
                ChangeBlockOwnerRequest(player.Identity.IdentityId, MyOwnershipShareModeEnum.None);
        }

        public static int AvailableMedicalRoomsCount(long playerId)
        {
            int ret = 0;
            List<MyCubeGrid> cubeGrids = MyEntities.GetEntities().OfType<MyCubeGrid>().ToList();

            foreach (var grid in cubeGrids)
            {
                grid.GridSystems.UpdatePower();
                foreach (var slimBlock in grid.GetBlocks())
                {
                    MyMedicalRoom medicalRoom = slimBlock.FatBlock as MyMedicalRoom;
                    if (medicalRoom != null)
                    {
                        medicalRoom.UpdateIsWorking();
                        if (medicalRoom.IsWorking && medicalRoom.HasPlayerAccess(playerId))
                            ret++;
                    }

                }
            }
            return ret;
        }

        #region IMyConveyorEndpointBlock implementation

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            return null;
        }

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            return null;
        }

        #endregion
    }
}
