#region Using

using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using VRage.Import;
using VRageMath;
using Sandbox.Game.World;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;

using Sandbox.Game.Localization;
using VRage.Game;
using VRage.Game.Entity;

#endregion

namespace Sandbox.Game.Weapons
{
    class MyLargeMissileBarrel : MyLargeBarrelBase
    {
        private int m_burstFireTime_ms = 0;
        private int m_burstFireTimeLoadingIntervalConst_ms = 2000;
        private bool m_burstFinish = false;
        private int m_burstToFire = 0;

        private MyEntity3DSoundEmitter m_soundEmitter;

        public int BurstFireRate
        {
            get
            {
                return this.m_gunBase.BurstFireRate; 
            }
        }

        public MyLargeMissileBarrel()
        {
            m_soundEmitter = new MyEntity3DSoundEmitter(m_entity);
        }

        public override void Init(MyEntity entity, MyLargeTurretBase turretBase)
        {
            base.Init(entity, turretBase);

            // backward compatibility with old models/mods
            if (!m_gunBase.HasDummies)
            {
                Matrix muzzle = Matrix.Identity;
                muzzle.Translation += entity.PositionComp.WorldMatrix.Forward * 3;
                m_gunBase.AddMuzzleMatrix(MyAmmoType.Missile, muzzle);
            }
        }

        public void Init(Matrix localMatrix, MyLargeTurretBase parentObject)
        {
            m_burstToFire = BurstFireRate;
            m_burstFireTime_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            m_burstFireTime_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            // User settings:            
            m_burstFireTimeLoadingIntervalConst_ms = 2000;

            // This is imoprtant for missile launchers (they are not able to lauchching rackets on safe trajectory)
            BarrelElevationMin = -0.6f;
            // User settings:
            m_burstFireTimeLoadingIntervalConst_ms = 2000;
        }

        public override bool StartShooting()
        {
            if (!m_burstFinish)
            {
                if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot) < m_gunBase.ShootIntervalInMiliseconds) return false; //MyMissileConstants.MISSILE_LAUNCHER_SHOT_INTERVAL_IN_MILISECONDS

                var target = m_turretBase.Target;

                --m_burstToFire;

                if (target != null || m_turretBase.IsControlled)
                {
                    StartSound();
                    GetWeaponBase().RemoveAmmoPerShot();
                    m_gunBase.Shoot(m_turretBase.Parent.Physics.LinearVelocity);
                }
            }

            if (m_burstToFire <= 0)
            {
                if (m_turretBase.IsControlledByLocalPlayer)
                {
                    int notificationDuration_ms = m_burstFireTimeLoadingIntervalConst_ms;
                    var reloadingNotification = new MyHudNotification(MySpaceTexts.LargeMissileTurretReloadingNotification, notificationDuration_ms, level: MyNotificationLevel.Important);
                    MyHud.Notifications.Add(reloadingNotification);
                }
                m_burstFireTime_ms = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                m_burstToFire = BurstFireRate;
                m_burstFinish = true;
            }
            if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_burstFireTime_ms) > m_burstFireTimeLoadingIntervalConst_ms)
            {
                m_burstFinish = false;
            }

            m_lastTimeShoot = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            return true;
        }

        public override void StopShooting()
        {
            base.StopShooting();
            m_soundEmitter.StopSound(true);
        }

        //public override Matrix GetViewMatrix()
        //{
        //    Vector3 lookPosition = WorldMatrix.Translation + WorldMatrix.Backward * 3f + WorldMatrix.Up * 5f;
        //    Vector3 lookTarget = WorldMatrix.Translation + WorldMatrix.Forward * 1000000f;
        //    Vector3 lookDirection = Vector3.Normalize(lookTarget - lookPosition);
        //    Vector3 up = Vector3.Cross(WorldMatrix.Right, lookDirection);
        //    return Matrix.CreateLookAt(lookPosition, lookTarget, up);
        //}

        private void StartSound()
        {
            m_gunBase.StartShootSound(m_soundEmitter);
        }

        public override void Close()
        {
            base.Close();
            m_soundEmitter.StopSound(true);
        }
    }
}
