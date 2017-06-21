using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Weapons;
using VRageMath;

using VRage.Game.Components;

namespace Sandbox.Game.Components
{
    class MyRenderComponentAutomaticRifle : MyRenderComponent
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
            if (rifleBase.UseDefaultMuzzleFlash && deltaTime <= rifleBase.MuzzleFlashLifeSpan)
            {
                MyParticleEffects.GenerateMuzzleFlash(rifleBase.GetMuzzleWorldPosition(), rifleBase.GetMuzzleWorldMatrix().Forward, 0.1f, 0.3f);
            }
        }
        #endregion
    }
}
