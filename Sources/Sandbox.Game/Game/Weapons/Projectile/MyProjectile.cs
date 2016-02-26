﻿using System;
using Sandbox.Common;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Utils;
using Sandbox.Game.World;
using VRage.Utils;
using VRageMath;

using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.Multiplayer;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.Game.Components;
using System.Collections.Generic;
using VRage.Game.Models;
using VRage.Game.Entity;
using VRage.Game;

namespace Sandbox.Game.Weapons
{

    public delegate void MyCustomHitParticlesMethod(ref Vector3D hitPoint, ref Vector3 normal, ref Vector3D direction, IMyEntity entity, MyEntity weapon, float scale, MyEntity ownerEntity = null);
    public delegate void MyCustomHitMaterialMethod(ref Vector3D hitPoint, ref Vector3 normal, ref Vector3D direction, IMyEntity entity, MySurfaceImpactEnum surfaceImpact, MyEntity weapon, float scale);

    public enum MySurfaceImpactEnum
    {
        METAL,
        DESTRUCTIBLE,
        INDESTRUCTIBLE,
        CHARACTER
    }

    // One projectile eats 700 B of memory (316 directly, rest in members)
    // Imho quite a lot
    class MyProjectile
    {
        //  Projectiles are killed in two states. First we get collision/timeout in update, but still need to draw
        //  trail polyline, so we can't remove it from buffer. Second state is after 'killed' projectile is drawn
        //  and only then we remove it from buffer.
        enum MyProjectileStateEnum : byte
        {
            ACTIVE,
            KILLED,
            KILLED_AND_DRAWN
        }

        const int CHECK_INTERSECTION_INTERVAL = 5; //projectile will check for intersection each n-th frame with n*longer line

        //  IMPORTANT: This class isn't realy inicialized by constructor, but by Start()
        //  So don't initialize members here, do it in Start()

        MyProjectileStateEnum m_state;
        Vector3D m_origin;
        Vector3D m_velocity;
        Vector3D m_directionNormalized;
        float m_speed;
        float m_maxTrajectory;

        Vector3D m_position;
        MyEntity m_ignoreEntity;
        MyEntity m_weapon;

        public float LengthMultiplier = 1;

        //  Type of this projectile
        MyProjectileAmmoDefinition m_projectileAmmoDefinition;

        public MyEntity OwnerEntity = null;//rifle, block, ...
        public MyEntity OwnerEntityAbsolute = null;//character, main ship cockpit, ...

        int m_checkIntersectionIndex; //actual index to keep distributed checking intersection
        static int checkIntersectionCounter = 0; //counter of started projectiles
        //Vector3 m_positionToCheck; //position which needs to be tested for line intersection
        bool m_positionChecked;

        private VRage.Game.Models.MyIntersectionResultLineTriangleEx? m_intersection = null;
        private List<MyLineSegmentOverlapResult<MyEntity>> m_entityRaycastResult = null;

        // Default 50% of energy is consumed by damage
        private const float m_impulseMultiplier = 0.5f; 

        public MyProjectile()
        {
        }

        //  This method realy initiates/starts the missile
        //  IMPORTANT: Direction vector must be normalized!
        // Projectile count multiplier - when real rate of fire it 45, but we shoot only 10 projectiles as optimization count multiplier will be 4.5
        public void Start(MyProjectileAmmoDefinition ammoDefinition, MyEntity ignoreEntity, Vector3D origin, Vector3 initialVelocity, Vector3 directionNormalized, MyEntity weapon)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Projectile.Start");

            m_projectileAmmoDefinition = ammoDefinition;
            m_state = MyProjectileStateEnum.ACTIVE;
            m_ignoreEntity = ignoreEntity;
            m_origin = origin + 0.1 * (Vector3D)directionNormalized;
            m_position = m_origin;
            m_weapon = weapon;

            if (ammoDefinition.ProjectileTrailProbability >= MyUtils.GetRandomFloat(0, 1))
                LengthMultiplier = 40;
            else
                LengthMultiplier = 0;
                        /*
            if (MyConstants.EnableAimCorrection)
            {
                if (m_ammoProperties.AllowAimCorrection) // Autoaim only available for player
                {
                    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Projectile.Start autoaim generic");
                    //Intersection ignores children of "ignoreEntity", thus we must not hit our own barrels
                    correctedDirection = MyEntities.GetDirectionFromStartPointToHitPointOfNearestObject(ignoreEntity, m_weapon, origin, m_ammoProperties.MaxTrajectory);
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                }
            }             */

