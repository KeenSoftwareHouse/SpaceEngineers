using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using Sandbox.Common;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.Game;

namespace Sandbox.Game.Weapons
{
    class MyLargeGatlingBarrel : MyLargeBarrelBase
    {
        //private Vector3 m_muzzleFlashStartPosition;  // Position of the barrel muzzle flashes from the dummy
        private Vector3D m_muzzleFlashPosition;
        float m_projectileMaxTrajectory;
        Vector3 m_projectileColor;

        float m_rotationAngle;                          //  Actual rotation angle (not rotation speed) around Z axis
        float m_rotationTimeout;


        public MyLargeGatlingBarrel()
        {
            m_rotationTimeout = (float)MyGatlingConstants.ROTATION_TIMEOUT + MyUtils.GetRandomFloat(-500, +500);
        }

        public override void Init(MyEntity entity, MyLargeTurretBase turretBase)
        {
            base.Init(entity, turretBase);

            // backward compatibility with old models/mods
            if (!m_gunBase.HasDummies)  
            {
                Vector3 pos = 2 * entity.PositionComp.WorldMatrix.Forward * entity.Model.ScaleFactor;
                m_gunBase.AddMuzzleMatrix(MyAmmoType.HighSpeed, Matrix.CreateTranslation(pos));
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // Draw smoke:
            if (m_shotSmoke != null)
            {
                m_shotSmoke.UserBirthMultiplier = m_smokeToGenerate;
                m_shotSmoke.WorldMatrix = MatrixD.CreateTranslation(m_muzzleFlashPosition);
            }

            //  Cannon is rotating while shoting. After that, it will slow-down.
            float normalizedRotationSpeed = 1.0f - MathHelper.Clamp((float)(MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot) / m_rotationTimeout, 0, 1);
            normalizedRotationSpeed = MathHelper.SmoothStep(0, 1, normalizedRotationSpeed);
            float rotationAngle = normalizedRotationSpeed * MyGatlingConstants.ROTATION_SPEED_PER_SECOND * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            if (rotationAngle != 0)
                Entity.PositionComp.LocalMatrix = Matrix.CreateRotationZ(rotationAngle) * Entity.PositionComp.LocalMatrix;
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
            // start shooting this kind of ammo ...
            if (!base.StartShooting())
                return false;

            if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot) < (m_gunBase.ShootIntervalInMiliseconds/* * 0.75f*/))
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
                m_shotSmoke.AutoDelete = false;
                m_shotSmoke.UserEmitterScale = m_smokeToGenerate;
                m_shotSmoke.WorldMatrix = MatrixD.CreateTranslation(m_muzzleFlashPosition);
                m_shotSmoke.Velocity = m_turretBase.Parent.Physics.LinearVelocity;
                m_shotSmoke.UserScale = 5;
            }

            GetWeaponBase().PlayShootingSound();

            // Shoot projectiles
            Shoot(Entity.PositionComp.GetPosition());

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
    }
}
