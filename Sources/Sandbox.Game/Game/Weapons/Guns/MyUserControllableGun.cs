using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;
using VRage.ModAPI;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_UserControllableGun))]
    abstract class MyUserControllableGun : MyFunctionalBlock, IMyUserControllableGun
    {
        new MySyncUserControllableGun SyncObject = null;

        protected bool m_isShooting = false;

        static MyUserControllableGun()
        {
            if (MyFakes.ENABLE_WEAPON_TERMINAL_CONTROL)
            {
                var shootOnce = new MyTerminalControlButton<MyUserControllableGun>("ShootOnce", MySpaceTexts.Terminal_ShootOnce, MySpaceTexts.Blank, (b) => b.SyncObject.SendShootOnceMessage());
                shootOnce.EnableAction();
                MyTerminalControlFactory.AddControl(shootOnce);

                var shoot = new MyTerminalControlOnOffSwitch<MyUserControllableGun>("Shoot", MySpaceTexts.Terminal_Shoot);
                shoot.Getter = (x) => x.m_isShooting;
                shoot.Setter = (x, v) => x.RequestShoot(v);
                shoot.EnableToggleAction();
                shoot.EnableOnOffActions();
                MyTerminalControlFactory.AddControl(shoot);
                MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyUserControllableGun>());
            }
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);
            MyObjectBuilder_UserControllableGun builder = (objectBuilder as MyObjectBuilder_UserControllableGun);
            this.m_isShooting = builder.IsLargeTurret ? builder.IsShootingFromTerminal : builder.IsShooting;
            if (m_isShooting)
            {
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            }
            SyncObject = new MySyncUserControllableGun(this);
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_UserControllableGun)base.GetObjectBuilderCubeBlock(copy);
            builder.IsShooting = m_isShooting;
            builder.IsShootingFromTerminal = m_isShooting;
            return builder;
        }

        public void RequestShoot(bool enable)
        {
            if (enable)
            {
                SyncObject.SendBeginShootMessage();
            }
            else
            {
                SyncObject.SendEndShootMessage();
            }
        }

        public void Shoot()
        {
            MyGunStatusEnum status;
            if (CanShoot(MyShootActionEnum.PrimaryAction, OwnerId, out status) && CanShoot(out status) && CanOperate())
            {
                ShootFromTerminal(WorldMatrix.Forward);
            }
        }

        public void BeginShoot()
        {
            Shoot();
            m_isShooting = true;
            RememberIdle();
            TakeControlFromTerminal();
        }

        public void EndShoot()
        {
            m_isShooting = false;
            RestoreIdle();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (m_isShooting)
            {
                TakeControlFromTerminal();
                Shoot();
                RotateModels();
            }
        }

        public abstract void ShootFromTerminal(Vector3 direction);

        public abstract bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status);

        public virtual bool CanShoot(out MyGunStatusEnum status)
        {
            status = MyGunStatusEnum.OK;
            return true;
        }

        public abstract bool CanOperate();

        public virtual void TakeControlFromTerminal() { }

        bool IMyUserControllableGun.IsShooting { get { return m_isShooting; } }

        public virtual void SyncRotationAndOrientation() { }

        protected virtual void RotateModels() { }

        protected virtual void RememberIdle() { }

        protected virtual void RestoreIdle() { }
    }

}
