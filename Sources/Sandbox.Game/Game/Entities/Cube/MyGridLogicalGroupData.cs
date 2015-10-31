using VRage.Groups;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Game.EntityComponents;

namespace Sandbox.Game.Entities
{
    public class MyGridLogicalGroupData : IGroupData<MyCubeGrid>
    {
        internal readonly MyGridTerminalSystem TerminalSystem = new MyGridTerminalSystem();
		internal readonly MyResourceDistributorComponent ResourceDistributor = new MyResourceDistributorComponent();
        internal readonly MyGridWeaponSystem WeaponSystem = new MyGridWeaponSystem();

        public void OnRelease()
        {
            Debug.Assert(TerminalSystem.Blocks.Count == 0, "Terminal system is not empty!");
            Debug.Assert(TerminalSystem.BlockGroups.Count == 0, "Terminal system is not empty, block groups are there");
        }

        public void OnNodeAdded(MyCubeGrid entity)
        {
            entity.OnAddedToGroup(this);
        }

        public void OnNodeRemoved(MyCubeGrid entity)
        {
            entity.OnRemovedFromGroup(this);
        }

        public void OnCreate<TGroupData>(MyGroups<MyCubeGrid, TGroupData>.Group group) where TGroupData : IGroupData<MyCubeGrid>, new()
        {
            Debug.Assert(TerminalSystem.Blocks.Count == 0, "Terminal system is not empty!");
            Debug.Assert(TerminalSystem.BlockGroups.Count == 0, "Terminal system is not empty, block groups are there");
        }
    }
}
