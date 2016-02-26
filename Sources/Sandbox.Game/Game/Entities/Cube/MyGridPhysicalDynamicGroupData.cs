using VRage.Groups;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities
{
    public class MyGridPhysicalDynamicGroupData : IGroupData<MyCubeGrid>
    {
        public void OnCreate<TGroupData>(MyGroups<MyCubeGrid, TGroupData>.Group group) where TGroupData : IGroupData<MyCubeGrid>, new()
        {
        }

        public void OnRelease()
        {
        }

        public void OnNodeAdded(MyCubeGrid entity)
        {
        }

        public void OnNodeRemoved(MyCubeGrid entity)
        {
        }
    }
}
