//#define ROTATE_DRILL_SPIKE

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

#endregion

namespace Sandbox.Game.Weapons
{
    [MyEntityType(typeof(MyObjectBuilder_HandDrill))]
    class MyHandDrill : MyEntity, IMyHandheldGunObject<MyToolBase>, IMyGunBaseUser
    {
	    private const float SPIKE_THRUST_DISTANCE_HALF = 0.2f;
        private const float SPIKE_THRUST_PERIOD_IN_SECONDS = 0.05f;
        private const float SPIKE_SLOWDOWN_TIME_IN_SECONDS = 0.5f;

        private int m_lastTimeDrilled;
        private MyDrillBase m_drillBase;
        private MyCharacter m_owner;
        private MyDefinitionId m_handItemDefId;

        private MyEntitySubpart m_spike;
        private Vector3 m_spikeBasePos;
        private Vector3 m_lastSparksPosition;
#if ROTATE_DRILL_SPIKE
        private float m_spikeRotationAngle;
#endif
        private float m_spikeThrustPosition;
        private int m_spikeLastUpdateTime;

        MyOreDetectorComponent m_oreDetectorBase = new MyOreDetectorComponent();


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
        MyDefinitionId m_physicalItemId;

        public MyHandDrill()
        {
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (objectBuilder.SubtypeName != null && objectBuilder.SubtypeName.Length > 0)
            {
                PhysicalObject = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>(objectBuilder.SubtypeName + "Item");
                m_physItemDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), objectBuilder.SubtypeName + "Item"));
            }
            else
            {
                PhysicalObject = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>("HandDrillItem");
                m_physItemDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "HandDrillItem"));
            }

            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;

            m_handItemDefId = objectBuilder.GetId();

            var definition = MyDefinitionManager.Static.TryGetHandItemDefinition(ref m_handItemDefId);
            m_speedMultiplier = 1f/(definition as MyHandDrillDefinition).SpeedMultiplier;
            m_drillBase = new MyDrillBase(this,
                MyDrillConstants.DRILL_HAND_DUST_EFFECT,
                MyDrillConstants.DRILL_HAND_DUST_STONES_EFFECT,
                MyDrillConstants.DRILL_HAND_SPARKS_EFFECT,
                new MyDrillSensorRayCast(-0.5f, 1.8f),
                new MyDrillCutOut(0.5f, 0.35f*(definition as MyHandDrillDefinition).DistanceMultiplier),
                SPIKE_SLOWDOWN_TIME_IN_SECONDS,
                floatingObjectSpawnOffset: -0.25f,
                floatingObjectSpawnRadius: 1.4f * 0.25f
            );
            m_drillBase.VoxelHarvestRatio = MyDrillConstants.VOXEL_HARVEST_RATIO * (definition as MyHandDrillDefinition).HarvestRatioMultiplier;
            AddDebugRenderComponent(new Components.MyDebugRenderCompomentDrawDrillBase(m_drillBase));
            base.Init(objectBuilder);

            var physDefinition = MyDefinitionManager.Static.GetPhysicalItemDefinition(definition.PhysicalItemId);
            Init(null, physDefinition.Model, null, null, null);
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
                () => m_tryingToDrill ? SinkComp.MaxRequiredInput : 0f);
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

        public void Shoot(MyShootActionEnum action, Vector3 direction, string gunAction)
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

			if (!SinkComp.IsPowered)
                return false;

            m_lastTimeDrilled = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_drillBase.Drill(collectOre, assignDamagedMaterial: true);
            m_spikeLastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            return true;
        }
        
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            m_drillBase.UpdateAfterSimulation();

            CreateCollisionSparks();

            if (m_drillBase.IsDrilling || m_drillBase.AnimationMaxSpeedRatio > 0f)
            {
                float timeDelta = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_spikeLastUpdateTime) / 1000f;
#if ROTATE_DRILL_SPIKE
                m_spikeRotationAngle += timeDelta * m_drillBase.AnimationMaxSpeedRatio * SPIKE_MAX_ROTATION_SPEED;
                if (m_spikeRotationAngle > MathHelper.TwoPi) m_spikeRotationAngle -= MathHelper.TwoPi;
#endif

                m_spikeThrustPosition += timeDelta * m_drillBase.AnimationMaxSpeedRatio / SPIKE_THRUST_PERIOD_IN_SECONDS;
                if (m_spikeThrustPosition > 1.0f) m_spikeThrustPosition -= 2.0f;

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
            var origin = m_drillBase.Sensor.Center * 2 - m_drillBase.Sensor.FrontPoint;

            foreach (var entry in m_drillBase.Sensor.EntitiesInRange)
            {
                const float sparksMoveDist = 0.1f;

                var pt = entry.Value.DetectionPoint;
                if (Vector3.DistanceSquared(pt, origin) < distSq)
                {
                    if (Vector3.DistanceSquared(m_lastSparksPosition, pt) > sparksMoveDist * sparksMoveDist)
                    {
                        m_lastSparksPosition = pt;
                        MyParticleEffect effect;
                        if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.CollisionSparksHandDrill, out effect))
                        {
                            effect.WorldMatrix = MatrixD.CreateWorld(pt, PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up);
                            effect.UserScale = 0.3f;
                        }
                        else
                        {
                            // here we sould probably play some collision sound
                        }
                    }
                    break;
                }
            }
        }

        private void WorldPositionChanged(object source)
        {
            m_drillBase.OnWorldPositionChanged(PositionComp.WorldMatrix);
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
            m_drillBase.OutputInventory = null;
            m_drillBase.IgnoredEntities.Add(m_owner);
        }

        public void OnControlReleased()
        {
            m_drillBase.IgnoredEntities.Remove(m_owner);
            m_drillBase.StopDrill();
            m_tryingToDrill = false;
			SinkComp.Update();
            m_drillBase.OutputInventory = null;

            if (m_owner.ControllerInfo.IsLocallyControlled())
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

        public MyDefinitionId DefinitionId
        {
            get { return m_handItemDefId; }
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            m_drillBase.UpdateAfterSimulation100();
            m_oreDetectorBase.Update(PositionComp.GetPosition());
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

        MyEntity IMyGunBaseUser.IgnoreEntity
        {
            get { return this; }
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
