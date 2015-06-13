using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Weapons;
using VRageMath;
using Sandbox.Common.Components;
using VRage.Components;

namespace Sandbox.Game.Components
{
    class MyRenderComponentAutomaticRifle:MyRenderComponent
    {
        MyAutomaticRifleGun m_rifleGun;
        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_rifleGun = Container.Entity as MyAutomaticRifleGun;
        }

        public override void Draw()
        {
            //  Draw muzzle flash
            int deltaTime = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_rifleGun.LastTimeShoot;
            MyGunBase rifleBase = m_rifleGun.GunBase;
            if (deltaTime <= rifleBase.MuzzleFlashLifeSpan)
            {
                MyParticleEffects.GenerateMuzzleFlashLocal(Container.Entity, rifleBase.GetMuzzleLocalPosition(), Vector3.Forward, 0.2f, 0.3f);
            }
        }
        #endregion
    }
}
