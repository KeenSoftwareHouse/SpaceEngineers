#define ROTATE_DRILL_SPIKE

#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Weapons.Guns;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Diagnostics;
using Sandbox.Game.EntityComponents;
using VRageMath;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Utils;
using Sandbox.Engine.Networking;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.ModAPI.Weapons;

#endregion

namespace Sandbox.Game.Weapons
{
    [MyEntityType(typeof(MyObjectBuilder_HandDrill))]
    public class MyHandDrill : MyEntity, IMyHandheldGunObject<MyToolBase>, IMyGunBaseUser, IMyHandDrill
    {
        private const float SPIKE_THRUST_DISTANCE_HALF = 0.03f;
        private const float SPIKE_THRUST_PERIOD_IN_SECONDS = 0.06f;
        private const float SPIKE_SLOWDOWN_TIME_IN_SECONDS = 0.5f;
        private const float SPIKE_MAX_ROTATION_SPEED = -25f;

        private int m_lastTimeDrilled;
        private MyDrillBase m_drillBase;
        private MyCharacter m_owner;
        private MyDefinitionId m_handItemDefId;

        private MyEntitySubpart m_spike;
        private Vector3 m_spikeBasePos;
#if ROTATE_DRILL_SPIKE
        private float m_spikeRotationAngle;
#endif
        private float m_spikeThrustPosition;
        private int m_spikeLastUpdateTime;

        MyOreDetectorComponent m_oreDetectorBase = new MyOreDetectorComponent();

        private MyEntity[] m_shootIgnoreEntities;   // for projectiles to know which entities to ignore

        float m_speedMultiplier=1f;

		private MyResourceSinkComponent m_sinkComp;
		public MyResourceSinkComponent SinkComp
		{
			get { return m_sinkComp; }
			set { if (Components.Contains(typeof(MyResourceSinkComponent))) Components.Remove<MyResourceSinkComponent>(); Components.Add<MyResourceSinkComponent>(value); m_sinkComp = value; }
		}

        public float BackkickForcePerSecond
        {
            get { return 0.0f; }
        }

        public float ShakeAmount
        {
            get { return 2.5f; }
            protected set { }
        }

        public MyCharacter Owner { get { return m_owner; } }

        public bool EnabledInWorldRules
        {
            get { return true; }
        }

        public MyObjectBuilder_PhysicalGunObject PhysicalObject { get; private set; }

        public bool IsShooting
        {
            get { return m_drillBase.IsDrilling; }
        }

        public bool ForceAnimationInsteadOfIK { get { return false; } }

        public bool IsBlocking
        {
            get { return false; }
        }
        public int ShootDirectionUpdateTime
        {
            get { return 0; }
        }
        
        static MyHandDrill()
        {
            
        }

	    MyPhysicalItemDefinition m_physItemDef;
        static MyDefinitionId m_physicalItemId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "HandDrillItem");

