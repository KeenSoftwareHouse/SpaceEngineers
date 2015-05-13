using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Weapons;
using VRageMath;
using Sandbox.Common.Components;

namespace Sandbox.Game.Components
{
    class MyRenderComponentAutomaticRifle:MyRenderComponent
    {
        MyAutomaticRifleGun m_rifleGun;
        #region overrides
        public override void OnAddedToContainer(MyComponentContainer container)
        {
            base.OnAddedToContainer(container);
            m_rifleGun = Entity as MyAutomaticRifleGun;
        }

        public override void Draw()
        {
            //  Draw muzzle flash
            int deltaTime = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_rifleGun.LastTimeShoot;
            MyGunBase rifleBase = m_rifleGun.GunBase;
            if (deltaTime <= rifleBase.MuzzleFlashLifeSpan)
            {
                MyParticleEffects.GenerateMuzzleFlashLocal(this.Entity, rifleBase.GetMuzzleLocalPosition(), Vector3.Forward, 0.2f, 0.3f);
            }
        }
        #endregion
    }
}
