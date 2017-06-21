using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using VRageMath;
using Sandbox.Game.Multiplayer;
using System.Collections.Generic;
using Sandbox.Game.World;
using Sandbox.Game.Gui;
using Sandbox.Game.Entities.Cube;

using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems;
using Sandbox.Common;
using Sandbox.ModAPI;
using Sandbox.Game.Localization;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_SmallMissileLauncherReload))]
    public class MySmallMissileLauncherReload : MySmallMissileLauncher, IMySmallMissileLauncherReload
    {
        const int COOLDOWN_TIME_MILISECONDS = 5000;
        int m_numRocketsShot = 0;

        private static readonly MyHudNotification MISSILE_RELOAD_NOTIFICATION = new MyHudNotification( MySpaceTexts.MissileLauncherReloadingNotification, COOLDOWN_TIME_MILISECONDS, level: MyNotificationLevel.Important);

        public int BurstFireRate
        {
            get
            {
                return this.GunBase.ShotsInBurst;
            }
        }

        public MySmallMissileLauncherReload()
        {
            CreateTerminalControls();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MySmallMissileLauncher>())
                return;
            base.CreateTerminalControls();
            var useConveyor = new MyTerminalControlOnOffSwitch<MySmallMissileLauncher>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConveyor.Getter = (x) => x.UseConveyorSystem;
            useConveyor.Setter = (x, v) => x.UseConveyorSystem = v;
            useConveyor.Visible = (x) => (true);
            useConveyor.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConveyor);
        }

        public override void Shoot(MyShootActionEnum action, Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            //small reloadable launcher have cooldown 
            if ((BurstFireRate == m_numRocketsShot) && (MySandboxGame.TotalGamePlayTimeInMilliseconds < m_nextShootTime))
            {
                return;
            }
            if (BurstFireRate == m_numRocketsShot)
            {
                m_numRocketsShot = 0;
            }
            m_numRocketsShot++;

            base.Shoot(action, direction, overrideWeaponPos, gunAction);
        }
    }
}