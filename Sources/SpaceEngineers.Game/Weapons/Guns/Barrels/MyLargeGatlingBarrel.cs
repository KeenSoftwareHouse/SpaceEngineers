using Sandbox;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Weapons;
using Sandbox.Game.Weapons.Guns.Barrels;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.Weapons.Guns.Barrels
{
    class MyLargeGatlingBarrel : MyLargeBarrelBase
    {
        //private Vector3 m_muzzleFlashStartPosition;  // Position of the barrel muzzle flashes from the dummy
        private Vector3D m_muzzleFlashPosition;
        float m_projectileMaxTrajectory;
        Vector3 m_projectileColor;

        private int m_nextNotificationTime = 0;
        private MyHudNotification m_reloadNotification = null;

        float m_rotationAngle;                          //  Actual rotation angle (not rotation speed) around Z axis
        float m_rotationTimeout;
        private int m_shotsLeftInBurst = 0;
        private int m_reloadCompletionTime = 0;

        public int ShotsInBurst
        {
            get
            {
                return this.m_gunBase.ShotsInBurst;
            }
        }


        public MyLargeGatlingBarrel()
        {
            m_rotationTimeout = (float)MyGatlingConstants.ROTATION_TIMEOUT + MyUtils.GetRandomFloat(-500, +500);
        }

        public override void Init(MyEntity entity, MyLargeTurretBase turretBase)
        {
            base.Init(entity, turretBase);

            m_shotsLeftInBurst = ShotsInBurst;
            // backward compatibility with old models/mods
            if (!m_gunBase.HasDummies)  
            {
                Vector3 pos = 2 * entity.PositionComp.WorldMatrix.Forward;
                m_gunBase.AddMuzzleMatrix(MyAmmoType.HighSpeed, Matrix.CreateTranslation(pos));
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // Draw smoke:
            if (m_shotSmoke != null)
            {
                if (m_smokeToGenerate == 0)
                {
                    m_shotSmoke.Stop();
                    m_shotSmoke = null;
                }
                else
                {
                    m_shotSmoke.UserBirthMultiplier = m_smokeToGenerate;
                    m_shotSmoke.WorldMatrix = m_gunBase.GetMuzzleWorldMatrix();
                }
            }

            //  Cannon is rotating while shoting. After that, it will slow-down.
            float normalizedRotationSpeed = 1.0f - MathHelper.Clamp((float)(MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot) / m_rotationTimeout, 0, 1);
            normalizedRotationSpeed = MathHelper.SmoothStep(0, 1, normalizedRotationSpeed);
            float rotationAngle = normalizedRotationSpeed * MyGatlingConstants.ROTATION_SPEED_PER_SECOND * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            if (rotationAngle != 0)
                Entity.PositionComp.LocalMatrix = Matrix.CreateRotationZ(rotationAngle) * Entity.PositionComp.LocalMatrix;

            UpdateReloadNotification();
        }

        public override void Draw()
        {
            // Draw muzzle flash:
            int dt = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot;
            if (dt <= m_gunBase.MuzzleFlashLifeSpan && m_muzzleFlashLength > 0)
            {
                var worldToLocal = MatrixD.Invert(m_entity.WorldMatrix);
                MyParticleEffects.GenerateMuzzleFlash(m_gunBase.GetMuzzleWorldPosition(), (Vector3)m_entity.WorldMatrix.Forward, m_entity.Render.GetRenderObjectID(), ref worldToLocal, m_muzzleFlashRadius, m_muzzleFlashLength);
            }

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
            {
                VRageRender.MyRenderProxy.DebugDrawLine3D(m_entity.PositionComp.GetPosition(), m_entity.PositionComp.GetPosition() + m_entity.WorldMatrix.Forward, Color.Green, Color.GreenYellow, false);
                if (GetWeaponBase().Target != null)
                {
                    VRageRender.MyRenderProxy.DebugDrawSphere(GetWeaponBase().Target.PositionComp.GetPosition(), 0.4f, Color.Green, 1, false);
                }
            }
        }

        // Start shooting on the presented target in the queue:
        public override bool StartShooting()
        {
            // Wait for reload to finish
            if (m_reloadCompletionTime > MySandboxGame.TotalGamePlayTimeInMilliseconds)
                return false;

            // start shooting this kind of ammo ...
            if (!base.StartShooting())
                return false;

            if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot) < (m_gunBase.ShootIntervalInMiliseconds/* * 0.75f*/))
                return false;

            // no ammo
            if (m_shotsLeftInBurst <= 0 && ShotsInBurst != 0)
                return false;

            // Set muzzle flashes:
            m_muzzleFlashLength = MyUtils.GetRandomFloat(4, 6);
            m_muzzleFlashRadius = MyUtils.GetRandomFloat(1.2f, 2.0f);

            //Decrease size of muzzle flashes when player is controlling the turret for better visibility
            if (m_turretBase.IsControlledByLocalPlayer)
            {
                m_muzzleFlashRadius *= 0.33f;
            }

            // Increse smoke to generate
            IncreaseSmoke();

            // Make random trajectories for the bullet:
            //Matrix worldMatrix = (Matrix)m_entity.WorldMatrix;

            m_muzzleFlashPosition = m_gunBase.GetMuzzleWorldPosition();

            if (m_shotSmoke == null)
            {
                MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Smoke_LargeGunShot, out m_shotSmoke);
            }

            if (m_shotSmoke != null)
            {
                m_shotSmoke.WorldMatrix = MatrixD.CreateTranslation(m_muzzleFlashPosition);
                m_shotSmoke.Velocity = m_turretBase.Parent.Physics.LinearVelocity;
            }

            GetWeaponBase().PlayShootingSound();

            // Shoot projectiles
            Shoot(Entity.PositionComp.GetPosition());

            var time = m_gunBase.ReloadTime;

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

            // dont decrease ammo count ...
            m_lastTimeShoot = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            return true;
        }

        public override void Close()
        {
            if (m_shotSmoke != null)
            {
                m_shotSmoke.Stop();
                m_shotSmoke = null;
            }
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
