#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Lights;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;

#endregion

namespace Sandbox.Game.Weapons
{
    class MyMissile : MyAmmoBase, IMyDestroyableObject
    {
        protected MyMissileAmmoDefinition m_missileAmmoDefinition;
        MyLight m_light;

        protected float m_maxTrajectory; //max trajectory for missile
        MyParticleEffect m_smokeEffect;
        MyExplosionTypeEnum m_explosionType;
        private MyEntity m_collidedEntity;
        Vector3D? m_collisionPoint;
        Vector3 m_collisionNormal;
        long m_owner;
        private float m_smokeEffectOffsetMultiplier = 0.4f;

        private MyEntity3DSoundEmitter m_soundEmitter;

        public MyMissile()
        {
            m_collidedEntity_OnClose = collidedEntity_OnClose;
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
            if (MySession.Static.Settings.RealisticSound && MyFakes.ENABLE_NEW_SOUNDS)
            {
                Func<bool> expr = () =>
                MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity.Entity is MyCharacter && MySession.Static.ControlledEntity.Entity == m_collidedEntity;
                m_soundEmitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.ShouldPlay2D].Add(expr);
                m_soundEmitter.EmitterMethods[MyEntity3DSoundEmitter.MethodsEnum.CanHear].Add(expr);
            }
            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;
        }

        public virtual void Init(MyWeaponPropertiesWrapper weaponProperties)
        {
            m_missileAmmoDefinition = weaponProperties.GetCurrentAmmoDefinitionAs<MyMissileAmmoDefinition>();
            Init(weaponProperties, m_missileAmmoDefinition.MissileModelName, false, true, true);
            m_canByAffectedByExplosionForce = false;
            UseDamageSystem = true;
        }

