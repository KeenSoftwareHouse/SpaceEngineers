#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Decals;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Debris;
using Sandbox.Game.Lights;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Game.Weapons.Guns;
using Sandbox.Game.World;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using VRage.Audio;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Components;

#endregion

namespace Sandbox.Game
{  

    //  This tells us what exploded. It doesn't say what was hit, just what exploded so then explosion can be made for that exploded thing.
    internal enum MyExplosionTypeEnum : byte
    {
        MISSILE_EXPLOSION,          //  When missile explodes. By collision or by itself (e.g. timer).
        BOMB_EXPLOSION,             //  When mine/bomb explodes
        AMMO_EXPLOSION,
        // NEW
        GRID_DEFORMATION,
        GRID_DESTRUCTION,
        WARHEAD_EXPLOSION_02,
        WARHEAD_EXPLOSION_15,
        WARHEAD_EXPLOSION_30,
        WARHEAD_EXPLOSION_50, 
    }

    //  How will look explosion particles
    internal enum MyExplosionParticlesTypeEnum
    {
        EXPLOSIVE_AND_DIRTY,        //  When rocks (voxels or static asteroids) aare involved in explosion
        EXPLOSIVE_ONLY              //  When metalic only... usually just missile explosion
    }

    [Flags]
    internal enum MyExplosionFlags
    {
        CREATE_DEBRIS = 1 << 0,
        AFFECT_VOXELS = 1 << 1,
        APPLY_FORCE_AND_DAMAGE = 1 << 2,
        CREATE_DECALS = 1 << 3,
        FORCE_DEBRIS = 1 << 4,
        CREATE_PARTICLE_EFFECT = 1 << 5,
        CREATE_SHRAPNELS = 1 << 6,
        APPLY_DEFORMATION = 1 << 7,
    }

    internal struct MyExplosionInfo
    {
        public MyExplosionInfo(float playerDamage, float damage, BoundingSphereD explosionSphere, MyExplosionTypeEnum type, bool playSound, bool checkIntersection = true)
        {
            PlayerDamage = playerDamage;
            Damage = damage;
            ExplosionSphere = explosionSphere;
            StrengthImpulse = StrengthAngularImpulse = 0.0f;
            ExcludedEntity = OwnerEntity = HitEntity = null;
            CascadeLevel = 0;
            ExplosionFlags = MyExplosionFlags.AFFECT_VOXELS | MyExplosionFlags.APPLY_FORCE_AND_DAMAGE | MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_PARTICLE_EFFECT | MyExplosionFlags.APPLY_DEFORMATION;
            ExplosionType = type;
            LifespanMiliseconds = MyExplosionsConstants.EXPLOSION_LIFESPAN;
            ObjectsRemoveDelayInMiliseconds = 0;
            ParticleScale = 1.0f;
            VoxelCutoutScale = 1.0f;
            Direction = null;
            VoxelExplosionCenter = explosionSphere.Center;
            PlaySound = playSound;
            CheckIntersections = checkIntersection;
            Velocity = Vector3.Zero;
        }

        public float PlayerDamage;
        public float Damage;
        public BoundingSphereD ExplosionSphere;
        public float StrengthImpulse;
        public float StrengthAngularImpulse;
        public MyEntity ExcludedEntity;
        public MyEntity OwnerEntity;
        public MyEntity HitEntity;
        public int CascadeLevel;
        public MyExplosionFlags ExplosionFlags;
        public MyExplosionTypeEnum ExplosionType;
        public int LifespanMiliseconds;
        public int ObjectsRemoveDelayInMiliseconds;
        public float ParticleScale;
        public float VoxelCutoutScale;
        public Vector3? Direction;
        public Vector3D VoxelExplosionCenter;
        public bool PlaySound;
        public bool CheckIntersections;
        public Vector3 Velocity;


        private void SetFlag(MyExplosionFlags flag, bool value)
        {
            if (value) ExplosionFlags |= flag;
            else ExplosionFlags &= ~flag;
        }

        private bool HasFlag(MyExplosionFlags flag)
        {
            return (ExplosionFlags & flag) == flag;
        }

        public bool AffectVoxels { get { return HasFlag(MyExplosionFlags.AFFECT_VOXELS); } set { SetFlag(MyExplosionFlags.AFFECT_VOXELS, value); } }
        public bool CreateDebris { get { return HasFlag(MyExplosionFlags.CREATE_DEBRIS); } set { SetFlag(MyExplosionFlags.CREATE_DEBRIS, value); } }
        public bool ApplyForceAndDamage { get { return HasFlag(MyExplosionFlags.APPLY_FORCE_AND_DAMAGE); } set { SetFlag(MyExplosionFlags.APPLY_FORCE_AND_DAMAGE, value); } }
        public bool CreateDecals { get { return HasFlag(MyExplosionFlags.CREATE_DECALS); } set { SetFlag(MyExplosionFlags.CREATE_DECALS, value); } }
        public bool ForceDebris { get { return HasFlag(MyExplosionFlags.FORCE_DEBRIS); } set { SetFlag(MyExplosionFlags.FORCE_DEBRIS, value); } }
        public bool CreateParticleEffect { get { return HasFlag(MyExplosionFlags.CREATE_PARTICLE_EFFECT); } set { SetFlag(MyExplosionFlags.CREATE_PARTICLE_EFFECT, value); } }
        public bool CreateShrapnels { get { return HasFlag(MyExplosionFlags.CREATE_SHRAPNELS); } set { SetFlag(MyExplosionFlags.CREATE_SHRAPNELS, value); } }
    }

