using System.Collections.Generic;
using Sandbox.Common;
using Sandbox.Game.Entities;
using VRage.Generics;
using VRageMath;
using Sandbox.Definitions;
using Sandbox.Game.Weapons.Guns;
using System;
using System.Runtime.InteropServices;
using VRage.Game.Components;
using VRage.Game.Entity;


namespace Sandbox.Game.Weapons
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    class MyProjectiles : MySessionComponentBase
    {
        static MyObjectsPool<MyProjectile> m_projectiles = null;

        static MyProjectiles()
        {
        }

        public override void LoadData()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyProjectiles.LoadData");
            if (m_projectiles == null)
            {
                m_projectiles = new MyObjectsPool<MyProjectile>(MyProjectilesConstants.MAX_PROJECTILES_COUNT);
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        protected override void UnloadData()
        {
            if (m_projectiles != null)
            {
                m_projectiles.DeallocateAll();
            }
        }

        //  Add new projectile to the list  
        public static void Add(MyProjectileAmmoDefinition ammoDefinition, Vector3D origin, Vector3 initialVelocity, Vector3 directionNormalized, IMyGunBaseUser user, MyEntity owner)
        {
            //MyProjectile newProjectile = m_projectiles.Allocate();
            MyProjectile newProjectile;
            m_projectiles.AllocateOrCreate(out newProjectile);

            newProjectile.Start(
                ammoDefinition,
                user.IgnoreEntity,
                origin,
                initialVelocity,
                directionNormalized,
                user.Weapon
                );
            newProjectile.OwnerEntity = user.Owner != null ? user.Owner : user.IgnoreEntity;
            newProjectile.OwnerEntityAbsolute = owner;
        }

        public static void AddShrapnel(MyProjectileAmmoDefinition ammoDefinition, MyEntity ignoreEntity, Vector3 origin, Vector3 initialVelocity, Vector3 directionNormalized, bool groupStart, float thicknessMultiplier, float trailProbability, MyEntity weapon, MyEntity ownerEntity = null, float projectileCountMultiplier = 1)
        {
            MyProjectile newProjectile;
            m_projectiles.AllocateOrCreate(out newProjectile);

            newProjectile.Start(
                ammoDefinition,
                ignoreEntity,
                origin,
                initialVelocity,
                directionNormalized,
                weapon
                );
            newProjectile.OwnerEntity = ownerEntity != null ? ownerEntity : ignoreEntity; 
        }

        //  Not used apparently
        public static void AddShotgun(MyProjectileAmmoDefinition ammoDefinition, MyEntity ignorePhysObject, Vector3 origin, Vector3 initialVelocity, Vector3 directionNormalized, bool groupStart, float thicknessMultiplier, MyEntity weapon, float frontBillboardSize, MyEntity ownerEntity = null, float projectileCountMultiplier = 1)
        {
            MyProjectile newProjectile = m_projectiles.Allocate();
            if (newProjectile != null)
            {
                //newProjectile.Start(
                //    ammoDefinition,
                //    ignorePhysObject,
                //    origin,
                //    initialVelocity,
                //    directionNormalized,
                //    groupStart,
                //    thicknessMultiplier,
                //    1,
                //    weapon,
                //    projectileCountMultiplier
                //    );

            //    newProjectile.BlendByCameraDirection = true;
            //    newProjectile.FrontBillboardMaterial = "ShotgunParticle";
            //    newProjectile.LengthMultiplier = 2;
            //    newProjectile.FrontBillboardSize = frontBillboardSize;
             //   newProjectile.OwnerEntity = ownerEntity != null ? ownerEntity : ignorePhysObject;
            }
        }

        //  Update active projectiles. If projectile dies/timeouts, remove it from the list.
        public override void UpdateBeforeSimulation()
        {
            foreach (MyProjectile item in m_projectiles.Active)
            {
                if (item.Update() == false)
                {
                    item.Close();
                    m_projectiles.MarkForDeallocate(item);
                }
            }

            m_projectiles.DeallocateAllMarked();
        }

        //  Draw active projectiles
        public override void Draw()
        {
            foreach (MyProjectile item in m_projectiles.Active)
            {
                item.Draw();
            }
        }
    }
}
