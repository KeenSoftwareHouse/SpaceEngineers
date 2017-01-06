#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Utils;
using Sandbox.Game.AI;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.UseObject;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Audio;
using VRage.FileSystem;
using VRageMath;
using VRage;
using VRage.Input;
using VRage.Game.Entity.UseObject;
using VRage.ModAPI;
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.EntityComponents;
using VRage.Utils;
using VRage.Game.Components;
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Localization;
using VRage.Game;
using VRage.Game.Definitions.Animation;
using VRage.Game.Gui;
using VRage.Game.Entity;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Serialization;
using VRage.ObjectBuilders;
using Sandbox.Game.Replication;
using VRage.Game.ModAPI;
using VRage.Game.Utils;
using VRage.Sync;
using Sandbox.Engine.Physics;

#endregion

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Cockpit))]
    public class MyCockpit : MyShipController, IMyCameraController, IMyUsableEntity, IMyCockpit, IMyConveyorEndpointBlock, IMyGasBlock
    {
        #region Fields
        float DEFAULT_FPS_CAMERA_X_ANGLE = -10;

        private bool m_isLargeCockpit;

        Vector3 m_playerHeadSpring;
        Vector3 m_playerHeadShakeDir;
        const float MinHeadLocalXAngle = -60;
        const float MaxHeadLocalXAngle = 70;
        const float MinHeadLocalYAngle = -90;
        const float MaxHeadLocalYAngle = 90;

        protected MyCharacter m_pilot = null;
        MyCharacter m_savedPilot = null;

        MyAutopilotBase m_aiPilot = null;
        public MyAutopilotBase AiPilot { get { return m_aiPilot; } }
        protected readonly Sync<Matrix?> m_pilotRelativeWorld;
        protected MyDefinitionId? m_pilotGunDefinition = null;

        private bool m_updateSink = false;

        float m_headLocalXAngle = 0;
        float m_headLocalYAngle = 0;

        private long m_lastGasInputUpdateTick;

        string m_cockpitInteriorModel;
        string m_cockpitGlassModel;

        bool m_defferAttach;

        private bool m_playIdleSound;

        private float m_currentCameraShakePower = 0f;

        bool? m_lastNearFlag = null;
        private int m_forcedFpsTimeoutMs = 0;
        private const int m_forcedFpsTimeoutDefaultMs = 500;

        protected Action<MyEntity> m_pilotClosedHandler;
        private bool? m_pilotJetpackEnabledBackup;
        public bool PilotJetpackEnabledBackup { get { return m_pilotJetpackEnabledBackup == null ? false : m_pilotJetpackEnabledBackup.Value; } }

        public float GlassDirt = 1.0f;

        //Backwards compatibility for MyThirdPersonSpectator
        //Default needs to be true
        private bool m_isInFirstPersonView = true;
        public virtual bool IsInFirstPersonView
        {
            get { return m_isInFirstPersonView; }
            set
            {
                bool oldValue = m_isInFirstPersonView;
                m_isInFirstPersonView = value;
                if (m_isInFirstPersonView != oldValue)
                {
                    UpdateCameraAfterChange();
                }
            }
        }

        public override bool ForceFirstPersonCamera
        {
            get
            {
                return base.ForceFirstPersonCamera && m_forcedFpsTimeoutMs <= 0;
            }
            set
            {
                if (value && !base.ForceFirstPersonCamera)
                    m_forcedFpsTimeoutMs = m_forcedFpsTimeoutDefaultMs;
                base.ForceFirstPersonCamera = value;
            }
        }

        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get
            {
                return m_conveyorEndpoint;
            }
        }

        private float m_oxygenFillLevel;
        public float OxygenFillLevel { get { return m_oxygenFillLevel; } private set { m_oxygenFillLevel = MathHelper.Clamp(value, 0f, 1f); } }

        public float OxygenAmount
        {
            get { return OxygenFillLevel * BlockDefinition.OxygenCapacity; }
            set
            {
                if (BlockDefinition.OxygenCapacity != 0f)
                    ChangeGasFillLevel(MathHelper.Clamp(value / BlockDefinition.OxygenCapacity, 0f, 1f));

                ResourceSink.Update();
            }
        }
        public float OxygenAmountMissing { get { return (1f - OxygenFillLevel) * BlockDefinition.OxygenCapacity; } }

        readonly Sync<long?> m_attachedCharacterId;
        readonly Sync<bool> m_storeOriginalPlayerWorldMatrix;

        bool m_retryAttachPilot = false;

        long? m_attachedCharacterIdSaved;

        bool m_pilotFirstPerson = false;
        #endregion

        #region Init
        public MyCockpit()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_pilotRelativeWorld = SyncType.CreateAndAddProp<Matrix?>();
            m_attachedCharacterId = SyncType.CreateAndAddProp<long?>();
            m_storeOriginalPlayerWorldMatrix = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            m_pilotClosedHandler = new Action<MyEntity>(m_pilot_OnMarkForClose);
            ResourceSink = new MyResourceSinkComponent(2);

            m_attachedCharacterId.ValueChanged += (o) => OnCharacterChanged();
            m_soundEmitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.ShouldPlay2D].Add((Func<bool>)ShouldPlay2D);
            m_pilotRelativeWorld.ValidateNever();
        }

        bool ShouldPlay2D()
        {
            return MySession.Static.LocalCharacter != null && Pilot == MySession.Static.LocalCharacter;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            PostBaseInit();

            //MyDebug.AssertDebug(objectBuilder.TypeId == typeof(MyObjectBuilder_Cockpit));
            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(objectBuilder.GetId());
            m_isLargeCockpit = (def.CubeSize == MyCubeSize.Large);
            m_cockpitInteriorModel = BlockDefinition.InteriorModel;
            m_cockpitGlassModel = BlockDefinition.GlassModel;

            MyObjectBuilder_Cockpit cockpitOb = (MyObjectBuilder_Cockpit)objectBuilder;
            if (cockpitOb.Pilot != null)
            {
                MyEntity pilotEntity;
                MyCharacter pilot = null;
                if (MyEntities.TryGetEntityById(cockpitOb.Pilot.EntityId, out pilotEntity))
                { //Pilot already exists, can be the case after cube grid split
                    pilot = (MyCharacter)pilotEntity;
                    if (pilot.IsUsing is MyShipController && pilot.IsUsing != this)
                    {
                        Debug.Assert(false, "Pilot already sits on another place!");
                        pilot = null;
                    }
                }
                else
                {
                    pilot = (MyCharacter)MyEntities.CreateFromObjectBuilder(cockpitOb.Pilot);
                }

                if (pilot != null)
                {
                    m_savedPilot = pilot;
                    if (cockpitOb.PilotRelativeWorld.HasValue)
                        m_pilotRelativeWorld.Value = cockpitOb.PilotRelativeWorld.Value.GetMatrix();
                    else
                        m_pilotRelativeWorld.Value = null;

                    m_singleWeaponMode = cockpitOb.UseSingleWeaponMode;
                }

                IsInFirstPersonView = cockpitOb.IsInFirstPersonView;
            }

            if (cockpitOb.Autopilot != null)
            {
                MyAutopilotBase autopilot = MyAutopilotFactory.CreateAutopilot(cockpitOb.Autopilot);
                autopilot.Init(cockpitOb.Autopilot);
                AttachAutopilot(autopilot, updateSync: false);
            }

            m_pilotGunDefinition = cockpitOb.PilotGunDefinition;

            // backward compatibility check for automatic rifle without subtype
            if (m_pilotGunDefinition.HasValue)
            {
                if (m_pilotGunDefinition.Value.TypeId == typeof(MyObjectBuilder_AutomaticRifle)
                    && string.IsNullOrEmpty(m_pilotGunDefinition.Value.SubtypeName))
                    m_pilotGunDefinition = new MyDefinitionId(typeof(MyObjectBuilder_AutomaticRifle), "RifleGun");
            }

            if (!string.IsNullOrEmpty(m_cockpitInteriorModel))
            {

                if (VRage.Game.Models.MyModels.GetModelOnlyDummies(m_cockpitInteriorModel).Dummies.ContainsKey("head"))
                    m_headLocalPosition = VRage.Game.Models.MyModels.GetModelOnlyDummies(m_cockpitInteriorModel).Dummies["head"].Matrix.Translation;
            }
            else
            {
                if (VRage.Game.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model).Dummies.ContainsKey("head"))
                    m_headLocalPosition = VRage.Game.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model).Dummies["head"].Matrix.Translation;
            }

            AddDebugRenderComponent(new Components.MyDebugRenderComponentCockpit(this));

            InitializeConveyorEndpoint();

            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));

            OxygenFillLevel = cockpitOb.OxygenLevel;

            var sinkDataList = new List<MyResourceSinkInfo>
	        {
				new MyResourceSinkInfo {ResourceTypeId = MyResourceDistributorComponent.ElectricityId, MaxRequiredInput = 0, RequiredInputFunc = CalculateRequiredPowerInput },
				new MyResourceSinkInfo {ResourceTypeId = MyCharacterOxygenComponent.OxygenId, MaxRequiredInput = BlockDefinition.OxygenCapacity, RequiredInputFunc = ComputeRequiredGas},
	        };
            ResourceSink.Init(MyStringHash.GetOrCompute("Utility"), sinkDataList);
            ResourceSink.CurrentInputChanged += Sink_CurrentInputChanged;
            m_lastGasInputUpdateTick = MySession.Static.ElapsedGameTime.Ticks;

            if (cockpitOb.AttachedPlayerId.HasValue == false && cockpitOb.Pilot != null && m_pilot!=null)
            {
                m_attachedCharacterIdSaved = m_pilot.EntityId;
            }
            else
            {
                m_attachedCharacterIdSaved = cockpitOb.AttachedPlayerId;
            }
            if (this.GetInventory() == null)
            {
                Vector3 inv = Vector3.One*1.0f;
                MyInventory inventory = new MyInventory(inv.Volume, inv, MyInventoryFlags.CanSend | MyInventoryFlags.CanReceive);
                Components.Add<MyInventoryBase>(inventory);
            }

            m_defferAttach = true;
        }

        protected virtual void PostBaseInit()
        {
        }

        float CalculateRequiredPowerInput()
        {
            return 0.0f;
        }

        float ComputeRequiredGas()
        {
            if (!IsWorking)
                return 0f;

            float inputRequiredToFillIn100Updates = OxygenAmountMissing * VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND / 100f;
            return Math.Min(inputRequiredToFillIn100Updates, ResourceSink.MaxRequiredInputByType(MyCharacterOxygenComponent.OxygenId) * 0.1f);
        }

        protected override void ComponentStack_IsFunctionalChanged()
        {
            base.ComponentStack_IsFunctionalChanged();

            if (IsFunctional)
            {
                Debug.Assert(m_pilot == null, "A pilot was found in a newly working cockpit. He shouldn't have been there.");
            }
            else
            {
                if (m_pilot != null)
                {
                    RemovePilot();
                    if (Sync.IsServer)
                    {
                        m_attachedCharacterId.Value = null;
                    }

                }

                ChangeGasFillLevel(0);
                ResourceSink.Update();
            }
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_Cockpit objectBuilder = (MyObjectBuilder_Cockpit)base.GetObjectBuilderCubeBlock(copy);

            objectBuilder.Pilot = (m_pilot != null && m_pilot.Save) ? (MyObjectBuilder_Character)m_pilot.GetObjectBuilder(copy) : null;
            objectBuilder.Autopilot = (m_aiPilot != null) ? m_aiPilot.GetObjectBuilder() : null;
            objectBuilder.PilotGunDefinition = m_pilotGunDefinition;

            if (m_pilotRelativeWorld.Value.HasValue)
            {
                objectBuilder.PilotRelativeWorld = new MyPositionAndOrientation(m_pilotRelativeWorld.Value.Value);
            }
            else
            {
                objectBuilder.PilotRelativeWorld = null;
            }

            objectBuilder.IsInFirstPersonView = IsInFirstPersonView;
            objectBuilder.OxygenLevel = OxygenFillLevel;
            objectBuilder.AttachedPlayerId = m_attachedCharacterId;

            return objectBuilder;
        }


        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
        }


        #endregion

        #region View

        public override MatrixD GetHeadMatrix(bool includeY, bool includeX = true, bool forceHeadAnim = false, bool forceHeadBone = false)
        {
            var world = PositionComp.WorldMatrix;

            float headX = m_headLocalXAngle;
            float headY = m_headLocalYAngle;

            if (!includeX)
            {
                headX = DEFAULT_FPS_CAMERA_X_ANGLE;
            }

            MatrixD matrixRotation = MatrixD.CreateFromAxisAngle(Vector3D.Right, MathHelper.ToRadians(headX));

            if (includeY)
                matrixRotation = matrixRotation * Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.ToRadians(headY));

            world = matrixRotation * world;
            Vector3D headPosition = world.Translation;
            if (m_headLocalPosition != Vector3.Zero)
            {
                headPosition = Vector3D.Transform(m_headLocalPosition + m_playerHeadSpring, PositionComp.WorldMatrix);
            }
            else if (Pilot != null)
            {
                var headMatrix = Pilot.GetHeadMatrix(includeY, includeX, false, true, true);
                //VRageRender.MyRenderProxy.DebugDrawAxis(headMatrix, 0.5f, false);
                headPosition = headMatrix.Translation;
            }

            world.Translation = headPosition;
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_MISC)
            {
                VRageRender.MyRenderProxy.DebugDrawSphere(world.Translation, 0.05f, Color.Yellow, 1f, false);
                VRageRender.MyRenderProxy.DebugDrawText3D(world.Translation, "Cockpit camera", Color.Yellow, 0.5f, false);
            }

            return world;
        }

        public override float HeadLocalXAngle
        {
            get { return m_headLocalXAngle; }
            set { m_headLocalXAngle = value; }
        }

        public override float HeadLocalYAngle
        {
            get { return m_headLocalYAngle; }
            set { m_headLocalYAngle = value; }
        }

        public void Rotate(Vector2 rotationIndicator, float roll)
        {
            float sensitivity = MyInput.Static.GetMouseSensitivity() * 0.13f;
            if (rotationIndicator.X != 0)
                m_headLocalXAngle = MathHelper.Clamp(m_headLocalXAngle - rotationIndicator.X * sensitivity, MinHeadLocalXAngle, MaxHeadLocalXAngle);

            if (rotationIndicator.Y != 0)
            {
                bool isInFirstPerson = (IsInFirstPersonView || ForceFirstPersonCamera);

                if (MinHeadLocalYAngle != 0 && isInFirstPerson)
                    m_headLocalYAngle = MathHelper.Clamp(m_headLocalYAngle - rotationIndicator.Y * sensitivity, MinHeadLocalYAngle, MaxHeadLocalYAngle);
                else
                    m_headLocalYAngle = m_headLocalYAngle - rotationIndicator.Y * sensitivity;
            }

            if (!IsInFirstPersonView)
                MyThirdPersonSpectator.Static.Rotate(rotationIndicator, roll);

            rotationIndicator = Vector2.Zero;
        }

        public void RotateStopped()
        {
            MoveAndRotateStopped();
        }

        public void OnAssumeControl(IMyCameraController previousCameraController)
        {
            UpdateCameraAfterChange();
        }

        public override MatrixD GetViewMatrix()
        {
            if (!IsInFirstPersonView)
            {
                ForceFirstPersonCamera = !MyThirdPersonSpectator.Static.IsCameraPositionOk();
                if (!ForceFirstPersonCamera)
                {
                    return MyThirdPersonSpectator.Static.GetViewMatrix();
                }
            }

            var head = GetHeadMatrix(!ForceFirstPersonCamera, !ForceFirstPersonCamera);

            MatrixD result;
            MatrixD.Invert(ref head, out result);
            return result;
        }

        #endregion

        #region Update
        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (m_updateSink)
            {
                ResourceSink.Update();
                m_updateSink = false;
            }

            if (m_savedPilot != null && MarkedForClose == false &&Closed == false && m_savedPilot.MarkedForClose == false && m_savedPilot.Closed == false)
            {
                if ((m_savedPilot.NeedsUpdate & MyEntityUpdateEnum.BEFORE_NEXT_FRAME) != 0)
                {
                    m_savedPilot.UpdateOnceBeforeFrame();
                    m_savedPilot.NeedsUpdate &= ~MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

                    MySession.Static.Players.UpdatePlayerControllers(EntityId);
                    MySession.Static.Players.UpdatePlayerControllers(m_savedPilot.EntityId);
                }

                AttachPilot(m_savedPilot, false,calledFromInit: true);      
            }
            m_savedPilot = null;
            if(m_attachedCharacterIdSaved.HasValue)
            {
                m_attachedCharacterId.Value = m_attachedCharacterIdSaved.Value;
                m_attachedCharacterIdSaved = null;
            }

            m_defferAttach = false;
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            if (m_soundEmitter != null && m_soundEmitter.VolumeMultiplier < 1f)
                m_soundEmitter.VolumeMultiplier = Math.Min(1f, m_soundEmitter.VolumeMultiplier + 0.005f);

            if (m_forcedFpsTimeoutMs > 0)
                m_forcedFpsTimeoutMs -= MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();

            if (m_soundEmitter != null)
            {
                if (hasPower && m_playIdleSound && (m_soundEmitter.IsPlaying == false || (m_soundEmitter.SoundPair.Equals(m_baseIdleSound) == false && m_soundEmitter.SoundPair.Equals(GetInCockpitSound) == false)) && m_baseIdleSound.Equals(MySoundPair.Empty) == false)
                {
                    m_soundEmitter.VolumeMultiplier = 0f;
                    m_soundEmitter.PlaySound(m_baseIdleSound, true);
                }
                else if ((hasPower == false || IsWorking == false) && m_soundEmitter.IsPlaying && m_soundEmitter.SoundPair.Equals(m_baseIdleSound))
                    m_soundEmitter.StopSound(true);
            }

            if (GridResourceDistributor == null || GridGyroSystem == null || EntityThrustComponent == null)
                return;

            bool autopilotEnabled = false;
            var thrustComp = CubeGrid.Components.Get<MyEntityThrustComponent>();
            if (thrustComp != null)
                autopilotEnabled = thrustComp.AutopilotEnabled;

            bool shipControlled = CubeGrid.GridSystems.ControlSystem.IsControlled || autopilotEnabled;

            if (!shipControlled && m_aiPilot != null && Sync.IsServer)
            {
                m_aiPilot.Update();
            }
            else if (shipControlled && m_aiPilot != null && m_aiPilot.RemoveOnPlayerControl == true)
            {
                RemoveAutopilot();
            }

            if (m_pilot != null)
            {
                if (ControllerInfo.IsLocallyHumanControlled())
                {
                    m_pilot.RadioReceiver.UpdateHud();
                }
            }
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (m_pilot != null && OxygenFillLevel < 0.2f && CubeGrid.GridSizeEnum == MyCubeSize.Small)
                RefillFromBottlesOnGrid();

            ResourceSink.Update();
            float timeSinceLastUpdateSeconds = (MySession.Static.ElapsedPlayTime.Ticks - m_lastGasInputUpdateTick) / (float)TimeSpan.TicksPerSecond;
            m_lastGasInputUpdateTick = MySession.Static.ElapsedPlayTime.Ticks;
            float inputAmount = ResourceSink.CurrentInputByType(MyCharacterOxygenComponent.OxygenId) * timeSinceLastUpdateSeconds;
            ChangeGasFillLevel(OxygenFillLevel + inputAmount);

            if (m_retryAttachPilot)
            {
                m_retryAttachPilot = false;
                if (m_attachedCharacterId.Value.HasValue)
                {
                    TryAttachPilot(m_attachedCharacterId.Value.Value);
                }
            }
        }

        private void Sink_CurrentInputChanged(MyDefinitionId resourceTypeId, float oldInput, MyResourceSinkComponent sink)
        {
            if (resourceTypeId != MyCharacterOxygenComponent.OxygenId)
                return;

            float timeSinceLastUpdateSeconds = (MySession.Static.ElapsedPlayTime.Ticks - m_lastGasInputUpdateTick) / (float)TimeSpan.TicksPerSecond;
            m_lastGasInputUpdateTick = MySession.Static.ElapsedPlayTime.Ticks;

            float inputAmount = oldInput * timeSinceLastUpdateSeconds;
            ChangeGasFillLevel(OxygenFillLevel + inputAmount);
            m_updateSink = true;
        }

        private void RefillFromBottlesOnGrid()
        {
            List<IMyConveyorEndpoint> reachableVertices = new List<IMyConveyorEndpoint>();
            MyGridConveyorSystem.FindReachable(ConveyorEndpoint, reachableVertices, (vertex) => vertex.CubeBlock != null && FriendlyWithBlock(vertex.CubeBlock) && vertex.CubeBlock.HasInventory);

            bool bottlesUsed = false;
            foreach (var vertex in reachableVertices)
            {
                var inventoryOwner = vertex.CubeBlock;
                System.Diagnostics.Debug.Assert(inventoryOwner.HasInventory, "This is not inventory owner!");
                int inventoryCount = inventoryOwner.InventoryCount;
                
                for (int i = 0; i < inventoryCount; i++)
                {
                    var inventory = inventoryOwner.GetInventory(i);
                    Debug.Assert(inventory != null, "Wrong inventory type, or inventory returned is null!");
                    var items = inventory.GetItems();

                    foreach (var item in items)
                    {
                        var oxygenContainer = item.Content as MyObjectBuilder_GasContainerObject;
                        if (oxygenContainer != null)
                        {
                            if (oxygenContainer.GasLevel == 0f)
                                continue;

                            var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(oxygenContainer) as MyOxygenContainerDefinition;
                            if (physicalItem.StoredGasId != MyCharacterOxygenComponent.OxygenId)
                                continue;

                            float oxygenAmount = oxygenContainer.GasLevel * physicalItem.Capacity;

                            float transferredAmount = Math.Min(oxygenAmount, OxygenAmountMissing);
                            if (transferredAmount == 0)
                                continue;
                            oxygenContainer.GasLevel = (oxygenAmount - transferredAmount) / physicalItem.Capacity;

                            if (oxygenContainer.GasLevel < 0f)
                                oxygenContainer.GasLevel = 0f;

                            if (oxygenContainer.GasLevel > 1f)
                                Debug.Fail("Incorrect value");

                            inventory.UpdateGasAmount();

                            bottlesUsed = true;

                            OxygenAmount += transferredAmount;
                            if (OxygenFillLevel >= 1f)
                            {
                                ChangeGasFillLevel(1f);
                                ResourceSink.Update();
                                break;
                            }
                        }

                    }
                }
            }

            if (bottlesUsed)
            {
                MyHud.Notifications.Add(new MyHudNotification(text: Sandbox.Game.Localization.MySpaceTexts.NotificationBottleRefill, level: MyNotificationLevel.Important));
            }
        }

        public override void ShowInventory()
        {
            MyGuiScreenTerminal.Show(MyTerminalPageEnum.Inventory, m_pilot, this);
        }

        public override void ShowTerminal()
        {
            MyGuiScreenTerminal.Show(MyTerminalPageEnum.ControlPanel, m_pilot, this);
        }

        public override void OnRemovedFromScene(object source)
        {
            m_savedPilot = m_pilot;

            RemovePilot();
            if (Sync.IsServer)
            {
                m_attachedCharacterId.Value = null;
            }

            base.OnRemovedFromScene(source);
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            // Pilot needs to be added into the cockpit before next frame so that everything has been correctly initialized
            // by that point if this cockpit was added as a result of grid merging, deserialization, etc...
            if (m_savedPilot != null)
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, -1, "CockpitScreen_01", false, null, null);
            VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, -1, "CockpitScreen_02", false, null, null);
            VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, -1, "CockpitScreen_03", false, null, null);
            VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, -1, "CockpitScreen_04", false, null, null);
            VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, -1, "CockpitScreen_05", false, null, null);
            VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, -1, "CockpitScreen_06", false, null, null);
        }

        protected override void OnControlReleased(MyEntityController controller)
        {
            base.OnControlReleased(controller);

            if (controller.Player.IsLocalPlayer)
            {
                if (m_pilot != null)
                    m_pilot.RadioReceiver.Clear();
            }

            // to turn on/off sound in dependence of distance from listener
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        void m_pilot_OnMarkForClose(MyEntity obj)
        {
            if (m_pilot != null)
            {
                Hierarchy.RemoveChild(m_pilot);
                m_rechargeSocket.Unplug();
                m_pilot.SuitBattery.ResourceSink.TemporaryConnectedEntity = null;
                m_pilot = null;
            }
        }

        public void GiveControlToPilot()
        {
            MyCharacter pilot = m_pilot == null ? m_savedPilot : m_pilot;

            if (pilot.ControllerInfo != null && pilot.ControllerInfo.Controller != null)
            {
                pilot.SwitchControl(this);

                /*if (m_enableShipControl)
                    Sync.Controllers.ExtendControl(this, this.CubeGrid);*/
            }
        }

        public bool RemovePilot()
        {
            if (m_pilot == null)
                return true;
         
            MyAnalyticsHelper.ReportActivityEnd(m_pilot, "Cockpit");

            System.Diagnostics.Debug.Assert(m_pilot.Physics != null);
            if (m_pilot.Physics == null)
            { //probably already closed pilot left in cockpit
                m_pilot = null;
                return true;
            }

            StopLoopSound();

            m_pilot.OnMarkForClose -= m_pilotClosedHandler;

            if(MyVisualScriptLogicProvider.PlayerLeftCockpit != null)
                MyVisualScriptLogicProvider.PlayerLeftCockpit(Name, m_pilot.GetPlayerIdentityId(), CubeGrid.Name);

            Hierarchy.RemoveChild(m_pilot);

            if (m_pilot.IsDead)
            {
                if (this.ControllerInfo.Controller != null)
                    this.SwitchControl(m_pilot);

                
                MyEntities.Add(m_pilot);
                m_pilot.WorldMatrix = WorldMatrix;
                m_pilotGunDefinition = null;
                m_rechargeSocket.Unplug();
                m_pilot.SuitBattery.ResourceSink.TemporaryConnectedEntity = null;
                m_pilot = null;
                return true;
            }

            bool usePilotOriginalWorld = false;
            MatrixD placementMatrix = MatrixD.Identity;
            if (m_pilotRelativeWorld.Value.HasValue)
            {
                Vector3D cockpitWorldPos = Vector3D.Transform(Position * CubeGrid.GridSize, CubeGrid.WorldMatrix);
                placementMatrix = MatrixD.Multiply((MatrixD)m_pilotRelativeWorld.Value.Value, this.WorldMatrix);
                var hi = MyPhysics.CastRay(placementMatrix.Translation, cockpitWorldPos, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                if (hi != null && hi.HasValue)
                {
                    var ent = hi.Value.HkHitInfo.GetHitEntity();
                    if (CubeGrid.Equals(ent))
                        if (m_pilot.CanPlaceCharacter(ref placementMatrix))
                            usePilotOriginalWorld = true;
                }
            }

            Vector3D? allowedPosition = null;
            if (!usePilotOriginalWorld)
            {
                allowedPosition = FindFreeNeighbourPosition();

                if (!allowedPosition.HasValue)
                    allowedPosition = PositionComp.GetPosition();
            }

            RemovePilotFromSeat(m_pilot);

            EndShootAll();

            if (usePilotOriginalWorld || allowedPosition.HasValue)
            {
                Hierarchy.RemoveChild(m_pilot);
                MyEntities.Add(m_pilot);
                m_pilot.Physics.Enabled = true;
                m_rechargeSocket.Unplug();
                m_pilot.SuitBattery.ResourceSink.TemporaryConnectedEntity = null;
                m_pilot.Stand();

                // allowedPosition is in center of character
                MatrixD placeMatrix = (usePilotOriginalWorld)
                    ? placementMatrix
                    : MatrixD.CreateWorld(allowedPosition.Value - WorldMatrix.Up, WorldMatrix.Forward, WorldMatrix.Up);
                if (m_pilot.Physics.CharacterProxy != null)
                    m_pilot.Physics.CharacterProxy.ImmediateSetWorldTransform = true;
                if (!MyEntities.CloseAllowed)
                {
                    m_pilot.PositionComp.SetWorldMatrix(placeMatrix, this);
                }
                if (m_pilot.Physics.CharacterProxy != null)
                    m_pilot.Physics.CharacterProxy.ImmediateSetWorldTransform = false;

                if (m_pilotJetpackEnabledBackup.HasValue && m_pilot.JetpackComp != null)
                    m_pilot.JetpackComp.TurnOnJetpack(m_pilotJetpackEnabledBackup.Value);

                if (Parent != null && Parent.Physics != null) // Cockpit could be removing the pilot after it no longer belongs to any grid (e.g. during a split)
                {
                    m_pilot.Physics.LinearVelocity = Parent.Physics.LinearVelocity;

                    if (Parent.Physics.LinearVelocity.LengthSquared() > 100)
                    {
                        var jetpack = m_pilot.JetpackComp;
                        if (jetpack != null)
                        {
                            jetpack.EnableDampeners(false);
                            jetpack.TurnOnJetpack(true);
                        }
                    }
                }

                if (this.ControllerInfo.Controller != null)
                {
                    this.SwitchControl(m_pilot);
                }

                if (m_pilotGunDefinition != null)
                    m_pilot.SwitchToWeapon(m_pilotGunDefinition);
                else
                    m_pilot.SwitchToWeapon(null);

                var pilot = m_pilot;
                m_pilot = null;

                if (MySession.Static.CameraController == this && pilot == MySession.Static.LocalCharacter)
                {
                    bool isInFirstPerson = IsInFirstPersonView;
                    MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, pilot);                
                }
                pilot.IsInFirstPersonView = m_pilotFirstPerson;

                return true;
            }
            else
            {
                //System.Diagnostics.Debug.Assert(false, "There is no place where to put astronaut. Kill him!");
            }

            return false;
        }

        protected virtual void RemovePilotFromSeat(MyCharacter pilot)
        {
            CubeGrid.SetInventoryMassDirty();
        }

        public void AttachAutopilot(MyAutopilotBase newAutopilot, bool updateSync = true)
        {
            RemoveAutopilot();
            m_aiPilot = newAutopilot;
            m_aiPilot.AttachedToShipController(this);

            if (updateSync && Sync.IsServer)
            {
                MyMultiplayer.RaiseEvent(this, x => x.AttachAutopilot_message, newAutopilot.GetObjectBuilder());
            }

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public void RemoveAutopilot()
        {
            if (m_aiPilot != null)
            {
                m_aiPilot.RemovedFromCockpit();
                m_aiPilot = null;
            }

            if (!Sync.IsServer && (ControllerInfo.Controller == null || !ControllerInfo.IsLocallyControlled()))
                NeedsUpdate &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public void RemoveOriginalPilotPosition()
        {
            m_pilotRelativeWorld.Value = null;
        }


        public void OnReleaseControl(IMyCameraController newCameraController)
        {
            UpdateNearFlag();
            if (m_enableFirstPerson)
            {
                UpdateCockpitModel();
            }
        }

        protected override void UpdateCameraAfterChange(bool resetHeadLocalAngle = true)
        {
            base.UpdateCameraAfterChange(resetHeadLocalAngle);

            if (resetHeadLocalAngle)
            {
                m_headLocalXAngle = 0;
                m_headLocalYAngle = 0;
            }

            if (m_enableFirstPerson)
            {
                UpdateNearFlag();
            }
            else
            {
                //Disable FPS mode for large cockpit
                if (MySession.Static.IsCameraControlledObject() && MySession.Static.Settings.Enable3rdPersonView)
                {
                    if (Pilot != null && Pilot.ControllerInfo.IsLocallyControlled())
                        MySession.Static.SetCameraController(MyCameraControllerEnum.ThirdPersonSpectator);
                }
            }

            if (m_enableFirstPerson)
            {
                UpdateCockpitModel();
            }
        }

        private void UpdateNearFlag()
        {
            Render.NearFlag = MySession.Static.CameraController == this && (IsInFirstPersonView || ForceFirstPersonCamera);
        }

        protected virtual void UpdateCockpitModel()
        {
            if (Render.NearFlag)
            {
                VRageRender.MyRenderProxy.ChangeModel(Render.RenderObjectIDs[0], string.IsNullOrEmpty(m_cockpitInteriorModel) ? BlockDefinition.Model : m_cockpitInteriorModel);
                if (!ForceFirstPersonCamera)
                    m_headLocalXAngle = DEFAULT_FPS_CAMERA_X_ANGLE;
                VRageRender.MyRenderProxy.UpdateRenderObjectVisibility(Render.RenderObjectIDs[0], Render.Visible, Render.NearFlag);
            }
            else
            {
                VRageRender.MyRenderProxy.ChangeModel(Render.RenderObjectIDs[0], string.IsNullOrEmpty(m_cockpitInteriorModel) ? BlockDefinition.Model : m_cockpitInteriorModel);
                VRageRender.MyRenderProxy.ChangeModel(Render.RenderObjectIDs[0], BlockDefinition.Model);
            }
        }

        readonly Vector3I[] m_neighbourPositions = {
            new Vector3I(1, 0, 0),
            new Vector3I(-1, 0, 0),            
            new Vector3I(0, 0, -1),
            new Vector3I(0, 0, 1),
            new Vector3I(0, 1, 0),
            new Vector3I(0, -1, 0),
            

            new Vector3I(1, 1, 0),
            new Vector3I(-1, 1, 0),
            new Vector3I(1, -1, 0),
            new Vector3I(-1, -1, 0),

            new Vector3I(1, 1, -1),
            new Vector3I(-1, 1, -1),
            new Vector3I(1, -1, -1),
            new Vector3I(-1, -1, -1),
            new Vector3I(1, 0, -1),
            new Vector3I(-1, 0, -1),
            new Vector3I(0, 1, -1),
            new Vector3I(0, -1, -1),

            new Vector3I(1, 1, 1),
            new Vector3I(-1, 1, 1),
            new Vector3I(1, -1, 1),
            new Vector3I(-1, -1, 1),
            new Vector3I(1, 0, 1),
            new Vector3I(-1, 0, 1),
            new Vector3I(0, 1, 1),
            new Vector3I(0, -1, 1),
        };

        public Vector3I[] NeighbourPositions { get { return m_neighbourPositions; } }
        protected Vector3D? FindFreeNeighbourPosition()
        {
            int maxIterations = 512;
            int distanceOffset = 1;

            while (maxIterations > 0)
            {
                foreach (Vector3I testPosI in m_neighbourPositions)
                {
                    Vector3D translation;
                    Vector3I testPosIOffset = testPosI * distanceOffset;
                    if (IsNeighbourPositionFree(testPosIOffset, out translation))
                        return translation;
                }

                distanceOffset++;
                maxIterations--;
            }

            return null;
        }

        public bool IsNeighbourPositionFree(Vector3I neighbourOffsetI, out Vector3D translation)
        {
            Vector3D delta = 0.5f * PositionComp.LocalAABB.Size.X * neighbourOffsetI.X * PositionComp.WorldMatrix.Right
                          + 0.5f * PositionComp.LocalAABB.Size.Y * neighbourOffsetI.Y * PositionComp.WorldMatrix.Up
                          - 0.5f * PositionComp.LocalAABB.Size.Z * neighbourOffsetI.Z * PositionComp.WorldMatrix.Forward;

            //character offset
            delta += 0.9f * neighbourOffsetI.X * PositionComp.WorldMatrix.Right
                   + 0.9f * neighbourOffsetI.Y * PositionComp.WorldMatrix.Up
                   - 0.9f * neighbourOffsetI.Z * PositionComp.WorldMatrix.Forward;

            var placeMatrix = MatrixD.CreateWorld(PositionComp.WorldMatrix.Translation + delta, PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up);
            translation = placeMatrix.Translation;

            return m_pilot.CanPlaceCharacter(ref placeMatrix, true, true);
        }

        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();

            if (m_defferAttach)
            {
                return;
            }

            if (m_savedPilot != null)
            {
                AttachPilot(m_savedPilot, false, merged:true);
                m_savedPilot = null;
            }
        }

        public override void OnUnregisteredFromGridSystems()
        {
            base.OnUnregisteredFromGridSystems();

            if (m_pilot != null)
            {
                //sync_ControlledEntity_Used();

                var pilot = m_pilot;

                if (!MyEntities.CloseAllowed)
                {
                    RemovePilot();
                    if (Sync.IsServer)
                    {
                        m_attachedCharacterId.Value = null;
                    }

                    pilot.DoDamage(1000, MyDamageType.Unknown, false);
                }
                else
                    if (MySession.Static.CameraController == this)
                    {
                        MySession.Static.SetCameraController(MySession.Static.GetCameraControllerEnum(), m_pilot);
                    }
            }
        }

        public void AttachPilot(MyCharacter pilot, bool storeOriginalPilotWorld = true, bool calledFromInit = false, bool merged = false)
        {
            System.Diagnostics.Debug.Assert(pilot != null);
            System.Diagnostics.Debug.Assert(m_pilot == null);

            if (Sync.IsServer)
            {
                MyMultiplayer.ReplicateImmediatelly(MyExternalReplicable.FindByObject(pilot), MyExternalReplicable.FindByObject(this.CubeGrid));
            }

            MyAnalyticsHelper.ReportActivityStart(pilot, "cockpit", "cockpit", string.Empty, string.Empty);

            m_pilot = pilot;
            m_pilot.OnMarkForClose += m_pilotClosedHandler;
            m_pilot.IsUsing = this;

            if (storeOriginalPilotWorld)
            {
                m_pilotRelativeWorld.Value = (Matrix)MatrixD.Multiply(pilot.WorldMatrix, this.PositionComp.WorldMatrixNormalizedInv);
            }

            long playerId = pilot.GetPlayerIdentityId();
            if (pilot.InScene)
                MyEntities.Remove(pilot);

            m_pilot.Physics.Enabled = false;
            m_pilot.PositionComp.SetWorldMatrix(WorldMatrix, this);
            m_pilot.Physics.Clear();
            //m_pilot.SetPosition(GetPosition() - WorldMatrix.Forward * 0.5f);

            if (!Hierarchy.Children.Any(x => x.Entity == m_pilot))  //may contain after load
                Hierarchy.AddChild(m_pilot, true, true);

            var gunEntity = m_pilot.CurrentWeapon as MyEntity;
            if (gunEntity != null && !m_forgetTheseWeapons.Contains(m_pilot.CurrentWeapon.DefinitionId))
            {
                m_pilotGunDefinition = m_pilot.CurrentWeapon.DefinitionId;
            }
            else
                m_pilotGunDefinition = null;

            MyAnimationDefinition animationDefinition;
            MyDefinitionId id = new MyDefinitionId(typeof(MyObjectBuilder_AnimationDefinition), BlockDefinition.CharacterAnimation);
            if (!MyDefinitionManager.Static.TryGetDefinition(id, out animationDefinition) && !MyFileSystem.FileExists(BlockDefinition.CharacterAnimation))
            {
                BlockDefinition.CharacterAnimation = null;
            }
            m_pilotFirstPerson = pilot.IsInFirstPersonView;
            PlacePilotInSeat(pilot);
            m_pilot.SuitBattery.ResourceSink.TemporaryConnectedEntity = this;
            m_rechargeSocket.PlugIn(m_pilot.SuitBattery.ResourceSink);

            if (pilot.ControllerInfo.Controller != null)
            {
                Sync.Players.SetPlayerToCockpit(pilot.ControllerInfo.Controller.Player, this);
            }
            // Control should be handled elsewhere if we initialize the grid in the Init(...)
            if (!calledFromInit)
            {
                GiveControlToPilot();
                m_pilot.SwitchToWeapon(null);

            }

            if (Sync.IsServer)
            {               
                m_attachedCharacterId.Value = m_pilot.EntityId;
                m_storeOriginalPlayerWorldMatrix.Value = storeOriginalPilotWorld;
            }

            var jetpack = m_pilot.JetpackComp;
            if (jetpack != null)
            {
                m_pilotJetpackEnabledBackup = jetpack.TurnedOn;
                m_pilot.JetpackComp.TurnOnJetpack(false);
            }
            else
            {
                m_pilotJetpackEnabledBackup = null;
            }

            m_lastPilot = pilot;
            if (GetInCockpitSound != MySoundPair.Empty && !calledFromInit && !merged)
                PlayUseSound(true);
            m_playIdleSound = true;

            if(MyVisualScriptLogicProvider.PlayerEnteredCockpit != null)
                MyVisualScriptLogicProvider.PlayerEnteredCockpit(Name, playerId, CubeGrid.Name);

        }

        protected virtual void PlacePilotInSeat(MyCharacter pilot)
        {
            bool pilotIsLocal = MySession.Static.LocalHumanPlayer != null && MySession.Static.LocalHumanPlayer.Identity.Character == pilot;
            m_pilot.Sit(m_enableFirstPerson, pilotIsLocal, m_isLargeCockpit || !m_enableShipControl, BlockDefinition.CharacterAnimation);
            CubeGrid.SetInventoryMassDirty();
        }

        // These weapons will not be remembered when sitting inside the cockpit
        // TODO: move to SBC
        private static readonly MyDefinitionId[] m_forgetTheseWeapons = new MyDefinitionId[]
        {
            new MyDefinitionId(typeof(MyObjectBuilder_CubePlacer))
        };

        public void AddShake(float shakePower)
        {
            m_currentCameraShakePower += shakePower;
        }

        private void ChangeGasFillLevel(float newFillLevel)
        {
            if (OxygenFillLevel != newFillLevel)
            {
                OxygenFillLevel = newFillLevel;
            }
        }

        public override bool IsLargeShip()
        {
            return m_isLargeCockpit;
        }

        public new MyCockpitDefinition BlockDefinition
        {
            get { return base.BlockDefinition as MyCockpitDefinition; }
        }

        public override string CalculateCurrentModel(out Matrix orientation)
        {
            Orientation.GetMatrix(out orientation);

            if (Render.NearFlag)
            {
                return string.IsNullOrEmpty(m_cockpitInteriorModel) ? BlockDefinition.Model : m_cockpitInteriorModel;
            }
            else
            {
                return BlockDefinition.Model;
            }
        }
        #endregion

        #region Properties

        public override MyCharacter Pilot
        {
            get 
            { 
                if(m_pilot == null && m_savedPilot != null)
                {
                    return m_savedPilot;
                }
                return m_pilot; 
            }
        }

        public MyEntity IsBeingUsedBy
        {
            get
            {
                return m_pilot;
            }
        }

        #endregion

        public virtual UseActionResult CanUse(UseActionEnum actionEnum, IMyControllableEntity user)
        {
            if (m_pilot != null)
                return UseActionResult.UsedBySomeoneElse;

            if (!IsFunctional)
                return UseActionResult.CockpitDamaged;

            long identityId = user.ControllerInfo.ControllingIdentityId;
            if (identityId != 0)
            {
                bool accessAllowed = HasPlayerAccess(identityId);
                if (!accessAllowed)
                    return UseActionResult.AccessDenied;

                return UseActionResult.OK;
            }

            return UseActionResult.AccessDenied;
        }

        protected override void UpdateSoundState()
        {
            base.UpdateSoundState();
        }

        protected override void StartLoopSound()
        {
            m_playIdleSound = true;
            if (m_soundEmitter != null && hasPower && m_baseIdleSound.SoundId.IsNull == false)
                m_soundEmitter.PlaySound(m_baseIdleSound, true);
        }

        protected override void StopLoopSound()
        {
            m_playIdleSound = false;
            if (m_soundEmitter != null && m_soundEmitter.IsPlaying)
                m_soundEmitter.StopSound(true);
        }

        protected override bool IsCameraController()
        {
            return true;
        }

        protected override void OnControlAcquired_UpdateCamera()
        {
            CubeGrid.RaiseGridChanged();
            base.OnControlAcquired_UpdateCamera();

            m_currentCameraShakePower = 0;
        }

        protected override void OnControlledEntity_Used()
        {
            // This should be sync using IControlledEntity - it's actions
            // It should be pretty easy, because this entity is controlled by player
            // It should be sent same as update position is sent
            //Debug.Assert(m_pilot != null, "Pilot cannot be null");

            var pilot = m_pilot;

            RemovePilot();
            if(Sync.IsServer)
            {
                m_attachedCharacterId.Value = null;
            }

            base.OnControlledEntity_Used();
        }

        protected override void OnControlReleased_UpdateCamera()
        {
            base.OnControlReleased_UpdateCamera();

            m_currentCameraShakePower = 0;
        }

        protected override void RemoveLocal()
        {
            if (MyCubeBuilder.Static.IsActivated)
            {
                //MyCubeBuilder.Static.Deactivate();
                MySession.Static.GameFocusManager.Clear();
            }
            base.RemoveLocal();
            RemovePilot();
            if (Sync.IsServer)
            {
                m_attachedCharacterId.Value = null;
            }

        }

        protected override void OnOwnershipChanged()
        {
            base.OnOwnershipChanged();

            if (m_pilot != null && Sync.IsServer)
            {
                if (ControllerInfo.Controller != null)
                {
                    var relation = GetUserRelationToOwner(ControllerInfo.ControllingIdentityId);
                    if (relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies || relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral)
                    {
                        RaiseControlledEntityUsed();
                    }
                }
            }
        }

        public override List<MyHudEntityParams> GetHudParams(bool allowBlink)
        {
            var hudParams = base.GetHudParams(allowBlink);
            long localPlayerId = MySession.Static.LocalHumanPlayer == null ? 0 : MySession.Static.LocalHumanPlayer.Identity.IdentityId;
            bool showPilotName = ControllerInfo.ControllingIdentityId != localPlayerId && Pilot != null;

            if (ShowOnHUD || IsBeingHacked)
                hudParams[0].Text.AppendLine();
            else
                hudParams[0].Text.Clear();

            if (showPilotName)
            {
                if (Pilot != null)
                {
                    hudParams[0].Text.Append(Pilot.UpdateCustomNameWithFaction());
                }
            }

            if (!ShowOnHUD)
                m_hudParams.Clear();
            return hudParams;
        }

        protected override bool ShouldSit()
        {
            return m_isLargeCockpit || base.ShouldSit();
        }

        protected override bool CanBeMainCockpit()
        {
            return BlockDefinition.EnableShipControl;
        }

        void IMyCameraController.ControlCamera(MyCamera currentCamera)
        {
            if (!m_enableFirstPerson)
            {
                IsInFirstPersonView = false;
            }

            // in case that the cockpit was destroyed but still is for some reason camera controller
            // (e.g. user was looking through camera block but both camera block and this cockpit were destroyed)
            if (Closed && MySession.Static.LocalCharacter != null)
            {
                MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, MySession.Static.LocalCharacter);
            }

            currentCamera.SetViewMatrix(GetViewMatrix());
            
            currentCamera.CameraSpring.Enabled = true;
            currentCamera.CameraSpring.SetCurrentCameraControllerVelocity(Physics != null ? Physics.LinearVelocity : Vector3.Zero);

            if (m_currentCameraShakePower > 0)
            {
                currentCamera.CameraShake.AddShake(m_currentCameraShakePower);
                m_currentCameraShakePower = 0;
            }

            if (Pilot != null && Pilot.InScene && ControllerInfo.IsLocallyControlled())
                Pilot.EnableHead(!IsInFirstPersonView && !ForceFirstPersonCamera);
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
            OnAssumeControl(previousCameraController);
        }

        void IMyCameraController.OnReleaseControl(IMyCameraController newCameraController)
        {
            OnReleaseControl(newCameraController);
            if (Pilot != null && Pilot.InScene)
                Pilot.EnableHead(true);
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
                return ForceFirstPersonCamera || MySession.Static.Settings.Enable3rdPersonView == false;
            }
            set
            {
                ForceFirstPersonCamera = value;
            }
        }

        bool IMyCameraController.HandleUse()
        {
            return false;
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

        bool IMyGasBlock.IsWorking()
        {
            return IsWorking && BlockDefinition.IsPressurized;
        }

        public void RequestUse(UseActionEnum actionEnum, MyCharacter user)
        {
            if (user.IsDead)
            {
                return;
            }
            UseActionResult result = UseActionResult.OK;
            IMyControllableEntity controllableEntity = user as IMyControllableEntity;
            if ((result = CanUse(actionEnum, controllableEntity)) == UseActionResult.OK)
            {
                MyMultiplayer.RaiseEvent(this, x => x.AttachPilotEvent, actionEnum, user.EntityId);
            }
            else
            {
                AttachPilotEventFailed(result);
            }

        }

        [Event, Reliable, Server]
        public void AttachPilotEvent(UseActionEnum actionEnum, long characterID)
        {
            var usableEntity = this as IMyUsableEntity;
            MyEntity controlledEntity;
            bool entityExists = MyEntities.TryGetEntityById<MyEntity>(characterID, out controlledEntity);
            IMyControllableEntity controllableEntity = controlledEntity as IMyControllableEntity;
            Debug.Assert(controllableEntity != null, "Controllable entity needs to get control from another controllable entity");
            Debug.Assert(entityExists && usableEntity != null);

            UseActionResult result = UseActionResult.OK;
            MyCharacter character = controllableEntity as MyCharacter;
            if (entityExists && usableEntity != null && (result = usableEntity.CanUse(actionEnum, controllableEntity)) == UseActionResult.OK)
            {
                AttachPilot(character);

            }
        }

        public void AttachPilotEventFailed(UseActionResult actionResult)
        {
            if (actionResult == UseActionResult.UsedBySomeoneElse)
                MyHud.Notifications.Add(new MyHudNotification(MyCommonTexts.AlreadyUsedBySomebodyElse, 2500, MyFontEnum.Red));
            else if (actionResult == UseActionResult.AccessDenied)
                MyHud.Notifications.Add(MyNotificationSingletons.AccessDenied);
            else if (actionResult == UseActionResult.Unpowered)
                MyHud.Notifications.Add(new MyHudNotification(MySpaceTexts.BlockIsNotPowered, 2500, MyFontEnum.Red));
            else if (actionResult == UseActionResult.CockpitDamaged)
                MyHud.Notifications.Add(new MyHudNotification(MySpaceTexts.Notification_CockpitIsDamaged, 2500, MyFontEnum.Red));
        }

        void OnCharacterChanged()
        {
            m_retryAttachPilot = false;
            if (m_attachedCharacterId.Value.HasValue)
            {
                TryAttachPilot(m_attachedCharacterId.Value.Value);
            }
            else if (m_pilot != null)
            {
                RemovePilot();
                if (Sync.IsServer)
                {
                    m_attachedCharacterId.Value = null;
                }
            }
        }

        void TryAttachPilot(long pilotId)
        {
            if (m_pilot == null || m_pilot.EntityId != pilotId)
            {
                Debug.Assert(m_savedPilot == null, "saved pilot ");
                m_savedPilot = null;
                RemovePilot();
                MyEntity controlledEntity;
                if (MyEntities.TryGetEntityById<MyEntity>(pilotId, out controlledEntity))
                {
                    MyCharacter character = controlledEntity as MyCharacter;
                    if (character != null)
                    {
                        AttachPilot(character,m_storeOriginalPlayerWorldMatrix);
                    }
                }
                else
                {
                    m_retryAttachPilot = true;
                }
             
            }
        }

        public void ClearSavedpilot()
        {
            m_attachedCharacterIdSaved = null;
            m_attachedCharacterId.Value = null;
            m_savedPilot = null;
        }

        [Event, Reliable, Broadcast]
        void AttachAutopilot_message([Serialize(MyObjectFlags.Dynamic | MyObjectFlags.Nullable, DynamicSerializerType = typeof(MyObjectBuilderDynamicSerializer))] MyObjectBuilder_AutopilotBase autopilot)
        {
            AttachAutopilot(MyAutopilotFactory.CreateAutopilot(autopilot), updateSync: false);
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
