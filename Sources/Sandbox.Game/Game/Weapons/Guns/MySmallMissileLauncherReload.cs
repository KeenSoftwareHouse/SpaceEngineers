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
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_SmallMissileLauncherReload))]
    class MySmallMissileLauncherReload : MySmallMissileLauncher, IMySmallMissileLauncherReload
    {
        const int NUM_ROCKETS_TO_COOLDOWN = 4;
        const int COOLDOWN_TIME_MILISECONDS = 5000;
        int m_numRocketsShot = 0;

        private static readonly MyHudNotification MISSILE_RELOAD_NOTIFICATION = new MyHudNotification( MySpaceTexts.MissileLauncherReloadingNotification, COOLDOWN_TIME_MILISECONDS, level: MyNotificationLevel.Important);

        static MySmallMissileLauncherReload()
        {
            var useConveyor = new MyTerminalControlOnOffSwitch<MySmallMissileLauncher>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConveyor.Getter = (x) => x.UseConveyorSystem;
            useConveyor.Setter = (x, v) => MySyncConveyors.SendChangeUseConveyorSystemRequest(x.EntityId, v);
            useConveyor.Visible = (x) => (true);
            useConveyor.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConveyor);
        }

        override public void Shoot(MyShootActionEnum action, Vector3 direction, string gunAction)
        {
            //small reloadable launcher have cooldown 
            if ((NUM_ROCKETS_TO_COOLDOWN == m_numRocketsShot) && (COOLDOWN_TIME_MILISECONDS > MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot))
            {
                return;
            }
            if (NUM_ROCKETS_TO_COOLDOWN == m_numRocketsShot)
            {
                m_numRocketsShot = 0;
            }
            m_numRocketsShot++;

            base.Shoot(action, direction, gunAction);

            if (m_numRocketsShot == NUM_ROCKETS_TO_COOLDOWN)
            {
                MyHud.Notifications.Add(MISSILE_RELOAD_NOTIFICATION);
            }
        }
    }
}