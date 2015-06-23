using Havok;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Lights;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Graphics.GUI;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Utils;
using VRageMath;
using VRageRender;
using MyGuiConstants = Sandbox.Graphics.GUI.MyGuiConstants;
using VRage.ModAPI;
using VRage.Components;

namespace Sandbox.Game.Weapons
{
    public abstract class MyShipToolBase : MyFunctionalBlock, IMyGunObject<MyToolBase>, IMyPowerConsumer, IMyInventoryOwner, IMyConveyorEndpointBlock, IMyShipToolBase
    {
        private MyInventory m_inventory;
        protected MyInventory Inventory
        {
            get
            {
                return m_inventory;
            }
        }

        ModAPI.Interfaces.IMyInventory ModAPI.Interfaces.IMyInventoryOwner.GetInventory(int index)
        {
            return Inventory;
        }
		public bool IsDeconstructor { get { return false; } }

        private MyMultilineConveyorEndpoint m_endpoint;
        private MyDefinitionId m_defId;

        // State variables
        private bool m_wantsToActivate;
        private bool m_isActivated;
        private bool m_isActivatedOnSomething;
        protected int m_lastTimeActivate;

        private int m_shootHeatup;

        private bool m_effectsSet;

        //Debugging variable
        private int m_activateCounter;

        private Dictionary<MyEntity, int> m_entitiesInContact;
        protected BoundingSphere m_detectorSphere;

        private HashSet<MySlimBlock> m_blocksToActivateOn;
        private HashSet<MySlimBlock> m_tempBlocksBuffer;

        private bool m_useConveyorSystem;

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        static MyShipToolBase()
        {
            var useConvSystem = new MyTerminalControlOnOffSwitch<MyShipToolBase>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConvSystem.Getter = (x) => (x as IMyInventoryOwner).UseConveyorSystem;
            useConvSystem.Setter = (x, v) => MySyncConveyors.SendChangeUseConveyorSystemRequest(x.EntityId, v);
            useConvSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConvSystem);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            m_entitiesInContact = new Dictionary<MyEntity, int>();
            m_blocksToActivateOn = new HashSet<MySlimBlock>();
            m_tempBlocksBuffer = new HashSet<MySlimBlock>();

            m_isActivated = false;
            m_isActivatedOnSomething = false;
            m_wantsToActivate = false;
            m_effectsSet = false;

            m_shootHeatup = 0;
            m_activateCounter = 0;

            m_defId = objectBuilder.GetId();
            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(m_defId);

            var typedBuilder = objectBuilder as MyObjectBuilder_ShipToolBase;

            //each dimension of size needs to be scaled by grid size not only one 
            float inventoryVolume = def.Size.X * cubeGrid.GridSize*def.Size.Y *cubeGrid.GridSize* def.Size.Z * cubeGrid.GridSize * 0.5f;
            Vector3 inventorySize = new Vector3(def.Size.X, def.Size.Y, def.Size.Z * 0.5f);

            m_inventory = new MyInventory(inventoryVolume, inventorySize, MyInventoryFlags.CanSend, this);
            m_inventory.Init(typedBuilder.Inventory);

            SlimBlock.UsesDeformation = false;
            SlimBlock.DeformationRatio = typedBuilder.DeformationRatio; // 3x times harder for destruction by high speed

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Defense,
                false,
                MyEnergyConstants.MAX_REQUIRED_POWER_SHIP_GRINDER,
                ComputeRequiredPower);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;

            Enabled = typedBuilder.Enabled;
            UseConveyorSystem = typedBuilder.UseConveyorSystem;

            base.EnabledChanged += MyShipToolBase_EnabledChanged;
            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            LoadDummies();

