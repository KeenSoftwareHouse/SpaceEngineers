using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Weapons;
using VRageMath;

using VRage.Game.Components;

namespace Sandbox.Game.Components
{
    class MyRenderComponentSmallGatlingGun : MyRenderComponentCubeBlock
    {
        MySmallGatlingGun m_gatlingGun = null;
        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_gatlingGun = Container.Entity as MySmallGatlingGun;
        }
        public override void Draw()
        {
            base.Draw();
            //  Draw muzzle flash
            int deltaTime = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_gatlingGun.LastTimeShoot;
            if (deltaTime <= m_gatlingGun.GunBase.MuzzleFlashLifeSpan)
            {
                var worldToLocal = MatrixD.Invert(m_gatlingGun.PositionComp.WorldMatrix);
                MyParticleEffects.GenerateMuzzleFlash(m_gatlingGun.GunBase.GetMuzzleWorldPosition(), m_gatlingGun.PositionComp.WorldMatrix.Forward, 
                    GetRenderObjectID(), ref worldToLocal, m_gatlingGun.MuzzleFlashRadius, m_gatlingGun.MuzzleFlashLength);
            }
        }    
        #endregion
    }
}
