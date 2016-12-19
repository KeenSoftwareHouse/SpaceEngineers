using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRageMath;

namespace Sandbox.Game.SessionComponents
{
    /*
     * Session component handling trash in Space Master
     */
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 2000)]
    class MySessionComponentTrash : MySessionComponentBase
    {
        // How often let compoment to check trash (not related to trash interval)
        private const uint CHECK_INTERVAL_S = 1;

        // Check timer data (not related to trash interval)
        private uint m_lastCheckS;

        public override void Init(VRage.Game.MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
        }

        public override void BeforeStart()
        {
            m_lastCheckS = 0;
            m_timeFromNextTrash = 0;
        }

        public override void UpdateAfterSimulation()
        {
            //call makes sense only on server
            if (!Sync.IsServer)
                return;

            uint playTimeS = (uint)MySession.Static.ElapsedPlayTime.TotalSeconds;
            uint elapsedTimeS = playTimeS - m_lastCheckS;
            // Check interval.
            if (elapsedTimeS > CHECK_INTERVAL_S)
            {
                m_lastCheckS = playTimeS;
                UpdateTrash((int)elapsedTimeS);
            }
        }

        private int m_timeFromNextTrash;

        private void UpdateTrash(int deltaTime)
        {
            //Paused trash do not tick
            if (MyTrashRemoval.RemovalPaused)
                return;

            //Do nothing if nothing is choosed
            if (MyTrashRemoval.TrashOperation == MyTrashRemovalOperation.None)
                return;

            m_timeFromNextTrash += deltaTime;
            var trashIntervalTime = MyTrashRemoval.CurrentRemovalInterval;

            //Check trash interval
            if (m_timeFromNextTrash < trashIntervalTime)
                return;

            foreach (var entity in MyEntities.GetEntities())
            {
                //Trash want only grid
                MyCubeGrid grid = entity as MyCubeGrid;
                if (grid == null)
                    continue;

                if (grid.IsTrash() && !MyDebugDrawSettings.DEBUG_DRAW_TRASH_REMOVAL)
                {
                    switch (MyTrashRemoval.TrashOperation)
                    {
                        case MyTrashRemovalOperation.None:
                            break;
                        case MyTrashRemovalOperation.Remove:
                            grid.SyncObject.SendCloseRequest();
                            break;
                        case MyTrashRemovalOperation.Stop:
                            grid.Physics.LinearVelocity = Vector3.Zero;
                            grid.Physics.AngularVelocity = Vector3.Zero;
                            break;
                        case MyTrashRemovalOperation.Depower:
                            grid.SendPowerDistributorState(VRage.MyMultipleEnabledEnum.AllDisabled, -1);
                            break;
                    }
                }
            }
        }
    }
}