    internal struct MyDamageInfo
    {
        public bool GridWasHit;
        public MyExplosionDamage ExplosionDamage;
        public HashSet<MyCubeGrid> AffectedCubeGrids;
        public HashSet<MySlimBlock> AffectedCubeBlocks;
        public BoundingSphereD Sphere;
    }

    class MyExplosion
    {
        //  IMPORTANT: This class isn't realy inicialized by constructor, but by Start()
        //  So don't initialize members here, do it in Start()

        private static readonly MyProjectileAmmoDefinition SHRAPNEL_DATA;

        BoundingSphereD m_explosionSphere;
        MyLight m_light;
        public int ElapsedMiliseconds;
        private List<MyEntity> m_destroyHelper = new List<MyEntity>();

        private Vector3 m_velocity;
        private MyParticleEffect m_explosionEffect;

        HashSet<MySlimBlock> m_explodedBlocksInner = new HashSet<MySlimBlock>();
        HashSet<MySlimBlock> m_explodedBlocksExact = new HashSet<MySlimBlock>();
        HashSet<MySlimBlock> m_explodedBlocksOuter = new HashSet<MySlimBlock>();

        MyExplosionInfo m_explosionInfo;
        private bool m_explosionTriggered = false;

        Vector2[] m_explosionForceSlices = new Vector2[]
        {
            //radius multiplier, force multiplier
            new Vector2(0.8f, 1),
            new Vector2(1.0f, 0.5f),
            new Vector2(1.2f, 0.2f),
        };
        HashSet<MyEntity> m_pushedEntities = new HashSet<MyEntity>();

        private MyEntity3DSoundEmitter m_soundEmitter;

        //Use this bool to enable debug draw and better support for debugging explosions
        public static bool DEBUG_EXPLOSIONS = false;

        public MyExplosion()
        {
        }

        static MyExplosion()
        {
            // Hardcoded -> so far we dont want to let players modify the values
            SHRAPNEL_DATA = new MyProjectileAmmoDefinition();
            SHRAPNEL_DATA.DesiredSpeed = 100;
            SHRAPNEL_DATA.SpeedVar = 0;
            SHRAPNEL_DATA.MaxTrajectory = 1000;
            SHRAPNEL_DATA.ProjectileHitImpulse = 10.0f;
            SHRAPNEL_DATA.ProjectileMassDamage = 10;
            SHRAPNEL_DATA.ProjectileHealthDamage = 10;
            SHRAPNEL_DATA.ProjectileTrailColor = MyProjectilesConstants.GetProjectileTrailColorByType(MyAmmoType.HighSpeed);
            SHRAPNEL_DATA.AmmoType = MyAmmoType.HighSpeed;
            SHRAPNEL_DATA.ProjectileTrailScale = 0.1f;
            SHRAPNEL_DATA.ProjectileOnHitMaterialParticles = MyParticleEffects.GetCustomHitMaterialMethodById((int)MyCustomHitMaterialMethodType.Small);
            SHRAPNEL_DATA.ProjectileOnHitParticles = MyParticleEffects.GetCustomHitParticlesMethodById((int)MyCustomHitParticlesMethodType.BasicSmall);
        }