            m_directionNormalized = directionNormalized;
            m_speed = ammoDefinition.DesiredSpeed * (ammoDefinition.SpeedVar > 0.0f ? MyUtils.GetRandomFloat(1 - ammoDefinition.SpeedVar, 1 + ammoDefinition.SpeedVar) : 1.0f);
            m_velocity = initialVelocity + m_directionNormalized * m_speed; ;
            m_maxTrajectory = ammoDefinition.MaxTrajectory * MyUtils.GetRandomFloat(0.8f, 1.2f); // +/- 20%

            m_checkIntersectionIndex = checkIntersectionCounter % CHECK_INTERSECTION_INTERVAL;
            checkIntersectionCounter += 3;
            m_positionChecked = false;

            LineD line = new LineD(m_origin, m_origin + m_directionNormalized * m_maxTrajectory);

            if (m_entityRaycastResult == null)
            {
                m_entityRaycastResult = new List<MyLineSegmentOverlapResult<MyEntity>>(16);
            }
            else
            {
                m_entityRaycastResult.Clear();
            }
            MyGamePruningStructure.GetAllEntitiesInRay(ref line, m_entityRaycastResult, MyEntityQueryType.Static);

            foreach (var entity in m_entityRaycastResult)
            {
                MyVoxelPhysics planetPhysics = entity.Element as MyVoxelPhysics;
                if (planetPhysics != null)
                {
                    planetPhysics.PrefetchShapeOnRay(ref line);
                }
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }
        
        //  Update position, check collisions, etc.
        //  Return false if projectile dies/timeouts in this tick.
        public bool Update()
        {
            //  Projectile was killed , but still not last time drawn, so we don't need to do update (we are waiting for last draw)
            if (m_state == MyProjectileStateEnum.KILLED)
                return true;
            //  Projectile was killed and last time drawn, so we can finally remove it from buffer
            if (m_state == MyProjectileStateEnum.KILLED_AND_DRAWN)
            {
                StopEffect();
                return false;
            }

            Vector3D position = m_position;
            m_position += m_velocity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            //  Distance timeout
            Vector3 positionDelta = m_position - m_origin;
            if (Vector3.Dot(positionDelta, positionDelta) >= m_maxTrajectory * m_maxTrajectory)
            {
                StopEffect();
                m_state = MyProjectileStateEnum.KILLED;
                return true;
            }

            m_checkIntersectionIndex = ++m_checkIntersectionIndex % CHECK_INTERSECTION_INTERVAL;
            if (m_checkIntersectionIndex != 0 && m_positionChecked) //check only each n-th intersection
                return true;

            //  Calculate hit point, create decal and throw debris particles
            Vector3D lineEndPosition = position + CHECK_INTERSECTION_INTERVAL * (m_velocity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);

            LineD line = new LineD(m_positionChecked ? position : m_origin, lineEndPosition);
            m_positionChecked = true;

            IMyEntity entity;
            Vector3D hitPosition;
            Vector3 hitNormal;
            bool headShot;

            GetHitEntityAndPosition(line, out entity, out hitPosition, out hitNormal, out headShot);
            if (entity == null || entity == m_ignoreEntity || entity.Physics == null)
                return true;
            if ((m_ignoreEntity is IMyGunBaseUser) && (m_ignoreEntity as IMyGunBaseUser).Owner is MyCharacter
                && (m_ignoreEntity as IMyGunBaseUser).Owner == entity)
            {
                return true; // prevent player shooting himself
            }

            ProfilerShort.Begin("Projectile.Update");

            {
                MyCharacter hitCharacter = entity as MyCharacter;
                if (hitCharacter != null)
                {
                    IStoppableAttackingTool stoppableTool = hitCharacter.CurrentWeapon as IStoppableAttackingTool;
                    if (stoppableTool != null)
                        stoppableTool.StopShooting(OwnerEntity);
                }
            }

            m_position = hitPosition;

            bool isProjectileGroupKilled = false;

            if (!isProjectileGroupKilled)
            {
                MySurfaceImpactEnum surfaceImpact;
                MyStringHash materialType;
                GetSurfaceAndMaterial(entity, ref  hitPosition, out surfaceImpact, out materialType);

                PlayHitSound(materialType, entity, hitPosition);
                DoDamage(headShot ? m_projectileAmmoDefinition.ProjectileHeadShotDamage : m_projectileAmmoDefinition.ProjectileMassDamage, hitPosition, entity);
                //  Create smoke and debris particle at the place of voxel/model hit
                if (surfaceImpact != MySurfaceImpactEnum.CHARACTER)
                    m_projectileAmmoDefinition.ProjectileOnHitParticles(ref hitPosition, ref hitNormal, ref line.Direction, entity, m_weapon, 1, OwnerEntity);

                if (surfaceImpact == MySurfaceImpactEnum.CHARACTER && entity is MyCharacter)
                {
                    MyStringHash bullet = MyStringHash.GetOrCompute("RifleBullet");//temporary
                    MyMaterialPropertiesHelper.Static.TryCreateCollisionEffect(
                                        MyMaterialPropertiesHelper.CollisionType.Start,
                                        hitPosition,
                                        hitNormal,
                                        bullet, materialType);
                }

                Vector3D particleHitPosition = hitPosition + line.Direction * -0.2;
                m_projectileAmmoDefinition.ProjectileOnHitMaterialParticles(ref particleHitPosition, ref hitNormal, ref line.Direction, entity, surfaceImpact, m_weapon, 1);

                CreateDecal(materialType);

                if (m_weapon == null || (entity.GetTopMostParent() != m_weapon.GetTopMostParent()))
                    ApplyProjectileForce(entity, hitPosition, m_directionNormalized, false, m_projectileAmmoDefinition.ProjectileHitImpulse * m_impulseMultiplier);

                StopEffect();
                m_state = MyProjectileStateEnum.KILLED;
            }
            ProfilerShort.End();
            return true;
        }

