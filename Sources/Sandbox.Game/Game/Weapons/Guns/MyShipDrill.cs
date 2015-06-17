using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Havok;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Weapons.Guns;

using VRageMath;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Debugging;
using System.Reflection;
using Sandbox.Common;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems;
using VRageRender;
using Sandbox.Game.Gui;
using MyGuiConstants = Sandbox.Graphics.GUI.MyGuiConstants;
using Sandbox.ModAPI.Interfaces;
using VRage.Utils;
using Sandbox.ModAPI;
using Sandbox.Game.World;
using Sandbox.Game.Localization;
using VRage;
using VRage.ModAPI;
using VRage.Components;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Drill))]
    class MyShipDrill : MyFunctionalBlock, IMyGunObject<MyToolBase>, IMyPowerConsumer, IMyInventoryOwner, IMyConveyorEndpointBlock, IMyShipDrill
    {
        public readonly static MyDrillBase.Sounds m_sounds;

        private const float HEAD_MAX_ROTATION_SPEED = MathHelper.TwoPi*2f;
        private const float HEAD_SLOWDOWN_TIME_IN_SECONDS = 0.5f;

        private static int m_countdownDistributor;

        private MyInventory m_inventory;
        private int m_blockLength;
        private float m_cubeSideLength;
        private MyDefinitionId m_defId;
        private int m_headLastUpdateTime;
        private bool m_isControlled;

        // Drilling logic
        private MyDrillBase m_drillBase;
        private int m_drillFrameCountdown = MyDrillConstants.DRILL_UPDATE_INTERVAL_IN_FRAMES;
        private bool m_wantsToDrill;
        private bool m_wantsToCollect;

        private MyCharacter m_owner;
        private bool m_useConveyorSystem;
		public bool IsDeconstructor { get { return false; } }
        private IMyConveyorEndpoint m_multilineConveyorEndpoint;

        static MyShipDrill()
        {
            var useConvSystem = new MyTerminalControlOnOffSwitch<MyShipDrill>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConvSystem.Getter = (x) => (x as IMyInventoryOwner).UseConveyorSystem;
            useConvSystem.Setter = (x, v) => MySyncConveyors.SendChangeUseConveyorSystemRequest(x.EntityId, v);
            useConvSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConvSystem);

            m_sounds = new MyDrillBase.Sounds()
            {
                IdleLoop = new MySoundPair("ToolShipDrillIdle"),
                MetalLoop = new MySoundPair("ToolShipDrillMetal"),
                RockLoop = new MySoundPair("ToolShipDrillRock"),
            };
        }

        public MyCharacter Owner { get { return m_owner; } }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        protected override void OnEnabledChanged()
        {
            PowerReceiver.Update();

            base.OnEnabledChanged();
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
        public bool EnabledInWorldRules
        {
            get { return true; }
        }
        public MyDefinitionId DefinitionId
        {
            get { return m_defId; }
        }
        public MyInventoryOwnerTypeEnum InventoryOwnerType
        {
            get { return MyInventoryOwnerTypeEnum.System; }
        }
        public int InventoryCount
        {
            get { return 1; }
        }

        public MyShipDrill()
        {
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            Debug.Assert((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0, "Base class of ship drill uses Update10, and ship drill turns it on and off. Things might break!");
            SetupDrillFrameCountdown();
        }

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            base.Init(builder, cubeGrid);
            m_defId = builder.GetId();
            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(m_defId) as MyShipDrillDefinition;
            
            m_blockLength = def.Size.Z;
            m_cubeSideLength = MyDefinitionManager.Static.GetCubeSize(def.CubeSize);

            float inventoryVolume = def.Size.X * def.Size.Y * def.Size.Z * m_cubeSideLength * m_cubeSideLength * m_cubeSideLength * 0.5f;
            Vector3 inventorySize = new Vector3(def.Size.X, def.Size.Y, def.Size.Z * 0.5f);

            m_inventory = new MyInventory(inventoryVolume, inventorySize, MyInventoryFlags.CanSend, this);
            m_inventory.Constraint = new MyInventoryConstraint(MySpaceTexts.ToolTipItemFilter_AnyOre)
                .AddObjectBuilderType(typeof(MyObjectBuilder_Ore));

            SlimBlock.UsesDeformation = false;
            SlimBlock.DeformationRatio = def.DeformationRatio; // 3x times harder for destruction by high speed        

            m_drillBase = new MyDrillBase(this,
                                          MyDrillConstants.DRILL_SHIP_DUST_EFFECT,
                                          MyDrillConstants.DRILL_SHIP_DUST_STONES_EFFECT,
                                          MyDrillConstants.DRILL_SHIP_SPARKS_EFFECT,
                                          new MyDrillSensorSphere(def.SensorRadius, def.SensorOffset),
                                          new MyDrillCutOut(def.SensorOffset , def.SensorRadius),
                                          HEAD_SLOWDOWN_TIME_IN_SECONDS,
                                          floatingObjectSpawnOffset: -0.4f,
                                          floatingObjectSpawnRadius: 0.4f,
                                          sounds: m_sounds,
                                          inventoryCollectionRatio: 0.7f
            );
            m_drillBase.OutputInventory = m_inventory;
            m_drillBase.IgnoredEntities.Add(this);
            m_drillBase.OnWorldPositionChanged(WorldMatrix);
            m_wantsToDrill = false;
            m_wantsToCollect = false;
            AddDebugRenderComponent(new Components.MyDebugRenderCompomentDrawDrillBase(m_drillBase));

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Defense,
                false,
                ComputeMaxRequiredPower(),
                ComputeRequiredPower);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.Update();

            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawPowerReciever(PowerReceiver, this));

            var obDrill = (MyObjectBuilder_Drill)builder;
            m_inventory.Init(obDrill.Inventory);

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            m_useConveyorSystem = obDrill.UseConveyorSystem;

            UpdateDetailedInfo();
        }

        void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            if ((Enabled || m_wantsToDrill) && IsFunctional && PowerReceiver.IsPowered)
            {
                // starts the animation
                m_drillBase.Drill(collectOre: false, performCutout: false);
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
            else
            {
                NeedsUpdate &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;
                SetupDrillFrameCountdown();
                m_drillBase.StopDrill();
            }
        }

        private void SetupDrillFrameCountdown()
        {
            m_countdownDistributor += 10;
            if (m_countdownDistributor > MyDrillConstants.DRILL_UPDATE_DISTRIBUTION_IN_FRAMES)
                m_countdownDistributor = -MyDrillConstants.DRILL_UPDATE_DISTRIBUTION_IN_FRAMES;
            m_drillFrameCountdown = MyDrillConstants.DRILL_UPDATE_INTERVAL_IN_FRAMES + m_countdownDistributor;
        }

        void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var obDrill = (MyObjectBuilder_Drill)base.GetObjectBuilderCubeBlock(copy);
            obDrill.Inventory = m_inventory.GetObjectBuilder();
            obDrill.UseConveyorSystem = m_useConveyorSystem;
            return obDrill;
        }

        protected override void Closing()
        {
            base.Closing();
            m_drillBase.Close();
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(m_inventory);
            base.OnRemovedByCubeBuilder();
        }

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);

            // OnWorldPositionChanged() gets called from Init() as well.
            // At that point, however, DrillBase was not yet created so check this.
            if (m_drillBase != null)
                m_drillBase.OnWorldPositionChanged(WorldMatrix);
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            m_drillBase.UpdateAfterSimulation100();

            if (Sync.IsServer && IsFunctional && m_useConveyorSystem && m_inventory.GetItems().Count > 0)
            {
                MyGridConveyorSystem.PushAnyRequest(this, m_inventory, OwnerId);
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();

            Debug.Assert(m_wantsToDrill || Enabled);

            if (Parent == null || Parent.Physics == null)
                return;

            m_drillFrameCountdown -= 10;
            if (m_drillFrameCountdown > 0)
                return;
            m_drillFrameCountdown += MyDrillConstants.DRILL_UPDATE_INTERVAL_IN_FRAMES;
            m_drillBase.IgnoredEntities.Add(Parent);
            if (m_drillBase.Drill(collectOre: Enabled || m_wantsToCollect, performCutout: true))
            {
                foreach (var c in CubeGrid.GetBlocks())
                {
                    if (c.FatBlock != null && c.FatBlock is MyCockpit)
                    {
                        ((MyCockpit)c.FatBlock).AddShake(ShakeAmount);
                    }
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            PowerReceiver.Update();
            base.UpdateAfterSimulation();

            m_drillBase.UpdateAfterSimulation();

            if (m_wantsToDrill || m_drillBase.AnimationMaxSpeedRatio > 0f)
            {
                if (MySession.Static.EnableToolShake && MyFakes.ENABLE_TOOL_SHAKE)
                {
                    ApplyShakeForce();
                }

                float timeDelta = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_headLastUpdateTime) / 1000f;
                float rotationDeltaAngle = timeDelta * m_drillBase.AnimationMaxSpeedRatio * HEAD_MAX_ROTATION_SPEED;

                m_headLastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

                var rotationDeltaMatrix = Matrix.CreateRotationZ(rotationDeltaAngle);
                foreach (var subpart in Subparts)
                    subpart.Value.PositionComp.LocalMatrix = subpart.Value.PositionComp.LocalMatrix * rotationDeltaMatrix;
            }
        }

        private void UpdateDetailedInfo()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.AppendFormat("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.MaxRequiredInput, DetailedInfo);
            DetailedInfo.AppendFormat("\n");

            RaisePropertiesChanged();
        }

        public bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            status = MyGunStatusEnum.OK;

            if (action != MyShootActionEnum.PrimaryAction && action != MyShootActionEnum.SecondaryAction)
            {
                status = MyGunStatusEnum.Failed;
                return false;
            }
            if (!IsFunctional)
            {
                status = MyGunStatusEnum.NotFunctional;
                return false;
            }
            if (!HasPlayerAccess(shooter))
            {
                status = MyGunStatusEnum.AccessDenied;
                return false;
            }
            if (Enabled)
            {
                status = MyGunStatusEnum.Disabled;
                return false;
            }

            return true;
        }

        public void Shoot(MyShootActionEnum action, Vector3 direction)
        {
            if (action != MyShootActionEnum.PrimaryAction && action != MyShootActionEnum.SecondaryAction) return;

            m_wantsToDrill = true;
            m_wantsToCollect = action == MyShootActionEnum.PrimaryAction;
            ShakeAmount = 2.5f;

            PowerReceiver.Update();
        }

        public void EndShoot(MyShootActionEnum action)
        {
            m_wantsToDrill = false;
            PowerReceiver.Update();
        }

        public void OnControlAcquired(MyCharacter owner)
        {
            m_owner = owner;
            m_isControlled = true;
        }

        public void OnControlReleased()
        {
            m_owner = null;
            m_isControlled = false;

            if (!Enabled)
                m_drillBase.StopDrill();
        }

        public void DrawHud(IMyCameraController camera, long playerId)
        {
        }

        public MyInventory GetInventory(int index = 0)
        {
            Debug.Assert(index == 0);
            return m_inventory;
        }

        bool ModAPI.Interfaces.IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return (this as IMyInventoryOwner).UseConveyorSystem;
            }
            set
            {
                (this as IMyInventoryOwner).UseConveyorSystem = value;
            }
        }

        Sandbox.ModAPI.Interfaces.IMyInventory Sandbox.ModAPI.Interfaces.IMyInventoryOwner.GetInventory(int index)
        {
            return GetInventory(index);
        }

        public override void OnDestroy()
        {
            ReleaseInventory(m_inventory);
            base.OnDestroy();
        }

        private void ApplyShakeForce(float standbyRotationRatio = 1.0f)
        {
            const float PeriodA = 13.35f;
            const float PeriodB = 18.154f;
            const float ShakeForceStrength = 240.0f;
            int offset = GetHashCode(); // Different offset for each drill

            float strength = this.CubeGrid.GridSizeEnum == MyCubeSize.Small ? 1.0f : 5.0f;
            var axisA = this.WorldMatrix.Up;
            var axisB = this.WorldMatrix.Right;
            var force = Vector3.Zero;
            float timeMs = (float)Sandbox.Game.Debugging.MyPerformanceCounter.TicksToMs(Sandbox.Game.Debugging.MyPerformanceCounter.ElapsedTicks);
            force += axisA * (float)Math.Sin(offset + timeMs * PeriodA / 5);
            force += axisB * (float)Math.Sin(offset + timeMs * PeriodB / 5);
            force *= standbyRotationRatio * strength * ShakeForceStrength * m_drillBase.AnimationMaxSpeedRatio * m_drillBase.AnimationMaxSpeedRatio; // Quadratic fade out looks better
            this.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, force, PositionComp.GetPosition(), null);
        }

        private Vector3 ComputeDrillSensorCenter()
        {
            return WorldMatrix.Forward * (m_blockLength-2) * m_cubeSideLength + WorldMatrix.Translation;
        }

        private float ComputeMaxRequiredPower()
        {
            return MyEnergyConstants.MAX_REQUIRED_POWER_SHIP_DRILL * m_powerConsumptionMultiplier;
        }

        private float ComputeRequiredPower()
        {
            return (IsFunctional && (Enabled || m_wantsToDrill)) ? PowerReceiver.MaxRequiredInput : 0f;
        }

        String IMyInventoryOwner.DisplayNameText
        {
            get { return CustomName.ToString(); }
        }

        bool IMyInventoryOwner.UseConveyorSystem
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

        public int GetAmmunitionAmount()
        {
            return 0;
        }

        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_multilineConveyorEndpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_multilineConveyorEndpoint = new MyMultilineConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_multilineConveyorEndpoint));
        }

        public bool IsShooting
        {
            get { return m_drillBase.IsDrilling; }
        }

        int IMyGunObject<MyToolBase>.ShootDirectionUpdateTime
        {
            get { return 0; }
        }

        public Vector3 DirectionToTarget(Vector3D target)
        {
            throw new NotImplementedException();
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
        bool Sandbox.ModAPI.Ingame.IMyShipDrill.UseConveyorSystem
        {
            get
            {
                return (this as IMyInventoryOwner).UseConveyorSystem;
            }
        }

        private float m_drillMultiplier = 1f;
        float Sandbox.ModAPI.IMyShipDrill.DrillHarvestMultiplier
        {
            get
            {
                return m_drillMultiplier;
            }
            set
            {
                m_drillMultiplier = value;
                if (m_drillBase != null)
                {
                    m_drillBase.VoxelHarvestRatio = MyDrillConstants.VOXEL_HARVEST_RATIO * m_drillMultiplier;
                    m_drillBase.VoxelHarvestRatio = MathHelper.Clamp(m_drillBase.VoxelHarvestRatio, 0f, 1f);
                }
            }
        }

        private float m_powerConsumptionMultiplier = 1f;
        float Sandbox.ModAPI.IMyShipDrill.PowerConsumptionMultiplier
        {
            get
            {
                return m_powerConsumptionMultiplier;
            }
            set
            {
                m_powerConsumptionMultiplier = value;
                if (m_powerConsumptionMultiplier < 0.01f)
                {
                    m_powerConsumptionMultiplier = 0.01f;
                }

                if (PowerReceiver != null)
                {
                    PowerReceiver.MaxRequiredInput = ComputeMaxRequiredPower();
                    PowerReceiver.Update();

                    UpdateDetailedInfo();
                }
            }
        }
    }
}