        public void Start(MyExplosionInfo explosionInfo)
        {
            MyDebug.AssertDebug(explosionInfo.ExplosionSphere.Radius > 0);

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyExplosion.Start");

            m_explosionInfo = explosionInfo;
            ElapsedMiliseconds = 0;

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        void StartInternal()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyExplosion.StartInternal");

            if (m_soundEmitter == null)
                m_soundEmitter = new MyEntity3DSoundEmitter(null);

            m_velocity = m_explosionInfo.Velocity;
            m_explosionSphere = m_explosionInfo.ExplosionSphere;

            if (m_explosionInfo.PlaySound)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Sound");
                //  Play explosion sound
                PlaySound();
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Light");
            //  Light of explosion
            m_light = MyLights.AddLight();
            if (m_light != null)
            {
                m_light.Start(MyLight.LightTypeEnum.PointLight, m_explosionSphere.Center, MyExplosionsConstants.EXPLOSION_LIGHT_COLOR, 1, Math.Min((float)m_explosionSphere.Radius * 8.0f, MyLightsConstants.MAX_POINTLIGHT_RADIUS));
                m_light.Intensity = 2.0f;
                m_light.Range = Math.Min((float)m_explosionSphere.Radius * 3.0f, MyLightsConstants.MAX_POINTLIGHT_RADIUS);

            } 
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            MyParticleEffectsIDEnum newParticlesType;

            switch (m_explosionInfo.ExplosionType)
            {
                case MyExplosionTypeEnum.MISSILE_EXPLOSION:
                    newParticlesType = MyParticleEffectsIDEnum.Explosion_Missile;
                    break;

                case MyExplosionTypeEnum.BOMB_EXPLOSION:
                    newParticlesType = MyParticleEffectsIDEnum.Explosion_Bomb;
                    break;

                case MyExplosionTypeEnum.AMMO_EXPLOSION:
                    newParticlesType = MyParticleEffectsIDEnum.Explosion_Ammo;
                    break;

                case MyExplosionTypeEnum.GRID_DEFORMATION:
                    newParticlesType = MyParticleEffectsIDEnum.Grid_Deformation;
                    break;

                case MyExplosionTypeEnum.GRID_DESTRUCTION:
                    newParticlesType = MyParticleEffectsIDEnum.Grid_Destruction;
                    break;

                case MyExplosionTypeEnum.WARHEAD_EXPLOSION_02:
                    newParticlesType = MyParticleEffectsIDEnum.Explosion_Warhead_02;
                    break;

                case MyExplosionTypeEnum.WARHEAD_EXPLOSION_15:
                    newParticlesType = MyParticleEffectsIDEnum.Explosion_Warhead_15;
                    break;

                case MyExplosionTypeEnum.WARHEAD_EXPLOSION_30:
                    newParticlesType = MyParticleEffectsIDEnum.Explosion_Warhead_30;
                    break;

                case MyExplosionTypeEnum.WARHEAD_EXPLOSION_50:
                    newParticlesType = MyParticleEffectsIDEnum.Explosion_Warhead_50;
                    break;

                default:
                    throw new System.NotImplementedException();
                    break;
            }

            if (m_explosionInfo.CreateParticleEffect)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Particles");


                //  Explosion particles
                GenerateExplosionParticles(newParticlesType, m_explosionSphere, m_explosionInfo.ParticleScale);


                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

            m_explosionTriggered = false;

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        private void PlaySound()
        {
            MySoundPair cueEnum = GetCueByExplosionType(m_explosionInfo.ExplosionType);
            m_soundEmitter.SetPosition(m_explosionSphere.Center);
            m_soundEmitter.SetVelocity(Vector3.Zero);
            //m_soundEmitter.Entity = m_explosionInfo.OwnerEntity;
            m_soundEmitter.PlaySingleSound(cueEnum, true);
            if (m_explosionInfo.HitEntity == MySession.ControlledEntity)
                MyAudio.Static.PlaySound(m_explPlayer.SoundId);
        }

        private void RemoveDestroyedObjects()
        {
            Debug.Assert(Sync.IsServer);
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("RemoveDestroyedObjects");
            bool gridWasHit = false;

            if (m_explosionInfo.Damage > 0)
            {
                BoundingSphereD influenceExplosionSphere = m_explosionSphere;
                influenceExplosionSphere.Radius *= MyExplosionsConstants.EXPLOSION_RADIUS_MULTIPLIER_FOR_IMPULSE;
                for (int i = 0; i < m_explosionInfo.CascadeLevel; i++)
                {
                    influenceExplosionSphere.Radius *= MyExplosionsConstants.EXPLOSION_CASCADE_FALLOFF;
                }

                List<MyEntity> entities = MyEntities.GetEntitiesInSphere(ref influenceExplosionSphere);

                //TODO ALEX FLOREA: Remove non-volumetric explosions after testing is done
                if (MyFakes.ENABLE_VOLUMETRIC_EXPLOSION)
                {
                    ApplyVolumetricExplosion(ref m_explosionInfo, ref influenceExplosionSphere, entities);
                }
                else
                {
                    ApplyExplosion(ref m_explosionInfo, ref influenceExplosionSphere, entities);
                }
                //  Look for objects in explosion radius
                //BoundingBox boundingBox;
                //BoundingBox.CreateFromSphere(ref influenceExplosionSphere, out boundingBox);

                //if (explosionInfo.CreateDecals && explosionInfo.Direction.HasValue && explosionInfo.EmpDamage == 0)
                //{
                //    CreateDecals(explosionInfo.Direction.Value);
                //}
            } // Damage > 0


            if (gridWasHit && m_explosionInfo.CreateShrapnels)
            {
                for (int i = 0; i < 10; i++)
                {
                    MyProjectiles.AddShrapnel(
                        SHRAPNEL_DATA,
                        (m_explosionInfo.HitEntity is MyWarhead) ? m_explosionInfo.HitEntity : null, // do not play bullet sound when coming from warheads
                        m_explosionSphere.Center,
                        Vector3.Zero,
                        MyUtils.GetRandomVector3Normalized(),
                        false,
                        1.0f,
                        1.0f,
                        null);
                }
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        private bool ApplyExplosion(ref MyExplosionInfo m_explosionInfo, ref BoundingSphereD influenceExplosionSphere, List<MyEntity> entities)
        {
            bool gridWasHit = false;

            ApplyExplosionOnVoxel(ref m_explosionInfo);

            ApplyExplosionOnEntities(ref m_explosionInfo, entities);

            if (((m_explosionInfo.ExplosionFlags & MyExplosionFlags.APPLY_DEFORMATION) == MyExplosionFlags.APPLY_DEFORMATION))
            {
                gridWasHit = ApplyExplosionOnGrid(ref m_explosionInfo, ref influenceExplosionSphere, entities);
            }

            entities.Clear();


            //  Throws surrounding objects away from centre of the explosion.
            if (m_explosionInfo.ApplyForceAndDamage)
            {
                m_explosionInfo.StrengthImpulse = MyExplosionsConstants.EXPLOSION_STRENGTH_IMPULSE * (float)m_explosionSphere.Radius;
                m_explosionInfo.StrengthAngularImpulse = MyExplosionsConstants.EXPLOSION_STRENGTH_ANGULAR_IMPULSE;
                m_explosionInfo.HitEntity = m_explosionInfo.HitEntity != null ? m_explosionInfo.HitEntity.GetBaseEntity() : null;

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ApplyExplosionForceAndDamage");
                //MyEntities.ApplyExplosionForceAndDamage(ref explosionInfo);


                for (int s = 0; s < m_explosionForceSlices.Length; s++)
                {
                    BoundingSphereD forceImpactSphere = m_explosionSphere;
                    forceImpactSphere.Radius *= m_explosionForceSlices[s].X;
                    entities = MyEntities.GetEntitiesInSphere(ref forceImpactSphere);

                    foreach (var entity in entities)
                    {
                        if (!m_pushedEntities.Contains(entity))
                        {
                            if (entity.PositionComp.WorldAABB.Center != m_explosionSphere.Center)
                            {
                                if (entity.Physics != null && entity.Physics.Enabled)
                                {
                                    entity.Physics.AddForce(
                                        MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                                        m_explosionForceSlices[s].Y * m_explosionInfo.StrengthImpulse * Vector3.Normalize(entity.PositionComp.WorldAABB.Center - m_explosionSphere.Center),
                                        entity.PositionComp.WorldAABB.Center,
                                        null);
                                }
                            }
                            m_pushedEntities.Add(entity);

                            if (entity.Physics != null && entity.Physics.Enabled)
                            {
                                var character = entity as Sandbox.Game.Entities.Character.MyCharacter;
                                if (character != null && !(character.IsUsing is MyCockpit))
                                {
                                    character.DoDamage(m_explosionInfo.Damage * m_explosionForceSlices[s].Y, MyDamageType.Explosion, true);
                                }

                                var ammoBase = entity as MyAmmoBase;
                                if (ammoBase != null)
                                {
                                    if (Vector3.DistanceSquared(m_explosionSphere.Center, ammoBase.PositionComp.GetPosition()) < 4 * m_explosionSphere.Radius * m_explosionSphere.Radius)
                                    {
                                        ammoBase.MarkedToDestroy = true;
                                    }
                                    ammoBase.Explode();
                                }
                            }
                        }
                    }

                    entities.Clear();
                }

                m_pushedEntities.Clear();
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }
            return gridWasHit;
        }

        private bool ApplyVolumetricExplosion(ref MyExplosionInfo m_explosionInfo, ref BoundingSphereD influenceExplosionSphere, List<MyEntity> entities)
        {
            bool gridWasHit = false;

            ApplyExplosionOnVoxel(ref m_explosionInfo);

            var damageInfo = ApplyVolumetricExplosionOnGrid(ref m_explosionInfo, ref influenceExplosionSphere, entities);
            if ((m_explosionInfo.ExplosionFlags & MyExplosionFlags.APPLY_DEFORMATION) == MyExplosionFlags.APPLY_DEFORMATION)
            {
                damageInfo.ExplosionDamage.ComputeDamagedBlocks();
                gridWasHit = damageInfo.GridWasHit;
                ApplyVolumetriDamageToGrid(damageInfo);
            }

            if (m_explosionInfo.HitEntity is MyWarhead)
            {
                var warhead = (m_explosionInfo.HitEntity as MyWarhead).SlimBlock;
                if (!warhead.CubeGrid.BlocksDestructionEnabled)
                {
                    warhead.CubeGrid.RemoveDestroyedBlock(warhead);
                    foreach (var neighbour in warhead.Neighbours)
                    {
                        neighbour.CubeGrid.Physics.AddDirtyBlock(neighbour);
                    }
                    warhead.CubeGrid.Physics.AddDirtyBlock(warhead);
                }
            }
            ApplyVolumetricExplosionOnEntities(ref m_explosionInfo, entities, damageInfo);

            entities.Clear();
            return gridWasHit;
        }

        private void ApplyExplosionOnEntities(ref MyExplosionInfo m_explosionInfo,List<MyEntity> entities)
        {
            Debug.Assert(Sync.IsServer);
            foreach (var entity in entities)
            {
                if (!(entity is IMyDestroyableObject))
                    continue;
                float damage;
                if (entity is MyCharacter)
                    damage = m_explosionInfo.PlayerDamage;
                else
                    damage = m_explosionInfo.Damage;
                if (damage == 0)
                    continue;
                var destroyableObj = entity as IMyDestroyableObject;
                destroyableObj.DoDamage(damage, MyDamageType.Explosion, true);
            }
        }

        private void ApplyVolumetricExplosionOnEntities(ref MyExplosionInfo m_explosionInfo, List<MyEntity> entities, MyDamageInfo explosionDamageInfo)
        {
            Debug.Assert(Sync.IsServer);
            foreach (var entity in entities)
            {
                //Special case for characters in cockpits
                var character = entity as MyCharacter;
                if (character != null)
                {
                    var cockpit = character.IsUsing as MyCockpit;
                    if (cockpit != null)
                    {
                        if (explosionDamageInfo.ExplosionDamage.DamagedBlocks.ContainsKey(cockpit.SlimBlock))
                        {
                            float damageRemaining = explosionDamageInfo.ExplosionDamage.DamageRemaining[cockpit.SlimBlock].DamageRemaining;
                            character.DoDamage(damageRemaining, MyDamageType.Explosion, true);
                        }
                        continue;
                    }
                }

                var raycastDamageInfo = explosionDamageInfo.ExplosionDamage.ComputeDamageForEntity(entity.PositionComp.WorldAABB.Center);
                float damage = raycastDamageInfo.DamageRemaining;

                float entityDistanceToExplosion = (float)(entity.PositionComp.WorldAABB.Center - explosionDamageInfo.Sphere.Center).Length();
                damage *= (float)(1 - (entityDistanceToExplosion - raycastDamageInfo.DistanceToExplosion) / (explosionDamageInfo.Sphere.Radius - raycastDamageInfo.DistanceToExplosion));

                if (damage <= 0f)
                    continue;

                if (entity.Physics != null && entity.Physics.Enabled)
                {
                    var ammoBase = entity as MyAmmoBase;
                    if (ammoBase != null)
                    {
                        if (Vector3.DistanceSquared(m_explosionSphere.Center, ammoBase.PositionComp.GetPosition()) < 4 * m_explosionSphere.Radius * m_explosionSphere.Radius)
                        {
                            ammoBase.MarkedToDestroy = true;
                        }
                        ammoBase.Explode();
                    }

                    float forceMultiplier = damage / 50000f;

                    //  Throws surrounding objects away from centre of the explosion.
                    if (m_explosionInfo.ApplyForceAndDamage)
                    {
                        m_explosionInfo.StrengthImpulse = MyExplosionsConstants.EXPLOSION_STRENGTH_IMPULSE * (float)m_explosionSphere.Radius;
                        m_explosionInfo.StrengthAngularImpulse = MyExplosionsConstants.EXPLOSION_STRENGTH_ANGULAR_IMPULSE;
                        m_explosionInfo.HitEntity = m_explosionInfo.HitEntity != null ? m_explosionInfo.HitEntity.GetBaseEntity() : null;

                        VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ApplyExplosionForceAndDamage");

                        if (entity.PositionComp.WorldAABB.Center != m_explosionSphere.Center)
                        {
                            entity.Physics.AddForce(
                                MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                                forceMultiplier * m_explosionInfo.StrengthImpulse * Vector3.Normalize(entity.PositionComp.WorldAABB.Center - m_explosionSphere.Center),
                                entity.PositionComp.WorldAABB.Center,
                                null);
                        }

                        VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                    }
                }

                if (!(entity is IMyDestroyableObject))
                    continue;
                
                var destroyableObj = entity as IMyDestroyableObject;

                destroyableObj.DoDamage(damage, MyDamageType.Explosion, true);
            }
        }

        void ApplyExplosionOnVoxel(ref MyExplosionInfo explosionInfo)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ApplyExplosionOnVoxel");

            if (explosionInfo.Damage > 0)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Voxel or collision");

                //  If explosion sphere intersects a voxel map, we need to cut out a sphere, spawn debrises, etc
                MyVoxelBase voxelMap = explosionInfo.AffectVoxels ? MySession.Static.VoxelMaps.GetOverlappingWithSphere(ref m_explosionSphere) : null;
                if (voxelMap != null)
                {
                    bool createDebris = true; // We want to create debris

                    if (explosionInfo.HitEntity != null) // but not when we hit prefab
                    {
                        createDebris &= explosionInfo.HitEntity is MyVoxelBase;
                    }

                    createDebris &= explosionInfo.CreateDebris && (createDebris || explosionInfo.ForceDebris);

                    CutOutVoxelMap((float)m_explosionSphere.Radius * explosionInfo.VoxelCutoutScale, explosionInfo.VoxelExplosionCenter, voxelMap, createDebris);

                    //Sync
                    voxelMap.GetSyncObject.RequestVoxelCutoutSphere(explosionInfo.VoxelExplosionCenter, (float)m_explosionSphere.Radius * explosionInfo.VoxelCutoutScale, createDebris);
                }
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void CutOutVoxelMap(float radius, Vector3D center, MyVoxelBase voxelMap, bool createDebris)
        {
            MyVoxelMaterialDefinition voxelMaterial = null;
            float voxelContentRemovedInPercent = 0;

            //cut off
            var sphereShape = new MyShapeSphere()
            {
                Center = center,
                Radius = radius
            };
            MyVoxelGenerator.CutOutShapeWithProperties(voxelMap, sphereShape, out voxelContentRemovedInPercent, out voxelMaterial, null, false);

            //  Only if at least something was removed from voxel map
            //  If voxelContentRemovedInPercent is more than zero than also voxelMaterial shouldn't be null, but I rather check both of them.
            if ((voxelContentRemovedInPercent > 0) && (voxelMaterial != null))
            {
                BoundingSphereD voxelExpSphere = new BoundingSphereD(center, radius);

                //remove decals
                MyDecals.HideTrianglesAfterExplosion(voxelMap, ref voxelExpSphere);

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("CreateDebris");

                if (createDebris && MyRenderConstants.RenderQualityProfile.ExplosionDebrisCountMultiplier > 0)
                {
                    //  Create debris rocks thrown from the explosion
                    //  This must be called before ApplyExplosionForceAndDamage (because there we apply impulses to the debris)
                    MyDebris.Static.CreateExplosionDebris(ref voxelExpSphere, voxelContentRemovedInPercent, voxelMaterial, voxelMap);
                }

                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("CreateParticleEffect");

                if (createDebris)
                {
                    MyParticleEffect explosionEffect;
                    if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.MaterialExplosion_Destructible, out explosionEffect))
                    {
                        explosionEffect.WorldMatrix = MatrixD.CreateTranslation(voxelExpSphere.Center);
                        explosionEffect.UserRadiusMultiplier = (float)voxelExpSphere.Radius;
                    }
                }

                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }
        }

        bool ApplyExplosionOnGrid(ref MyExplosionInfo explosionInfo, ref BoundingSphereD sphere, List<MyEntity> entities)
        {
            Debug.Assert(Sandbox.Game.Multiplayer.Sync.IsServer, "This is supposed to be only server method");
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ApplyExplosionOnGrid");

            bool gridWasHit = false;
            foreach (var entity in entities)
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                if (grid != null && grid.BlocksDestructionEnabled)
                {
                    var detectionHalfSize = grid.GridSize / 2 / 1.25f;
                    var invWorldGrid = MatrixD.Invert(grid.WorldMatrix);

                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GetBlocksInsideSpheres");
                    BoundingSphereD innerSphere = new BoundingSphereD(sphere.Center, (float)Math.Max(0.1f, sphere.Radius - grid.GridSize));
                    BoundingSphereD exactSphere = new BoundingSphereD(sphere.Center, sphere.Radius);
                    BoundingSphereD outerSphere = new BoundingSphereD(sphere.Center, sphere.Radius + grid.GridSize * 0.5f * (float)Math.Sqrt(3));
                    grid.GetBlocksInsideSpheres(
                        ref innerSphere,
                        ref exactSphere,
                        ref outerSphere,
                        m_explodedBlocksInner,
                        m_explodedBlocksExact,
                        m_explodedBlocksOuter, 
                        true, detectionHalfSize, ref invWorldGrid);
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("foreach (var cubeBlock in m_explodedBlocks)");
                    foreach (var cubeBlock in m_explodedBlocksInner)
                    {
                        if (cubeBlock.FatBlock != null && cubeBlock.FatBlock.MarkedForClose)
                                continue;

                        if (cubeBlock.FatBlock != null)
                        {
                            grid.RemoveBlock(cubeBlock, updatePhysics: true);
                        }
                        else
                        {
                            grid.RemoveDestroyedBlock(cubeBlock);
                            grid.Physics.AddDirtyBlock(cubeBlock);
                        }
                    }
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GetBlocksInsideSphere outerSphere");
                    
                    foreach (var cubeBlock in m_explodedBlocksExact)
                    {
                        if ((cubeBlock.FatBlock != null) && cubeBlock.FatBlock.MarkedForClose)
                            continue;

                        if ((cubeBlock.FatBlock != null) && cubeBlock.FatBlock.MarkedToExplode)
                        {
                            grid.RemoveBlock(cubeBlock, updatePhysics: true);
                        }
                        else// if (MyVRageUtils.GetRandomFloat(0, 2) > 1)
                        {
                            grid.ApplyDestructionDeformation(cubeBlock);
                            grid.Physics.AddDirtyBlock(cubeBlock);
                        }
                        //else if (cubeBlock.FatBlock is IMyExplosiveObject)
                        //{
                        //    cubeBlock.OnDestroy();
                        //}
                    }
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("outerSphere2");

                    foreach (var cubeBlock in m_explodedBlocksOuter)
                    {
                        grid.Physics.AddDirtyBlock(cubeBlock);
                    }

                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                    m_explodedBlocksInner.Clear();
                    m_explodedBlocksExact.Clear();
                    m_explodedBlocksOuter.Clear();

                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("AddDirtyArea");
                    
                    //Vector3 cornerVec = new Vector3(sphere.Radius + grid.GridSize / (float)grid.Skeleton.BoneDensity);
                    //BoundingBox box = new BoundingBox(sphere.Center - cornerVec, sphere.Center + cornerVec);

                    //var invee = Matrix.Invert(grid.WorldMatrix);
                    //box = box.Transform(ref invee);
                    //Vector3 min = box.Min;
                    //Vector3 max = box.Max;
                    //Vector3I start = new Vector3I((int)Math.Round(min.X / grid.GridSize), (int)Math.Round(min.Y / grid.GridSize), (int)Math.Round(min.Z / grid.GridSize));
                    //Vector3I end = new Vector3I((int)Math.Round(max.X / grid.GridSize), (int)Math.Round(max.Y / grid.GridSize), (int)Math.Round(max.Z / grid.GridSize));

                    //grid.Physics.AddDirtyArea(start, end);
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("UpdateDirty");
                    grid.UpdateDirty();
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("CreateExplosionDebris");
                    if (m_explosionInfo.HitEntity == grid)
                    {
                        BoundingBoxD aabb = BoundingBoxD.CreateFromSphere(new BoundingSphereD(sphere.Center, sphere.Radius * 1.5f));
                        MyDebris.Static.CreateExplosionDebris(ref sphere, grid, ref aabb, 0.5f, false);
                    }

                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                    gridWasHit = true;
                }
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            return gridWasHit;
        }

        MyDamageInfo ApplyVolumetricExplosionOnGrid(ref MyExplosionInfo explosionInfo, ref BoundingSphereD sphere, List<MyEntity> entities)
        {
            Debug.Assert(Sandbox.Game.Multiplayer.Sync.IsServer, "This is supposed to be only server method");
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ApplyExplosionOnGrid");

            bool gridWasHit = false;

            HashSet<MySlimBlock> explodedBlocks = new HashSet<MySlimBlock>();
            Dictionary<MySlimBlock, float> damagedBlocks = new Dictionary<MySlimBlock, float>();
            HashSet<MyCubeGrid> explodedGrids = new HashSet<MyCubeGrid>();

            foreach (var entity in entities)
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                if (grid != null && grid.CreatePhysics)
                {
                    explodedGrids.Add(grid);

                    var detectionHalfSize = grid.GridSize / 2 / 1.25f;
                    var invWorldGrid = MatrixD.Invert(grid.WorldMatrix);

                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GetBlocksInsideSpheres");
                    BoundingSphereD innerSphere = new BoundingSphereD(sphere.Center, (float)Math.Max(0.1f, sphere.Radius - grid.GridSize));
                    BoundingSphereD exactSphere = new BoundingSphereD(sphere.Center, sphere.Radius);
                    BoundingSphereD outerSphere = new BoundingSphereD(sphere.Center, sphere.Radius + grid.GridSize * 0.5f * (float)Math.Sqrt(3));
                    grid.GetBlocksInsideSpheres(
                        ref innerSphere,
                        ref exactSphere,
                        ref outerSphere,
                        m_explodedBlocksInner,
                        m_explodedBlocksExact,
                        m_explodedBlocksOuter,
                        false, detectionHalfSize, ref invWorldGrid);
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                    m_explodedBlocksInner.UnionWith(m_explodedBlocksExact);

                    explodedBlocks.UnionWith(m_explodedBlocksInner);

                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("outerSphere2");

                    foreach (var cubeBlock in m_explodedBlocksOuter)
                    {
                        grid.Physics.AddDirtyBlock(cubeBlock);
                    }

                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                    m_explodedBlocksInner.Clear();
                    m_explodedBlocksExact.Clear();
                    m_explodedBlocksOuter.Clear();

                    gridWasHit = true;
                }
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            var damage = new MyExplosionDamage(explodedBlocks, sphere, explosionInfo.Damage);
            

            var damageInfo = new MyDamageInfo
            {
                GridWasHit = gridWasHit,
                ExplosionDamage = damage,
                AffectedCubeBlocks = explodedBlocks,
                AffectedCubeGrids = explodedGrids,
                Sphere = sphere
            };

            return damageInfo;
        }

        private void ApplyVolumetriDamageToGrid(MyDamageInfo damageInfo)
        {
            var damagedBlocks = damageInfo.ExplosionDamage.DamagedBlocks;
            var explodedBlocks = damageInfo.AffectedCubeBlocks;
            var explodedGrids = damageInfo.AffectedCubeGrids;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("foreach (var damagedBlock in damagedBlocks)");

            if (MyDebugDrawSettings.DEBUG_DRAW_VOLUMETRIC_EXPLOSION_COLORING)
            {
                foreach (var explodedBlock in explodedBlocks)
                {
                    explodedBlock.CubeGrid.ChangeColor(explodedBlock, new Vector3(0.66f, 1f, 1f));
                }
                foreach (var damagedBlock in damagedBlocks)
                {
                    float hue = 1f - damagedBlock.Value / damageInfo.ExplosionDamage.Damage;
                    damagedBlock.Key.CubeGrid.ChangeColor(damagedBlock.Key, new Vector3(hue / 3f, 1.0f, 0.5f));
                }
            }
            else
            {
                foreach (var damagedBlock in damagedBlocks)
                {
                    var cubeBlock = damagedBlock.Key;
                    if (cubeBlock.FatBlock != null && cubeBlock.FatBlock.MarkedForClose)
                        continue;

                    if (!cubeBlock.CubeGrid.BlocksDestructionEnabled)
                        continue;

                    if (cubeBlock.FatBlock == null && cubeBlock.Integrity / cubeBlock.DeformationRatio < damagedBlock.Value)
                    {
                        VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("RemoveBlock");
                        cubeBlock.CubeGrid.RemoveDestroyedBlock(cubeBlock);
                        VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                    }
                    else
                    {
                        VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("ApplyDestructionDeformation");
                        float damage = damagedBlock.Value;
                        if (cubeBlock.FatBlock != null)
                        {
                            damage *= 7f;
                        }
                        (cubeBlock as IMyDestroyableObject).DoDamage(damage, MyDamageType.Explosion, true);
                        if (!cubeBlock.IsDestroyed)
                        {
                            cubeBlock.CubeGrid.ApplyDestructionDeformation(cubeBlock);
                        }
                        VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                    }

                    foreach (var neighbour in cubeBlock.Neighbours)
                    {
                        neighbour.CubeGrid.Physics.AddDirtyBlock(neighbour);
                    }
                    cubeBlock.CubeGrid.Physics.AddDirtyBlock(cubeBlock);
                }
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            var sphere = damageInfo.Sphere;
            if (!MyDebugDrawSettings.DEBUG_DRAW_VOLUMETRIC_EXPLOSION_COLORING)
            {
                foreach (var grid in explodedGrids)
                {
                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("UpdateDirty");
                    grid.UpdateDirty();
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("CreateExplosionDebris");
                    if (m_explosionInfo.HitEntity == grid)
                    {
                        BoundingBoxD aabb = BoundingBoxD.CreateFromSphere(new BoundingSphereD(sphere.Center, sphere.Radius * 1.5f));
                        MyDebris.Static.CreateExplosionDebris(ref sphere, grid, ref aabb, 0.5f, false);
                    }

                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                }
            }
        }

        static MySoundPair m_explPlayer = new MySoundPair("WepExplOnPlay");
        static MySoundPair m_smMissileShip = new MySoundPair("WepSmallMissileExplShip");
        static MySoundPair m_smMissileExpl = new MySoundPair("WepSmallMissileExpl");
        static MySoundPair m_bombExpl = new MySoundPair("WepBombExplosion");
        static MySoundPair m_missileExpl = new MySoundPair("WepMissileExplosion");

        MySoundPair GetCueByExplosionType(MyExplosionTypeEnum explosionType)
        {
            MySoundPair cueEnum = null;
            switch (explosionType)
            {
                case MyExplosionTypeEnum.MISSILE_EXPLOSION:
                    {
                        {
                            bool found = false;
                            if (m_explosionInfo.HitEntity is MyCubeGrid)
                            {
                                MyCubeGrid grid = m_explosionInfo.HitEntity as MyCubeGrid;
                                foreach (var slimBlock in grid.GetBlocks())
                                {
                                    if (slimBlock.FatBlock is MyCockpit)
                                    {
                                        MyCockpit cockpit = slimBlock.FatBlock as MyCockpit;
                                        if (cockpit.Pilot == MySession.ControlledEntity)
                                        {
                                            // player's ship hit
                                            cueEnum = m_smMissileShip;
                                            found = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!found)
                                // any other hit or no hit
                                cueEnum = m_smMissileExpl;
                        }

                        break;
                    }
                case MyExplosionTypeEnum.BOMB_EXPLOSION:
                    {
                        cueEnum = m_bombExpl;
                        break;
                    }
                default:
                    {
                        cueEnum = m_missileExpl;
                        break;
                    }
            }

            return cueEnum;
        }

        private Vector4 GetSmutDecalRandomColor()
        {
            float randomColor = MyUtils.GetRandomFloat(0.2f, 0.3f);
            return new Vector4(randomColor, randomColor, randomColor, 1);
        }

        //  We have only Update method for explosions, because drawing of explosion is mantained by particles and lights itself
        public bool Update()
        {
            if (ElapsedMiliseconds == 0)
            {
                StartInternal();
            }

            ElapsedMiliseconds += MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;

            if (ElapsedMiliseconds > m_explosionInfo.ObjectsRemoveDelayInMiliseconds)
            {
                if (Sandbox.Game.Multiplayer.Sync.IsServer)
                {
                    RemoveDestroyedObjects();
                }
                m_explosionInfo.ObjectsRemoveDelayInMiliseconds = int.MaxValue;
                m_explosionTriggered = true;
            }

            //if ((m_sound != null) && m_sound.IsPlaying)
            //{
            //    ////occlusions are disabled;
            //    //MyAudio.Static.CalculateOcclusion(m_sound, m_explosionSphere.Center);
            //}
            
            if (m_light != null)
            {
                float normalizedTimeElapsed = 1 - (float)ElapsedMiliseconds / (float)m_explosionInfo.LifespanMiliseconds;

                m_light.Intensity = 2 * normalizedTimeElapsed;
            }

            if (m_explosionEffect != null)
            {
                m_explosionSphere.Center += m_velocity * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                m_explosionEffect.WorldMatrix = CalculateEffectMatrix(m_explosionSphere);
            }
            //Added flag because explosion particles are not played reliably on DS
            else if (ElapsedMiliseconds >= m_explosionInfo.LifespanMiliseconds && m_explosionTriggered)
            {
                if (DEBUG_EXPLOSIONS)
                {
                    return true;
                }
                else
                {
                    Close();
                    return false;
                }
            }

            if (m_explosionEffect != null)
                return !m_explosionEffect.IsStopped;

            return true;
        }

        public void DebugDraw()
        {
            if (DEBUG_EXPLOSIONS)
            {
                if (m_light != null) //to appear after StartInternal
                {
                  //  BoundingSphere boundingSphere = new BoundingSphere(m_explosionInfo.HitEntity is MyVoxelMap ? m_explosionInfo.VoxelExplosionCenter : m_explosionInfo.ExplosionSphere.Center, m_explosionInfo.ExplosionSphere.Radius);
                 //   VRageRender.MyRenderProxy.DebugDrawSphere(boundingSphere.Center, boundingSphere.Radius, Color.Red.ToVector3(), 1, false);
                }
            }
        }

        public void Close()
        {
            if (m_light != null)
            {
                MyLights.RemoveLight(m_light);
                m_light = null;
            }

            m_soundEmitter.StopSound(true);
        }

        MatrixD CalculateEffectMatrix(BoundingSphereD explosionSphere)
        {
            Vector3D dirToCamera = MySector.MainCamera.Position - explosionSphere.Center;

            if (MyUtils.IsZero(dirToCamera))
                dirToCamera = MySector.MainCamera.ForwardVector;
            else
                dirToCamera = MyUtils.Normalize(dirToCamera);

            //  Move explosion particles in the direction of camera, so we won't see billboards intersecting the large ship
            return MatrixD.CreateTranslation(m_explosionSphere.Center + dirToCamera * 0.9f);
        }

        //  Generate explosion particles. These will be smoke, explosion and some polyline particles.
        void GenerateExplosionParticles(MyParticleEffectsIDEnum newParticlesType, BoundingSphere explosionSphere, float particleScale)
        {
            if (MyParticlesManager.TryCreateParticleEffect((int)newParticlesType, out m_explosionEffect))
            {
                m_explosionEffect.OnDelete += delegate
                {
                    m_explosionInfo.LifespanMiliseconds = 0;
                    m_explosionEffect = null;
                };
                m_explosionEffect.WorldMatrix = CalculateEffectMatrix(explosionSphere);
                //explosionEffect.UserRadiusMultiplier = tempExplosionSphere.Radius;
                m_explosionEffect.UserScale = particleScale;
            }

           // m_lifespanInMiliseconds = Math.Max(m_lifespanInMiliseconds, (int)(m_explosionEffect.GetLength() * 1000));
        }
    }
}