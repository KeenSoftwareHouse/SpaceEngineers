using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using System;
using System.Diagnostics;
using VRage.Game.Entity;
using VRageMath;

namespace Sandbox.Game.Weapons
{

    public static class MyWeaponPrediction
    {
        /// <summary>
        /// Algorithm to predict the position of the target
        /// </summary>
     
        public static bool GetPredictedTargetPosition(MyGunBase gun, MyEntity shooter, MyEntity target, out Vector3 predictedPosition, out float timeToHit, float shootDelay = 0)
        {
            Debug.Assert(target != null && target.PositionComp != null, "Null target!");
            Debug.Assert(shooter != null && shooter.PositionComp != null, "Null shooter!");

            if (target == null || target.PositionComp == null || shooter == null || shooter.PositionComp == null)
            {
                predictedPosition = Vector3.Zero;
                timeToHit = 0;
                return false;
            }
            
            Vector3 targetPosition = target.PositionComp.WorldAABB.Center;
            Vector3 muzzlePosition = gun.GetMuzzleWorldPosition();
            
            Vector3 toTarget = targetPosition - muzzlePosition;
            Vector3 targetVelocity = Vector3.Zero;
            if (target.Physics != null)
            {
                targetVelocity = target.Physics.LinearVelocity;
            }
            Vector3 shooterVelocity = Vector3.Zero;
            if (shooter.Physics != null)
            {
                shooterVelocity = shooter.Physics.LinearVelocity;
            }
            Vector3 diffVelocity = targetVelocity - shooterVelocity;

            float projectileSpeed = GetProjectileSpeed(gun);

            float a = diffVelocity.LengthSquared() - projectileSpeed * projectileSpeed;
            float b = 2 * Vector3.Dot(diffVelocity, toTarget);
            float c = toTarget.LengthSquared();

            float p = -b / (2 * a);
            float q = (float)Math.Sqrt((b * b) - 4 * a * c) / (2 * a);

            float t1 = p - q;
            float t2 = p + q;
            float t;

            if (t1 > t2 && t2 > 0)
            {
                t = t2;
            }
            else
            {
                t = t1;
            }

            t = t + shootDelay;

            predictedPosition = targetPosition + diffVelocity * t;
            Vector3 bulletPath = predictedPosition - muzzlePosition;
            timeToHit = bulletPath.Length() / projectileSpeed;

            return true;
        }
        
            
        public static float GetProjectileSpeed(MyGunBase gun)
        {
            if (gun == null)
            {
                Debug.Fail("Invalid argument");
                return 0;
            }

            float shotSpeed = 0;
            if (gun.CurrentAmmoMagazineDefinition != null)
            {
                var ammoDefinition = MyDefinitionManager.Static.GetAmmoDefinition(gun.CurrentAmmoMagazineDefinition.AmmoDefinitionId);

                shotSpeed = ammoDefinition.DesiredSpeed;

                //if (ammoDefinition.AmmoType == MyAmmoType.Missile)
                //{
                //    //missiles are accelerating, shotSpeed is reached later
                //    var mDef = (Sandbox.Definitions.MyMissileAmmoDefinition)ammoDefinition;
                //    if (mDef.MissileInitialSpeed == 100f && mDef.MissileAcceleration == 600f && ammoDefinition.DesiredSpeed == 700f)//our missile
                //    {//This is very good&fast correction for our missile, but not for some modded exotics with different performance
                //        //still does not take parallel component of velocity into account, I know, but its accurate enough
                //        shotSpeed = 800f - 238431f / (397.42f + (float)(predictedPosition - gun.GetMuzzleWorldPosition()).Length());
                //    }
                //    //else {unknown missile, keep shotSpeed without correction}
                //}
            }

            return shotSpeed;
        }

    }
}
