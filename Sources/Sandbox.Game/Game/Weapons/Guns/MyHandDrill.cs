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
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Diagnostics;
using VRageMath;
using VRage.ObjectBuilders;
using VRage.ModAPI;

#endregion

namespace Sandbox.Game.Weapons
{
    [MyEntityType(typeof(MyObjectBuilder_HandDrill))]
    class MyHandDrill : MyEntity, IMyHandheldGunObject<MyToolBase>, IMyPowerConsumer
    {
        public readonly static MyDrillBase.Sounds m_sounds;

        private const float SPIKE_MAX_ROTATION_SPEED = MathHelper.TwoPi; // radians per second
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

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
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
		public bool IsDeconstructor { get { return false; } }

        public bool EnabledInWorldRules
        {
            get { return true; }
        }

        public MyObjectBuilder_PhysicalGunObject PhysicalObject { get; private set; }

        public bool IsShooting
        {
            get { return m_drillBase.IsDrilling; }
        }

        public int ShootDirectionUpdateTime
        {
            get { return 0; }
        }
        
        static MyHandDrill()
        {
            m_sounds = new MyDrillBase.Sounds()
            {
                IdleLoop = new MySoundPair("ToolPlayDrillIdle"),
                MetalLoop = new MySoundPair("ToolPlayDrillMetal"),
                RockLoop = new MySoundPair("ToolPlayDrillRock")
            };
        }

        MyPhysicalItemDefinition m_physItemDef;

        public MyHandDrill()
        {
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;

            PhysicalObject = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>("HandDrillItem");
            m_physItemDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "HandDrillItem"));
            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            m_drillBase = new MyDrillBase(this,
                MyDrillConstants.DRILL_HAND_DUST_EFFECT,
                MyDrillConstants.DRILL_HAND_DUST_STONES_EFFECT,
                MyDrillConstants.DRILL_HAND_SPARKS_EFFECT,
                new MyDrillSensorRayCast(-0.5f, 1.8f),
                new MyDrillCutOut(0.5f, 0.7f),
                SPIKE_SLOWDOWN_TIME_IN_SECONDS,
                floatingObjectSpawnOffset: -0.25f,
                floatingObjectSpawnRadius: 1.4f * 0.25f,
                sounds: m_sounds 
            );
            AddDebugRenderComponent(new Components.MyDebugRenderCompomentDrawDrillBase(m_drillBase));
            m_handItemDefId = objectBuilder.GetId();
            base.Init(objectBuilder);

            Init(null, "Models\\Weapons\\HandDrill.mwm", null, null, null);
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

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Utility,
                false,
                MyEnergyConstants.REQUIRED_INPUT_HAND_DRILL,
                () => m_tryingToDrill ? PowerReceiver.MaxRequiredInput : 0f);
        }

        public bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeDrilled) < MyDrillConstants.DRILL_UPDATE_INTERVAL_IN_MILISECONDS)
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

        public void Shoot(MyShootActionEnum action, Vector3 direction)
        {
            DoDrillAction(collectOre: action == MyShootActionEnum.PrimaryAction);
        }

        public void EndShoot(MyShootActionEnum action)
        {
            m_drillBase.StopDrill();
            m_tryingToDrill = false;
            PowerReceiver.Update();
        }

        private bool DoDrillAction(bool collectOre)
        {
            m_tryingToDrill = true;
            PowerReceiver.Update();

            if (!PowerReceiver.IsPowered)
                return false;

            m_lastTimeDrilled = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_drillBase.Drill(collectOre);
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
                        if (m_drillBase.IsDrilling)
                        {
                            // sets sound to play while drilling
                            if (entry.Value.Entity is MyCubeGrid)
                                m_drillBase.CurrentLoopCueEnum = m_sounds.MetalLoop;
                            else if (entry.Value.Entity is MyVoxelMap)
                                m_drillBase.CurrentLoopCueEnum = m_sounds.RockLoop;
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
            PowerReceiver.Update();
            m_drillBase.OutputInventory = null;

            if (m_owner.ControllerInfo.IsLocallyControlled())
            {
                m_oreDetectorBase.Clear();
            }

            m_owner = null;
        }

        public void DrawHud(IMyCameraController camera, long playerId)
        {
            MyHud.Crosshair.Position = MyHudCrosshair.ScreenCenter;
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
            return Sandbox.Game.World.MySession.ControlledEntity != null && ((MyEntity)Sandbox.Game.World.MySession.ControlledEntity == Owner);
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
    }
}
