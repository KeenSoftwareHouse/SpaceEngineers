using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;

namespace Sandbox.Game.Entities.Cube
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MyCubeGrids : MySessionComponentBase
    {
        MyBinaryHeap<long, MyGridPhysics.PredictiveDestructionData> m_predictiveDestructionUpdate = new MyBinaryHeap<long, MyGridPhysics.PredictiveDestructionData>();

        public static event Action<MyCubeGrid, MySlimBlock> BlockBuilt;
        public static event Action<MyCubeGrid, MySlimBlock> BlockDestroyed;

        private long Now { get { return DateTime.Now.Ticks; } }

        internal static void NotifyBlockBuilt(MyCubeGrid grid, MySlimBlock block)
        {
            if (BlockBuilt != null)
                BlockBuilt(grid, block);
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            BlockBuilt = null;
            BlockDestroyed = null;
        }

        internal static void NotifyBlockDestroyed(MyCubeGrid grid, MySlimBlock block)
        {
            if (BlockDestroyed != null)
                BlockDestroyed(grid, block);
        }

        internal void EnqueueCorrectionExpiration(MyGridPhysics.PredictiveDestructionData data)
        {
            var time = Now + MyGridPhysics.PredictiveDestructionData.ExpirationTime;

            m_predictiveDestructionUpdate.Insert(data, time);
        }

        public override void UpdateBeforeSimulation()
        {
            var now = Now;

            while (m_predictiveDestructionUpdate.Count > 0 && m_predictiveDestructionUpdate.Min().HeapKey < now)
            {
                var min = m_predictiveDestructionUpdate.RemoveMin();

                min.Expire();
            }
        }
    }
}
