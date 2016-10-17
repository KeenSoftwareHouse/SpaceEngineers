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
using VRage;
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using VRage.Game;
using VRage.Sync;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_UserControllableGun))]
    public abstract class MyUserControllableGun : MyFunctionalBlock, IMyUserControllableGun
    {
        protected Sync<bool> m_isShooting;

        bool m_shootingSaved = false;

        public MyUserControllableGun()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_isShooting = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();

            m_isShooting.ValueChanged += (x) => ShootingChanged();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyUserControllableGun>())
                return;
            base.CreateTerminalControls();
            if (MyFakes.ENABLE_WEAPON_TERMINAL_CONTROL)
            {
                var shootOnce = new MyTerminalControlButton<MyUserControllableGun>("ShootOnce", MySpaceTexts.Terminal_ShootOnce, MySpaceTexts.Blank, (b) => b.OnShootOncePressed());
                shootOnce.EnableAction();
                MyTerminalControlFactory.AddControl(shootOnce);

                var shoot = new MyTerminalControlOnOffSwitch<MyUserControllableGun>("Shoot", MySpaceTexts.Terminal_Shoot);
                shoot.Getter = (x) => x.m_isShooting;
                shoot.Setter = (x, v) => x.OnShootPressed(v);
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
            m_shootingSaved = builder.IsLargeTurret ? builder.IsShootingFromTerminal : builder.IsShooting;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;      
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_UserControllableGun)base.GetObjectBuilderCubeBlock(copy);
            builder.IsShooting = m_isShooting;
            builder.IsShootingFromTerminal = m_isShooting;
            return builder;
        }

        public virtual bool IsStationary()
        {
            return false;
        }

        void OnShootOncePressed()
        {
           SyncRotationAndOrientation();
           MyMultiplayer.RaiseEvent(this, x => x.ShootOncePressedEvent);
        }

        [Event, Reliable, Server, Broadcast]
        public void ShootOncePressedEvent()
        {
            Shoot();
        }

        public void SetShooting(bool shooting)
        {
            OnShootPressed(shooting);
        }

        void OnShootPressed(bool isShooting)
        {
            if (isShooting)
            {
                SyncRotationAndOrientation();
            }
            m_isShooting.Value = isShooting;
        }

        void Shoot()
        {
            MyGunStatusEnum status;
            if (CanShoot(MyShootActionEnum.PrimaryAction, OwnerId, out status) && CanShoot(out status) && CanOperate())
            {
                ShootFromTerminal(WorldMatrix.Forward);
            }
        }

        void BeginShoot()
        {
            Shoot();
            RememberIdle();
            TakeControlFromTerminal();
        }

        void EndShoot()
        {
            RestoreIdle();
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            this.m_isShooting.Value = m_shootingSaved;
            if (m_isShooting)
            {
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            }
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

        protected void ShootingChanged()
        {
            if (m_isShooting)
            {
                BeginShoot();
            }
            else
            {
                EndShoot();
            }
        }

        public override void OnRemovedByCubeBuilder()
        {
            MyInventory inventory = this.GetInventory();
            if(inventory != null)
                ReleaseInventory(inventory);
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            MyInventory inventory = this.GetInventory();
            if (inventory != null)
                ReleaseInventory(inventory);
            base.OnDestroy();
        }
    }

}
