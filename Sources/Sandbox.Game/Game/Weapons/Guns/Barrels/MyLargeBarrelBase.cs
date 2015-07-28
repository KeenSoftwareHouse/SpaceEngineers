#region Using

using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using Sandbox.Common.ObjectBuilders.Definitions;

#endregion

namespace Sandbox.Game.Weapons
{
    abstract class MyLargeBarrelBase 
    {
        #region Fields
        // used ammo type for this barrel:
        protected MyGunBase m_gunBase;

        public MyModelDummy CameraDummy { get; private set; }

        // time diffs:
        protected int m_lastTimeShoot;
        private int m_lastTimeSmooke;

        // Here because of the usability with the other types of barrels:
        public float BarrelElevationMin { get; protected set; } // is actually set to nice friendly angle..
        protected MyParticleEffect m_shotSmoke;


        protected int m_smokeLastTime;             // Smoke time stamp
        protected int m_smokeToGenerate;           // How much moke to generate
        protected float m_muzzleFlashLength;        // Length of muzzle flash
        protected float m_muzzleFlashRadius;        // Radius of the muzzle flash

        protected MyEntity m_entity;
        protected MyLargeTurretBase m_turretBase;

        #endregion

        public MyLargeBarrelBase()
        {            
            m_lastTimeShoot = 0;
            m_lastTimeSmooke = 0;
            BarrelElevationMin = -0.6f;
        }

        public virtual void Draw()
        { 
        }

        public virtual void Init(MyEntity entity, MyLargeTurretBase turretBase)
        {
            m_entity = entity;
            m_turretBase = turretBase;
            m_gunBase = turretBase.GunBase as MyGunBase;

            // Check for the dummy cubes for the muzzle flash positions:
            if (m_entity.Model != null)
            {
                if (m_entity.Model.Dummies.ContainsKey("camera"))
                {
                    CameraDummy = m_entity.Model.Dummies["camera"];
                }

                m_gunBase.LoadDummies(m_entity.Model.Dummies);
            }

            m_entity.OnClose += m_entity_OnClose;

        }

        void m_entity_OnClose(MyEntity obj)
        {
            if (m_shotSmoke != null)
            {
                MyParticlesManager.RemoveParticleEffect(m_shotSmoke);
                m_shotSmoke = null;
            }
        }

        public virtual bool StartShooting()
        {
            if (IsControlledByPlayer())
            {
                return true;
            }

            return true;
         

            StopShooting();
            return false;
        }

        public virtual void StopShooting()
        {
            GetWeaponBase().StopShootingSound();
        }

        protected MyLargeTurretBase GetWeaponBase()
        {
            return m_turretBase;
        }

        //protected float GetDeviatedAngleByDamageRatio() 
        //{
        //    MyPrefabLargeWeapon prefabLargeWeapon = GetWeaponBase().PrefabParent;
        //    if (MySession.PlayerShip != null &&
        //       MyFactions.GetFactionsRelation(prefabLargeWeapon, MySession.PlayerShip) == MyFactionRelationEnum.Enemy)
        //    {
        //        float degrees = (float)Math.Pow(120, prefabLargeWeapon.GetDamageRatio() * 1.5 - 1.2) * 4f;
        //        return MathHelper.ToRadians(degrees);
        //    }
        //    return 0f;
        //}

        protected void Shoot(Vector3 muzzlePosition)
        {
            Vector3 projectileForwardVector = m_entity.WorldMatrix.Forward;
            Vector3 velocity = m_turretBase.Parent.Physics.LinearVelocity;
            GetWeaponBase().RemoveAmmoPerShot();
            m_gunBase.Shoot(velocity);
        }

        //protected override void DoDamageInternal(float playerDamage, float damage, float empDamage, MyStringHash damageType, MyAmmoType ammoType, MyEntity damageSource, bool justDeactivate)
        //{
        //    //Parent.DoDamage(playerDamage, damage, empDamage, damageType, ammoType, damageSource, justDeactivate);
        //}

      

        private void DrawCrossHair()
        {            
            //if (!MyHud.Visible)
            //{
            //    return;
            //}

            //// Recompute crosshair size for zoom mode
            //float crosshairSize = 1150;
            //if (MyCamera.Zoom.GetZoomLevel() < 1)
            //{
            //    crosshairSize = crosshairSize / (float)(Math.Tan(MyCamera.FieldOfView / 2) / Math.Tan(MyCamera.Zoom.GetFOV() / 2));
            //}

            //MyTransparentGeometry.AddBillboardOriented(VRageRender.MyTransparentMaterialEnum.Crosshair, Vector4.One, WorldMatrix.Translation + WorldMatrix.Forward * 25000,
            //    WorldMatrix.Up, WorldMatrix.Right, crosshairSize, 1);
        }

        public bool IsControlledByPlayer()
        {
            return MySession.ControlledEntity == this;
        }

        //public override MyEntity GetBaseEntity()
        //{
        //    return GetWeaponBase().PrefabParent;
        //}

        protected void IncreaseSmoke()
        {
            m_smokeToGenerate += MyGatlingConstants.SMOKE_INCREASE_PER_SHOT;
            m_smokeToGenerate = MyUtils.GetClampInt(m_smokeToGenerate, 0, MyGatlingConstants.SMOKES_MAX);
        }

        protected void DecreaseSmoke()
        {
            m_smokeToGenerate -= MyGatlingConstants.SMOKE_DECREASE;
            m_smokeToGenerate = MyUtils.GetClampInt(m_smokeToGenerate, 0, MyGatlingConstants.SMOKES_MAX);
        }

        public virtual void UpdateAfterSimulation()
        {
            DecreaseSmoke();
        }

        public void RemoveSmoke()
        {
            m_smokeToGenerate = 0;
        }

        public MyEntity Entity
        {
            get { return m_entity; }
        }

        public virtual void Close()
        {
        }

        public void WorldPositionChanged()
        {
            m_gunBase.WorldMatrix = Entity.PositionComp.WorldMatrix;
        }
    }
}
