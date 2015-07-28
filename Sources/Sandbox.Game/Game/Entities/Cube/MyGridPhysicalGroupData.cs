using VRage.Groups;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities
{
    public class MyGridPhysicalGroupData : IGroupData<MyCubeGrid>
    {
        internal readonly MyGroupControlSystem ControlSystem = new MyGroupControlSystem();

        public void OnRelease()
        {
        }

        public void OnNodeAdded(MyCubeGrid entity)
        {
            entity.OnAddedToGroup(this);
        }

        public void OnNodeRemoved(MyCubeGrid entity)
        {
            entity.OnRemovedFromGroup(this);
        }

        internal static bool IsMajorGroup(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group a, MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group b)
        {
            float sum = 0;
            foreach (var n in a.Nodes)
            {
                if(n.NodeData.Physics != null)
                {
                    sum += n.NodeData.Physics.Mass;
                }
            }

            foreach (var n in b.Nodes)
            {
                if (n.NodeData.Physics != null)
                {
                    sum -= n.NodeData.Physics.Mass;
                }
            }

            return sum > 0;

            // Default implementation so far
            //return MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.IsMajorGroup(a, b);
        }

        public void OnCreate<TGroupData>(MyGroups<MyCubeGrid, TGroupData>.Group group) where TGroupData : IGroupData<MyCubeGrid>, new()
        {
        }
    }
}