            UpdateActivationState();
            PowerReceiver.Update();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.EACH_FRAME;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy);

            MyObjectBuilder_ShipToolBase obShipToolBase = (MyObjectBuilder_ShipToolBase)ob;
            obShipToolBase.Inventory = m_inventory.GetObjectBuilder();
            obShipToolBase.UseConveyorSystem = UseConveyorSystem;

            return obShipToolBase;
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(m_inventory);
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            ReleaseInventory(m_inventory, true);
            base.OnDestroy();
        }

        private void LoadDummies()
        {
            var finalModel = Engine.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);
            foreach (var dummy in finalModel.Dummies)
            {
                if (dummy.Key.ToLower().Contains("detector_shiptool"))
                {
                    var matrix = dummy.Value.Matrix;
                    float radius = matrix.Scale.AbsMin();

                    Matrix blockMatrix = this.PositionComp.LocalMatrix;
                    Vector3 gridDetectorPosition = Vector3.Transform(matrix.Translation, blockMatrix);

                    m_detectorSphere = new BoundingSphere(gridDetectorPosition, radius);

                    var phantom = new HkPhantomCallbackShape(phantom_Enter, phantom_Leave);
                    var sphereShape = new HkSphereShape(radius);
                    var detectorShape = new HkBvShape(sphereShape, phantom, HkReferencePolicy.TakeOwnership);

                    Physics = new Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_DEFAULT);
                    Physics.IsPhantom = true;
                    Physics.CreateFromCollisionObject(detectorShape, matrix.Translation, WorldMatrix, null, MyPhysics.ObjectDetectionCollisionLayer);
                    detectorShape.Base.RemoveReference();
                    break;
                }
            }
        }

        private void phantom_Leave(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            MyEntity entity = bodyToOtherEntity(body);
            if (entity == null) return;

            if (!(entity is MyCubeGrid) && !(entity is MyCharacter)) return;

            int entityCounter;
            bool registered = m_entitiesInContact.TryGetValue(entity, out entityCounter);
            Debug.Assert(registered, "Unregistering not registered entity from ship tool");
            if (!registered)
                return;

            m_entitiesInContact.Remove(entity);
            if (entityCounter > 1)
                m_entitiesInContact.Add(entity, entityCounter - 1);
        }

        private void phantom_Enter(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            MyEntity entity = bodyToOtherEntity(body);
            if (entity == null) return;

            if (!(entity is MyCubeGrid) && !(entity is MyCharacter)) return;

            int entityCounter;
            if (m_entitiesInContact.TryGetValue(entity, out entityCounter))
            {
                m_entitiesInContact.Remove(entity);
                m_entitiesInContact.Add(entity, entityCounter + 1);
            }
            else
            {
                m_entitiesInContact.Add(entity, 1);
            }
        }

        protected virtual bool CanInteractWithSelf
        {
            get
            {
                return false;
            }
        }

        private MyEntity bodyToOtherEntity(HkRigidBody body)
        {
            var entity = body.GetEntity();
            if (entity == null) return null;
            if (entity == this.CubeGrid && !CanInteractWithSelf) return null;

            return entity as MyEntity;
        }

        void MyShipToolBase_EnabledChanged(MyTerminalBlock obj)
        {
            PowerReceiver.Update();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateActivationState();
            UpdateIsWorking();
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            UpdateActivationState();
        }

        private void UpdateActivationState()
        {
            if ((Enabled || m_wantsToActivate) && IsFunctional && PowerReceiver.IsPowered)
            {
                StartShooting();
            }
            else
            {
                StopShooting();
            }
        }

        private float ComputeRequiredPower()
        {
            return (IsFunctional && (Enabled || m_wantsToActivate)) ? PowerReceiver.MaxRequiredInput : 0f;
        }

        public override void UpdateAfterSimulation()
        {
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_SHIP_TOOLS)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), m_isActivated ? "Activated" : "Not activated", Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 90.0f), "Activation counter: " + m_activateCounter, Color.Red, 1.0f);
            }

            base.UpdateAfterSimulation();

            if (m_isActivated && MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeActivate >= MyShipGrinderConstants.GRINDER_COOLDOWN_IN_MILISECONDS)
            {
                ActivateCommon();
            }

            if (IsFunctional)
                UpdateAnimationCommon();
        }

        protected abstract bool Activate(HashSet<MySlimBlock> targets);

        protected abstract void UpdateAnimation(bool activated);

        private void UpdateAnimationCommon()
        {
            UpdateAnimation(m_isActivated);

            if (m_isActivatedOnSomething && m_effectsSet == false)
            {
                StartEffects();
                m_effectsSet = true;
            }
            else if (m_isActivatedOnSomething)
                UpdateEffects();
            else if (!m_isActivatedOnSomething && m_effectsSet == true)
            {
                StopEffects();
                m_effectsSet = false;
            }
        }

        protected abstract void StartEffects();

        protected abstract void StopEffects();

        protected abstract void UpdateEffects();

        private void ActivateCommon()
        {
            BoundingSphereD globalSphere = new BoundingSphereD(Vector3D.Transform(m_detectorSphere.Center, CubeGrid.WorldMatrix), m_detectorSphere.Radius);

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
            {
                MyRenderProxy.DebugDrawSphere(globalSphere.Center, (float)globalSphere.Radius, Color.Red.ToVector3(), 1.0f, false);
            }

            m_isActivatedOnSomething = false;

            foreach (var entry in m_entitiesInContact)
            {
                MyCubeGrid grid = entry.Key as MyCubeGrid;
                MyCharacter character = entry.Key as MyCharacter;

                if (grid != null)
                {
                    m_tempBlocksBuffer.Clear();
                    grid.GetBlocksInsideSphere(ref globalSphere, m_tempBlocksBuffer);
                    m_blocksToActivateOn.UnionWith(m_tempBlocksBuffer);
                }
                if (character != null && Sync.IsServer)
                {
                    character.DoDamage(20, MyDamageType.Drill, true);
                }
            }

            m_isActivatedOnSomething |= Activate(m_blocksToActivateOn);

            m_activateCounter++;
            m_lastTimeActivate = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            PlayLoopSound(m_isActivatedOnSomething);

            m_blocksToActivateOn.Clear();
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);

            StopEffects();
            StopLoopSound();
        }

        protected override void Closing()
        {
            base.Closing();

            StopEffects();
            StopLoopSound();
        }

        public float BackkickForcePerSecond
        {
            get { return 0; }
        }

        public float ShakeAmount
        {
            get;
            protected set;
        }

        public Definitions.MyDefinitionId DefinitionId
        {
            get { return m_defId; }
        }

        public bool EnabledInWorldRules
        {
            get { return true; }
        }

        protected virtual void StartShooting()
        {
            Physics.Enabled = true;
            m_isActivated = true;
        }

        protected virtual void StopShooting()
        {
            m_wantsToActivate = false;
            m_isActivated = false;
            m_isActivatedOnSomething = false;

            if (Physics != null)
                Physics.Enabled = false;
            if (PowerReceiver != null)
                PowerReceiver.Update();

            m_shootHeatup = 0;

            StopEffects();
            StopLoopSound();
        }

        public int GetAmmunitionAmount()
        {
            throw new NotImplementedException();
        }

        public void OnControlAcquired(Entities.Character.MyCharacter owner)
        { }

        public void OnControlReleased()
        {
            if (!Enabled && !Closed)
                StopShooting();
        }

        public void DrawHud(IMyCameraController camera, long playerId)
        { }

        public int InventoryCount
        {
            get { return 1; }
        }

        public MyInventory GetInventory(int index)
        {
            Debug.Assert(index == 0);
            return m_inventory;
        }

        public MyInventoryOwnerTypeEnum InventoryOwnerType
        {
            get { return MyInventoryOwnerTypeEnum.System; }
        }

        public bool UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
            set
            {
                m_useConveyorSystem = value;
            }
        }

        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_endpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_endpoint = new MyMultilineConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_endpoint));
        }

        protected abstract void StopLoopSound();
        protected abstract void PlayLoopSound(bool activated);

        public Vector3 DirectionToTarget(Vector3D target)
        {
            throw new NotImplementedException();
        }

        public bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            status = MyGunStatusEnum.OK;

            if (action != MyShootActionEnum.PrimaryAction)
            {
                status = MyGunStatusEnum.Failed;
                return false;
            }
            if (!IsFunctional)
            {
                status = MyGunStatusEnum.NotFunctional;
                return false;
            }
            if (Enabled)
            {
                status = MyGunStatusEnum.Disabled;
                return false;
            }
            if (!HasPlayerAccess(shooter))
            {
                status = MyGunStatusEnum.AccessDenied;
                return false;
            }
            if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeActivate < MyShipGrinderConstants.GRINDER_COOLDOWN_IN_MILISECONDS)
            {
                status = MyGunStatusEnum.Cooldown;
                return false;
            }

            return true;
        }

        public void Shoot(MyShootActionEnum action, Vector3 direction)
        {
            if (action != MyShootActionEnum.PrimaryAction) return;

            if (m_shootHeatup < MyShipGrinderConstants.GRINDER_HEATUP_FRAMES)
            {
                m_shootHeatup++;
                return;
            }

            m_wantsToActivate = true;
            PowerReceiver.Update();
        }

        public void EndShoot(MyShootActionEnum action)
        {
            if (action != MyShootActionEnum.PrimaryAction) return;

            if (!Enabled)
                StopShooting();
        }

        public bool IsShooting
        {
            get { return m_isActivated; }
        }

        public int ShootDirectionUpdateTime
        {
            get { return 0; }
        }

        public void BeginFailReaction(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public void BeginFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public void ShootFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public MyToolBase GunBase
        {
            get { return null; }
        }
        bool IMyShipToolBase.UseConveyorSystem { get { return (this as IMyInventoryOwner).UseConveyorSystem; } }
    }
}