        //  This method realy initiates/starts the missile
        //  IMPORTANT: Direction vector must be normalized!       
        public override void Start(Vector3D position, Vector3D initialVelocity, Vector3D direction, long owner)
        {
            m_collidedEntity = null;
            m_collisionPoint = null;
            m_maxTrajectory = m_missileAmmoDefinition.MaxTrajectory;
            m_owner = owner;

            m_isExploded = false;

            base.Start(position, initialVelocity, direction, owner);
            Physics.RigidBody.MaxLinearVelocity = m_missileAmmoDefinition.DesiredSpeed;

            m_explosionType = MyExplosionTypeEnum.MISSILE_EXPLOSION;

            MySoundPair shootSound = m_weaponDefinition.WeaponAmmoDatas[(int)MyAmmoType.Missile].ShootSound;
            if (shootSound != null)
            {
                //  Plays cue (looping)
                m_soundEmitter.PlaySingleSound(shootSound, true);
            }

            m_light = MyLights.AddLight();
            if (m_light != null)
            {
                m_light.Start(MyLight.LightTypeEnum.PointLight, (Vector3)PositionComp.GetPosition(), GetMissileLightColor(), 1, MyMissileConstants.MISSILE_LIGHT_RANGE);
            }

            if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Smoke_Missile, out m_smokeEffect))
            {
                var matrix = PositionComp.WorldMatrix;
                matrix.Translation -= matrix.Forward * m_smokeEffectOffsetMultiplier;
                m_smokeEffect.WorldMatrix = matrix;
                m_smokeEffect.CalculateDeltaMatrix = true;
            }

        }

        public static Vector4 GetMissileLightColor()
        {
            float rnd = MyUtils.GetRandomFloat(-0.1f, +0.1f);
            return new Vector4(MyMissileConstants.MISSILE_LIGHT_COLOR.X + rnd, MyMissileConstants.MISSILE_LIGHT_COLOR.Y + rnd, MyMissileConstants.MISSILE_LIGHT_COLOR.Z + rnd, MyMissileConstants.MISSILE_LIGHT_COLOR.W);
        }

        /// <summary>
        /// Updates resource.
        /// </summary>
        public override void UpdateBeforeSimulation()
        {
            try
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyMissile.UpdateBeforeSimulation");

                if (m_isExploded)
                {
                    Vector3D explosionPoint;
                    if (m_collisionPoint.HasValue)
                    {
                        explosionPoint = PlaceDecal();
                    }
                    else
                    {
                        // Can have no collision point when exploding from cascade explosions
                        explosionPoint = PositionComp.GetPosition();
                    }

                    if (Sandbox.Game.Multiplayer.Sync.IsServer)
                    {
                        //  Create explosion
                        float radius = m_missileAmmoDefinition.MissileExplosionRadius;
                        BoundingSphereD explosionSphere = new BoundingSphereD(explosionPoint, radius);

                        MyEntity ownerEntity = null;
                        var ownerId = Sync.Players.TryGetIdentity(m_owner);
                        if (ownerId != null)
                            ownerEntity = ownerId.Character;
                        //MyEntities.TryGetEntityById(m_owner, out ownerEntity);
                        

                        //  Call main explosion starter
                        MyExplosionInfo info = new MyExplosionInfo()
                        {
                            PlayerDamage = 0,
                            //Damage = m_ammoProperties.Damage,
                            Damage = MyFakes.ENABLE_VOLUMETRIC_EXPLOSION ? m_missileAmmoDefinition.MissileExplosionDamage : 200,
                            ExplosionType = m_explosionType,
                            ExplosionSphere = explosionSphere,
                            LifespanMiliseconds = MyExplosionsConstants.EXPLOSION_LIFESPAN,
                            CascadeLevel = CascadedExplosionLevel,
                            HitEntity = m_collidedEntity,
                            ParticleScale = 1.0f,
                            OwnerEntity = ownerEntity,

                            Direction = WorldMatrix.Forward,
                            VoxelExplosionCenter = explosionSphere.Center + radius * WorldMatrix.Forward * 0.25f,

                            ExplosionFlags = MyExplosionFlags.AFFECT_VOXELS | MyExplosionFlags.APPLY_FORCE_AND_DAMAGE | MyExplosionFlags.CREATE_DEBRIS | MyExplosionFlags.CREATE_DECALS | MyExplosionFlags.CREATE_SHRAPNELS | MyExplosionFlags.APPLY_DEFORMATION,
                            VoxelCutoutScale = 0.3f,
                            PlaySound = true,
                            ApplyForceAndDamage = true
                        };
                        if (!MarkedToDestroy)
                            info.ExplosionFlags |= MyExplosionFlags.CREATE_PARTICLE_EFFECT;
                        MyExplosions.AddExplosion(ref info);

                        if (m_collidedEntity != null && !(m_collidedEntity is MyAmmoBase))
                        {
                            if (m_collidedEntity.Physics != null && !m_collidedEntity.Physics.IsStatic)
                            {
                                m_collidedEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                                    100 * Physics.LinearVelocity, m_collisionPoint, null);
                            }
                        }
                    }

                    Close();

                    return;
                }

                base.UpdateBeforeSimulation();

                if (m_missileAmmoDefinition.MissileSkipAcceleration)
                    Physics.LinearVelocity = WorldMatrix.Forward * m_missileAmmoDefinition.DesiredSpeed * 0.7f;
                else
                    Physics.LinearVelocity += PositionComp.WorldMatrix.Forward * m_missileAmmoDefinition.MissileAcceleration * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                if (m_smokeEffect == null)
                {
                    // if (MyCamera.GetDistanceWithFOV(GetPosition()) < 150)
                    {
                        if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Smoke_Missile, out m_smokeEffect))
                        {
                            m_smokeEffect.UserScale = 0.3f;
                            
                            var matrix = PositionComp.WorldMatrix;
                            matrix.Translation -= matrix.Forward * m_smokeEffectOffsetMultiplier;
                            m_smokeEffect.WorldMatrix = matrix;
                            //m_smokeEffect.WorldMatrix = PositionComp.WorldMatrix;
                            m_smokeEffect.CalculateDeltaMatrix = true;
                        }
                    }
                }
                Physics.AngularVelocity = Vector3.Zero;

                if ((Vector3.Distance(PositionComp.GetPosition(), m_origin) >= m_maxTrajectory))
                {
                    Explode();
                    return;
                }
            }
            finally
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }
        }

        private Vector3D PlaceDecal()
        {
            Vector3D explosionPoint = m_collisionPoint.Value;
            MyHitInfo hitInfo = new MyHitInfo() { Position = explosionPoint, Normal = m_collisionNormal };
            MyDecals.HandleAddDecal(m_collidedEntity, hitInfo, this.m_missileAmmoDefinition.PhysicalMaterial);
            return explosionPoint;
        }

        /// <summary>
        /// Called when [world position changed].
        /// </summary>
        /// <param name="source">The source object that caused this event.</param>
        private void WorldPositionChanged(object source)
        {
            //  Update light position
            if (m_light != null)
            {
                m_light.Position = (Vector3)PositionComp.GetPosition();
                m_light.Color = Vector4.One;
                m_light.Range = MyMissileConstants.MISSILE_LIGHT_RANGE;
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (m_smokeEffect != null)
            {
                m_smokeEffect.WorldMatrix = MatrixD.CreateWorld(PositionComp.WorldMatrix.Translation - 0.5 * PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Forward, PositionComp.WorldMatrix.Up);                
            }
        }

        //  Kills this missile. Must be called at her end (after explosion or timeout)
        //  This method must be called when this object dies or is removed
        //  E.g. it removes lights, sounds, etc
        protected override void Closing()
        {
            base.Closing();

            if (this.Physics != null)
            {
                MyMissiles.Remove(this);
            }

            if (m_collidedEntity != null)
            {
                m_collidedEntity.OnClose -= m_collidedEntity_OnClose;
                m_collidedEntity = null;
            }

            if (m_smokeEffect != null)
            {
                m_smokeEffect.Stop();
                m_smokeEffect = null;
            }

            //  Free the light
            if (m_light != null)
            {
                MyLights.RemoveLight(m_light);
                m_light = null;
            }

            //  Stop cue (needed because sound is looping)
            m_soundEmitter.StopSound(true);
        }      

        public override void OnContactStart(ref MyPhysics.MyContactPointEvent value)
        {
            MyEntity collidedEntity = value.ContactPointEvent.GetOtherEntity(this) as MyEntity;

            if (collidedEntity == null)
                return;

            if (!Sandbox.Game.Multiplayer.Sync.IsServer)
            {
                m_collidedEntity = collidedEntity;
                m_collisionPoint = value.Position;
                m_collisionNormal = value.Normal;

                PlaceDecal();
                Close();
                return;
            }

            Debug.Assert(!collidedEntity.Closed);

            m_collidedEntity = collidedEntity;
            m_collidedEntity.OnClose += m_collidedEntity_OnClose;
            m_collisionPoint = value.Position;
            m_collisionNormal = value.Normal;

            Explode();
        }

        public Action<MyEntity> m_collidedEntity_OnClose;
        void collidedEntity_OnClose(MyEntity obj)
        {
            m_collidedEntity = null;
            obj.OnClose -= m_collidedEntity_OnClose;
        }

        public override void Explode()
        {
            base.Explode();
            //MyDangerZones.Instance.NotifyExplosion(WorldMatrix.Translation, m_ammoProperties.ExplosionRadius, OwnerEntity);
        }

        public void OnDestroy()
        {
        }

        public void DoDamage(float damage, MyStringHash damageType, bool sync, long attackerId)
        {
            if (sync)
            {
                if (Sync.IsServer)
                    MySyncDamage.DoDamageSynced(this, damage, damageType, attackerId);
            }
            else
            {
                if (UseDamageSystem)
                    MyDamageSystem.Static.RaiseDestroyed(this, new MyDamageInformation(false, damage, damageType, attackerId));

                Explode();
            }
        }

        public float Integrity { get { return 1; } }

        public bool UseDamageSystem { get; private set; }

        public long Owner
        {
            get { return m_owner; }
        }

        void IMyDestroyableObject.OnDestroy()
        {
            OnDestroy();
        }

        bool IMyDestroyableObject.DoDamage(float damage, MyStringHash damageType, bool sync, MyHitInfo? hitInfo, long attackerId)
        {
            DoDamage(damage, damageType, sync, attackerId);
            return true;
        }

        float IMyDestroyableObject.Integrity
        {
            get { return Integrity; }
        }

        bool IMyDestroyableObject.UseDamageSystem
        {
            get { return UseDamageSystem; }
        }
    }
}
