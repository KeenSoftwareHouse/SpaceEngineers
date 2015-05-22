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
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Audio;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Cockpit))]
    class MyCockpit : MyShipController, IMyCameraController, IMyUsableEntity, IMyCockpit, IMyOxygenConsumer, IMyConveyorEndpointBlock
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
        public MyAutopilotBase AiPilot { get{ return m_aiPilot;} }
        protected Matrix? m_pilotRelativeWorld = null;
        protected MyDefinitionId? m_pilotGunDefinition = null;

        float m_headLocalXAngle = 0;
        float m_headLocalYAngle = 0;
        MyCameraSpring m_cameraSpring;
        MyCameraHeadShake m_cameraShake;

        string m_cockpitInteriorModel;
        string m_cockpitGlassModel;

        protected Action<MyEntity> m_pilotClosedHandler;

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

        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get
            {
                return m_conveyorEndpoint;
            }
        }

        private float m_oxygenLevel;
        public float OxygenLevel
        {
            get
            {
                return m_oxygenLevel;
            }
        }
        public float OxygenAmount
        {
            get
            {
                return m_oxygenLevel * BlockDefinition.OxygenCapacity;
            }
            set
            {
                if (BlockDefinition.OxygenCapacity != 0f)
                {
                    m_oxygenLevel = value / BlockDefinition.OxygenCapacity;
                    if (m_oxygenLevel < 0f)
                    {
                        m_oxygenLevel = 0f;
                    }
                    if (m_oxygenLevel > 1f)
                    {
                        m_oxygenLevel = 1f;
                    }
                }
            }
        }
        public float OxygenAmountMissing
        {
            get
            {
                return (1f - m_oxygenLevel) * BlockDefinition.OxygenCapacity;
            }
        }
        #endregion

        #region Init
        public MyCockpit()
        {
            m_pilotClosedHandler = new Action<MyEntity>(m_pilot_OnMarkForClose);
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

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;

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
                        System.Diagnostics.Debug.Assert(false, "Pilot already sits on another place!");
                        pilot = null;
                    }
                }
                else
                {
                    pilot = (MyCharacter)MyEntities.CreateFromObjectBuilder(cockpitOb.Pilot);
                }

                if (pilot != null)
                {
                    AttachPilot(pilot, storeOriginalPilotWorld: false, calledFromInit: true);
                    if (cockpitOb.PilotRelativeWorld.HasValue)
                        m_pilotRelativeWorld = cockpitOb.PilotRelativeWorld.Value.GetMatrix();
                    else
                        m_pilotRelativeWorld = null;

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

                if (MyModels.GetModelOnlyDummies(m_cockpitInteriorModel).Dummies.ContainsKey("head"))
                    m_headLocalPosition = MyModels.GetModelOnlyDummies(m_cockpitInteriorModel).Dummies["head"].Matrix.Translation;
            }
            else
            {
                if (MyModels.GetModelOnlyDummies(BlockDefinition.Model).Dummies.ContainsKey("head"))
                    m_headLocalPosition = MyModels.GetModelOnlyDummies(BlockDefinition.Model).Dummies["head"].Matrix.Translation;
            }

            AddDebugRenderComponent(new Components.MyDebugRenderComponentCockpit(this));

            InitializeConveyorEndpoint();

            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));

            m_oxygenLevel = cockpitOb.OxygenLevel;
        }

        protected virtual void PostBaseInit()
        {
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
                }

                m_oxygenLevel = 0f;
            }
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_Cockpit objectBuilder = (MyObjectBuilder_Cockpit)base.GetObjectBuilderCubeBlock(copy);

            objectBuilder.Pilot = (m_pilot != null && m_pilot.Save) ? (MyObjectBuilder_Character)m_pilot.GetObjectBuilder(copy) : null;
            objectBuilder.Autopilot = (m_aiPilot != null) ? m_aiPilot.GetObjectBuilder() : null;
            objectBuilder.PilotGunDefinition = m_pilotGunDefinition;

            if (m_pilotRelativeWorld.HasValue)
                objectBuilder.PilotRelativeWorld = new MyPositionAndOrientation(m_pilotRelativeWorld.Value);
            else
                objectBuilder.PilotRelativeWorld = null;

            objectBuilder.IsInFirstPersonView = IsInFirstPersonView;
            objectBuilder.OxygenLevel = m_oxygenLevel;

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
                var headMatrix = Pilot.GetHeadMatrix(includeY, includeX, forceHeadAnim, IsInFirstPersonView);
                headPosition = headMatrix.Translation + headMatrix.Backward * 0.15f + m_playerHeadSpring;
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
            float sensitivity = 0.5f;
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

            if (m_savedPilot != null)
            {
                AttachPilot(m_savedPilot, false);
                m_savedPilot = null;
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();

            if (GridPowerDistributor == null)
                return;
            if (GridGyroSystem == null)
                return;
            if (GridThrustSystem == null)
                return;

            if (m_pilot != null)
                m_pilot.SuitBattery.UpdateOnServer();

            bool shipControlled = CubeGrid.GridSystems.ControlSystem.IsControlled;

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
            if (m_pilot != null && m_oxygenLevel < 0.2f && CubeGrid.GridSizeEnum == MyCubeSize.Small)
            {
                List<IMyConveyorEndpoint> reachableVertices = new List<IMyConveyorEndpoint>();
                MyGridConveyorSystem.Pathfinding.FindReachable(this.ConveyorEndpoint, reachableVertices, (vertex) => vertex.CubeBlock != null && FriendlyWithBlock(vertex.CubeBlock) && vertex.CubeBlock is IMyInventoryOwner);

                bool bottlesUsed = false;

                foreach (var vertex in reachableVertices)
                {
                    var inventoryOwner = vertex.CubeBlock as IMyInventoryOwner;
                    int inventoryCount = inventoryOwner.InventoryCount;

                    for (int i = 0; i < inventoryCount; i++)
                    {
                        var inventory = inventoryOwner.GetInventory(i);
                        var items = inventory.GetItems();

                        foreach (var item in items)
                        {
                            var oxygenContainer = item.Content as MyObjectBuilder_OxygenContainerObject;
                            if (oxygenContainer != null)
                            {
                                if (oxygenContainer.OxygenLevel == 0f)
                                {
                                    continue;
                                }

                                var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(oxygenContainer) as MyOxygenContainerDefinition;
                                float oxygenAmount = oxygenContainer.OxygenLevel * physicalItem.Capacity;

                                float transferredAmount = Math.Min(oxygenAmount, OxygenAmountMissing);
                                oxygenContainer.OxygenLevel = (oxygenAmount - transferredAmount) / physicalItem.Capacity;

                                if (oxygenContainer.OxygenLevel < 0f)
                                {
                                    oxygenContainer.OxygenLevel = 0f;
                                }

                                if (oxygenContainer.OxygenLevel > 1f)
                                {
                                    Debug.Fail("Incorrect value");
                                }

                                inventory.UpdateOxygenAmount();
                                inventory.SyncOxygenContainerLevel(item.ItemId, oxygenContainer.OxygenLevel);

                                bottlesUsed = true;

                                OxygenAmount += transferredAmount;
                                if (m_oxygenLevel >= 1f)
                                {
                                    m_oxygenLevel = 1f;
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
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (m_cameraShake != null && m_cameraSpring == null)
            {
                Debug.Assert(CubeGrid != null && CubeGrid.Physics != null, "Grid is null or has no physics!");
                m_cameraSpring = new MyCameraSpring(CubeGrid.Physics);
            }

            if (m_cameraShake != null && m_cameraSpring != null)
            {
                m_cameraSpring.Update(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, PositionComp.GetWorldMatrixNormalizedInv(), ref m_playerHeadSpring);
                m_cameraShake.UpdateShake(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, ref m_playerHeadSpring, ref m_playerHeadShakeDir);
            }
        }

        public override void ShowInventory()
        {
            if (m_enableShipControl)
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

            base.OnRemovedFromScene(source);
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            // Pilot needs to be added into the cockpit before next frame so that everything has been correctly initialized
            // by that point if this cockpit was added as a result of grid merging, deserialization, etc...
            if (m_savedPilot != null)
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        protected override void OnControlReleased(MyEntityController controller)
        {
            base.OnControlReleased(controller);

            if (controller.Player.IsLocalPlayer())
            {
                if (m_pilot != null)
                    m_pilot.RadioReceiver.Clear();
            }

            // to turn on/off sound in dependence of distance from listener
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        void m_pilot_OnMarkForClose(MyEntity obj)
        {
            if (m_pilot != null)
            {
                Hierarchy.RemoveChild(m_pilot);
                m_rechargeSocket.Unplug();
                m_pilot = null;
            }
        }

        public void GiveControlToPilot()
        {
            if (m_pilot.ControllerInfo != null && m_pilot.ControllerInfo.Controller != null)
            {
                m_pilot.SwitchControl(this);

                /*if (m_enableShipControl)
                    Sync.Controllers.ExtendControl(this, this.CubeGrid);*/
            }
        }

        public bool RemovePilot()
        {
            if (m_pilot == null)
                return true;

            System.Diagnostics.Debug.Assert(m_pilot.Physics != null);
            if (m_pilot.Physics == null)
            { //probably already closed pilot left in cockpit
                m_pilot = null;
                return true;
            }

            //m_soundEmitter.OwnedBy = null;
            if (MyFakes.ENABLE_NEW_SOUNDS)
                StopLoopSound();

            m_pilot.OnMarkForClose -= m_pilotClosedHandler;

            if (m_pilot.IsDead)
            {
                if (this.ControllerInfo.Controller != null)
                    this.SwitchControl(m_pilot);

                Hierarchy.RemoveChild(m_pilot);
                MyEntities.Add(m_pilot);
                m_pilot.WorldMatrix = WorldMatrix;
                m_pilotGunDefinition = null;
                m_rechargeSocket.Unplug();
                m_pilot = null;
                return true;
            }

            bool usePilotOriginalWorld = false;
            MatrixD placementMatrix = MatrixD.Identity;
            if (m_pilotRelativeWorld.HasValue)
            {
                placementMatrix = MatrixD.Multiply((MatrixD)m_pilotRelativeWorld.Value, this.WorldMatrix);
                if (m_pilot.CanPlaceCharacter(ref placementMatrix))
                    usePilotOriginalWorld = true;
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
                m_pilot.Stand();

                // allowedPosition is in center of character
                MatrixD placeMatrix = (usePilotOriginalWorld)
                    ? placementMatrix
                    : MatrixD.CreateWorld(allowedPosition.Value - WorldMatrix.Up, WorldMatrix.Forward, WorldMatrix.Up);
                if (m_pilot.Physics.CharacterProxy != null)
                    m_pilot.Physics.CharacterProxy.ImmediateSetWorldTransform = true;
                if (!MyEntities.CloseAllowed)
                {
                    m_pilot.PositionComp.SetWorldMatrix(placeMatrix);
                }
                if (m_pilot.Physics.CharacterProxy != null)
                    m_pilot.Physics.CharacterProxy.ImmediateSetWorldTransform = false;

                if (Parent != null) // Cockpit could be removing the pilot after it no longer belongs to any grid (e.g. during a split)
                {
                    m_pilot.Physics.LinearVelocity = Parent.Physics.LinearVelocity;

                    if (Parent.Physics.LinearVelocity.LengthSquared() > 100)
                    {
                        m_pilot.EnableDampeners(false);
                        m_pilot.EnableJetpack(true);
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

                if (MySession.Static.CameraController == this)
                {
                    MySession.SetCameraController(MyCameraControllerEnum.Entity, pilot);
                }

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

        }

        public void AttachAutopilot(MyAutopilotBase newAutopilot, bool updateSync = true)
        {
            RemoveAutopilot();
            m_aiPilot = newAutopilot;
            m_aiPilot.AttachedToShipController(this);

            if (updateSync && Sync.IsServer)
            {
                SyncObject.SendAutopilotAttached(newAutopilot.GetObjectBuilder());
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
            m_pilotRelativeWorld = null;
        }


        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);
            UpdateCockpitGlass();
        }

        public void OnReleaseControl(IMyCameraController newCameraController)
        {
            UpdateNearFlag();
            if (m_enableFirstPerson)
            {
                UpdateCockpitModel();
                UpdateCockpitGlass();

                if (Pilot != null)
                    Pilot.EnableHead(!Render.NearFlag);
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
                if (MySession.IsCameraControlledObject()&& MySession.Static.Settings.Enable3rdPersonView)
                {
                    MySession.SetCameraController(MyCameraControllerEnum.ThirdPersonSpectator);
                }
            }

            if (m_enableFirstPerson)
            {
                UpdateCockpitModel();
                UpdateCockpitGlass();

                //Pilot can be null when quiting the game and the player was remote controlling a ship from the same cockpit
                if (Pilot != null)
                    Pilot.EnableHead(!Render.NearFlag);
            }
        }

        private void UpdateNearFlag()
        {
            Render.NearFlag = MySession.Static.CameraController == this && (IsInFirstPersonView || ForceFirstPersonCamera);
        }

        public override void SwitchThrusts()
        {
            base.SwitchThrusts();
            if (m_pilot != null && m_enableShipControl)
            {
                m_pilot.SwitchThrusts();
            }
        }

        protected virtual void UpdateCockpitModel()
        {
            if (Render.NearFlag)
            {
                VRageRender.MyRenderProxy.ChangeModel(Render.RenderObjectIDs[0], 0, string.IsNullOrEmpty(m_cockpitInteriorModel) ? BlockDefinition.Model : m_cockpitInteriorModel, true);
                if (!ForceFirstPersonCamera)
                    m_headLocalXAngle = DEFAULT_FPS_CAMERA_X_ANGLE;
                VRageRender.MyRenderProxy.UpdateRenderObjectVisibility(Render.RenderObjectIDs[0], Render.Visible, Render.NearFlag);
            }
            else
            {
                VRageRender.MyRenderProxy.ChangeModel(Render.RenderObjectIDs[0], 0, string.IsNullOrEmpty(m_cockpitInteriorModel) ? BlockDefinition.Model : m_cockpitInteriorModel, true);
                VRageRender.MyRenderProxy.ChangeModel(Render.RenderObjectIDs[0], 0, BlockDefinition.Model, false);
                if (!string.IsNullOrEmpty(m_cockpitGlassModel))
                    VRageRender.MyRenderProxy.UpdateCockpitGlass(false, m_cockpitGlassModel, PositionComp.WorldMatrix, GlassDirt);
            }
        }

        Vector3I[] m_neighbourPositions = new Vector3I[]
        {
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

            if (m_savedPilot != null)
            {
                AttachPilot(m_savedPilot, false);
                m_savedPilot = null;
            }

            if (CubeGrid.GridSystems.OxygenSystem != null && m_conveyorEndpoint != null)
            {
                CubeGrid.GridSystems.OxygenSystem.RegisterOxygenBlock(this);
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
                    pilot.DoDamage(1000, MyDamageType.Unknown, false);
                }
                else
                    if (MySession.Static.CameraController == this)
                    {
                        MySession.SetCameraController(MySession.GetCameraControllerEnum(), m_pilot);
                    }
            }

            if (CubeGrid.GridSystems.OxygenSystem != null && m_conveyorEndpoint != null)
            {
                CubeGrid.GridSystems.OxygenSystem.UnregisterOxygenBlock(this);
            }
        }

        protected override void sync_PilotRelativeEntryUpdated(MyPositionAndOrientation relativeEntry)
        {
            base.sync_PilotRelativeEntryUpdated(relativeEntry);
            m_pilotRelativeWorld = relativeEntry.GetMatrix();
        }

        protected override void sync_UseSuccess(UseActionEnum actionEnum, IMyControllableEntity user)
        {
            Debug.Assert(user is MyCharacter, "Cockpits can only take control from characters");
            AttachPilot(user as MyCharacter);
        }

        public void AttachPilot(MyCharacter pilot, bool storeOriginalPilotWorld = true, bool calledFromInit = false)
        {
            System.Diagnostics.Debug.Assert(pilot != null);
            System.Diagnostics.Debug.Assert(m_pilot == null);

            m_pilot = pilot;
            m_pilot.OnMarkForClose += m_pilotClosedHandler;
            m_pilot.IsUsing = this;

            //m_soundEmitter.OwnedBy = m_pilot;
            if (MyFakes.ENABLE_NEW_SOUNDS)
                StartLoopSound();

            if (storeOriginalPilotWorld)
            {
                m_pilotRelativeWorld = (Matrix)MatrixD.Multiply(pilot.WorldMatrix, this.PositionComp.GetWorldMatrixNormalizedInv());
                if (Sync.IsServer)
                {
                    var relativeEntry = new MyPositionAndOrientation(m_pilotRelativeWorld.Value);
                    SyncObject.SendPilotRelativeEntryUpdate(ref relativeEntry);
                }
            }

            if (pilot.InScene)
                MyEntities.Remove(pilot);

            m_pilot.Physics.Enabled = false;
            m_pilot.PositionComp.SetWorldMatrix(WorldMatrix);
            m_pilot.Physics.Clear();
            //m_pilot.SetPosition(GetPosition() - WorldMatrix.Forward * 0.5f);

            Hierarchy.AddChild(m_pilot, true, true);

            var gunEntity = m_pilot.CurrentWeapon as MyEntity;
            if (gunEntity != null)
            {
                var ob = gunEntity.GetObjectBuilder();
                m_pilotGunDefinition = ob.GetId();
            }
            else
                m_pilotGunDefinition = null;

            MyAnimationDefinition animationDefinition;
            MyDefinitionId id = new MyDefinitionId(typeof(MyObjectBuilder_AnimationDefinition), BlockDefinition.CharacterAnimation);
            if (!MyDefinitionManager.Static.TryGetDefinition(id, out animationDefinition) && !MyFileSystem.FileExists(BlockDefinition.CharacterAnimation))
            {
                BlockDefinition.CharacterAnimation = null;
            }

            PlacePilotInSeat(pilot);
            m_rechargeSocket.PlugIn(m_pilot.SuitBattery);

            // Control should be handled elsewhere if we initialize the grid in the Init(...)
            if (!calledFromInit) GiveControlToPilot();
            m_pilot.SwitchToWeapon(null);
        }

        protected virtual void PlacePilotInSeat(MyCharacter pilot)
        {
            bool pilotIsLocal = MySession.LocalHumanPlayer != null && MySession.LocalHumanPlayer.Identity.Character == pilot;
            m_pilot.Sit(m_enableFirstPerson, pilotIsLocal, m_isLargeCockpit || !m_enableShipControl, BlockDefinition.CharacterAnimation);
        }

        bool? m_lastNearFlag = null;
        internal void UpdateCockpitGlass()
        {
            if (string.IsNullOrEmpty(m_cockpitGlassModel))
                return;
            if (m_lastNearFlag != Render.NearFlag)
            {
                m_lastNearFlag = Render.NearFlag;

                if (!Render.NearFlag)
                    VRageRender.MyRenderProxy.UpdateCockpitGlass(false, m_cockpitGlassModel, WorldMatrix, GlassDirt);
            }

            if (Render.NearFlag)
                VRageRender.MyRenderProxy.UpdateCockpitGlass(true, m_cockpitGlassModel, WorldMatrix, GlassDirt);
        }

        public void AddShake(float shakePower)
        {
            if (m_cameraShake != null)
                m_cameraShake.AddShake(shakePower);
        }

        public MyCameraSpring CameraSpring
        {
            get { return m_cameraSpring; }
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
            get { return m_pilot; }
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
            if (MyFakes.ENABLE_NEW_SOUNDS)
        {
                if (m_pilot != null)
                    StartLoopSound();
                else
                    StopLoopSound();

                if (!MyAudio.Static.SourceIsCloseEnoughToPlaySound(m_soundEmitter, m_soundEmitter.SoundId))
                    m_soundEmitter.StopSound(true);
            }
        }

        protected override void StartLoopSound()
        {
            if (MyFakes.ENABLE_NEW_SOUNDS)
            {
                if ((m_soundEmitter.Sound != null) && (m_soundEmitter.Sound.IsPlaying))
                    return;

                if (!MyAudio.Static.SourceIsCloseEnoughToPlaySound(m_soundEmitter, BlockDefinition.PrimarySound.SoundId))
                    return;

                m_soundEmitter.PlaySingleSound(BlockDefinition.PrimarySound, true);
            }
        }

        protected override void StopLoopSound()
        {
            if (MyFakes.ENABLE_NEW_SOUNDS)
            {
                m_soundEmitter.StopSound(true);
            }
        }

        protected override bool IsCameraController()
        {
            return true;
        }

        protected override void OnControlAcquired_UpdateCamera()
        {
            CubeGrid.RaiseGridChanged();
            base.OnControlAcquired_UpdateCamera();

            m_cameraShake = new MyCameraHeadShake();
        }

        protected override void OnControlledEntity_Used()
        {
            // This should be sync using IControlledEntity - it's actions
            // It should be pretty easy, because this entity is controlled by player
            // It should be sent same as update position is sent
            //Debug.Assert(m_pilot != null, "Pilot cannot be null");

            var pilot = m_pilot;

            RemovePilot();

            base.OnControlledEntity_Used();
        }

        protected override void OnControlReleased_UpdateCamera()
        {
            base.OnControlReleased_UpdateCamera();

            m_cameraSpring = null;
            m_cameraShake = null;
        }

        protected override void RemoveLocal()
        {
            base.RemoveLocal();
            RemovePilot();
        }

        protected override void OnOwnershipChanged()
        {
            base.OnOwnershipChanged();

            if (m_pilot != null && Sync.IsServer)
            {
                if (ControllerInfo.Controller != null)
                {
                    var relation = GetUserRelationToOwner(ControllerInfo.ControllingIdentityId);
                    if (relation == MyRelationsBetweenPlayerAndBlock.Enemies || relation == MyRelationsBetweenPlayerAndBlock.Neutral)
                        SyncObject.ControlledEntity_Use();
                }
            }
        }

        public override List<MyHudEntityParams> GetHudParams(bool allowBlink)
        {
            var hudParams = base.GetHudParams(allowBlink);
            long localPlayerId = MySession.LocalHumanPlayer == null ? 0 : MySession.LocalHumanPlayer.Identity.IdentityId;
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

        MatrixD IMyCameraController.GetViewMatrix()
        {
            return GetViewMatrix();
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

        bool IMyCameraController.AllowCubeBuilding
        {
            get
            {
                return false;
            }
        }

        bool IMyOxygenBlock.IsWorking()
        {
            return IsWorking && BlockDefinition.IsPressurized;
        }

        int IMyOxygenConsumer.GetPriority()
        {
            return 0;
        }

        float IMyOxygenConsumer.ConsumptionNeed(float deltaTime)
        {
            float OXYGEN_REGEN_PER_SECOND = 2f;

            return Math.Min(OXYGEN_REGEN_PER_SECOND * deltaTime, OxygenAmountMissing);
        }

        void IMyOxygenConsumer.Consume(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            OxygenAmount += amount;
            if (m_oxygenLevel > 1f)
            {
                m_oxygenLevel = 1f;
            }
        }
    }
}