        public MyHandDrill()
        {
            m_shootIgnoreEntities = new MyEntity[] { this };
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            m_physicalItemId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "HandDrillItem");
            if (objectBuilder.SubtypeName != null && objectBuilder.SubtypeName.Length > 0)
                m_physicalItemId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), objectBuilder.SubtypeName + "Item");
            PhysicalObject = (MyObjectBuilder_PhysicalGunObject)MyObjectBuilderSerializer.CreateNewObject(m_physicalItemId);

            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;

            m_handItemDefId = objectBuilder.GetId();

            var definition = MyDefinitionManager.Static.TryGetHandItemDefinition(ref m_handItemDefId);
            m_speedMultiplier = 1f/(definition as MyHandDrillDefinition).SpeedMultiplier;
            m_drillBase = new MyDrillBase(this,
                MyDrillConstants.DRILL_HAND_DUST_EFFECT,
                MyDrillConstants.DRILL_HAND_DUST_STONES_EFFECT,
                MyDrillConstants.DRILL_HAND_SPARKS_EFFECT,
                new MyDrillSensorRayCast(-0.5f, 2.15f),
                new MyDrillCutOut(1.0f, 0.35f*(definition as MyHandDrillDefinition).DistanceMultiplier),
                SPIKE_SLOWDOWN_TIME_IN_SECONDS,
                floatingObjectSpawnOffset: -0.25f,
                floatingObjectSpawnRadius: 1.4f * 0.25f
            );
            m_drillBase.VoxelHarvestRatio = MyDrillConstants.VOXEL_HARVEST_RATIO * (definition as MyHandDrillDefinition).HarvestRatioMultiplier;
            AddDebugRenderComponent(new Components.MyDebugRenderCompomentDrawDrillBase(m_drillBase));
            base.Init(objectBuilder);

            m_physItemDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(m_physicalItemId);
            Init(null, m_physItemDef.Model, null, null, null);
            Render.CastShadows = true;
            Render.NeedsResolveCastShadow = false;

            m_spike = Subparts["Spike"];
            m_spikeBasePos = m_spike.PositionComp.LocalMatrix.Translation;

            m_drillBase.IgnoredEntities.Add(this);
            m_drillBase.OnWorldPositionChanged(PositionComp.WorldMatrix);

            PhysicalObject.GunEntity = (MyObjectBuilder_EntityBase)objectBuilder.Clone();
            PhysicalObject.GunEntity.EntityId = this.EntityId;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            m_oreDetectorBase.DetectionRadius = 20;
            m_oreDetectorBase.OnCheckControl = OnCheckControl;

			var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                MyStringHash.GetOrCompute("Utility"), 
                MyEnergyConstants.REQUIRED_INPUT_HAND_DRILL,
                () => m_tryingToDrill ? SinkComp.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0f);
            SinkComp = sinkComp;

            foreach (ToolSound toolSound in definition.ToolSounds)
            {
                if (toolSound.type == null || toolSound.subtype == null || toolSound.sound == null)
                    continue;
                if (toolSound.type.Equals("Main"))
                {
                    if (toolSound.subtype.Equals("Idle"))
                        m_drillBase.m_idleSoundLoop = new MySoundPair(toolSound.sound);
                    if (toolSound.subtype.Equals("Soundset"))
                        m_drillBase.m_drillMaterial = MyStringHash.GetOrCompute(toolSound.sound);
                }
            }
        }

        public bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeDrilled) < MyDrillConstants.DRILL_UPDATE_INTERVAL_IN_MILISECONDS * m_speedMultiplier)
            {
                status = MyGunStatusEnum.Cooldown;
                return false;
            }

            Debug.Assert(Owner is MyCharacter, "Only character can use hand drill!");
            if (Owner == null)
            {
                status = MyGunStatusEnum.Failed;
                return false;
            }

            status = MyGunStatusEnum.OK;
            return true;
        }

        public void Shoot(MyShootActionEnum action, Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            MyAnalyticsHelper.ReportActivityStartIf(!IsShooting, this.Owner, "Drilling", "Character", "HandTools", "HandDrill", true);

            DoDrillAction(collectOre: action == MyShootActionEnum.PrimaryAction);
        }

        public void EndShoot(MyShootActionEnum action)
        {
            MyAnalyticsHelper.ReportActivityEnd(this.Owner, "Drilling");
            m_drillBase.StopDrill();
            m_tryingToDrill = false;
			SinkComp.Update();
        }

        private bool DoDrillAction(bool collectOre)
        {
            m_tryingToDrill = true;
			SinkComp.Update();

            if (!SinkComp.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
                return false;

            m_lastTimeDrilled = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_drillBase.Drill(collectOre, assignDamagedMaterial: true, speedMultiplier: m_speedMultiplier);
            m_spikeLastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            return true;
        }
        
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            m_drillBase.UpdateAfterSimulation();

            if(IsShooting)
                CreateCollisionSparks();

            if (m_drillBase.IsDrilling || m_drillBase.AnimationMaxSpeedRatio > 0f)
            {
                float timeDelta = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_spikeLastUpdateTime) / 1000f;

                if (m_objectInDrillingRange && Owner != null && Owner.ControllerInfo.IsLocallyControlled() && (Owner.IsInFirstPersonView || Owner.ForceFirstPersonCamera))
                    m_drillBase.PerformCameraShake();

                


#if ROTATE_DRILL_SPIKE
                m_spikeRotationAngle += timeDelta * m_drillBase.AnimationMaxSpeedRatio * SPIKE_MAX_ROTATION_SPEED;
                if (m_spikeRotationAngle > MathHelper.TwoPi) m_spikeRotationAngle -= MathHelper.TwoPi;
                if (m_spikeRotationAngle < MathHelper.TwoPi) m_spikeRotationAngle += MathHelper.TwoPi;
#endif

                m_spikeThrustPosition += timeDelta * m_drillBase.AnimationMaxSpeedRatio / SPIKE_THRUST_PERIOD_IN_SECONDS; 
                if (m_spikeThrustPosition > 1.0f)
                {
                    m_spikeThrustPosition -= 2.0f;
                    if (Owner != null && m_objectInDrillingRange)
                        Owner.WeaponPosition.AddBackkick(0.035f);
                }

                m_spikeLastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

                m_spike.PositionComp.LocalMatrix =
#if ROTATE_DRILL_SPIKE
                    Matrix.CreateRotationZ(m_spikeRotationAngle) *
#endif
                    Matrix.CreateTranslation(m_spikeBasePos + Math.Abs(m_spikeThrustPosition) * Vector3.UnitZ * SPIKE_THRUST_DISTANCE_HALF); 
            }

            //MyTrace.Watch("MyHandDrill.RequiredPowerInput", RequiredPowerInput);
        }

        private void CreateCollisionSparks()
        {
            float distSq = MyDrillConstants.DRILL_HAND_REAL_LENGTH * MyDrillConstants.DRILL_HAND_REAL_LENGTH;
            var origin = m_drillBase.Sensor.Center;

            m_objectInDrillingRange = false;
            bool cubeGrid = false;
            bool voxel = false;
            foreach (var entry in m_drillBase.Sensor.EntitiesInRange)
            {
                var pt = entry.Value.DetectionPoint;
                if (Vector3.DistanceSquared(pt, origin) < distSq)
                {
                    cubeGrid = entry.Value.Entity is MyCubeGrid;
                    voxel = entry.Value.Entity is MyVoxelBase;

                    m_objectInDrillingRange = true;

                    if (cubeGrid)
                    {
                        if (m_drillBase.SparkEffect != null)
                        {
                            if (m_drillBase.SparkEffect.IsEmittingStopped)
                                m_drillBase.SparkEffect.Play();
                            m_drillBase.SparkEffect.WorldMatrix = MatrixD.CreateWorld(pt, PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up);
                        }
                        else
                        {
                            if (MyParticlesManager.TryCreateParticleEffect((int)MyDrillConstants.DRILL_HAND_SPARKS_EFFECT, out m_drillBase.SparkEffect))
                                m_drillBase.SparkEffect.WorldMatrix = MatrixD.CreateWorld(pt, PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up);
                        }
                    }
                    if (voxel)
                    {
                        if (m_drillBase.DustParticles != null)
                        {
                            if (m_drillBase.DustParticles.IsEmittingStopped)
                                m_drillBase.DustParticles.Play();
                            m_drillBase.DustParticles.WorldMatrix = MatrixD.CreateWorld(pt, PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up);
                        }
                        else
                        {
                            if (MyParticlesManager.TryCreateParticleEffect((int)MyDrillConstants.DRILL_HAND_DUST_STONES_EFFECT, out m_drillBase.DustParticles))
                                m_drillBase.DustParticles.WorldMatrix = MatrixD.CreateWorld(pt, PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up);
                        }
                    }
                    break;
                }
            }
            if (m_drillBase.SparkEffect != null && cubeGrid == false)
                m_drillBase.SparkEffect.StopEmitting();

            if (m_drillBase.DustParticles != null && voxel == false)
                m_drillBase.DustParticles.StopEmitting();
        }

        public void WorldPositionChanged(object source)
        {
            // pass logical position to drill base!
            MatrixD logicalPositioning = MatrixD.Identity;
            logicalPositioning.Right = m_owner.WorldMatrix.Right;
            logicalPositioning.Forward = m_owner.ShootDirection;
            logicalPositioning.Up = Vector3D.Normalize(logicalPositioning.Right.Cross(logicalPositioning.Forward));
            logicalPositioning.Translation = m_owner.WeaponPosition.LogicalPositionWorld;
            m_drillBase.OnWorldPositionChanged(logicalPositioning);
        }

        protected override void Closing()
        {
            base.Closing();
            m_drillBase.Close();
            // stop any particle effects and sounds
        }

        private Vector3 ComputeDrillSensorCenter()
        {
            return PositionComp.WorldMatrix.Forward * 1.3f + PositionComp.WorldMatrix.Translation;
        }

        public void OnControlAcquired(MyCharacter owner)
        {
            m_owner = owner;

            if (owner != null)
                m_shootIgnoreEntities = new MyEntity[] { this, owner };

            m_drillBase.OutputInventory = null;
            m_drillBase.IgnoredEntities.Add(m_owner);
        }

        public void OnControlReleased()
        {
            if (m_drillBase != null)
            {
                m_drillBase.IgnoredEntities.Remove(m_owner);
                m_drillBase.StopDrill();

                m_tryingToDrill = false;
                SinkComp.Update();
                m_drillBase.OutputInventory = null;
            }

            if (m_owner != null && m_owner.ControllerInfo != null && m_owner.ControllerInfo.IsLocallyControlled())
            {
                m_oreDetectorBase.Clear();
            }

            m_owner = null;
        }

        public void DrawHud(IMyCameraController camera, long playerId)
        {
            MyHud.Crosshair.Recenter();
        }

        private bool m_tryingToDrill;
        private bool m_objectInDrillingRange;

        public MyDefinitionId DefinitionId
        {
            get { return m_handItemDefId; }
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            m_drillBase.UpdateSoundEmitter();
            m_oreDetectorBase.Update(PositionComp.GetPosition());
        }

        public void UpdateSoundEmitter()
        {
            m_drillBase.UpdateSoundEmitter();
        }

        bool OnCheckControl()
        {
            return Sandbox.Game.World.MySession.Static.ControlledEntity != null && ((MyEntity)Sandbox.Game.World.MySession.Static.ControlledEntity == Owner);
        }

        public Vector3 DirectionToTarget(Vector3D target)
        {
            return PositionComp.WorldMatrix.Forward;
        }

        public void BeginFailReaction(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public void BeginFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public void ShootFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public int GetAmmunitionAmount()
        {
            return 0;
        }

        public MyToolBase GunBase
        {
            get { return null;  }
        }

        public MyPhysicalItemDefinition PhysicalItemDefinition
        {
            get { return m_physItemDef; }
        }

        public int CurrentAmmunition { set; get; }
        public int CurrentMagazineAmmunition { set; get; }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            MyObjectBuilder_EntityBase ob = base.GetObjectBuilder(copy);

            ob.SubtypeName = m_handItemDefId.SubtypeName;
            return ob;
        }

        MyEntity[] IMyGunBaseUser.IgnoreEntities
        {
            get { return m_shootIgnoreEntities; }
        }

        MyEntity IMyGunBaseUser.Weapon
        {
            get { return this; }
        }

        MyEntity IMyGunBaseUser.Owner
        {
            get { return m_owner; }
        }

        IMyMissileGunObject IMyGunBaseUser.Launcher
        {
            get { return null; }
        }

        MyInventory IMyGunBaseUser.AmmoInventory
        {
            get
            {
                if (m_owner != null)
                {
                    return m_owner.GetInventory() as MyInventory;
                }

                return null;
            }
        }

        MyDefinitionId IMyGunBaseUser.PhysicalItemId
        {
            get { return new MyDefinitionId(); }
        }

        MyInventory IMyGunBaseUser.WeaponInventory
        {
            get { return null; }
        }

        long IMyGunBaseUser.OwnerId
        {
            get
            {
                if (m_owner != null)
                    return m_owner.ControllerInfo.ControllingIdentityId;
                return 0;
            }
        }

        string IMyGunBaseUser.ConstraintDisplayName
        {
            get { return null; }
        }
    }
}
