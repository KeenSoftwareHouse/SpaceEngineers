#region Using

using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Weapons;
using Sandbox.Game.Weapons.Guns.Barrels;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

#endregion

namespace SpaceEngineers.Game.Weapons.Guns.Barrels
{
    class MyLargeMissileBarrel : MyLargeBarrelBase
    {
        private int m_reloadCompletionTime = 0;
        private int m_nextShootTime = 0;
        private int m_shotsLeftInBurst = 0;

        private int m_nextNotificationTime = 0;
        private MyHudNotification m_reloadNotification = null;

        private MyEntity3DSoundEmitter m_soundEmitter;

        public int ShotsInBurst
        {
            get
            {
                return this.m_gunBase.ShotsInBurst; 
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

            m_shotsLeftInBurst = ShotsInBurst;
            if (m_soundEmitter != null)
                m_soundEmitter.Entity = (MyEntity)turretBase;
        }

        // REMOVE-ME: This seems to be dead code
        public void Init(Matrix localMatrix, MyLargeTurretBase parentObject)
        {
            m_shotsLeftInBurst = ShotsInBurst;

            // This prevents missile launches from shooting missiles into their own base
            BarrelElevationMin = -0.6f;
        }

        public override bool StartShooting()
        {
            // Wait for reload to finish
            if (m_reloadCompletionTime > MySandboxGame.TotalGamePlayTimeInMilliseconds)
                return false;

            // Wait shot interval time
            if (m_nextShootTime > MySandboxGame.TotalGamePlayTimeInMilliseconds)
                return false;

            // If we still have ammo in the clip, or there is no clip
            if (m_shotsLeftInBurst > 0 || ShotsInBurst == 0)
            {
                var target = m_turretBase.Target;
                if (target != null || m_turretBase.IsControlled)
                {
                    StartSound();
                    GetWeaponBase().RemoveAmmoPerShot();
                    m_gunBase.Shoot(m_turretBase.Parent.Physics.LinearVelocity);

                    m_lastTimeShoot = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                    m_nextShootTime = MySandboxGame.TotalGamePlayTimeInMilliseconds + m_gunBase.ShootIntervalInMiliseconds;

                    if (ShotsInBurst > 0)
                    {
                        m_shotsLeftInBurst -= 1;

                        // If the clip ran out, start reloading
                        if (m_shotsLeftInBurst <= 0)
                        {
                            m_reloadCompletionTime = MySandboxGame.TotalGamePlayTimeInMilliseconds + m_gunBase.ReloadTime;
                            m_shotsLeftInBurst = ShotsInBurst;
                        }
                    }
                }
            }

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

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            
            UpdateReloadNotification();
        }

        private void UpdateReloadNotification()
        {
            // Remove expired notification
            if (MySandboxGame.TotalGamePlayTimeInMilliseconds > m_nextNotificationTime)
            {
                m_reloadNotification = null;
            }

            // If there is no ammo, don't show reloading text
            if (!m_gunBase.HasEnoughAmmunition() && Sandbox.Game.World.MySession.Static.SurvivalMode)
            {
                MyHud.Notifications.Remove(m_reloadNotification);
                m_reloadNotification = null;
                return;
            }

            if (!m_turretBase.IsControlledByLocalPlayer)
            {
                // Remove reload notification when not reloading
                if (m_reloadNotification != null)
                {
                    MyHud.Notifications.Remove(m_reloadNotification);
                    m_reloadNotification = null;
                }

                return;
            }

            // Wait reload interval
            if (m_reloadCompletionTime > MySandboxGame.TotalGamePlayTimeInMilliseconds)
            {
                ShowReloadNotification(m_reloadCompletionTime - MySandboxGame.TotalGamePlayTimeInMilliseconds);
                return;
            }

            // Wait shot interval time
            if (m_nextShootTime > MySandboxGame.TotalGamePlayTimeInMilliseconds)
            {
                ShowReloadNotification(m_nextShootTime - MySandboxGame.TotalGamePlayTimeInMilliseconds);
                return;
            }
        }

        /// <summary>
        /// Will show the reload notification for the specified duration.
        /// </summary>
        /// <param name="duration">The time in MS it should show reloading.</param>
        private void ShowReloadNotification(int duration)
        {
            int desiredEndTime = MySandboxGame.TotalGamePlayTimeInMilliseconds + duration;

            if (m_reloadNotification == null)
            {
                // Removing 250ms to remove overlap in notification display.
                duration = System.Math.Max(0, duration - 250);
                if (duration == 0)
                {
                    // No notification
                    return;
                }

                m_reloadNotification = new MyHudNotification(MySpaceTexts.LargeMissileTurretReloadingNotification, duration, level: MyNotificationLevel.Important);
                MyHud.Notifications.Add(m_reloadNotification);

                m_nextNotificationTime = desiredEndTime;
            }
            else
            {
                // Append with extra time
                int extraTime = desiredEndTime - m_nextNotificationTime;
                m_reloadNotification.AddAliveTime(extraTime);

                m_nextNotificationTime = desiredEndTime;
            }
        }
    }
}