        private void GetHitEntityAndPosition(LineD line, out IMyEntity entity, out Vector3D hitPosition, out Vector3 hitNormal, out bool hitHead)
        {
            entity = null;
            hitPosition = hitNormal = Vector3.Zero;
            hitHead = false;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyEntities.GetIntersectionWithLine()");
            //m_intersection = MyEntities.GetIntersectionWithLine(ref line, m_ignoreEntity, m_weapon, false, false, true, IntersectionFlags.ALL_TRIANGLES, VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * CHECK_INTERSECTION_INTERVAL);
            m_intersection = null;
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if (m_intersection != null) 
            { 
                // will never trigger, see commented code above ^
                entity = m_intersection.Value.Entity;
                hitPosition = m_intersection.Value.IntersectionPointInWorldSpace;
                hitNormal = m_intersection.Value.NormalInWorldSpace;
            }
            // 1. rough raycast
            if (entity == null)
            {
                ProfilerShort.Begin("MyGamePruningStructure::CastProjectileRay");
                MyPhysics.HitInfo? hitInfo = MyPhysics.CastRay(line.From, line.To, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                //MyPhysics.HitInfo? hitInfo = null;
                //if (Sandbox.Game.Gui.MyMichalDebugInputComponent.Static.CastLongRay)
                //    hitInfo = MyPhysics.CastLongRay(line.From, line.To);
                //else
                //    hitInfo = MyPhysics.CastRay(line.From, line.To, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                ProfilerShort.End();
                if (hitInfo.HasValue)
                {
                    entity = hitInfo.Value.HkHitInfo.GetHitEntity() as MyEntity;
                    hitPosition = hitInfo.Value.Position;
                    hitNormal = hitInfo.Value.HkHitInfo.Normal;
                }
            }

            // 2. prevent shooting through characters, retest trajectory between entity and player
            if (!(entity is MyCharacter) || entity == null)
            {
                // first: raycast, get all entities in line, limit distance if possible
                LineD lineLimited = new LineD(line.From, entity == null ? line.To : hitPosition);
                if (m_entityRaycastResult == null)
                {
                    m_entityRaycastResult = new List<MyLineSegmentOverlapResult<MyEntity>>(16);
                }
                else
                {
                    m_entityRaycastResult.Clear();
                }
                MyGamePruningStructure.GetAllEntitiesInRay(ref lineLimited, m_entityRaycastResult);
                // second: precise tests, find best result
                double bestDistanceSq = double.MaxValue;
                IMyEntity entityBest = null;
                for (int i = 0; i < m_entityRaycastResult.Count; i++)
                {
                    if (m_entityRaycastResult[i].Element is MyCharacter)
                    {
                        MyCharacter hitCharacter = m_entityRaycastResult[i].Element as MyCharacter;
                        VRage.Game.Models.MyIntersectionResultLineTriangleEx? t;
                        hitCharacter.GetIntersectionWithLine(ref line, out t, out hitHead);

                        if (t != null)
                        {
                            double distanceSq = Vector3D.DistanceSquared(t.Value.IntersectionPointInWorldSpace, line.From);
                            if (distanceSq < bestDistanceSq)
                            {
                                bestDistanceSq = distanceSq;
                                entityBest = hitCharacter;
                                hitPosition = t.Value.IntersectionPointInWorldSpace;
                                hitNormal = t.Value.NormalInWorldSpace;
                            }
                        }
                    }
                }
                // finally: do we have best result? then return it
                if (entityBest != null)
                {
                    entity = entityBest; 
                    return; // this was precise result, so return
                }
            }

            // 3. nothing found in the precise test? then fallback to already found results
            if (entity == null)
                return; // no fallback results

            if (entity is MyCharacter) // retest character found in fallback
            {
                MyCharacter hitCharacter = entity as MyCharacter;
                VRage.Game.Models.MyIntersectionResultLineTriangleEx? t;
                hitCharacter.GetIntersectionWithLine(ref line, out t, out hitHead);
                if (t == null)
                {
                    entity = null; // no hit.
                }
                else
                {
                    hitPosition = t.Value.IntersectionPointInWorldSpace;
                    hitNormal = t.Value.NormalInWorldSpace;
                    hitHead = hitHead && m_projectileAmmoDefinition.HeadShot; // allow head shots only for ammo supporting it in definition
                }
            }
            else
            {
                //entity = entity.GetTopMostParent();
            }
        }

        private void DoDamage(float damage, Vector3D hitPosition, IMyEntity damagedEntity)
        {
            //damage tracking
            MyEntity ent = (MyEntity)MySession.Static.ControlledEntity;
            if (this.OwnerEntityAbsolute != null && this.OwnerEntityAbsolute.Equals(MySession.Static.ControlledEntity) && (damagedEntity is IMyDestroyableObject || damagedEntity is MyCubeGrid))
            {
                MySession.Static.TotalDamageDealt += (uint)damage;
            }

            if (!Sync.IsServer)
                return;

            if (m_projectileAmmoDefinition.ProjectileType == MyProjectileType.Bolt)
            {
                if (damagedEntity is IMyDestroyableObject && damagedEntity is MyCharacter)
                    (damagedEntity as IMyDestroyableObject).DoDamage(damage, MyDamageType.Bolt, true, attackerId: m_weapon != null ? GetSubpartOwner(m_weapon).EntityId : 0);
            }
            else
            {
                if (damagedEntity is MyCubeGrid)
                {
                    var grid = damagedEntity as MyCubeGrid;
                    if (grid.Physics != null && grid.Physics.Enabled && grid.BlocksDestructionEnabled)
                    {
                        bool causeDeformation = false;
                        Vector3I blockPos;
                        grid.FixTargetCube(out blockPos, Vector3D.Transform(hitPosition, grid.PositionComp.WorldMatrixNormalizedInv) / grid.GridSize);
                        var block = grid.GetCubeBlock(blockPos);
                        if (block != null)
                        {
                            (block as IMyDestroyableObject).DoDamage(damage, MyDamageType.Bullet, true, attackerId: m_weapon != null ? GetSubpartOwner(m_weapon).EntityId : 0);
                            if (block.FatBlock == null)
                                causeDeformation = true;
                        }

                        if (causeDeformation)
                            ApllyDeformationCubeGrid(hitPosition, grid);
                    }
                }
                //By Gregory: When MyEntitySubpart (e.g. extended parts of pistons and doors) damage the whole parent component
                //Temporary fix! Maybe other solution? MyEntitySubpart cannot implement IMyDestroyableObject cause is on dependent namespace
                else if (damagedEntity is MyEntitySubpart)
                {
                    if (damagedEntity.Parent != null && damagedEntity.Parent.Parent is MyCubeGrid)
                    {
                        DoDamage(damage, damagedEntity.Parent.WorldAABB.Center, damagedEntity.Parent.Parent);
                    }

                }
                else if (damagedEntity is IMyDestroyableObject)
                    (damagedEntity as IMyDestroyableObject).DoDamage(damage, MyDamageType.Bullet, true, attackerId: m_weapon != null ? GetSubpartOwner(m_weapon).EntityId : 0);
            }

            //Handle damage ?? some WIP code by Ondrej
            //MyEntity damagedObject = entity;
            //damagedObject.DoDamage(m_ammoProperties.HealthDamage, m_ammoProperties.ShipDamage, m_ammoProperties.EMPDamage, m_ammoProperties.DamageType, m_ammoProperties.AmmoType, m_ignorePhysObject);
            //if (MyMultiplayerGameplay.IsRunning)
            //    MyMultiplayerGameplay.Static.ProjectileHit(damagedObject, intersectionValue.IntersectionPointInWorldSpace, this.m_directionNormalized, MyAmmoConstants.FindAmmo(m_ammoProperties), this.OwnerEntity);

        }

        private MyEntity GetSubpartOwner(MyEntity entity)
        {
            if (entity == null)
                return null;

            if (!(entity is MyEntitySubpart))
                return entity;

            MyEntity result = entity;
            while (result is MyEntitySubpart && result != null)
                result = result.Parent;

            if (result == null)
                return entity;
            else
                return result;
        }

        private static void GetSurfaceAndMaterial(IMyEntity entity, ref Vector3D hitPosition, out MySurfaceImpactEnum surfaceImpact, out MyStringHash materialType)
        {
            var voxelBase = entity as MyVoxelBase;
            if (voxelBase != null)
            {
                materialType = MyMaterialType.ROCK;
                surfaceImpact = MySurfaceImpactEnum.DESTRUCTIBLE;

                var voxelDefinition = voxelBase.GetMaterialAt(ref hitPosition);
                if(voxelDefinition != null)
                    materialType = MyStringHash.GetOrCompute(voxelDefinition.MaterialTypeName);
            }
            else if (entity is MyCharacter)
            {
                surfaceImpact = MySurfaceImpactEnum.CHARACTER;
                materialType = MyMaterialType.CHARACTER;
                if ((entity as MyCharacter).Definition.PhysicalMaterial != null) materialType = MyStringHash.GetOrCompute((entity as MyCharacter).Definition.PhysicalMaterial);
            }
            else if (entity is MyFloatingObject)
            {
                MyFloatingObject obj = entity as MyFloatingObject;
                materialType = (obj.VoxelMaterial != null) ? MyMaterialType.ROCK : MyMaterialType.METAL;
                surfaceImpact = MySurfaceImpactEnum.METAL;
            }
            else if (entity is MyTrees)
            {
                surfaceImpact = MySurfaceImpactEnum.DESTRUCTIBLE;
                materialType = MyMaterialType.WOOD;
            }
            else
            {
                surfaceImpact = MySurfaceImpactEnum.METAL;
                materialType = MyMaterialType.METAL;
                if (entity is MyCubeGrid)
                {
                    Vector3I blockPos;
                    var grid = (entity as MyCubeGrid);
                    if (grid != null)
                    {
                        grid.FixTargetCube(out blockPos, Vector3D.Transform(hitPosition, grid.PositionComp.WorldMatrixNormalizedInv) / grid.GridSize);
                        var block = grid.GetCubeBlock(blockPos);
                        if (block != null)
                        {
                            if (block.BlockDefinition.PhysicalMaterial != null)
                            {
                                materialType = MyStringHash.GetOrCompute(block.BlockDefinition.PhysicalMaterial.Id.SubtypeName);
                            }
                        }
                    }
                }
                if (materialType.GetHashCode() == 0) materialType = MyMaterialType.METAL;
            }
        }

        private void StopEffect()
        {
            //if (m_trailEffect != null)
            //{
            //    // stop the trail effect
            //    m_trailEffect.Stop();
            //    m_trailEffect = null;
            //}
        }

        private void CreateDecal(MyStringHash materialType)
        {
            //TODO Update decals for skinned objects
            //{
            //    //  Decal size depends on material. But for mining ship create smaller decal as original size looks to large on the ship.
            //    float decalSize = MyVRageUtils.GetRandomFloat(materialProperties.BulletHoleSizeMin,
            //                                                materialProperties.BulletHoleSizeMax) * m_ammoProperties.TrailScale;

            //    //  Place bullet hole decal
            //    var intersection = m_intersection.Value;
            //    float randomColor = MyVRageUtils.GetRandomFloat(0.5f, 1.0f);
            //    float decalAngle = MyVRageUtils.GetRandomRadian();

            //    MyDecals.Add(
            //        materialProperties.BulletHoleDecal,
            //        decalSize,
            //        decalAngle,
            //        new Vector4(randomColor, randomColor, randomColor, 1),
            //        false,
            //        ref intersection,
            //        0.0f,
            //        m_ammoProperties.DecalEmissivity * m_ammoProperties.TrailScale, MyDecalsConstants.DECAL_OFFSET_BY_NORMAL);
            //}
        }

        private void PlayHitSound(MyStringHash materialType, IMyEntity entity, Vector3D position)
        {
            if ((OwnerEntity == null) || !(OwnerEntity is MyWarhead)) // do not play bullet sound when coming from warheads
            {
                var emitter = MyAudioComponent.TryGetSoundEmitter();
                if (emitter == null)
                    return;

                ProfilerShort.Begin("Play projectile sound");
                emitter.SetPosition(m_position);
                emitter.SetVelocity(Vector3.Zero);
                MyAutomaticRifleGun rifleGun = m_weapon as MyAutomaticRifleGun;

                MySoundPair cueEnum = null;
                MyStringHash thisType;
                if (m_projectileAmmoDefinition.IsExplosive)
                    thisType = MyMaterialType.EXPBULLET;
                else if (rifleGun != null && rifleGun.GunBase.IsAmmoProjectile && m_projectileAmmoDefinition.ProjectileType == MyProjectileType.Bullet)
                    thisType = MyMaterialType.RIFLEBULLET;
                else if (m_projectileAmmoDefinition.ProjectileType == MyProjectileType.Bolt)
                    thisType = MyMaterialType.BOLT;
                else
                    thisType = MyMaterialType.GUNBULLET;

                cueEnum = MyMaterialPropertiesHelper.Static.GetCollisionCue(MyMaterialPropertiesHelper.CollisionType.Start,thisType, materialType);
                if (cueEnum.SoundId.IsNull && entity is MyVoxelBase)    // Play rock sounds if we have a voxel material that doesn't have an assigned sound for thisType
                {
                    materialType = MyMaterialType.ROCK;
                    cueEnum = MyMaterialPropertiesHelper.Static.GetCollisionCue(MyMaterialPropertiesHelper.CollisionType.Start, thisType, materialType);
                }

                if (MySession.Static != null && MySession.Static.Settings.RealisticSound)
                {
                    Func<bool> canHear = () => MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity.Entity == entity;
                    emitter.StoppedPlaying += (e) => { e.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.CanHear].Remove(canHear); } ;
                    emitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.CanHear].Add(canHear);
                }

                emitter.PlaySound(cueEnum, false);
                
                ProfilerShort.End();
            }
        }

