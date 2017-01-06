#region Using
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.UseObject;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Game.Entity.UseObject;
using VRage.Import;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using IMyCameraController = VRage.Game.ModAPI.Interfaces.IMyCameraController;
using IMyDestroyableObject = VRage.Game.ModAPI.Interfaces.IMyDestroyableObject;
using IMyInventoryOwner = VRage.Game.ModAPI.Ingame.IMyInventoryOwner;
using VRage.Game.Models;
using VRage.Game.Gui;
using VRage.Game.Entity;
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Weapons.Guns.Barrels;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.Utils;
using VRage.Profiler;
using VRage.Sync;
using VRageRender.Import;
using IMyControllableEntity = Sandbox.Game.Entities.IMyControllableEntity;
using IMyEntity = VRage.ModAPI.IMyEntity;
using IMyInventory = VRage.Game.ModAPI.Ingame.IMyInventory;
using VRageRender;
using VRage.Input;

#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
using System.Reflection;
using VRage.Reflection;
#endif // XB1


#endregion

namespace Sandbox.Game.Weapons
{
    [Flags]
    public enum MyTurretTargetFlags : ushort
    {
        Players = 1 << 0,
        SmallShips = 1 << 1,
        LargeShips = 1 << 2,
        Stations = 1 << 3,
        Asteroids = 1 << 4,
        Missiles = 1 << 5,
        Moving = 1 << 6,
        NotNeutrals = 1 << 7 //inverted for backwards compatibility
    }

    [MyCubeBlockType(typeof(MyObjectBuilder_TurretBase))]
    public abstract partial class MyLargeTurretBase : MyUserControllableGun, IMyGunObject<MyGunBase>, VRage.Game.ModAPI.Ingame.IMyInventoryOwner, VRage.Game.ModAPI.Interfaces.IMyCameraController, IMyControllableEntity, IMyUsableEntity, IMyGunBaseUser
    {
        private bool m_hidetoolbar;

        //Should be empty added for consistency with checks for other Entities with toolbae (e.g. Character, MyCockpit see MyToolbarComponent)
        private MyToolbar m_toolbar;

        interface IMyPredicionType
        {
            bool ManualTargetPosition { get; }

            Vector3D GetPredictedTargetPosition(IMyEntity entity);
        }

        private MyEntity3DSoundEmitter m_soundEmitterForRotation;

        class MyTargetPredictionType : IMyPredicionType
        {
            public bool ManualTargetPosition { get { return false; } }

            public MyLargeTurretBase Turret { get; set; }

            //http://danikgames.com/blog/?p=809
            public Vector3D GetPredictedTargetPosition(IMyEntity target)
            {
                Debug.Assert(target != null);
                if (target == null)
                    return new Vector3D();
                if (target.MarkedForClose)
                    return target.PositionComp.GetPosition();

                Vector3D predictedPosition = target.PositionComp.GetPosition();
                if (target is MyCharacter)
                {
                    //AB: Terrible terrible hack
                    if ((target as MyCharacter).Definition.Id.SubtypeName.Equals("Space_Wolf"))
                    {
                        predictedPosition = predictedPosition + Vector3.Transform(target.Physics.Center, target.WorldMatrix.GetOrientation()) / 2;
                    }
                    else
                    {
                        predictedPosition = predictedPosition + Vector3.Transform(target.Physics.Center, target.WorldMatrix.GetOrientation());
                    }
                }

                Vector3D dirToTarget = Vector3D.Normalize(predictedPosition - Turret.GunBase.GetMuzzleWorldPosition());

                float shotSpeed = 0;

                if (Turret.GunBase.CurrentAmmoMagazineDefinition != null)
                {
                    var ammoDefinition = MyDefinitionManager.Static.GetAmmoDefinition(Turret.GunBase.CurrentAmmoMagazineDefinition.AmmoDefinitionId);

                    shotSpeed = ammoDefinition.DesiredSpeed;

                    if (ammoDefinition.AmmoType == MyAmmoType.Missile)
                    {
                        //missiles are accelerating, shotSpeed is reached later
                        var mDef = (Sandbox.Definitions.MyMissileAmmoDefinition)ammoDefinition;
                        if (mDef.MissileInitialSpeed == 100f && mDef.MissileAcceleration == 600f && ammoDefinition.DesiredSpeed == 700f)//our missile
                        {//This is very good&fast correction for our missile, but not for some modded exotics with different performance
                            //still does not take parallel component of velocity into account, I know, but its accurate enough
                            shotSpeed = 800f - 238431f / (397.42f + (float)(predictedPosition - Turret.GunBase.GetMuzzleWorldPosition()).Length());
                        }
                        //else {unknown missile, keep shotSpeed without correction}
                    }
                }

                Vector3 targetVelocity = target.Physics != null ? target.Physics.LinearVelocity : target.GetTopMostParent().Physics.LinearVelocity;

                //Include turret velocity into calculations
                targetVelocity -= Turret.Parent.Physics.LinearVelocity;

                // Decompose the target's velocity into the part parallel to the
                // direction to the cannon and the part tangential to it.
                // The part towards the cannon is found by projecting the target's
                // velocity on dirToTarget using a dot product.
                Vector3 targetVelOrth = Vector3.Dot(targetVelocity, dirToTarget) * dirToTarget;

                // The tangential part is then found by subtracting the
                // result from the target velocity.
                Vector3 targetVelTang = targetVelocity - targetVelOrth;


                // The tangential component of the velocities should be the same
                // (or there is no chance to hit)
                // THIS IS THE MAIN INSIGHT!
                Vector3 shotVelTang = targetVelTang;

                // Now all we have to find is the orthogonal velocity of the shot

                float shotVelSpeed = shotVelTang.Length();
                if (shotVelSpeed > shotSpeed)
                {
                    // Shot is too slow to intercept target, it will never catch up.
                    // Do our best by aiming in the direction of the targets velocity.
                    //return Vector3.Normalize(target.Physics.LinearVelocity) * shotSpeed;
                    return predictedPosition;
                }
                else
                {
                    // We know the shot speed, and the tangential velocity.
                    // Using pythagoras we can find the orthogonal velocity.
                    float shotSpeedOrth = (float)Math.Sqrt(shotSpeed * shotSpeed - shotVelSpeed * shotVelSpeed);
                    Vector3 shotVelOrth = dirToTarget * shotSpeedOrth;

                    // Finally, add the tangential and orthogonal velocities.
                    //return shotVelOrth + shotVelTang;

                    // Find the time of collision (distance / relative velocity)
                    float timeDiff = shotVelOrth.Length() - targetVelOrth.Length();
                    var timeToCollision = timeDiff != 0 ? ((Turret.PositionComp.GetPosition() - target.WorldMatrix.Translation).Length()) / timeDiff : 0;

                    // Calculate where the shot will be at the time of collision
                    Vector3 shotVel = shotVelOrth + shotVelTang;
                    predictedPosition = timeToCollision > 0.01f ? Turret.GunBase.GetMuzzleWorldPosition() + (Vector3D)shotVel * timeToCollision : predictedPosition;

                    return predictedPosition;
                }
            }

            public MyTargetPredictionType(MyLargeTurretBase turret)
            {
                Turret = turret;
            }
        }

        class MyTargetNoPredictionType : IMyPredicionType
        {
            public bool ManualTargetPosition { get { return false; } }

            public Vector3D GetPredictedTargetPosition(IMyEntity target)
            {
                return target.PositionComp.WorldAABB.Center;
            }
        }

        class MyPositionNoPredictionType : IMyPredicionType
        {
            public bool ManualTargetPosition { get { return true; } }

            public Vector3D TrackedPosition { get; set; }

            public Vector3D GetPredictedTargetPosition(IMyEntity target)
            {
                return TrackedPosition;
            }
        }

        class MyPositionPredictionType : IMyPredicionType
        {
            public bool ManualTargetPosition { get { return true; } }

            public MyLargeTurretBase Turret { get; set; }

            public Vector3D TrackedPosition { get; set; }

            public Vector3D TrackedVelocity { get; set; }

            public Vector3D GetPredictedTargetPosition(IMyEntity target)
            {
                Vector3D predictedPosition = TrackedPosition;

                Vector3D dirToTarget = Vector3D.Normalize(predictedPosition - Turret.GunBase.GetMuzzleWorldPosition());

                float shotSpeed = 0;

                if (Turret.GunBase.CurrentAmmoMagazineDefinition != null)
                {
                    var ammoDefinition = MyDefinitionManager.Static.GetAmmoDefinition(Turret.GunBase.CurrentAmmoMagazineDefinition.AmmoDefinitionId);

                    shotSpeed = ammoDefinition.DesiredSpeed;

                    if (ammoDefinition.AmmoType == MyAmmoType.Missile)
                    {
                        //missiles are accelerating, shotSpeed is reached later
                        var mDef = (Sandbox.Definitions.MyMissileAmmoDefinition)ammoDefinition;
                        if (mDef.MissileInitialSpeed == 100f && mDef.MissileAcceleration == 600f && ammoDefinition.DesiredSpeed == 700f)//our missile
                        {//This is very good&fast correction for our missile, but not for some modded exotics with different performance
                            //still does not take parallel component of velocity into account, I know, but its accurate enough
                            shotSpeed = 800f - 238431f / (397.42f + (float)(predictedPosition - Turret.GunBase.GetMuzzleWorldPosition()).Length());
                        }
                        //else {unknown missile, keep shotSpeed without correction}
                    }
                }

                Vector3 targetVelocity = TrackedVelocity;

                //Include turret velocity into calculations
                targetVelocity -= Turret.Parent.Physics.LinearVelocity;

                // Decompose the target's velocity into the part parallel to the
                // direction to the cannon and the part tangential to it.
                // The part towards the cannon is found by projecting the target's
                // velocity on dirToTarget using a dot product.
                Vector3 targetVelOrth = Vector3.Dot(targetVelocity, dirToTarget) * dirToTarget;

                // The tangential part is then found by subtracting the
                // result from the target velocity.
                Vector3 targetVelTang = targetVelocity - targetVelOrth;


                // The tangential component of the velocities should be the same
                // (or there is no chance to hit)
                // THIS IS THE MAIN INSIGHT!
                Vector3 shotVelTang = targetVelTang;

                // Now all we have to find is the orthogonal velocity of the shot

                float shotVelSpeed = shotVelTang.Length();
                if (shotVelSpeed > shotSpeed)
                {
                    // Shot is too slow to intercept target, it will never catch up.
                    // Do our best by aiming in the direction of the targets velocity.
                    //return Vector3.Normalize(target.Physics.LinearVelocity) * shotSpeed;
                    return predictedPosition;
                }
                else
                {
                    // We know the shot speed, and the tangential velocity.
                    // Using pythagoras we can find the orthogonal velocity.
                    float shotSpeedOrth = (float)Math.Sqrt(shotSpeed * shotSpeed - shotVelSpeed * shotVelSpeed);
                    Vector3 shotVelOrth = dirToTarget * shotSpeedOrth;

                    // Finally, add the tangential and orthogonal velocities.
                    //return shotVelOrth + shotVelTang;

                    // Find the time of collision (distance / relative velocity)
                    float timeDiff = shotVelOrth.Length() - targetVelOrth.Length();
                    var timeToCollision = timeDiff != 0 ? ((Turret.PositionComp.GetPosition() - target.WorldMatrix.Translation).Length()) / timeDiff : 0;

                    // Calculate where the shot will be at the time of collision
                    Vector3 shotVel = shotVelOrth + shotVelTang;
                    predictedPosition = timeToCollision > 0.01f ? Turret.GunBase.GetMuzzleWorldPosition() + (Vector3D)shotVel * timeToCollision : predictedPosition;

                    return predictedPosition;
                }
            }

            public MyPositionPredictionType(MyLargeTurretBase turret)
            {
                Turret = turret;
            }
        }

        #region Fields

        public enum MyLargeShipGunStatus
        {
            MyWeaponStatus_Deactivated,
            MyWeaponStatus_Searching,
            MyWeaponStatus_Shooting,
            MyWeaponStatus_ShootDelaying,
        }

        public const float MAX_DISTANCE_FOR_RANDOM_ROTATING_LARGESHIP_GUNS = 600;
        const float DEFAULT_MIN_RANGE = 4.0f;
        const float DEFAULT_MAX_RANGE = 800.0f;

        private const float MIN_FOV = 0.00001f;
        private const float MAX_FOV = 3.12413936f;
        private static float m_minFov, m_maxFov; //from definition

        protected MyLargeBarrelBase m_barrel;
        protected MyEntity m_base1;
        protected MyEntity m_base2;

        private MyLargeShipGunStatus m_status = MyLargeShipGunStatus.MyWeaponStatus_Deactivated;
        private float m_rotation;
        private float m_elevation;
        private float m_rotationLast;
        private float m_elevationLast;
        protected float m_rotationSpeed;
        protected float m_elevationSpeed;
        protected int m_rotationInterval_ms;
        protected int m_elevationInterval_ms;
        protected int m_randomStandbyChange_ms;
        protected int m_randomStandbyChangeConst_ms;
        private float m_randomStandbyRotation;
        private float m_randomStandbyElevation;
        private bool m_randomIsMoving;
        private double m_laserLength = 100f;
        private int m_shootDelayIntervalConst_ms;
        private int m_shootIntervalConst_ms;
        private int m_shootStatusChanged_ms;
        private int m_shootDelayInterval_ms;
        private int m_shootInterval_ms;
        private int m_shootIntervalVarianceConst_ms;
        private bool m_isPotentialTarget;
        private float m_requiredPowerInput;
        private bool m_resetInterpolationFlag = true;
        private bool m_isPlayerShooting = false;
        private IMyControllableEntity m_previousControlledEntity;
        private long? m_savedPreviousControlledEntityId;
        private MyCharacter m_cockpitPilot;
        private MyHudNotification m_outOfAmmoNotification;

