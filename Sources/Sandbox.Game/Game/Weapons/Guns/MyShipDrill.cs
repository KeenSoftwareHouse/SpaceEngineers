﻿using System;
using System.Diagnostics;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons.Guns;

using VRageMath;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.ModAPI.Interfaces;
using VRage.Utils;
using Sandbox.ModAPI;
using Sandbox.Game.World;
using Sandbox.Game.Localization;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI.Ingame;
using IMyInventory = VRage.ModAPI.Ingame.IMyInventory;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Drill))]
    class MyShipDrill : MyFunctionalBlock, IMyGunObject<MyToolBase>, IMyInventoryOwner, IMyConveyorEndpointBlock, IMyShipDrill
    {
        private const float HEAD_MAX_ROTATION_SPEED = MathHelper.TwoPi*2f;
        private const float HEAD_SLOWDOWN_TIME_IN_SECONDS = 0.5f;

        private static int m_countdownDistributor;
        
        private int m_blockLength;
        private float m_cubeSideLength;
        private MyDefinitionId m_defId;
        private int m_headLastUpdateTime;
        private bool m_isControlled;

        // Drilling logic
        private MyDrillBase m_drillBase;
        private int m_drillFrameCountdown = MyDrillConstants.DRILL_UPDATE_INTERVAL_IN_FRAMES;
        private bool m_wantsToDrill;
        private bool WantsToDrill { get { return m_wantsToDrill; } set { m_wantsToDrill = value; WantstoDrillChanged(); } }

        private bool m_wantsToCollect;
        private bool drillStart = true;

        private MyCharacter m_owner;
        private readonly Sync<bool> m_useConveyorSystem;
		public bool IsDeconstructor { get { return false; } }
        private IMyConveyorEndpoint m_multilineConveyorEndpoint;

        static MyShipDrill()
        {
            var useConvSystem = new MyTerminalControlOnOffSwitch<MyShipDrill>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConvSystem.Getter = (x) => (x).UseConveyorSystem;
            useConvSystem.Setter = (x, v) =>(x).UseConveyorSystem = v;
            useConvSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConvSystem);
        }

        public MyCharacter Owner { get { return m_owner; } }

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

        public MyShipDrill()
        {
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            Debug.Assert((NeedsUpdate & MyEntityUpdateEnum.EACH_10TH_FRAME) == 0, "Base class of ship drill uses Update10, and ship drill turns it on and off. Things might break!");
            SetupDrillFrameCountdown();
        }

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            m_defId = builder.GetId();
            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(m_defId) as MyShipDrillDefinition;

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                def.ResourceSinkGroup,
                ComputeMaxRequiredPower(),
                ComputeRequiredPower);
            ResourceSink = sinkComp;

            m_drillBase = new MyDrillBase(this,
                                       MyDrillConstants.DRILL_SHIP_DUST_EFFECT,
                                       MyDrillConstants.DRILL_SHIP_DUST_STONES_EFFECT,
                                       MyDrillConstants.DRILL_SHIP_SPARKS_EFFECT,
                                       new MyDrillSensorSphere(def.SensorRadius, def.SensorOffset),
                                       new MyDrillCutOut(def.SensorOffset, def.SensorRadius),
                                       HEAD_SLOWDOWN_TIME_IN_SECONDS,
                                       floatingObjectSpawnOffset: -0.4f,
                                       floatingObjectSpawnRadius: 0.4f,
                                       inventoryCollectionRatio: 0.7f
         );

            base.Init(builder, cubeGrid);
            
            m_blockLength = def.Size.Z;
            m_cubeSideLength = MyDefinitionManager.Static.GetCubeSize(def.CubeSize);

            float inventoryVolume = def.Size.X * def.Size.Y * def.Size.Z * m_cubeSideLength * m_cubeSideLength * m_cubeSideLength * 0.5f;
            Vector3 inventorySize = new Vector3(def.Size.X, def.Size.Y, def.Size.Z * 0.5f);

            if (this.GetInventory() == null)
            {
                Components.Add<MyInventoryBase>( new MyInventory(inventoryVolume, inventorySize, MyInventoryFlags.CanSend, this));
            }
            Debug.Assert(this.GetInventory().Owner == this, "Ownership was not set!");

            this.GetInventory().Constraint = new MyInventoryConstraint(MySpaceTexts.ToolTipItemFilter_AnyOre)
                .AddObjectBuilderType(typeof(MyObjectBuilder_Ore));

            SlimBlock.UsesDeformation = false;
            SlimBlock.DeformationRatio = def.DeformationRatio; // 3x times harder for destruction by high speed        

         
            m_drillBase.OutputInventory = this.GetInventory();
            m_drillBase.IgnoredEntities.Add(this);
            m_drillBase.OnWorldPositionChanged(WorldMatrix);
            m_wantsToCollect = false;
            AddDebugRenderComponent(new Components.MyDebugRenderCompomentDrawDrillBase(m_drillBase));

			
			ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
			ResourceSink.Update();

            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawPowerReciever(ResourceSink, this));

            var obDrill = (MyObjectBuilder_Drill)builder;
            this.GetInventory().Init(obDrill.Inventory);			

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            m_useConveyorSystem.Value = obDrill.UseConveyorSystem;

            UpdateDetailedInfo();

            m_wantsToDrill = obDrill.Enabled;
            IsWorkingChanged += OnIsWorkingChanged;

            m_drillBase.m_drillMaterial = MyStringHash.GetOrCompute("ShipDrill");
            m_drillBase.m_idleSoundLoop = new MySoundPair("ToolShipDrillIdle");
        }

        protected override void OnInventoryComponentAdded(MyInventoryBase inventory)
        {
            base.OnInventoryComponentAdded(inventory);
            Debug.Assert(this.GetInventory() != null, "Added inventory to collector, but different type than MyInventory?! Check this.");
            if (this.GetInventory() != null && MyPerGameSettings.InventoryMass)
            {
                this.GetInventory().ContentsChanged += Inventory_ContentsChanged;
            }
        }

        protected override void OnInventoryComponentRemoved(MyInventoryBase inventory)
        {
            base.OnInventoryComponentRemoved(inventory);
            var removedInventory = inventory as MyInventory;
            Debug.Assert(removedInventory != null, "Removed inventory is not MyInventory type? Check this.");
            if (removedInventory != null && MyPerGameSettings.InventoryMass)
            {
                removedInventory.ContentsChanged -= Inventory_ContentsChanged;
            }
        }


        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPowered && base.CheckIsWorking();
        }

        protected override void OnEnabledChanged()
        {
            WantsToDrill = Enabled;

            base.OnEnabledChanged();
        }

        void OnIsWorkingChanged(MyCubeBlock obj)
        {
            ResourceSink.Update();
        }

        void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            WantstoDrillChanged();
        }

		void Inventory_ContentsChanged(MyInventoryBase obj)
		{
			CubeGrid.SetInventoryMassDirty();
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
            ResourceSink.Update();
        }

        void WantstoDrillChanged()
        {
            if ((Enabled || WantsToDrill) && IsFunctional && ResourceSink!=null && ResourceSink.IsPowered)
            {
                // starts the animation
                if(drillStart)
                    m_drillBase.Drill(collectOre: false, performCutout: false);
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                drillStart = false;
            }
            else
            {
                NeedsUpdate &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;
                SetupDrillFrameCountdown();
                m_drillBase.StopDrill();
                drillStart = true;
            }
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var obDrill = (MyObjectBuilder_Drill)base.GetObjectBuilderCubeBlock(copy);
            obDrill.Inventory = this.GetInventory().GetObjectBuilder();
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
            ReleaseInventory(this.GetInventory());
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

            if (Sync.IsServer && IsFunctional && m_useConveyorSystem && this.GetInventory().GetItems().Count > 0)
            {
                MyGridConveyorSystem.PushAnyRequest(this, this.GetInventory(), OwnerId);
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();

            Debug.Assert(WantsToDrill || Enabled);

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
            ResourceSink.Update();
            base.UpdateAfterSimulation();

            m_drillBase.UpdateAfterSimulation();

            if (WantsToDrill || m_drillBase.AnimationMaxSpeedRatio > 0f)
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
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.AppendFormat("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.MaxRequiredInput, DetailedInfo);
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

        public void Shoot(MyShootActionEnum action, Vector3 direction, string gunAction)
        {
            if (action != MyShootActionEnum.PrimaryAction && action != MyShootActionEnum.SecondaryAction) return;

            WantsToDrill = true;
            m_wantsToCollect = action == MyShootActionEnum.PrimaryAction;
            ShakeAmount = 2.5f;

            ResourceSink.Update();
        }

        public void EndShoot(MyShootActionEnum action)
        {
            WantsToDrill = false;
            ResourceSink.Update();
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

        public override void OnDestroy()
        {
            ReleaseInventory(this.GetInventory());
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
            return (IsFunctional && (Enabled || WantsToDrill)) ? ResourceSink.MaxRequiredInput : 0f;
        }

        public bool UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
            set
            {
                m_useConveyorSystem.Value = value;
            }
        }

        IMyInventory IMyInventoryOwner.GetInventory(int index)
        {
            return this.GetInventory(index);
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
                return UseConveyorSystem;
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

                if (ResourceSink != null)
                {
                    ResourceSink.MaxRequiredInput = ComputeMaxRequiredPower() * m_powerConsumptionMultiplier;
                    ResourceSink.Update();

                    UpdateDetailedInfo();
                }
            }
        }
    }
}