        private void ApllyDeformationCubeGrid(Vector3D hitPosition, MyCubeGrid grid)
        {
            MatrixD gridInv = grid.PositionComp.WorldMatrixNormalizedInv;
            var hitPositionInObjectSpace = Vector3D.Transform(hitPosition, gridInv);
            var hitDirLoc = Vector3D.TransformNormal(m_directionNormalized, gridInv);

            float deformationOffset = 0.000664f * m_projectileAmmoDefinition.ProjectileMassDamage;
            float softAreaPlanar = 0.011904f * m_projectileAmmoDefinition.ProjectileMassDamage;
            float softAreaVertical = 0.008928f * m_projectileAmmoDefinition.ProjectileMassDamage;
            softAreaPlanar = MathHelper.Clamp(softAreaPlanar, grid.GridSize * 0.75f, grid.GridSize * 1.3f);
            softAreaVertical = MathHelper.Clamp(softAreaVertical, grid.GridSize * 0.9f, grid.GridSize * 1.3f);
            grid.Physics.ApplyDeformation(deformationOffset, softAreaPlanar, softAreaVertical, hitPositionInObjectSpace, hitDirLoc, MyDamageType.Bullet);
        }

        public static void ApplyProjectileForce(IMyEntity entity, Vector3D intersectionPosition, Vector3 normalizedDirection, bool isPlayerShip, float impulse)
        {
            //  If we hit model that belongs to physic object, apply some force to it (so it's thrown in the direction of shoting)
            if (entity.Physics != null && entity.Physics.Enabled)
            {
                entity.Physics.AddForce(
                        MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                        normalizedDirection * impulse,
                        intersectionPosition, Vector3.Zero);
            }
        }