        private float m_fov;
        private float m_targetFov;

        public MatrixD InitializationMatrix { get; private set; }
        public MatrixD InitializationBarrelMatrix { get; set; }
        MyEntity m_target;

        readonly Sync<float> m_shootingRange;
        float m_searchingRange = 800;
        bool m_checkOtherTargets = true;
        readonly Sync<bool> m_enableIdleRotation;
        bool m_previousIdleRotationState = true;
        // When large ship is controlled, owner is set to player ship which controls this large ship
        public MyEntity WeaponOwner { get; set; }

        static int m_intervalShift = 0; //use this to avoid updating all weapons in one frame

        private MyDefinitionId m_defId;

        protected MySoundPair m_shootingCueEnum = new MySoundPair();
        protected MySoundPair m_rotatingCueEnum = new MySoundPair();

        static HashSet<MyEntity> m_targets = new HashSet<MyEntity>();

        protected Vector3D m_hitPosition;

        protected MyGunBase m_gunBase;

        long m_targetToSet = 0;
        IMyPredicionType m_currentPrediction = null;
        IMyPredicionType m_targetNoPrediction = new MyTargetNoPredictionType();
        IMyPredicionType m_positionNoPrediction = new MyPositionNoPredictionType();

        IMyPredicionType m_targetPrediction = null;
        IMyPredicionType m_positionPrediction = null;

        float m_minElevationRadians = 0;
        float m_maxElevationRadians = (float)(2.0 * Math.PI);
        float m_minAzimuthRadians = 0;
        float m_maxAzimuthRadians = (float)(2.0 * Math.PI);


        float m_minRangeMeter = DEFAULT_MIN_RANGE;
        float m_maxRangeMeter = DEFAULT_MAX_RANGE;
        protected bool m_isControlled = false;

        private static HashSet<long> m_ignoredEntities = new HashSet<long>();

        private MyEntity[] m_shootIgnoreEntities;  // entities ignored by the projectile

        struct SyncRotationAndElevation
        {
            public float Rotation;
            public float Elevation;
        }

#if !XB1 // XB1_SYNC_SERIALIZER_NOEMIT
        struct CurrentTargetSync
#else // XB1
        struct CurrentTargetSync : IMySetGetMemberDataHelper
#endif // XB1
        {
            public long TargetId;
            public bool IsPotential;

#if XB1 // XB1_SYNC_SERIALIZER_NOEMIT
            public object GetMemberData(MemberInfo m)
            {
                if (m.Name == "TargetId")
                    return TargetId;
                if (m.Name == "IsPotential")
                    return IsPotential;

                System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
                return null;
            }
#endif // XB1
        }

        readonly Sync<SyncRotationAndElevation> m_rotationAndElevationSync;
        readonly Sync<CurrentTargetSync> m_targetSync;
        #endregion

        #region Properties

        //       protected abstract MyAmmoCategoryEnum AmmoType { get; }

        public MyLargeShipGunStatus GetStatus()
        {
            return m_status;
        }

        public IMyControllableEntity PreviousControlledEntity
        {
            get
            {
                if (m_savedPreviousControlledEntityId != null)
                {
                    if (TryFindSavedEntity())
                    {
                        m_savedPreviousControlledEntityId = null;
                        SetCameraOverlay();
                    }
                }
                return m_previousControlledEntity;
            }
            private set
            {
                if (value != m_previousControlledEntity)
                {
                    if (m_previousControlledEntity != null)
                    {
                        m_previousControlledEntity.Entity.OnMarkForClose -= Entity_OnPreviousMarkForClose;
                        if (m_cockpitPilot != null)
                        {
                            m_cockpitPilot.OnMarkForClose -= Entity_OnPreviousMarkForClose;
                        }
                    }
                    m_previousControlledEntity = value;
                    if (m_previousControlledEntity != null)
                    {
                        AddPreviousControllerEvents();

                        if (PreviousControlledEntity is MyCockpit)
                        {
                            m_cockpitPilot = (PreviousControlledEntity as MyCockpit).Pilot;
                            if (m_cockpitPilot != null)
                            {
                                m_cockpitPilot.OnMarkForClose += Entity_OnPreviousMarkForClose;
                            }
                        }
                    }
                }
            }
        }

        public MyModelDummy CameraDummy { get; private set; }

        public new MyLargeTurretBaseDefinition BlockDefinition
        {
            get { return base.BlockDefinition as MyLargeTurretBaseDefinition; }
        }

        //For backwards compatibility
        private bool AiEnabled
        {
            get
            {
                if (BlockDefinition != null)
                {
                    return BlockDefinition.AiEnabled;
                }
                else
                {
                    return true;
                }
            }
        }

        public bool IsControlled
        {
            get
            {
                return PreviousControlledEntity != null || m_isControlled;
            }
        }

        public bool IsPlayerControlled
        {
            get
            {
                if (IsControlled)
                {
                    return true;
                }

                if (Sync.Players.GetControllingPlayer(this) != null)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsControlledByLocalPlayer
        {
            get
            {
                if (IsControlled)
                {
                    if (ControllerInfo.Controller != null)
                    {
                        return ControllerInfo.IsLocallyControlled();
                    }
                }
                return false;
            }
        }
        public MyCharacter Pilot
        {
            get
            {
                var character = PreviousControlledEntity as MyCharacter;
                if (character != null)
                {
                    return character;
                }
                return m_cockpitPilot;
            }
        }



        protected abstract float ForwardCameraOffset { get; }
        protected abstract float UpCameraOffset { get; }
        public MyLargeBarrelBase Barrel { get { return m_barrel; } }

        public MyGunBase GunBase
        {
            get { return m_gunBase; }
        }

        bool EnableIdleRotation
        {
            get
            {
                return m_enableIdleRotation;
            }
            set
            {
                m_enableIdleRotation.Value = value;
            }
        }

        readonly Sync<MyTurretTargetFlags> m_targetFlags;
        public MyTurretTargetFlags TargetFlags
        {
            get
            {
                return m_targetFlags;
            }
            set
            {
                m_targetFlags.Value = value;
            }
        }

        public static HashSet<long> IgnoredEntities
        {
            get { return m_ignoredEntities; }
            set { m_ignoredEntities = value; }
        }

        #endregion

        #region Init

        public MyLargeTurretBase()
            : base()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_shootingRange = SyncType.CreateAndAddProp<float>();
            m_enableIdleRotation = SyncType.CreateAndAddProp<bool>();
            m_rotationAndElevationSync = SyncType.CreateAndAddProp<SyncRotationAndElevation>();
            m_targetSync = SyncType.CreateAndAddProp<CurrentTargetSync>();
            m_targetFlags = SyncType.CreateAndAddProp<MyTurretTargetFlags>();
#endif // XB1

            m_shootIgnoreEntities = new MyEntity[] { this };

            CreateTerminalControls();

            m_status = MyLargeShipGunStatus.MyWeaponStatus_Deactivated;
            m_randomStandbyChange_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_randomStandbyChangeConst_ms = MyUtils.GetRandomInt(3500, 4500);
            m_randomStandbyRotation = 0.0f;
            m_randomStandbyElevation = 0.0f;
            m_rotation = 0.0f;
            m_elevation = 0.0f;
            m_rotationSpeed = MyLargeTurretsConstants.ROTATION_SPEED;
            m_elevationSpeed = MyLargeTurretsConstants.ELEVATION_SPEED;
            m_rotationInterval_ms = 0;
            m_elevationInterval_ms = 0;
            m_shootDelayIntervalConst_ms = 200;
            m_shootIntervalConst_ms = 1200;
            m_shootIntervalVarianceConst_ms = 500;
            m_shootStatusChanged_ms = 0;
            m_isPotentialTarget = false;
            m_targetPrediction = new MyTargetPredictionType(this);
            m_currentPrediction = m_targetPrediction;
            m_positionPrediction = new MyPositionPredictionType(this);

            m_soundEmitter = new MyEntity3DSoundEmitter(this, true);
            m_soundEmitterForRotation = new MyEntity3DSoundEmitter(this, true);

            ControllerInfo.ControlReleased += OnControlReleased;

#if XB1 // XB1_SYNC_NOREFLECTION
            m_gunBase = new MyGunBase(SyncType);
#else // !XB1
            m_gunBase = new MyGunBase();
#endif // !XB1
            m_outOfAmmoNotification = new MyHudNotification(MyCommonTexts.OutOfAmmo, 1000, level: MyNotificationLevel.Important);

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

#if !XB1 // XB1_SYNC_NOREFLECTION
            SyncType.Append(m_gunBase);
#endif // !XB1

            m_shootingRange.ValueChanged += (x) => ShootingRangeChanged();
            m_rotationAndElevationSync.ValueChanged += (x) => RotationAndElevationSync();
            m_targetSync.ValidateNever();
            m_targetSync.ValueChanged +=  (x) => TargetChanged();

            m_toolbar = new MyToolbar(ToolbarType);
        }

        void TargetChanged()
        {
            MyEntity target = null;
            if (m_targetSync.Value.TargetId != 0)
            {
                MyEntities.TryGetEntityById(m_targetSync.Value.TargetId, out target);
            }

            SetTarget(target, m_targetSync.Value.IsPotential);
        }

        void RotationAndElevationSync()
        {
            UpdateRotationAndElevation(m_rotationAndElevationSync.Value.Rotation, m_rotationAndElevationSync.Value.Elevation);
        }

        void ShootingRangeChanged()
        {
            if (IsWorking && AiEnabled)
            {
                CheckNearTargets();
            }
        }
        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;
            var builder = (MyObjectBuilder_TurretBase)objectBuilder;
            var weaponBlockDefinition = base.BlockDefinition as MyWeaponBlockDefinition;

            if (MyFakes.ENABLE_INVENTORY_FIX)
            {
                FixSingleInventory();
            }

            if (this.GetInventory() == null)
            {
                MyInventory inventory = null;
                if (weaponBlockDefinition != null)
                    inventory = new MyInventory(weaponBlockDefinition.InventoryMaxVolume, new Vector3(0.4f, 0.4f, 0.4f), MyInventoryFlags.CanReceive);
                else
                    inventory = new MyInventory(6 * 64.0f / 1000, new Vector3(0.4f, 0.4f, 0.4f), MyInventoryFlags.CanReceive);

                Components.Add<MyInventoryBase>(inventory);

                inventory.Init(builder.Inventory);
            }
            Debug.Assert(this.GetInventory().Owner == this, "Ownership was not set!");


            m_gunBase.Init(builder.GunBase, base.BlockDefinition, this);

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                BlockDefinition.ResourceSinkGroup,
                MyEnergyConstants.MAX_REQUIRED_POWER_TURRET,
                () => (Enabled && IsFunctional) ? ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0.0f);
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);

            m_rotationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_elevationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            InitializationMatrix = (MatrixD)PositionComp.LocalMatrix;
            InitializationBarrelMatrix = MatrixD.Identity;

            m_defId = builder.GetId();

            m_shootingRange.Value = builder.Range;
            m_searchingRange = m_shootingRange + 100;

            TargetMeteors = builder.TargetMeteors;
            TargetMoving = builder.TargetMoving;
            TargetMissiles = builder.TargetMissiles;
            TargetCharacters = builder.TargetCharacters;
            TargetSmallGrids = builder.TargetSmallGrids;
            TargetLargeGrids = builder.TargetLargeGrids;
            TargetStations = builder.TargetStations;
            TargetNeutrals = builder.TargetNeutrals;
        
            ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
            ResourceSink.Update();

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            m_targetToSet = builder.Target;
            m_isPotentialTarget = builder.IsPotentialTarget;

            m_savedPreviousControlledEntityId = builder.PreviousControlledEntityId;

            m_rotation = builder.Rotation;
            m_elevation = builder.Elevation;

            m_isPlayerShooting = builder.IsShooting;

            if (BlockDefinition != null)
            {
                m_maxRangeMeter = BlockDefinition.MaxRangeMeters;
                m_minElevationRadians = MathHelper.ToRadians(NormalizeAngle(BlockDefinition.MinElevationDegrees));
                m_maxElevationRadians = MathHelper.ToRadians(NormalizeAngle(BlockDefinition.MaxElevationDegrees));

                if (m_minElevationRadians > m_maxElevationRadians)
                {
                    m_minElevationRadians -= MathHelper.TwoPi;
                }

                m_minAzimuthRadians = MathHelper.ToRadians(NormalizeAngle(BlockDefinition.MinAzimuthDegrees));
                m_maxAzimuthRadians = MathHelper.ToRadians(NormalizeAngle(BlockDefinition.MaxAzimuthDegrees));

                if (m_minAzimuthRadians > m_maxAzimuthRadians)
                {
                    m_minAzimuthRadians -= MathHelper.TwoPi;
                }

                m_rotationSpeed = BlockDefinition.RotationSpeed;
                m_elevationSpeed = BlockDefinition.ElevationSpeed;

                m_enableIdleRotation.Value = BlockDefinition.IdleRotation;
                ClampRotationAndElevation();
            }
            //this must be & with block definition 
            m_enableIdleRotation.Value &= builder.EnableIdleRotation;

            m_previousIdleRotationState = builder.PreviousIdleRotationState;

