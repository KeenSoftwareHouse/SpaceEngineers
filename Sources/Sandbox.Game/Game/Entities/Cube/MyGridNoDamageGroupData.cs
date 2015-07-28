using VRage.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Cube
{
    public class MyGridNoDamageGroupData : IGroupData<MyCubeGrid>
    {
        public void OnRelease()
        {
        }

        public void OnNodeAdded(MyCubeGrid entity)
        {
        }

        public void OnNodeRemoved(MyCubeGrid entity)
        {
        }

        public void OnCreate<TGroupData>(MyGroups<MyCubeGrid, TGroupData>.Group group) where TGroupData : IGroupData<MyCubeGrid>, new()
        {
        }
    }
}