        //  Draw the projectile but only if desired polyline trail distance can fit in the trajectory (otherwise we will see polyline growing from the origin and it's ugly).
        //  Or draw if this is last draw of this projectile (useful for short-distance shots).
        public void Draw()
        {
            const float PROJECTILE_POLYLINE_DESIRED_LENGTH = 120;

            //var velPerFrame = m_velocity * VRage.Game.MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            //for (int i = 0; i < 70; i += 5)
            //{
            //    Color col = new Color(255, 0, i * 5, 255);
            //    VRageRender.MyRenderProxy.DebugDrawLine3D(m_position + i * velPerFrame, m_position + i * velPerFrame + 5 * velPerFrame, col, Color.Yellow, false);
            //}

            double trajectoryLength = Vector3D.Distance(m_position, m_origin);
            if ((trajectoryLength > 0) || (m_state == MyProjectileStateEnum.KILLED))
            {
                if (m_state == MyProjectileStateEnum.KILLED)
                {
                    m_state = MyProjectileStateEnum.KILLED_AND_DRAWN;
                }

                if (!m_positionChecked)
                    return;
             
                //  If we calculate previous position using normalized direction (insted of velocity), projectile trails will 
                //  look like coming from cannon, and that is desired. Even during fast movement, acceleration, rotation or changes in movement directions.
                //Vector3 previousPosition = m_position - m_directionNormalized * projectileTrailLength * 1.05f;
                Vector3D previousPosition = m_position - m_directionNormalized * PROJECTILE_POLYLINE_DESIRED_LENGTH * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                //Vector3 previousPosition = m_previousPosition;
                //Vector3 previousPosition = m_initialSunWindPosition - MyMwcUtils.Normalize(m_desiredVelocity) * projectileTrailLength;

                Vector3D direction = Vector3D.Normalize(m_position - previousPosition);

                double projectileTrailLength = LengthMultiplier * m_projectileAmmoDefinition.ProjectileTrailScale;// PROJECTILE_POLYLINE_DESIRED_LENGTH;

                projectileTrailLength *= MyUtils.GetRandomFloat(0.6f, 0.8f);

                if (trajectoryLength < projectileTrailLength)
                {
                    projectileTrailLength = trajectoryLength;
                }

                previousPosition = m_position - projectileTrailLength * direction;


                //float color = MyMwcUtils.GetRandomFloat(1, 2);
                float color = MyUtils.GetRandomFloat(1, 2);
                float thickness = MyUtils.GetRandomFloat(0.2f, 0.3f) * m_projectileAmmoDefinition.ProjectileTrailScale;

                //  Line particles (polyline) don't look good in distance. Start and end aren't rounded anymore and they just
                //  look like a pieces of paper. Especially when zoom-in.
                thickness *= MathHelper.Lerp(0.2f, 0.8f, MySector.MainCamera.Zoom.GetZoomLevel());

                float alphaCone = 1;
                
                if (projectileTrailLength > 0)
                {
                    if (m_projectileAmmoDefinition.ProjectileTrailMaterial != null)
                    {
                        MyTransparentGeometry.AddLineBillboard(m_projectileAmmoDefinition.ProjectileTrailMaterial, new Vector4(m_projectileAmmoDefinition.ProjectileTrailColor, 1),
                            previousPosition, direction, (float)projectileTrailLength, thickness);
                    }
                    else
                    {
                        MyTransparentGeometry.AddLineBillboard("ProjectileTrailLine", new Vector4(m_projectileAmmoDefinition.ProjectileTrailColor * color, 1) * alphaCone,
                            previousPosition, direction, (float)projectileTrailLength, thickness);
                    }
                }                
            }
        }

        public void Close()
        {
            OwnerEntity = null;
            m_ignoreEntity = null;
            m_weapon = null;

            //  Don't stop sound
        }
    }
}