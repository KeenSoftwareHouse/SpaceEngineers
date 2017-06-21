using Sandbox.Game.Components;
using Sandbox.Game.Weapons;

namespace SpaceEngineers.Game.EntityComponents.Renders
{
    class MyRenderComponentLargeTurret : MyRenderComponentCubeBlock
    {
        MyLargeTurretBase m_turretBase = null;
        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_turretBase = Container.Entity as MyLargeTurretBase;
        }
        public override void Draw()
        {
            if (m_turretBase.IsWorking)
            {
                base.Draw();

                if (m_turretBase.Barrel != null)                
                    m_turretBase.Barrel.Draw();
            }
        }


        #endregion
    }
}