            m_minFov = builder.MinFov;
            m_maxFov = builder.MaxFov;
            m_fov = builder.MaxFov;
            m_targetFov = builder.MaxFov;
            
        }

        float NormalizeAngle(int angle)
        {
            int retVal = angle % 360;
            if (retVal == 0 && angle != 0)
            {
                return 360;
            }
            return retVal;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_TurretBase)base.GetObjectBuilderCubeBlock(copy);
            var userGunBuilder = builder as MyObjectBuilder_UserControllableGun;

            if (userGunBuilder != null)
            {
                userGunBuilder.IsLargeTurret = true;
            }

            builder.Inventory = this.GetInventory().GetObjectBuilder();
            builder.Range = m_shootingRange;
            builder.RemainingAmmo = m_gunBase.CurrentAmmo;
            builder.Target = Target != null ? Target.EntityId : 0;
            builder.IsPotentialTarget = m_isPotentialTarget;

            builder.TargetMeteors = TargetMeteors;
            builder.TargetMoving = TargetMoving;
            builder.TargetMissiles = TargetMissiles;
            builder.EnableIdleRotation = EnableIdleRotation;
            builder.TargetCharacters = TargetCharacters;
            builder.TargetSmallGrids = TargetSmallGrids;
            builder.TargetLargeGrids = TargetLargeGrids;
            builder.TargetStations = TargetStations;
            builder.TargetNeutrals = TargetNeutrals;

            if (PreviousControlledEntity != null)
            {
                builder.PreviousControlledEntityId = PreviousControlledEntity.Entity.EntityId;
                builder.Rotation = m_rotation;
                builder.Elevation = m_elevation;
                builder.IsShooting = m_isPlayerShooting;
            }
            builder.GunBase = m_gunBase.GetObjectBuilder();
            builder.PreviousIdleRotationState = m_previousIdleRotationState;

            return builder;
        }

        MatrixD InitializationMatrixWorld
        {
            get
            {
                return InitializationMatrix * Parent.WorldMatrix;
            }
        }


        protected override void Closing()
        {
            base.Closing();

            ReleaseControl();

            Target = null;

            if (m_barrel != null)
            {
                m_barrel.Close();
                m_barrel = null;
            }
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true);
            m_soundEmitterForRotation.StopSound(true);
        }

        #endregion

        #region Power and working

        protected override bool CheckIsWorking()
        {
            return ResourceSink != null && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }


        protected override void OnStopWorking()
        {
            base.OnStopWorking();

            StopShootingSound();
            StopAimingSound();

            if (m_barrel != null)
            {
                m_barrel.RemoveSmoke();
            }

            if (IsControlled)
            {
                ReleaseControl();
            }

            Target = null;
        }

        protected override void OnEnabledChanged()
        {
            ResourceSink.Update();
            base.OnEnabledChanged();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            ResourceSink.Update();
        }

        void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();

            System.Diagnostics.Debug.Assert(!Closed);

            if (IsWorking)
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);

                m_rotationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                m_elevationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                m_randomStandbyChange_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            }
            else
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White);
                OnStopWorking();
            }
        }

        #endregion

        #region Update

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (m_targetToSet != 0 && IsWorking)
            {
                MyEntity target = null;
                if (MyEntities.TryGetEntityById(m_targetToSet, out target))
                    Target = target;
            }

            if (m_savedPreviousControlledEntityId != null)
            {
                MySession.Static.Players.UpdatePlayerControllers(EntityId);
                if (m_savedPreviousControlledEntityId != null)
                {
                    TryFindSavedEntity();
                    m_savedPreviousControlledEntityId = null;
                }
            }
            RotateModels();
        }

        private bool TryFindSavedEntity()
        {
            //Check if player was dead when controlling the turret and saved (playerId was 0 when loading!)
            if (ControllerInfo.Controller != null)
            {
                MyEntity oldControllerEntity;
                if (MyEntities.TryGetEntityById(m_savedPreviousControlledEntityId.Value, out oldControllerEntity))
                {
                    PreviousControlledEntity = (IMyControllableEntity)oldControllerEntity;
                    if (m_previousControlledEntity is MyCockpit)
                    {
                        m_cockpitPilot = (m_previousControlledEntity as MyCockpit).Pilot;
                    }
                    RotateModels();
                    return true;
                }
            }
            return false;
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (!IsControlledByLocalPlayer) return;
            if (MyInput.Static.DeltaMouseScrollWheelValue() != 0 && MyGuiScreenCubeBuilder.Static == null && !MyGuiScreenTerminal.IsOpen)
            {
                ChangeZoom(MyInput.Static.DeltaMouseScrollWheelValue());
            }

        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            bool active = Render.IsVisible();

            if (!MyFakes.ENABLE_GATLING_TURRETS || !Sandbox.Game.World.MySession.Static.WeaponsEnabled)
                active = false;

            if (active && IsWorking)
            {
                if (IsControlled)
                {
                    if (!IsInRangeAndPlayerHasAccess())
                    {
                        ReleaseControl();
                        if (MyGuiScreenTerminal.IsOpen && MyGuiScreenTerminal.InteractedEntity == this)
                        {
                            MyGuiScreenTerminal.Hide();
                        }
                    }
                    else
                    {
                        var receiver = GetFirstRadioReceiver();
                        if (receiver != null)
                        {
                            receiver.UpdateHud(true);
                        }
                    }
                }
                else
                {
                    if (Sync.IsServer && AiEnabled)
                    {
                        CheckNearTargets();
                    }

                    if ((GetStatus() == MyLargeShipGunStatus.MyWeaponStatus_Deactivated && m_randomIsMoving) ||
                        (Target != null && m_isPotentialTarget))
                    {
                        SetupSearchRaycast();
                    }
                }
            }
        }

        //Shooting must be handled here because only here is the correct start position for projectile (because of physics)
        public override void UpdateAfterSimulation()
        {
            System.Diagnostics.Debug.Assert(!Closed);

            if (m_resetInterpolationFlag == true)
            {
                if (m_base1 != null)
                    VRageRender.MyRenderProxy.UpdateRenderObjectVisibility((uint)m_base1.Render.GetRenderObjectID(), true, false);
                if (m_base2 != null)
                    VRageRender.MyRenderProxy.UpdateRenderObjectVisibility((uint)m_base2.Render.GetRenderObjectID(), true, false);
                if (m_barrel != null)
                    VRageRender.MyRenderProxy.UpdateRenderObjectVisibility((uint)m_barrel.Entity.Render.GetRenderObjectID(), true, false);

                m_resetInterpolationFlag = false;
            }

            base.UpdateAfterSimulation();

            if (!IsWorking || Parent.Physics == null || !Parent.Physics.Enabled)
            {
                RotateModels();

                if (m_barrel != null)
                    m_barrel.UpdateAfterSimulation();
                return;
            }

            if (IsControlledByLocalPlayer)
            {
                m_fov = VRageMath.MathHelper.Lerp(m_fov, m_targetFov, 0.5f);
                SetFov(m_fov);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyLargeShipGunBase::UpdateAfterSimulation");

            bool active = Render.IsVisible();

            if (active && IsWorking)
            {
                base.UpdateAfterSimulation();

                if (!IsPlayerControlled && AiEnabled)
                {
                    UpdateAiWeapon();
                }
                else if (m_isPlayerShooting)
                {
                    MyGunStatusEnum turretStatus;
                    if (CanShoot(out turretStatus))
                    {
                        UpdateShooting(m_isPlayerShooting);
                    }
                    else
                    {
                        if (turretStatus == MyGunStatusEnum.OutOfAmmo)
                        {
                            if (m_gunBase.SwitchAmmoMagazineToFirstAvailable())
                                turretStatus = MyGunStatusEnum.OK;
                        }
                    }

                    if (IsControlledByLocalPlayer && turretStatus == MyGunStatusEnum.OutOfAmmo)
                    {
                        m_outOfAmmoNotification.SetTextFormatArguments(DisplayNameText);
                        MyHud.Notifications.Add(m_outOfAmmoNotification);
                    }
                }
            }

            if (m_barrel != null)
                m_barrel.UpdateAfterSimulation();

            if (!active || (!m_isShooting && !m_isPlayerShooting && !(!IsPlayerControlled && AiEnabled)))
            {
                StopShootingSound();
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        private void UpdateAiWeapon()
        {
            var targetDistance = GetTargetDistance();

            MyGunStatusEnum gunStatus = MyGunStatusEnum.Cooldown;

            if (targetDistance < m_searchingRange || m_currentPrediction.ManualTargetPosition)
            {
                //by Gregory RotationAndElevation uses target! maybe shouldn't?
                bool isAimed = (Target != null || m_currentPrediction.ManualTargetPosition) && RotationAndElevation() && CanShoot(out gunStatus) && IsTargetVisible(Target);
                UpdateShooting(isAimed && !m_isPotentialTarget);
            }
            else
            {
                if(!m_isShooting)
                    Deactivate();

                if (MySector.MainCamera.GetDistanceFromPoint(PositionComp.GetPosition()) <= MAX_DISTANCE_FOR_RANDOM_ROTATING_LARGESHIP_GUNS)
                {
                    RandomMovement();
                }
                else
                    StopAimingSound();
            }
        }

        private void UpdateShooting(bool shouldShoot)
        {
            if (shouldShoot)
            {
                UpdateShootStatus();

                if (m_status == MyLargeShipGunStatus.MyWeaponStatus_Shooting)
                {
                    m_canStopShooting = (m_barrel.StartShooting() && m_soundEmitter != null && m_soundEmitter.Loop);
                }
                else if (m_status != MyLargeShipGunStatus.MyWeaponStatus_ShootDelaying)
                {
                    if (m_canStopShooting || (m_soundEmitter != null && m_soundEmitter.Sound != null && m_soundEmitter.Sound.IsPlaying && m_soundEmitter.Loop))
                    {
                        m_barrel.StopShooting();
                        m_canStopShooting = false;
                    }
                }
            }
            else
            {
                m_status = MyLargeShipGunStatus.MyWeaponStatus_Searching;
                if (m_canStopShooting || (m_soundEmitter != null && m_soundEmitter.Sound != null && m_soundEmitter.Sound.IsPlaying && m_soundEmitter.Loop))
                {
                    m_barrel.StopShooting();
                    m_canStopShooting = false;
                }
            }
        }

        private void Deactivate()
        {
            CreateTerminalControls();

            m_status = MyLargeShipGunStatus.MyWeaponStatus_Deactivated;
            if (m_soundEmitter == null)
                return;
            if (m_canStopShooting || ((m_soundEmitter.Sound != null) && m_soundEmitter.Sound.IsPlaying && m_soundEmitter.Loop))
            {
                m_barrel.StopShooting();
                m_canStopShooting = false;
            }
        }

        private void UpdateControlledWeapon()
        {
            if (HasElevationOrRotationChanged())
            {
                m_stopShootingTime = 0;
            }
            else
            {
                if (m_stopShootingTime <= 0)
                {
                    m_stopShootingTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                }
                else
                {
                    if (m_stopShootingTime + MyLargeTurretsConstants.AIMING_SOUND_DELAY < MySandboxGame.TotalGamePlayTimeInMilliseconds)
                    {
                        StopAimingSound();
                    }
                }
            }

            m_rotationLast = m_rotation;
            m_elevationLast = m_elevation;

            RotateModels();

            if (m_status == MyLargeShipGunStatus.MyWeaponStatus_Shooting)
            {
                m_barrel.StopShooting();
                m_status = MyLargeShipGunStatus.MyWeaponStatus_Searching;
            }
        }

        private bool m_canStopShooting = false;
        private float m_stopShootingTime = 0;

        private void SetShootInterval(ref int shootInterval, ref int shootIntervalConst)
        {
            shootInterval = shootIntervalConst;// +MyVRageUtils.GetRandomInt(-m_shootIntervalVarianceConst_ms, m_shootIntervalVarianceConst_ms);
        }

        private void UpdateShootStatus()
        {
            switch (m_status)
            {
                case MyLargeShipGunStatus.MyWeaponStatus_Shooting:
                    {
                        if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_shootStatusChanged_ms) > m_shootInterval_ms)
                        {
                            StartShootDelaying();
                        }
                    }
                    break;

                case MyLargeShipGunStatus.MyWeaponStatus_ShootDelaying:
                    {
                        if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_shootStatusChanged_ms) > m_shootDelayInterval_ms)
                        {
                            StartShooting();
                        }
                    }
                    break;

                case MyLargeShipGunStatus.MyWeaponStatus_Searching:
                case MyLargeShipGunStatus.MyWeaponStatus_Deactivated:
                    {
                        StartShootDelaying();
                    }
                    break;
            }
        }

        private void StartShooting()
        {
            m_status = MyLargeShipGunStatus.MyWeaponStatus_Shooting;
            m_shootStatusChanged_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            SetShootInterval(ref m_shootInterval_ms, ref m_shootIntervalConst_ms);
        }

        private void StartShootDelaying()
        {
            m_status = MyLargeShipGunStatus.MyWeaponStatus_ShootDelaying;
            m_shootStatusChanged_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_shootDelayIntervalConst_ms = 0;
            SetShootInterval(ref m_shootDelayInterval_ms, ref m_shootDelayIntervalConst_ms);
        }

        private void ResetRandomAiming()
        {
            m_rotationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_rotationInterval_ms;
            m_elevationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_elevationInterval_ms;

            if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_randomStandbyChange_ms) > m_randomStandbyChangeConst_ms)
            {
                m_randomStandbyRotation = MyMath.NormalizeAngle(MyUtils.GetRandomFloat(-MathHelper.Pi * 2.0f, MathHelper.Pi * 2.0f));
                m_randomStandbyElevation = MyMath.NormalizeAngle(MyUtils.GetRandomFloat(0.0f, MathHelper.PiOver2));
                m_randomStandbyChange_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            }
        }



        #endregion

        #region Movement

        private void RandomMovement()
        {
            System.Diagnostics.Debug.Assert(m_barrel != null);
            if (m_barrel == null || m_enableIdleRotation == false)
                return;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyLargeShipGunBase::RandomMovement");

            ResetRandomAiming();

            // real rotation:
            float needRotation = m_randomStandbyRotation;
            float needElevation = m_randomStandbyElevation;
            float step = m_rotationSpeed * m_rotationInterval_ms;
            float diffRot = needRotation - m_rotation;

            bool playAimingSound = false;

            if (diffRot > float.Epsilon)
            {
                float value = MathHelper.Clamp(step, float.Epsilon, needRotation - m_rotation);
                m_rotation += value;
                playAimingSound = true;
            }
            else if (diffRot < -float.Epsilon)
            {
                float value = MathHelper.Clamp(step, float.Epsilon, Math.Abs(needRotation - m_rotation));
                m_rotation -= value;
                playAimingSound = true;
            }

            bool canElevate = false;
            step = m_elevationSpeed * m_elevationInterval_ms;

            if (needElevation > m_barrel.BarrelElevationMin)
            {
                canElevate = true;
            }
            else
            {
                canElevate = false;
            }

            if (canElevate)
            {
                diffRot = needElevation - m_elevation;

                if (diffRot > float.Epsilon)
                {
                    float value = MathHelper.Clamp(step, float.Epsilon, needElevation - m_elevation);
                    m_elevation += value;
                    playAimingSound = true;
                }
                if (diffRot < -float.Epsilon)
                {
                    float value = MathHelper.Clamp(step, float.Epsilon, Math.Abs(needElevation - m_elevation));
                    m_elevation -= value;
                    playAimingSound = true;
                }
            }
            ClampRotationAndElevation();
            m_elevationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_rotationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            if (playAimingSound)
                PlayAimingSound();
            else
                StopAimingSound();

            if (m_randomIsMoving)
            {
                if (!playAimingSound)//movement stopped
                {
                    //make one raycast:
                    SetupSearchRaycast();
                    m_randomIsMoving = false;
                }
            }
            else
            {
                if (playAimingSound)//movement started
                {
                    m_randomIsMoving = true;
                }
            }

            // rotate models by rotation & elevation:
            RotateModels();

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        private void SetupSearchRaycast()
        {
            var head = m_gunBase.GetMuzzleWorldMatrix();
            var from = head.Translation;
            var to = from + head.Forward * m_searchingRange;
            m_laserLength = m_searchingRange;
            //MyPhysics.HitInfo? hitInfo = null;
            //if (!MySandboxGame.IsDedicated)
            //{
            //    hitInfo = MyPhysics.CastRay(from, to);
            //}
            //if (!hitInfo.HasValue)
            m_hitPosition = to;
            //else
            //{
            //    m_hitPosition = hitInfo.Value.Position;
            //    m_laserLength = (m_hitPosition - from).Length();
            //}
        }


        protected void GetCameraDummy()
        {
            // Check for the dummy camera position  
            if (m_base2.Model != null)
            {
                if (m_base2.Model.Dummies.ContainsKey("camera"))
                {
                    CameraDummy = m_base2.Model.Dummies["camera"];
                }
            }
        }

        protected override void RotateModels()
        {
            if (m_base1 == null || m_barrel == null)
                return;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyLargeShipGunBase::RotateModels");
            ClampRotationAndElevation();

            Matrix m;// = (Matrix)InitializationMatrixWorld;
            Vector3D trans = m_base1.WorldMatrix.Translation;
            Matrix.CreateRotationY(m_rotation, out m);
            m.Translation = m_base1.PositionComp.LocalMatrix.Translation;
            //m *= Matrix.CreateFromAxisAngle(InitializationMatrixWorld.Up, m_rotation);
            //m.Translation = trans;
            m_base1.PositionComp.LocalMatrix = m;

            Matrix.CreateRotationX(m_elevation, out m);
            m.Translation = m_base2.PositionComp.LocalMatrix.Translation;
            m_base2.PositionComp.LocalMatrix = m;

            m_barrel.WorldPositionChanged();

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        private Vector3 LookAt(Vector3D target)
        {
            MatrixD m = MatrixD.CreateLookAt(m_gunBase.GetMuzzleWorldMatrix().Translation, target, m_gunBase.GetMuzzleWorldMatrix().Up);

            m = MatrixD.Invert(m);
            m = MatrixD.Normalize(m);
            m *= MatrixD.Invert(MatrixD.Normalize(InitializationMatrixWorld));

            Quaternion rot = Quaternion.CreateFromRotationMatrix(m);
            return MyMath.QuaternionToEuler(rot);
        }

        protected void ResetRotation()
        {
            m_rotation = 0;
            m_elevation = 0;
            ClampRotationAndElevation();
            m_randomStandbyElevation = 0;
            m_randomStandbyRotation = 0;
            m_randomStandbyChange_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        public bool RotationAndElevation()
        {
            bool playAimingSound = false;

            Vector3 lookAtPositionEuler = Vector3.Zero;

            var predictedTargetPosition = Vector3D.Zero;
            //var predictedTargetPosition = GetPredictedTargetPositionPrecise();

            if (Target != null || m_currentPrediction.ManualTargetPosition)
            {
                predictedTargetPosition = m_currentPrediction.GetPredictedTargetPosition(Target);
                lookAtPositionEuler = LookAt(predictedTargetPosition);
            }

            // real rotation:
            float needRotation = lookAtPositionEuler.Y;
            float needElevation = lookAtPositionEuler.X;
            float stepRotation = m_rotationSpeed * (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_rotationInterval_ms);
            float diffRot = needRotation - m_rotation;


            if (diffRot > MathHelper.Pi)
            {
                diffRot = diffRot - MathHelper.TwoPi;
            }
            else if (diffRot < -MathHelper.Pi)
            {
                diffRot = diffRot + MathHelper.TwoPi;
            }

            float diffRotAbs = Math.Abs(diffRot);

            //bool needUpdateMatrix = false;


            if (diffRotAbs > 0.001f)
            {
                float value = MathHelper.Clamp(stepRotation, float.Epsilon, diffRotAbs);
                m_rotation += diffRot > 0 ? value : -value;
                playAimingSound = true;
                //needUpdateMatrix = true;
            }
            else
            {
                m_rotation = needRotation;
                playAimingSound = false;
            }

            if (m_rotation > MathHelper.Pi)
                m_rotation = m_rotation - MathHelper.TwoPi;
            else
                if (m_rotation < -MathHelper.Pi)
                    m_rotation = m_rotation + MathHelper.TwoPi;

            // real elevation
            float stepElevation = m_elevationSpeed * (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_elevationInterval_ms);

            float diffElev = needElevation - m_elevation;
            float diffElevAbs = Math.Abs(diffElev);

            if (needElevation > m_barrel.BarrelElevationMin)
            {
                if (diffElevAbs > 0.001f)
                {
                    float value = MathHelper.Clamp(stepElevation, float.Epsilon, diffElevAbs);
                    m_elevation += diffElev > 0 ? value : -value;
                    //needUpdateMatrix = true;
                }
                else
                {
                    m_elevation = needElevation;
                }
            }


            m_elevationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_rotationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            ClampRotationAndElevation();
            //  if (needUpdateMatrix)
            {
                // rotate models by rotation & elevation:
                RotateModels();
            }

            if (playAimingSound)
                PlayAimingSound();
            else
                StopAimingSound();

            // if is properly rotated:
            if (Target != null || m_currentPrediction.ManualTargetPosition)
            {
                // test intervals of the aiming:
                float stapR = Math.Abs(Math.Abs(needRotation) - Math.Abs(m_rotation));
                float stapE = Math.Abs(Math.Abs(needElevation) - Math.Abs(m_elevation));
                if (stapE <= 0.1f && stapR <= float.Epsilon)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        private void ClampRotationAndElevation()
        {
            m_rotation = ClampRotation(m_rotation);
            m_elevation = ClampElevation(m_elevation);
        }

        private float ClampRotation(float value)
        {
            if (IsRotationLimited())
            {
                value = Math.Min(m_maxAzimuthRadians, Math.Max(m_minAzimuthRadians, value));
            }
            return value;
        }

        private bool IsRotationLimited()
        {
            return Math.Abs((m_maxAzimuthRadians - m_minAzimuthRadians) - MathHelper.TwoPi) > 0.01;
        }

        private float ClampElevation(float value)
        {
            if (IsElevationLimited())
            {
                value = Math.Min(m_maxElevationRadians, Math.Max(m_minElevationRadians, value));
            }
            return value;
        }

        private bool IsElevationLimited()
        {
            return Math.Abs((m_maxElevationRadians - m_minElevationRadians) - MathHelper.TwoPi) > 0.01;
        }

        private bool HasElevationOrRotationChanged()
        {
            if (Math.Abs(m_rotationLast - m_rotation) > MyLargeTurretsConstants.ROTATION_AND_ELEVATION_MIN_CHANGE)
            {
                return true;
            }
            if (Math.Abs(m_elevationLast - m_elevation) > MyLargeTurretsConstants.ROTATION_AND_ELEVATION_MIN_CHANGE)
            {
                return true;
            }
            return false;
        }

        #endregion

        #region Sound

        private void PlayAimingSound()
        {
            if (m_soundEmitterForRotation != null && m_soundEmitterForRotation.IsPlaying == false)
                m_soundEmitterForRotation.PlaySound(m_rotatingCueEnum, true);
        }


        public void PlayShootingSound()
        {
            if (m_soundEmitter != null)
            {
                StopAimingSound();
                m_gunBase.StartShootSound(m_soundEmitter);
            }
        }

        public void StopShootingSound()
        {
            if (m_soundEmitter != null && (m_soundEmitter.SoundId == m_shootingCueEnum.Arcade || m_soundEmitter.SoundId == m_shootingCueEnum.Realistic) && m_soundEmitter.Loop)
                m_soundEmitter.StopSound(false);
        }

        internal void StopAimingSound()
        {
            if (m_soundEmitterForRotation != null && (m_soundEmitterForRotation.SoundId == m_rotatingCueEnum.Arcade || m_soundEmitterForRotation.SoundId == m_rotatingCueEnum.Realistic))
                m_soundEmitterForRotation.StopSound(false);
        }

        #endregion

        #region Intersection

        public override bool GetIntersectionWithLine(ref LineD line, out MyIntersectionResultLineTriangleEx? t, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            System.Diagnostics.Debug.Assert(!Closed);

            if (base.GetIntersectionWithLine(ref line, out t))
                return true;

            if (m_barrel == null)
                return false;

            return m_barrel.Entity.GetIntersectionWithLine(ref line, out t);
        }

        public override bool GetIntersectionWithLine(ref LineD line, out Vector3D? v, bool useCollisionModel = true, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            System.Diagnostics.Debug.Assert(!Closed);

            if (base.GetIntersectionWithLine(ref line, out v, useCollisionModel))
                return true;

            return m_barrel.Entity.GetIntersectionWithLine(ref line, out v, useCollisionModel);
        }

        #endregion

        #region IMyGunObject

        public bool EnabledInWorldRules { get { return MySession.Static.WeaponsEnabled; } }

        public float BackkickForcePerSecond
        {
            get { return m_gunBase.BackkickForcePerSecond; }
        }

        public float ShakeAmount
        {
            get;
            protected set;
        }

        public override bool CanShoot(out MyGunStatusEnum status)
        {
            if (!m_gunBase.HasAmmoMagazines)
            {
                status = MyGunStatusEnum.Failed;
                return false;
            }

            if (!MySession.Static.CreativeMode && !HasEnoughAmmo())
            {
                status = MyGunStatusEnum.OutOfAmmo;
                return false;
            }

            status = MyGunStatusEnum.OK;
            return true;
        }

        protected int GetRemainingAmmo()
        {
            // return RemainingAmmo + (int)Inventory.GetItemAmount(m_currentAmmoMagazineId) * AmmoInMagazine;
            return m_gunBase.GetTotalAmmunitionAmount();
        }

        protected bool HasEnoughAmmo()
        {
            return m_gunBase.HasEnoughAmmunition();
        }

        public override bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            if (!HasPlayerAccess(shooter))
            {
                status = MyGunStatusEnum.AccessDenied;
                return false;
            }

            if (action == MyShootActionEnum.PrimaryAction)
            {
                status = MyGunStatusEnum.OK;
                return true;
            }
            else
            {
                status = MyGunStatusEnum.Failed;
                return false;
            }
        }

        public virtual void Shoot(MyShootActionEnum action, Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            throw new NotImplementedException();
        }

        public bool IsShooting
        {
            get
            {
                return m_isShooting.Value;
            }
        }

        int IMyGunObject<MyGunBase>.ShootDirectionUpdateTime
        {
            get
            {
                return 0;
            }
        }

        public Vector3 DirectionToTarget(Vector3D target)
        {
            throw new NotImplementedException();
        }

        public void BeginFailReaction(MyShootActionEnum action, MyGunStatusEnum status)
        {
            throw new NotImplementedException();
        }

        public bool PerformFailReaction
        {
            get { throw new NotImplementedException(); }
        }


        public void BeginFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        {
            throw new NotImplementedException();
        }


        public void ShootFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        {
            throw new NotImplementedException();
        }

        public void OnControlAcquired(MyCharacter owner)
        {

        }

        public void OnControlReleased()
        {

        }

        public MyDefinitionId DefinitionId
        {
            get { return m_defId; }
        }

        #endregion

        #region Targetting

        private bool IsTarget(MyEntity entity)
        {
            if (entity is Sandbox.Game.Entities.Debris.MyDebrisBase)
                return false;
            if (!TargetCharacters && (entity is MyCharacter||entity is MyGhostCharacter))
                return false;

            if (!TargetMeteors && entity is MyMeteor)
                return false;

            if (!TargetMissiles && entity is MyMissile)
                return false;

            if (entity.Physics != null && !entity.Physics.Enabled)
                return false;

            var topMostParent = entity.GetTopMostParent() ?? entity;

            if (topMostParent.Physics == null || !topMostParent.Physics.Enabled)
                return false;

            bool sameParent = false;
            if (topMostParent is MyCubeGrid)
            {
                if (CubeGrid.UsesTargetingList && !CubeGrid.TargetingCanAttackGrid(topMostParent.EntityId))
                    return false;
                var thisGrid = (MyCubeGrid)this.GetTopMostParent();
                var otherGrid = (MyCubeGrid)topMostParent;
                sameParent = thisGrid.GridSystems.TerminalSystem == otherGrid.GridSystems.TerminalSystem;
                
                //Also check if grids are logically connected (mostly for not detecting Pistons and Rotors as seperate grid). Maybe need to check all adjusent grids?
                //Haven't taken into account Big Owners. If causing bug then change
                if (MyCubeGridGroups.Static.Logical.HasSameGroup(thisGrid, otherGrid))
                    return false;
            }
                

            bool isMyShip = false;
            if (sameParent)
            {
                if ((topMostParent as MyCubeGrid).BigOwners.Contains(this.OwnerId))
                {
                    isMyShip = true;
                }
            }

            if (!isMyShip)
            {
                MyCubeGrid grid = (topMostParent as MyCubeGrid);
                if (grid != null)
                {
                    if (!TargetSmallGrids && grid.GridSizeEnum == MyCubeSize.Small)
                    {
                        return false;
                    }
                    if (grid.GridSizeEnum == MyCubeSize.Large)
                    {
                        if (!TargetLargeGrids && grid.IsStatic == false)
                        {
                            return false;
                        }
                        if (!TargetStations && grid.IsStatic == true)
                        {
                            return false;
                        }
                    }
                }

                if (!sameParent && TargetMoving && topMostParent.Physics.LinearVelocity.LengthSquared() > 9)
                {
                    return true;
                }

                if (entity is MyDecoy)
                    return true;

                if (TargetCharacters && (entity is MyGhostCharacter || entity is MyCharacter && !(entity as MyCharacter).IsDead))
                    return true;

                if (TargetMeteors && entity is MyMeteor)
                    return true;

                if (TargetMissiles && entity is MyMissile)
                    return true;

                var moduleOwner = entity as IMyComponentOwner<MyIDModule>;
                MyIDModule module;
                if (moduleOwner != null && moduleOwner.GetComponent(out module))
                    return true;
            }
            return false;
        }

        private bool IsTargetVisible(MyEntity target)
        {
            return IsTargetVisible(target, m_currentPrediction.GetPredictedTargetPosition(target));
        }

        private bool IsTargetVisible(MyEntity target, Vector3D predictedPos)
        {
            if (target == null || Barrel == null || Barrel.GunBase == null)
                return false;

            var head = WorldMatrix;
            var from = Barrel.GunBase.GetMuzzleWorldPosition();
            var to = predictedPos;

            ProfilerShort.Begin("RayCast");

            var physTarget = MyPhysics.CastRay(from, to, MyPhysics.CollisionLayers.DefaultCollisionLayer);
            //MyPhysics.HitInfo? physTarget = null;
            //if (Sandbox.Game.Gui.MyMichalDebugInputComponent.Static.CastLongRay)
            //    physTarget = MyPhysics.CastLongRay(from, to);
            //else
            //    physTarget = MyPhysics.CastRay(from, to, MyPhysics.CollisionLayers.DefaultCollisionLayer);
            ProfilerShort.End();
            IMyEntity hitEntity = null;

            if (physTarget.HasValue)
            {
                System.Diagnostics.Debug.Assert(physTarget.Value.HkHitInfo.Body.UserObject != null);

                if (physTarget.Value.HkHitInfo.Body != null && physTarget.Value.HkHitInfo.Body.UserObject != null && physTarget.Value.HkHitInfo.Body.UserObject is MyPhysicsBody)
                {
                    hitEntity = ((MyPhysicsBody)physTarget.Value.HkHitInfo.Body.UserObject).Entity;
                }
            }

            //VRageRender.MyRenderProxy.DebugDrawLine3D(from, to, Color.White, Color.White, false);

            if (hitEntity == null || target == hitEntity || target.Parent == hitEntity || (target.Parent != null && target.Parent == hitEntity.Parent) || hitEntity is MyMissile || hitEntity is MyFloatingObject)
            {
                m_notVisibleTargets.Remove(target);
                return true;
            }

            //AB: No you cannot shoot through cubegrid even if it belongs to nobody 
            //var grid = hitEntity as MyCubeGrid;
            //if (grid != null && grid.BigOwners.Count == 0)
            //{
            //    m_notVisibleTargets.Remove(target);
            //    return true;
            //}

            if (m_notVisibleTargets.ContainsKey(target))
                m_notVisibleTargets[target] = 2 * NotVisibleFrequency + VRage.Library.Utils.MyRandom.Instance.Next(NotVisibleFrequency);
            else
                m_notVisibleTargets[target] = NotVisibleFrequency + VRage.Library.Utils.MyRandom.Instance.Next(NotVisibleFrequency);
            return false;
        }


        Dictionary<MyEntity, int> m_notVisibleTargets = new Dictionary<MyEntity, int>();
        const int NotVisibleFrequency = 5;

        private MyEntity GetNearestVisibleTarget(float range, bool onlyPotential)
        {
            if (!HasEnoughAmmo() && !MySession.Static.CreativeMode && !onlyPotential)
                return null;

            var targetList = CubeGrid.Components.Get<MyGridTargeting>().TargetRoots;

            MyEntity nearestTarget = null;
            double minDistanceSq = range * range;// double.MaxValue;
            ProfilerShort.Begin("FindNearest");

            bool foundDecoy = false;
            foreach (var target in targetList)
            {
                TestTarget(target, onlyPotential, ref nearestTarget, ref minDistanceSq, ref foundDecoy);
            }

            ProfilerShort.End(targetList.Count);
            return nearestTarget;
        }

        private void TestTarget(MyEntity target, bool onlyPotential, ref MyEntity nearestTarget, ref double minDistanceSq, ref bool foundDecoy)
        {
            if (target.MarkedForClose)
                return;

            if (m_ignoredEntities.Contains(target.EntityId))
                return;

            var grid = target as MyCubeGrid;
            bool isDecoy = IsDecoy(target);
            double dist;
            if (grid != null)
            {
                if (grid.GridSystems.TerminalSystem == this.CubeGrid.GridSystems.TerminalSystem && grid.BigOwners.Contains(this.OwnerId))
                    return; // Me

                dist = grid.PositionComp.WorldAABB.DistanceSquared(PositionComp.GetPosition());
                if ((dist >= minDistanceSq && foundDecoy))
                    return; //none block closer than nearest target

                var blockList = CubeGrid.Components.Get<MyGridTargeting>().TargetBlocks.GetList(grid);
                if (blockList != null)
                {
                    foreach (var block in blockList)
                    {
                        TestTarget(block, onlyPotential, ref nearestTarget, ref minDistanceSq, ref foundDecoy);
                    }
                }
            }

            int delay;
            if (!onlyPotential && m_notVisibleTargets.TryGetValue(target, out delay))
            {
                if (delay > 0)
                {
                    m_notVisibleTargets[target] = delay - 1;
                    return;
                }
            }

            
            if (foundDecoy && !isDecoy) //found decoy search only for closer decoy
                return;
            dist = Vector3D.DistanceSquared(target.PositionComp.GetPosition(), PositionComp.GetPosition());

            //we have closer target;
            //if block is further away but is decoy and decoy is not found yet this block have to pass
            if ((dist >= minDistanceSq && (!isDecoy || foundDecoy)) || (!isDecoy && foundDecoy))
            //if (dist >= minDistanceSq)
                return;

            //only check targets
            if (!(IsTarget(target) && IsTargetEnemy(target)))
                return;

            //when not just potentional check visibility
            var predPos = m_currentPrediction.GetPredictedTargetPosition(target);
            if (!onlyPotential && !(IsTargetInView(target, predPos) && IsTargetVisible(target, predPos)))
                return;

            if (isDecoy)
            {
                nearestTarget = target;
                minDistanceSq = dist;
                foundDecoy = true;
                return;
            }

            if (!IsTargetAimed(target))
            {
                minDistanceSq = dist;
                nearestTarget = target;
            }
        }

        private bool IsDecoy(MyEntity target)
        {
            var decoy = target as MyDecoy;
            return (decoy != null) && decoy.IsWorking &&
                   (target.Parent.Physics != null && target.Parent.Physics.Enabled);
        }

        private bool IsTargetAimed(MyEntity target)
        {
            if (Target == target)
                return false;

            return m_targets.Contains(target) && (target is IMyDestroyableObject)
                // If small integrity, dont target with multiple turrets
                && (target as IMyDestroyableObject).Integrity < 2 * m_gunBase.MechanicalDamage;
        }

        private bool IsTargetInView(MyEntity target, Vector3D predPos)
        {
            ProfilerShort.Begin("InView");
            //var predPos = m_currentPrediction.GetPredictedTargetPosition(target);
            ProfilerShort.End();
            var lookAtPositionEuler = LookAt(predPos);
            float needElevation = lookAtPositionEuler.X;

            if (m_barrel != null && needElevation > m_barrel.BarrelElevationMin && IsInRange(lookAtPositionEuler))
            {
                return true;
            }
            return false;
        }


        private bool IsTargetEnemy(MyEntity target)
        {
            if (!MyFakes.SHOW_FACTIONS_GUI)
                return true;

            if (target is MyCubeGrid)
            {
                if ((target as MyCubeGrid).BigOwners.Count == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            var moduleOwner = target as IMyComponentOwner<MyIDModule>;
            if (moduleOwner != null)
            {
                MyIDModule targetModule = null;
                if (moduleOwner.GetComponent(out targetModule))
                {
                    VRage.Game.MyRelationsBetweenPlayerAndBlock relation = GetUserRelationToOwner(targetModule.Owner);
                    if (relation != VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies)
                        return false;
                    if (!TargetNeutrals && targetModule.Owner == 0)
                        return false;
                    return true;
                }
                else
                    return false;
            }

            if (target is MyMissile)
            {
                VRage.Game.MyRelationsBetweenPlayerAndBlock relation = GetUserRelationToOwner((target as MyMissile).Owner);
                if (relation != VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies)
                    return false;
                if (!TargetNeutrals && (target as MyMissile).Owner == 0)
                    return false;
                return true;
            }

            if (target is MyCharacter || target is MyGhostCharacter)
            {
                var controller = (target as IMyControllableEntity).ControllerInfo.Controller;
                if (controller == null)
                    return false;

                long targetPlayerId = controller.Player.Identity.IdentityId;
                VRage.Game.MyRelationsBetweenPlayerAndBlock relation = GetUserRelationToOwner(targetPlayerId);
                if (relation != VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies)
                    return false;
                return true;
            }
            return true;
        }

        private void CheckNearTargets()
        {
            if (m_checkOtherTargets == false)
            {
                return;
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyLargeShipGunBase::CheckNearTargets");

            MyEntity nearestTarget = null;
            float targetRange = 0;
            
            if (Target != null)
            {

                targetRange = (float)GetTargetDistance();
                //GR: if in range and not enemy then this is no target (may happen if entiy is in range and have changed faction)
                if (targetRange >= m_searchingRange || !IsTargetEnemy(Target))
                {
                    nearestTarget = GetNearestVisibleTarget(m_searchingRange, true);
                }
                else
                    nearestTarget = Target;
            }
            else
                nearestTarget = GetNearestVisibleTarget(m_searchingRange, true);

            bool oldPotentialState = m_isPotentialTarget;

            m_isPotentialTarget = true;

            if (nearestTarget != null)
            {
                MyEntity shootableTarget = null;
                if (Target != null)
                {
                    if (IsTargetVisible(Target) && (float)GetTargetDistance() < m_shootingRange && IsTarget(Target))
                    {
                        shootableTarget = Target;
                    }
                    else
                    {
                        shootableTarget = GetNearestVisibleTarget(Math.Min(targetRange, m_shootingRange), false);
                        if (shootableTarget == null)
                            shootableTarget = GetNearestVisibleTarget(m_shootingRange, false);
                    }
                }
                else
                    shootableTarget = GetNearestVisibleTarget(m_shootingRange, false);
                if (shootableTarget != null)
                {
                    nearestTarget = shootableTarget;
                    m_isPotentialTarget = false;
                }
            }

            //nearestTarget = GetNearestVisibleTarget(m_shootingRange, false);
            //m_isPotentialTarget = false;
            
            if (MyFakes.FakeTarget != null && IsTargetVisible(MyFakes.FakeTarget, MyFakes.FakeTarget.WorldMatrix.Translation))
            {
                Target = MyFakes.FakeTarget;
            }
            else
            {
                Target = nearestTarget;
            }

            if (nearestTarget == Target && m_isPotentialTarget != oldPotentialState)
            {
                Target = null;
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public double GetTargetDistance()
        {
            if (Target != null && m_barrel != null && m_barrel.Entity != null)
            {
                return (Target.PositionComp.GetPosition() - m_barrel.Entity.PositionComp.GetPosition()).Length();
            }
            return m_searchingRange;
        }

        public MyEntity Target
        {
            get { return m_target; }
            private set
            {
                if (m_target != value)
                {
                    var oldTarget = m_target;
                    if (m_target != null)
                    {
                        m_target.OnClose -= m_target_OnClose;
                        m_targets.Remove(m_target);
                        MyHud.LargeTurretTargets.UnregisterMarker(m_target);
                    }
                    m_target = value;
                    if (m_target != null)
                    {
                        m_target.OnClose += m_target_OnClose;

                        if (!m_isPotentialTarget)
                            m_targets.Add(m_target);

                        var hudParams = new MyHudEntityParams()
                        {
                            FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_ICON,
                            OffsetText = true,
                            Icon = MyHudTexturesEnum.TargetTurret,
                            IconSize = new Vector2(0.02f, 0.02f)
                        };

                        if (MySession.Static.LocalCharacter != null)
                        {
                            if (!m_isPotentialTarget && HasLocalPlayerAccess() && (Vector3D.Distance(((MyEntity)MySession.Static.LocalCharacter).PositionComp.GetPosition(), PositionComp.GetPosition()) < ShootingRange))
                            {
                                MyHud.LargeTurretTargets.RegisterMarker(m_target, hudParams);
                            }
                        }
                    }

                    if (oldTarget != m_target && Sync.IsServer)
                    {
                        m_targetSync.Value  = new CurrentTargetSync(){TargetId = m_target == null ? 0 : m_target.EntityId, IsPotential = m_isPotentialTarget};
                    }
                }
            }
        }

        public void SetTarget(MyEntity target, bool isPotential)
        {
            m_isPotentialTarget = isPotential;
            Target = target;
        }

        void m_target_OnClose(MyEntity obj)
        {
            Target = null;

            if (m_barrel != null && AiEnabled)
            {
                //CheckNearTargets();
            }
        }

        #endregion

        #region Inventory

        public void SetInventory(MyInventory inventory, int index)
        {
            Components.Add<MyInventoryBase>(inventory);
        }

        #endregion

        #region Ammo

        public int GetAmmunitionAmount()
        {
            return m_gunBase.GetInventoryAmmoMagazinesCount();
        }

        public void RemoveAmmoPerShot()
        {
            m_gunBase.ConsumeAmmo();
            //if (RemainingAmmo < AmmoPerShot)
            //{
            //    Inventory.RemoveItemsOfType(1, m_currentAmmoMagazineId);
            //    RemainingAmmo += AmmoInMagazine;
            //}

            //if (RemainingAmmo >= AmmoPerShot)
            //{
            //    RemainingAmmo -= AmmoPerShot;
            //}
        }

        #endregion

        #region Control panel

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyLargeTurretBase>())
                return;
            base.CreateTerminalControls();
            if (MyFakes.ENABLE_TURRET_CONTROL)
            {
                var controlBtn = new MyTerminalControlButton<MyLargeTurretBase>("Control", MySpaceTexts.ControlRemote, MySpaceTexts.Blank, (t) => t.RequestControl());
                controlBtn.Enabled = t => t.CanControl();
                controlBtn.SupportsMultipleBlocks = false;
                var action = controlBtn.EnableAction(MyTerminalActionIcons.TOGGLE);
                if (action != null)
                {
                    action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.ButtonPanel };
                    action.ValidForGroups = false;
                }

                MyTerminalControlFactory.AddControl(controlBtn);
            }

            var shootingRange = new MyTerminalControlSlider<MyLargeTurretBase>("Range", MySpaceTexts.BlockPropertyTitle_LargeTurretRadius, MySpaceTexts.BlockPropertyTitle_LargeTurretRadius);
            shootingRange.Normalizer = (x, f) => x.NormalizeRange(f);
            shootingRange.Denormalizer = (x, f) => x.DenormalizeRange(f);
            shootingRange.DefaultValue = 800;
            shootingRange.Getter = (x) => x.ShootingRange;
            shootingRange.Setter = (x, v) => x.ShootingRange = v;
            shootingRange.Writer = (x, result) => result.AppendInt32((int)x.m_shootingRange).Append(" m");
            shootingRange.EnableActions();
            MyTerminalControlFactory.AddControl(shootingRange);

            var enableIdleMovement = new MyTerminalControlOnOffSwitch<MyLargeTurretBase>("EnableIdleMovement", MySpaceTexts.BlockPropertyTitle_LargeTurretEnableTurretIdleMovement);
            enableIdleMovement.Getter = (x) => x.EnableIdleRotation;
            enableIdleMovement.Setter = (x, v) => x.EnableIdleRotation = v;
            enableIdleMovement.EnableToggleAction();
            enableIdleMovement.EnableOnOffActions();
            MyTerminalControlFactory.AddControl(enableIdleMovement);

            var separator = new MyTerminalControlSeparator<MyLargeTurretBase>();
            MyTerminalControlFactory.AddControl(separator);

            var targetMeteors = new MyTerminalControlOnOffSwitch<MyLargeTurretBase>("TargetMeteors", MySpaceTexts.BlockPropertyTitle_LargeTurretTargetMeteors);
            targetMeteors.Getter = (x) => x.TargetMeteors;
            targetMeteors.Setter = (x, v) =>
            {
                x.TargetMeteors = v;
            };
            targetMeteors.EnableToggleAction(MyTerminalActionIcons.METEOR_TOGGLE);
            targetMeteors.EnableOnOffActions(MyTerminalActionIcons.METEOR_ON, MyTerminalActionIcons.METEOR_OFF);
            MyTerminalControlFactory.AddControl(targetMeteors);

            var targetMoving = new MyTerminalControlOnOffSwitch<MyLargeTurretBase>("TargetMoving", MySpaceTexts.BlockPropertyTitle_LargeTurretTargetMoving);
            targetMoving.Getter = (x) => x.TargetMoving;
            targetMoving.Setter = (x, v) =>
            {
                x.TargetMoving = v;
            };
            targetMoving.EnableToggleAction(MyTerminalActionIcons.MOVING_OBJECT_TOGGLE);
            targetMoving.EnableOnOffActions(MyTerminalActionIcons.MOVING_OBJECT_ON, MyTerminalActionIcons.MOVING_OBJECT_OFF);
            MyTerminalControlFactory.AddControl(targetMoving);

            var targetMissiles = new MyTerminalControlOnOffSwitch<MyLargeTurretBase>("TargetMissiles", MySpaceTexts.BlockPropertyTitle_LargeTurretTargetMissiles);
            targetMissiles.Getter = (x) => x.TargetMissiles;
            targetMissiles.Setter = (x, v) =>
            {
                x.TargetMissiles = v;        
            };
            targetMissiles.EnableToggleAction(MyTerminalActionIcons.MISSILE_TOGGLE);
            targetMissiles.EnableOnOffActions(MyTerminalActionIcons.MISSILE_ON, MyTerminalActionIcons.MISSILE_OFF);
            MyTerminalControlFactory.AddControl(targetMissiles);

            var targetSmallGrids = new MyTerminalControlOnOffSwitch<MyLargeTurretBase>("TargetSmallShips", MySpaceTexts.BlockPropertyTitle_LargeTurretTargetSmallGrids);
            targetSmallGrids.Getter = (x) => x.TargetSmallGrids;
            targetSmallGrids.Setter = (x, v) =>
            {
                x.TargetSmallGrids = v;
            };
            targetSmallGrids.EnableToggleAction(MyTerminalActionIcons.SMALLSHIP_TOGGLE);
            targetSmallGrids.EnableOnOffActions(MyTerminalActionIcons.SMALLSHIP_ON, MyTerminalActionIcons.SMALLSHIP_OFF);
            MyTerminalControlFactory.AddControl(targetSmallGrids);

            var targetLargeGrids = new MyTerminalControlOnOffSwitch<MyLargeTurretBase>("TargetLargeShips", MySpaceTexts.BlockPropertyTitle_LargeTurretTargetLargeGrids);
            targetLargeGrids.Getter = (x) => x.TargetLargeGrids;
            targetLargeGrids.Setter = (x, v) =>
            {
                x.TargetLargeGrids = v;
            };
            targetLargeGrids.EnableToggleAction(MyTerminalActionIcons.LARGESHIP_TOGGLE);
            targetLargeGrids.EnableOnOffActions(MyTerminalActionIcons.LARGESHIP_ON, MyTerminalActionIcons.LARGESHIP_OFF);
            MyTerminalControlFactory.AddControl(targetLargeGrids);

            var targetCharacters = new MyTerminalControlOnOffSwitch<MyLargeTurretBase>("TargetCharacters", MySpaceTexts.BlockPropertyTitle_LargeTurretTargetCharacters);
            targetCharacters.Getter = (x) => x.TargetCharacters;
            targetCharacters.Setter = (x, v) =>
            {
                x.TargetCharacters = v;
            };
            targetCharacters.EnableToggleAction(MyTerminalActionIcons.CHARACTER_TOGGLE);
            targetCharacters.EnableOnOffActions(MyTerminalActionIcons.CHARACTER_ON, MyTerminalActionIcons.CHARACTER_OFF);
            MyTerminalControlFactory.AddControl(targetCharacters);

            var targetStations = new MyTerminalControlOnOffSwitch<MyLargeTurretBase>("TargetStations", MySpaceTexts.BlockPropertyTitle_LargeTurretTargetStations);
            targetStations.Getter = (x) => x.TargetStations;
            targetStations.Setter = (x, v) =>
            {
                x.TargetStations = v;
            };
            targetStations.EnableToggleAction(MyTerminalActionIcons.STATION_TOGGLE);
            targetStations.EnableOnOffActions(MyTerminalActionIcons.STATION_ON, MyTerminalActionIcons.STATION_OFF);
            MyTerminalControlFactory.AddControl(targetStations);

            var targetNeutrals = new MyTerminalControlOnOffSwitch<MyLargeTurretBase>("TargetNeutrals", MySpaceTexts.BlockPropertyTitle_LargeTurretTargetNeutrals);
            targetNeutrals.Getter = (x) => x.TargetNeutrals;
            targetNeutrals.Setter = (x, v) =>
            {
                x.TargetNeutrals = v;
            };
            targetNeutrals.EnableToggleAction(MyTerminalActionIcons.NEUTRALS_TOGGLE);
            targetNeutrals.EnableOnOffActions(MyTerminalActionIcons.NEUTRALS_ON, MyTerminalActionIcons.NEUTRALS_OFF);
            MyTerminalControlFactory.AddControl(targetNeutrals);

        }

        public float ShootingRange
        {
            get { return m_shootingRange; }
            set
            {
                m_shootingRange.Value = value;
            }
        }

        public bool TargetMeteors
        {
            get
            {
                return (TargetFlags & MyTurretTargetFlags.Asteroids) != 0;
            }
            set
            {
                if (value)
                    TargetFlags |= MyTurretTargetFlags.Asteroids;
                else
                    TargetFlags &= ~MyTurretTargetFlags.Asteroids;
            }
        }

        public bool TargetMoving
        {
            get
            {
                return (TargetFlags & MyTurretTargetFlags.Moving) != 0;
            }
            set
            {
                if (value)
                    TargetFlags |= MyTurretTargetFlags.Moving;
                else
                    TargetFlags &= ~MyTurretTargetFlags.Moving;
            }
        }

        public bool TargetMissiles
        {
            get
            {
                return (TargetFlags & MyTurretTargetFlags.Missiles) != 0;
            }
            set
            {
                if (value)
                    TargetFlags |= MyTurretTargetFlags.Missiles;
                else
                    TargetFlags &= ~MyTurretTargetFlags.Missiles;
            }
        }

        public bool TargetSmallGrids
        {
            get
            {
                return (TargetFlags & MyTurretTargetFlags.SmallShips) != 0;
            }
            set
            {
                if (value)
                    TargetFlags |= MyTurretTargetFlags.SmallShips;
                else
                    TargetFlags &= ~MyTurretTargetFlags.SmallShips;
            }
        }

        public bool TargetLargeGrids
        {
            get
            {
                return (TargetFlags & MyTurretTargetFlags.LargeShips) != 0;
            }
            set
            {
                if (value)
                    TargetFlags |= MyTurretTargetFlags.LargeShips;
                else
                    TargetFlags &= ~MyTurretTargetFlags.LargeShips;
            }
        }

        public bool TargetCharacters
        {
            get
            {
                return (TargetFlags & MyTurretTargetFlags.Players) != 0;
            }
            set
            {
                if (value)
                    TargetFlags |= MyTurretTargetFlags.Players;
                else
                    TargetFlags &= ~MyTurretTargetFlags.Players;
            }
        }

        public bool TargetStations
        {
            get
            {
                return (TargetFlags & MyTurretTargetFlags.Stations) != 0;
            }
            set
            {
                if (value)
                    TargetFlags |= MyTurretTargetFlags.Stations;
                else
                    TargetFlags &= ~MyTurretTargetFlags.Stations;
            }
        }

        public bool TargetNeutrals
        {
            get
            {
                return (TargetFlags & MyTurretTargetFlags.NotNeutrals) == 0;
            }
            set
            {
                if (value)
                    TargetFlags &= ~MyTurretTargetFlags.NotNeutrals;
                else
                    TargetFlags |= MyTurretTargetFlags.NotNeutrals;
            }
        }
        #endregion

        #region Player control

        private bool CanControl()
        {
            if (!IsWorking)
            {
                return false;
            }

            if (IsPlayerControlled)
            {
                return false;
            }

            //Cannot transfer control from remote control or other turrets (for now at least)
            //This is needed to prevent strange circular references that may appear
            //Also, there needs to be a valid connection
            //This is called on client only, so MySession.Static.ControlledEntity is valid
            var cockpit = MySession.Static.ControlledEntity as MyCockpit;
            if (cockpit != null)
            {
                if (cockpit is MyCryoChamber)
                {
                    return false;
                }

                return MyAntennaSystem.Static.CheckConnection(cockpit.CubeGrid, CubeGrid, cockpit.ControllerInfo.Controller.Player);
            }

            var character = MySession.Static.ControlledEntity as MyCharacter;
            if (character != null)
            {
                return MyAntennaSystem.Static.CheckConnection(character, CubeGrid, character.ControllerInfo.Controller.Player);
            }

            return false;
        }

        public bool WasControllingCockpitWhenSaved()
        {
            if (m_savedPreviousControlledEntityId != null)
            {
                MyEntity oldControllerEntity;
                if (MyEntities.TryGetEntityById(m_savedPreviousControlledEntityId.Value, out oldControllerEntity))
                {
                    return oldControllerEntity is MyCockpit;
                }
            }

            return false;
        }

        private void RequestControl()
        {
            if (!MyFakes.ENABLE_TURRET_CONTROL)
            {
                return;
            }
            if (!CanControl())
                return;

            if (MyGuiScreenTerminal.IsOpen)
            {
                MyGuiScreenTerminal.Hide();
            }
            //MyCubeBuilder.Static.Deactivate();
            MySession.Static.GameFocusManager.Clear();

            MyMultiplayer.RaiseEvent(this,x => x.RequestUseMessage, UseActionEnum.Manipulate,MySession.Static.ControlledEntity.Entity.EntityId);
        }

        private void AcquireControl(IMyControllableEntity previousControlledEntity)
        {
            PreviousControlledEntity = previousControlledEntity;
            previousControlledEntity.SwitchControl(this);

            SetCameraOverlay();
            if (IsControlledByLocalPlayer)
            {
                MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, this);
                m_targetFov = m_maxFov;
                SetFov(m_maxFov);
            }

            var character = PreviousControlledEntity as MyCharacter;
            if (character != null)
            {
                character.CurrentRemoteControl = this;
            }

            OnStopAI();
        }

        MyShipController m_controller;

        private void SetCameraOverlay()
        {
            if (IsControlledByLocalPlayer)
            {
                //This is for backwards compatibility.
                //If there are mods that changed turrets, BlockDefinition will be null
                if (BlockDefinition != null && BlockDefinition.OverlayTexture != null)
                {
                    MyHudCameraOverlay.TextureName = BlockDefinition.OverlayTexture;
                    MyHudCameraOverlay.Enabled = true;
                }
                else
                {
                    MyHudCameraOverlay.Enabled = false;
                }

                // MyGuiScreenHudSpace.Static instance is created only after deserialing
                m_hidetoolbar = true;
            }
        }

        public void ForceReleaseControl()
        {
            ReleaseControl(false);
        }

        private void ReleaseControl(bool previousClosed = false)
        {
            if (IsControlledByLocalPlayer)
            {
                MyGuiScreenHudSpace.Static.SetToolbarVisible(true);
                m_hidetoolbar = false;
            }

            if (IsPlayerControlled)
            {
                //On clients that are disconnecting, don't send this
                if (!MyEntities.CloseAllowed)
                {
                    EndShoot(MyShootActionEnum.PrimaryAction);
                }

                if (PreviousControlledEntity is MyCockpit)
                {
                    var cockpit = m_previousControlledEntity as MyCockpit;
                    if (previousClosed||cockpit.Pilot==null || cockpit.MarkedForClose || cockpit.Closed)
                    {
                        //This is null when loading from file
                        ReturnControl(m_cockpitPilot);
                        return;
                    }
                }

                var character = PreviousControlledEntity as MyCharacter;
                if (character != null)
                {
                    character.CurrentRemoteControl = null;
                }

                CubeGrid.ControlledFromTurret = false;

                ReturnControl(PreviousControlledEntity);
            }
        }

        //This is needed because server might trigger the release of control before ReleaseControl method is called
        private void OnControlReleased(MyEntityController controller)
        {
            if (IsControlled)
            {
                if (controller.Player == MySession.Static.LocalHumanPlayer)
                {
                    MyHudCameraOverlay.Enabled = false;
                    var receiver = GetFirstRadioReceiver();
                    if (receiver != null)
                    {
                        receiver.Clear();
                    }
                    ExitView();
                }
            }
        }

        [Event,Reliable,Broadcast,Server]
        void sync_ControlledEntity_Used()
        {
            ReleaseControl(false);
        }

        void sync_UseFailed(UseActionEnum action, UseActionResult actionResult, IMyControllableEntity user)
        {
            if (user != null && user.ControllerInfo.IsLocallyHumanControlled())
            {
                if (actionResult == UseActionResult.UsedBySomeoneElse)
                    MyHud.Notifications.Add(new MyHudNotification(MyCommonTexts.AlreadyUsedBySomebodyElse, 2500, MyFontEnum.Red));
                else
                    MyHud.Notifications.Add(MyNotificationSingletons.AccessDenied);
            }
        }

        void sync_UseSuccess(UseActionEnum action, IMyControllableEntity user)
        {
            AcquireControl(user);
        }

        private void OnStopAI()
        {
            if (m_soundEmitter == null)
                return;
            if (m_soundEmitter.IsPlaying)
                m_soundEmitter.StopSound(true);
            if (m_soundEmitterForRotation.IsPlaying)
                m_soundEmitterForRotation.StopSound(true);
        }

        public void UpdateRotationAndElevation(float newRotation, float newElevation)
        {
            m_rotation = newRotation;
            m_elevation = newElevation;

            RotateModels();
        }

        private void ReturnControl(IMyControllableEntity nextControllableEntity)
        {
            //Check if it was already switched by server
            if (ControllerInfo.Controller != null)
            {
                this.SwitchControl(nextControllableEntity);
            }
            PreviousControlledEntity = null;

            m_randomStandbyElevation = m_elevation;
            m_randomStandbyRotation = m_rotation;
            m_randomStandbyChange_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        private MyCharacter GetUser()
        {
            if (PreviousControlledEntity != null)
            {
                if (PreviousControlledEntity is MyCockpit)
                {
                    return (PreviousControlledEntity as MyCockpit).Pilot;
                }

                var character = PreviousControlledEntity as MyCharacter;
                MyDebug.AssertDebug(character != null, "Cannot get the user of this remote control block, even though it is used!");
                if (character != null)
                {
                    return character;
                }
            }

            return null;
        }

        private bool IsInRangeAndPlayerHasAccess()
        {
            if (ControllerInfo.Controller == null)
            {
                System.Diagnostics.Debug.Fail("Controller is null, but remote control was not properly released!");
                return false;
            }

            var terminal = PreviousControlledEntity as MyTerminalBlock;
            if (terminal == null)
            {
                var character = PreviousControlledEntity as MyCharacter;
                if (character != null)
                {
                    return MyAntennaSystem.Static.CheckConnection(character, CubeGrid, ControllerInfo.Controller.Player);
                }
                else
                {
                    return true;
                }
            }

            MyCubeGrid playerGrid = terminal.SlimBlock.CubeGrid;

            return MyAntennaSystem.Static.CheckConnection(playerGrid, CubeGrid, ControllerInfo.Controller.Player);
        }

        private MyDataReceiver GetFirstRadioReceiver()
        {
            var character = PreviousControlledEntity as MyCharacter;
            if (character != null)
            {
                return character.RadioReceiver;
            }

            var receivers = MyDataReceiver.GetGridRadioReceivers(CubeGrid);
            if (receivers.Count > 0)
            {
                return receivers.FirstElement();
            }

            return null;
        }

        private void AddPreviousControllerEvents()
        {
            m_previousControlledEntity.Entity.OnMarkForClose += Entity_OnPreviousMarkForClose;
            var functionalBlock = m_previousControlledEntity.Entity as MyTerminalBlock;
            if (functionalBlock != null)
            {
                functionalBlock.IsWorkingChanged += PreviousCubeBlock_IsWorkingChanged;
            }
        }

        private void PreviousCubeBlock_IsWorkingChanged(MyCubeBlock obj)
        {
            if (!obj.IsWorking && !(obj.Closed || obj.MarkedForClose))
            {
                ReleaseControl();
            }
        }

        protected override void OnOwnershipChanged()
        {
            base.OnOwnershipChanged();

            if (PreviousControlledEntity != null && Sync.IsServer)
            {
                if (ControllerInfo.Controller != null)
                {
                    if (!HasPlayerAccess(ControllerInfo.Controller.Player.Identity.IdentityId))
                    {
                        MyMultiplayer.RaiseEvent(this, x => x.sync_ControlledEntity_Used);
                    }
                }
            }
        }

        //When previous controller is closed, release control of remote
        private void Entity_OnPreviousMarkForClose(MyEntity obj)
        {
            ReleaseControl(true);
        }

        public UseActionResult CanUse(UseActionEnum actionEnum, IMyControllableEntity user)
        {
            if (IsWorking)
            {
                if (IsPlayerControlled)
                {
                    return UseActionResult.UsedBySomeoneElse;
                }
                return UseActionResult.OK;
            }
            else
            {
                return UseActionResult.AccessDenied;
            }
        }

        public void RemoveUsers(bool local)
        {

        }

        public bool PrimaryLookaround
        {
            get { return false; }
        }

        new MatrixD GetViewMatrix()
        {
            RotateModels();

            MatrixD viewMatrix;

            MatrixD worldMatrix = m_base2.WorldMatrix;

            if (CameraDummy != null)
            {
                Matrix dummyLocal = Matrix.Normalize(CameraDummy.Matrix);
                worldMatrix = MatrixD.Multiply(dummyLocal, worldMatrix);
            }
            else
            {
                worldMatrix.Translation += worldMatrix.Forward * ForwardCameraOffset;
                worldMatrix.Translation += worldMatrix.Up * UpCameraOffset;
            }

            MatrixD.Invert(ref worldMatrix, out viewMatrix);
            return viewMatrix;
        }

        #region IMyCameraController implementation

        void IMyCameraController.ControlCamera(MyCamera currentCamera)
        {
            currentCamera.SetViewMatrix(GetViewMatrix());
        }

        void IMyCameraController.Rotate(Vector2 rotationIndicator, float rollIndicator)
        {

        }

        void IMyCameraController.RotateStopped()
        {

        }

        void IMyCameraController.OnAssumeControl(IMyCameraController previousCameraController)
        {

        }
        void IMyCameraController.OnReleaseControl(IMyCameraController newCameraController)
        {

        }

        bool IMyCameraController.HandleUse()
        {
            if (MySession.Static.LocalCharacter != null)
            {
                MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, MySession.Static.LocalCharacter);
                m_targetFov = m_maxFov;
                SetFov(m_maxFov);
            }
            return false;

        }

        bool IMyCameraController.HandlePickUp()
        {
            return false;
        }

        bool IMyCameraController.IsInFirstPersonView
        {
            get { return true; }
            set { }
        }
        bool IMyCameraController.ForceFirstPersonCamera { get; set; }

        bool IMyCameraController.AllowCubeBuilding
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Zoom implementation

        void ChangeZoom(int deltaZoom)
        {
            if (deltaZoom > 0)
            {
                m_targetFov -= 0.15f;
                if (m_targetFov < m_minFov)
                {
                    m_targetFov = m_minFov;
                }
            }
            else
            {
                m_targetFov += 0.15f;
                if (m_targetFov > m_maxFov)
                {
                    m_targetFov = m_maxFov;
                }
            }
            SetFov(m_fov);
        }

        public void ExitView()
        {
            MySector.MainCamera.FieldOfView = MySandboxGame.Config.FieldOfView;
        }

        private static void SetFov(float fov)
        {
            System.Diagnostics.Debug.Assert(fov > MIN_FOV && fov < MAX_FOV, "FOV for camera has invalid values");
            fov = MathHelper.Clamp(fov, MIN_FOV, MAX_FOV);

            MySector.MainCamera.FieldOfView = fov;
        }

        #endregion

        #region IMyControllableEntity implementation
        private MyControllerInfo m_controllerInfo = new MyControllerInfo();
        public MyControllerInfo ControllerInfo
        {
            get
            {
                return m_controllerInfo;
            }
        }

        MyEntity IMyControllableEntity.Entity
        {
            get
            {
                return this;
            }
        }

        public IMyEntity Entity
        {
            get
            {
                return this;
            }
        }

        public bool ForceFirstPersonCamera { get; set; }

        public MatrixD GetHeadMatrix(bool includeY, bool includeX = true, bool forceBoneAnim = false, bool forceHeadBone = false)
        {
            return GetViewMatrix();
        }

        public void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        {
            bool rotationLocked = false;
            if (CubeGrid.HasMainCockpit() || CubeGrid.HasMainRemoteControl())
            {
                MyShipController controller = (MyShipController)(CubeGrid.HasMainCockpit() ? CubeGrid.MainCockpit : CubeGrid.MainRemoteControl);
                bool canControl = true;
                if (CubeGrid.HasMainCockpit())
                {
                    if (controller.Pilot == null || controller.Pilot != Sandbox.Game.World.MySession.Static.LocalCharacter)
                        canControl = false;
                }
                if (canControl && controller.HasLocalPlayerAccess())
                {
                    if (MyInput.Static.IsAnyAltKeyPressed())
                    {
                        controller.MoveAndRotate(moveIndicator, rotationIndicator, rollIndicator);
                        rotationLocked = true;
                    }
                    else
                    {
                        controller.MoveAndRotate(moveIndicator, Vector2.Zero, rollIndicator);
                    }
                    controller.MoveAndRotate();
                    CubeGrid.ControlledFromTurret = true;
                }
            }
            if (!rotationLocked && (rotationIndicator.X != 0f || rotationIndicator.Y != 0f))
            {
                if (m_barrel == null || SyncObject == null)
                {
                    //Mods may call this function incorrectly.
                    System.Diagnostics.Debug.Fail("Turret rotated when it was not initialized or functional!");
                    return;
                }

                //By Gregory: Maybe 2 dt(tick) computations are excessive? We only need one as a frame of reference
                m_rotationInterval_ms = (int)Math.Min(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 1000, MySandboxGame.TotalGamePlayTimeInMilliseconds - m_rotationInterval_ms);
                //m_elevationInterval_ms = (int)Math.Min(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 1000, MySandboxGame.TotalGamePlayTimeInMilliseconds - m_elevationInterval_ms);

                float slowDownCoeficient = 0.05f;

                //m_rotationSpeed should be fixed(from BlocDefinition)?
                float step = slowDownCoeficient * m_rotationSpeed * m_rotationInterval_ms;

                m_rotation -= rotationIndicator.Y * step;

                //step = slowDownCoeficient * m_elevationSpeed * m_elevationInterval_ms;

                m_elevation -= rotationIndicator.X * step;
                m_elevation = MathHelper.Clamp(m_elevation, m_barrel.BarrelElevationMin, MathHelper.PiOver2 - MathHelper.Pi / 180);

                RotateModels();

                m_rotationAndElevationSync.Value = new SyncRotationAndElevation(){Rotation = m_rotation,Elevation = m_elevation};

                m_rotationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                m_elevationInterval_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            }
        }

        public float HeadLocalXAngle { get; set; }
        public float HeadLocalYAngle { get; set; }

        public void MoveAndRotateStopped()
        {
            RotateModels();
        }

        /// <summary>
        /// This will be called locally to start shooting with the given action
        /// </summary>
        public void BeginShoot(MyShootActionEnum action)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnBeginShoot, action);
        }
        /// <summary>
        /// This will be called locally to start shooting with the given action
        /// </summary>
        public void EndShoot(MyShootActionEnum action)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnEndShoot, action);
        }

        /// <summary>
        /// This will be called back from the sync object both on local and remote clients
        /// </summary>
        /// 
        [Event,Reliable,Server,Broadcast]
        public void OnBeginShoot(MyShootActionEnum action)
        {
            m_isPlayerShooting = true;
        }
        /// <summary>
        /// This will be called back from the sync object both on local and remote clients
        /// </summary>
        /// 
        [Event, Reliable, Server, Broadcast]
        public void OnEndShoot(MyShootActionEnum action)
        {
            UpdateShooting(false);
            m_isPlayerShooting = false;
        }

        public void Use()
        {
            MyGuiAudio.PlaySound(MyGuiSounds.HudUse);
            MyMultiplayer.RaiseEvent(this, x => x.sync_ControlledEntity_Used);
        }
        public void UseContinues()
        {

        }
        public void UseFinished()
        {

        }
        public void PickUp()
        {

        }
        public void PickUpContinues()
        {

        }
        public void PickUpFinished()
        {

        }
        public void Sprint(bool enabled)
        {

        }
        public void Jump()
        {

        }
        public void SwitchWalk()
        {

        }
        public void Up()
        {

        }
        public void Crouch()
        {

        }
        public void Down()
        {

        }

        public void SwitchBroadcasting()
        {

        }

        //Duplicated code from MyRemoteControl. Needs refactoring
        public void ShowInventory()
        {
            var user = GetUser();
            if (user != null)
            {
                MyGuiScreenTerminal.Show(MyTerminalPageEnum.Inventory, user, this);
            }
        }

        public void ShowTerminal()
        {
            MyGuiScreenTerminal.Show(MyTerminalPageEnum.ControlPanel, MySession.Static.LocalCharacter, this);
        }

        public void SwitchThrusts()
        {

        }
        public void SwitchDamping()
        {

        }
        public void SwitchLights()
        {

        }
        public void SwitchLeadingGears()
        {

        }

        public bool EnabledThrusts
        {
            get { return false; }
        }

        public bool EnabledDamping
        {
            get { return false; }
        }

        public bool EnabledLights
        {
            get { return false; }
        }

        public bool EnabledLeadingGears
        {
            get { return false; }
        }

        public bool EnabledReactors
        {
            get { return false; }
        }

        public bool EnabledBroadcasting
        {
            get { return false; }
        }

        public bool EnabledHelmet
        {
            get { return false; }
        }

        public void SwitchToWeapon(MyDefinitionId weaponDefinition)
        {

        }

        public void SwitchToWeapon(MyToolbarItemWeapon weapon)
        {
        }

        public bool CanSwitchToWeapon(MyDefinitionId? weaponDefinition)
        {
            return false;
        }

        public void DrawHud(IMyCameraController camera, long playerId)
        {
            if (MyGuiScreenHudSpace.Static != null)
            {
                //Do not show toolbar component at all if in turret
                MyGuiScreenHudSpace.Static.SetToolbarVisible(false);
        }
        }

        public void SwitchReactors()
        {

        }

        public void SwitchHelmet()
        {

        }

        public void Die()
        {

        }

        public MyToolbarType ToolbarType
        {
            get
            {
                return MyToolbarType.LargeCockpit;
            }
        }

        public MyToolbar Toolbar
        {
            get
            {
                return m_toolbar;
            }
        }

        #endregion

        #endregion

        #region Draw


        protected void DrawLasers()
        {
            if (!MySandboxGame.IsDedicated && m_barrel != null && MyFakes.ENABLE_TURRET_LASERS)
            {
                MyGunStatusEnum gunStatus = MyGunStatusEnum.Cooldown;
                if (IsWorking)
                {
                    Vector4 colorWhite = Color.Green.ToVector4();
                    Vector4 color = colorWhite;
                    switch (GetStatus())
                    {
                        case MyLargeShipGunStatus.MyWeaponStatus_Searching:
                        case MyLargeShipGunStatus.MyWeaponStatus_Deactivated:
                            color = Color.Green.ToVector4();
                            break;
                        case MyLargeShipGunStatus.MyWeaponStatus_Shooting:
                        case MyLargeShipGunStatus.MyWeaponStatus_ShootDelaying:
                            color = Color.Red.ToVector4();
                            break;
                    }

                    var mat = "WeaponLaser";
                    float thickness = 0.1f;

                    Vector3D lineStart = Vector3D.Zero;
                    Vector3D lineEnd = Vector3D.Zero;

                    color *= 0.5f;

                    if (Target != null && !m_isPotentialTarget)
                    {
                        if (!CanShoot(out gunStatus))
                            color = 0.3f * Color.DarkRed.ToVector4();

                        lineStart = m_gunBase.GetMuzzleWorldMatrix().Translation + 2 * m_gunBase.GetMuzzleWorldMatrix().Forward;
                        var head = m_gunBase.GetMuzzleWorldMatrix();
                        var from = head.Translation;
                        var to = from + head.Forward * m_searchingRange;
                        var hitInfo = MyPhysics.CastRay(from, to);
                        if (!hitInfo.HasValue)
                            m_hitPosition = to;
                        else
                            m_hitPosition = hitInfo.Value.Position;
                        lineEnd = m_hitPosition;
                    }
                    else
                    {

                        var head = m_gunBase.GetMuzzleWorldMatrix();
                        m_hitPosition = head.Translation + head.Forward * m_laserLength;

                        lineStart = m_barrel.Entity.PositionComp.GetPosition();
                        lineEnd = m_hitPosition;
                    }

                    Vector3D cameraPosition = MySector.MainCamera.Position;
                    Vector3D closestPoint = MyUtils.GetClosestPointOnLine(ref lineStart, ref lineEnd, ref cameraPosition);
                    float distance = (float)MySector.MainCamera.GetDistanceFromPoint(closestPoint);

                    thickness *= Math.Min(distance, 10) * 0.05f;

                    MySimpleObjectDraw.DrawLine(lineStart, lineEnd, mat, ref color, thickness);
                }
            }
        }

        #endregion

        public void SwitchAmmoMagazine()
        {
            m_gunBase.SwitchAmmoMagazineToNextAvailable();
        }

        public bool CanSwitchAmmoMagazine()
        {
            return false;
        }

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);

            if (m_barrel != null)
            {
                m_barrel.WorldPositionChanged();
            }
        }

        #region IMyGunBaseUser
        MyEntity[] IMyGunBaseUser.IgnoreEntities
        {
            get { return m_shootIgnoreEntities; }
        }

        MyEntity IMyGunBaseUser.Weapon
        {
            get { return m_barrel != null ? m_barrel.Entity : null; }
        }

        MyEntity IMyGunBaseUser.Owner
        {
            get { return this.Parent; }
        }

        IMyMissileGunObject IMyGunBaseUser.Launcher
        {
            get { return null; }
        }

        MyInventory IMyGunBaseUser.AmmoInventory
        {
            get { return this.GetInventory(); }
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
            get { return this.OwnerId; }
        }

        string IMyGunBaseUser.ConstraintDisplayName
        {
            get { return base.BlockDefinition.DisplayNameText; }
        }
        #endregion

        private float NormalizeRange(float value)
        {
            if (value == 0)
                return 0;
            else
                return MathHelper.Clamp((value - m_minRangeMeter) / (m_maxRangeMeter - m_minRangeMeter), 0, 1);
        }

        private float DenormalizeRange(float value)
        {
            if (value == 0)
                return 0;
            else
                return MathHelper.Clamp(m_minRangeMeter + value * (m_maxRangeMeter - m_minRangeMeter), m_minRangeMeter, m_maxRangeMeter);
        }

        public override void TakeControlFromTerminal()
        {
            EnableIdleRotation = false;
            m_checkOtherTargets = false;
            if (Sync.IsServer)
            {
                m_rotationAndElevationSync.Value = new SyncRotationAndElevation(){Rotation = m_rotation,Elevation = m_elevation};
            }
        }

        public void ForceTarget(MyEntity entity, bool usePrediction)
        {
            this.Target = entity as MyEntity;
            m_currentPrediction = usePrediction ? m_targetPrediction : m_targetNoPrediction;
            m_checkOtherTargets = false;
        }

        public void TargetPosition(Vector3D pos, Vector3 velocity, bool usePrediction)
        {
            m_checkOtherTargets = false;
            Target = null;

            if (usePrediction)
            {
                m_currentPrediction = m_positionPrediction;

                var prediction = (m_currentPrediction as MyPositionPredictionType);
                prediction.TrackedPosition = pos;
                prediction.TrackedVelocity = velocity;
            }
            else
            {
                m_currentPrediction = m_positionNoPrediction;
                (m_currentPrediction as MyPositionNoPredictionType).TrackedPosition = pos;
            }
        }

        public void ChangeIdleRotation(bool enable)
        {
            EnableIdleRotation = enable;
        }

        [Event,Reliable,Server,Broadcast]
        public void ResetTargetParams()
        {
            this.Target = null;
            m_currentPrediction = m_targetPrediction;
            m_checkOtherTargets = true;
            EnableIdleRotation = BlockDefinition.IdleRotation;
        }

        public void SetManualAzimuth(float value)
        {
            if (m_rotation != value)
            {
                m_rotation = value;
                m_checkOtherTargets = false;
                Target = null;
                RotateModels();
            }
        }

        public void SetManualElevation(float value)
        {
            if (m_elevation != value)
            {
                m_elevation = value;
                m_checkOtherTargets = false;
                Target = null;
                RotateModels();
            }
        }

        private bool IsInRange(Vector3 lookAtPositionEuler)
        {
            float needRotation = lookAtPositionEuler.Y;
            float needElevation = lookAtPositionEuler.X;

            return (needRotation > m_minAzimuthRadians && needRotation < m_maxAzimuthRadians && needElevation > m_minElevationRadians && needElevation < m_maxAzimuthRadians);
        }

        public override bool CanOperate()
        {
            return CheckIsWorking();
        }

        public override void ShootFromTerminal(Vector3 direction)
        {
            m_barrel.StartShooting();
            m_checkOtherTargets = true;
        }

        public override void SyncRotationAndOrientation()
        {
             m_rotationAndElevationSync.Value = new SyncRotationAndElevation(){Rotation = m_rotation,Elevation = m_elevation};
        }

        protected override void RememberIdle()
        {
            m_previousIdleRotationState = EnableIdleRotation;
        }

        protected override void RestoreIdle()
        {
            EnableIdleRotation = m_previousIdleRotationState;
        }

        [Event, Reliable, Server]
        void RequestUseMessage(UseActionEnum useAction, long usedById)
        {
            MyEntity controlledEntity;
            bool entityExists = MyEntities.TryGetEntityById<MyEntity>(usedById, out controlledEntity);
            IMyControllableEntity controllableEntity = controlledEntity as IMyControllableEntity;
            Debug.Assert(controllableEntity != null, "Controllable entity needs to get control from another controllable entity");
            Debug.Assert(entityExists);

            UseActionResult useResult = UseActionResult.OK;

            if (entityExists && (useResult = (this as IMyUsableEntity).CanUse(useAction, controllableEntity)) == UseActionResult.OK)
            {
                MyMultiplayer.RaiseEvent(this, x => x.UseSuccessCallback, useAction, usedById, useResult);
                UseSuccessCallback(useAction, usedById, useResult);
            }
            else
            {
                MyMultiplayer.RaiseEvent(this, x => x.UseFailureCallback, useAction, usedById, useResult, MyEventContext.Current.Sender);
            }
        }

        [Event, Reliable, Broadcast]
        void UseSuccessCallback(UseActionEnum useAction, long usedById,UseActionResult useResult)
        {
            MyEntity controlledEntity;
            if (MyEntities.TryGetEntityById<MyEntity>(usedById, out controlledEntity))
            {
                var controllableEntity = controlledEntity as IMyControllableEntity;
                Debug.Assert(controllableEntity != null, "Controllable entity needs to get control from another controllable entity");

                if (controllableEntity != null)
                {
                    VRage.Game.MyRelationsBetweenPlayerAndBlock relation = VRage.Game.MyRelationsBetweenPlayerAndBlock.NoOwnership;
                    var cubeBlock = this as MyCubeBlock;
                    if (cubeBlock != null && controllableEntity.ControllerInfo.Controller != null)
                    {
                        relation = cubeBlock.GetUserRelationToOwner(controllableEntity.ControllerInfo.Controller.Player.Identity.IdentityId);
                    }

                    if (relation.IsFriendly())
                    {
                        sync_UseSuccess(useAction, controllableEntity);
                    }
                    else
                    {
                        sync_UseFailed(useAction, useResult, controllableEntity);
                    }
                }
            }
        }

        [Event, Reliable, Client]
        void UseFailureCallback(UseActionEnum useAction, long usedById, UseActionResult useResult)
        {
            MyEntity controlledEntity;
            bool userFound = MyEntities.TryGetEntityById<MyEntity>(usedById, out controlledEntity);
            Debug.Assert(userFound);
            IMyControllableEntity controllableEntity = controlledEntity as IMyControllableEntity;
            Debug.Assert(controllableEntity != null, "Controllable entity needs to get control from another controllable entity");
            sync_UseFailed(useAction, useResult, controllableEntity);
        }

        int IMyInventoryOwner.InventoryCount
        {
            get { return InventoryCount; }
        }

        IMyInventory IMyInventoryOwner.GetInventory(int index)
        {
            return this.GetInventory(index);
        }

        long IMyInventoryOwner.EntityId
        {
            get { return EntityId; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool IMyInventoryOwner.HasInventory
        {
            get { return HasInventory; }
        }

        public void UpdateSoundEmitter()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.Update();
        }
    }

}


