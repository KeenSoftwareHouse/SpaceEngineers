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
using VRage.Game.Entity;
using VRage.Game;

namespace Sandbox.Game.Weapons
{
    class MyLargeInteriorBarrel : MyLargeBarrelBase
    {
        float m_projectileMaxTrajectory;
        Vector3 m_projectileColor;
        
        public MyLargeInteriorBarrel()
        {
        }

        public override void Init(MyEntity entity, MyLargeTurretBase turretBase)
        {
            base.Init(entity, turretBase);

            // backward compatibility with old mods/models
            if (!m_gunBase.HasDummies)
            {
                Vector3 muzzleVec = -Entity.PositionComp.WorldMatrix.Forward * 0.8f;
                m_gunBase.AddMuzzleMatrix(MyAmmoType.HighSpeed, Matrix.CreateTranslation(muzzleVec));
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // Draw smoke:
            if (m_shotSmoke != null)
            {
                m_shotSmoke.UserBirthMultiplier = m_smokeToGenerate;
                m_shotSmoke.WorldMatrix = m_gunBase.GetMuzzleWorldMatrix();
            }
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
            m_muzzleFlashLength = MyUtils.GetRandomFloat(1, 2);
            m_muzzleFlashRadius = MyUtils.GetRandomFloat(0.3f, 0.5f);

            //Decrease size of muzzle flashes when player is controlling the turret for better visibility
            if (m_turretBase.IsControlledByLocalPlayer)
            {
                m_muzzleFlashLength *= 0.33f;
                m_muzzleFlashRadius *= 0.33f;
            }

            // Increse smoke to generate
            IncreaseSmoke();

            if (m_shotSmoke == null)
            {
                MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Smoke_LargeGunShot, out m_shotSmoke);
            }

            if (m_shotSmoke != null)
            {
                m_shotSmoke.AutoDelete = false;
                m_shotSmoke.UserEmitterScale = m_smokeToGenerate;
                m_shotSmoke.WorldMatrix = m_gunBase.GetMuzzleWorldMatrix();
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
