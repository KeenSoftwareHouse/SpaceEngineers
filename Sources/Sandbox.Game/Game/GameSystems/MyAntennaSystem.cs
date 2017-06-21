using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Utils;
using VRage.Game.Components;
using VRage.Game.Entity;


namespace Sandbox.Game.GameSystems
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, 588, typeof(MyObjectBuilder_AntennaSessionComponent))]
    public class MyAntennaSystem : MySessionComponentBase
    {
        public static MyAntennaSystem Static { get; private set; }

        private List<long> m_addedItems = new List<long>();
        private HashSet<BroadcasterInfo> m_output = new HashSet<BroadcasterInfo>(new BroadcasterInfoComparer());
        private HashSet<MyDataBroadcaster> m_tempPlayerRelayedBroadcasters = new HashSet<MyDataBroadcaster>();
        private List<MyDataBroadcaster> m_tempGridBroadcastersFromPlayer = new List<MyDataBroadcaster>();

        public override void BeforeStart()
        {
            Static = this;
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            m_addedItems.Clear();
            m_addedItems = null;

            m_output.Clear();
            m_output = null;

            m_tempGridBroadcastersFromPlayer.Clear();
            m_tempGridBroadcastersFromPlayer = null;

            m_tempPlayerRelayedBroadcasters.Clear();
            m_tempPlayerRelayedBroadcasters = null;

            m_tempPlayerReceivers.Clear();
            m_tempPlayerReceivers = null;

            Static = null;
        }

        public HashSet<BroadcasterInfo> GetMutuallyConnectedGrids(MyEntity interactedEntityRepresentative, MyPlayer player = null)
        {
            if (player == null)
            {
                player = MySession.Static.LocalHumanPlayer;
            }

            MyIdentity playerIdentity = player.Identity;
            MyCharacter playerCharacter = playerIdentity.Character;

            //First: You are always connected to the block/ship/character you're in
            m_addedItems.Clear();
            m_output.Clear();

            m_addedItems.Add(interactedEntityRepresentative.EntityId);
            m_output.Add(new BroadcasterInfo()
            {
                EntityId = interactedEntityRepresentative.EntityId,
                Name = interactedEntityRepresentative.DisplayName
            });

            //then you get the broadcasted ones
            m_tempPlayerRelayedBroadcasters.Clear();
            GetPlayerRelayedBroadcasters(playerCharacter, interactedEntityRepresentative, m_tempPlayerRelayedBroadcasters);
            foreach (var broadcaster in m_tempPlayerRelayedBroadcasters)
            {
                if (broadcaster.Entity == null || !(broadcaster.Entity is MyCubeBlock) || (broadcaster.Entity is MyBeacon))
                    continue;

                MyIDModule module;
                if ((broadcaster.Entity as IMyComponentOwner<MyIDModule>).GetComponent(out module))
                {
                    if (!((MyTerminalBlock)broadcaster.Entity).HasPlayerAccess(playerIdentity.IdentityId) || module.Owner == 0)
                        continue;
                }
                else
                    continue;

                var ownerCubeGrid = (broadcaster.Entity as MyCubeBlock).CubeGrid;
                //GR: If grid is preview grid do not take into account (this is needed for project antennas. Another fix would be do disable broadcasting on projected antennas)
                if (ownerCubeGrid.IsPreview)
                    continue;

                m_tempGridBroadcastersFromPlayer.Clear();
                GridBroadcastersFromPlayer(ownerCubeGrid, interactedEntityRepresentative, playerIdentity.IdentityId, m_tempGridBroadcastersFromPlayer);
                if (m_tempGridBroadcastersFromPlayer.Count == 0)
                    continue;

                var logicalGroupRepresentative = GetLogicalGroupRepresentative(ownerCubeGrid);
                if (m_addedItems.Contains(logicalGroupRepresentative.EntityId))
                    continue;

                m_addedItems.Add(logicalGroupRepresentative.EntityId);
                m_output.Add(new BroadcasterInfo()
                {
                    EntityId = logicalGroupRepresentative.EntityId,
                    Name = logicalGroupRepresentative.DisplayName
                });
            }

            return m_output;
        }

        private void GetPlayerRadioReceivers(MyCharacter playerCharacter, MyEntity interactedEntityRepresentative, HashSet<MyDataReceiver> output)
        {
            MyDebug.AssertDebug(output.Count == 0, "Output was not cleared before use!");

            if (interactedEntityRepresentative is MyCubeGrid)
                output.UnionWith(MyDataReceiver.GetGridRadioReceivers(interactedEntityRepresentative as MyCubeGrid));

            if (playerCharacter != null)
            {
                output.Add(playerCharacter.RadioReceiver);
            }
        }

        private HashSet<MyDataReceiver> m_tempPlayerReceivers = new HashSet<MyDataReceiver>();
        public void GetPlayerRelayedBroadcasters(MyCharacter playerCharacter, MyEntity interactedEntityRepresentative, HashSet<MyDataBroadcaster> output)
        {
            MyDebug.AssertDebug(output.Count == 0, "Output was not cleared before use!");

            m_tempPlayerReceivers.Clear();
            GetPlayerRadioReceivers(playerCharacter, interactedEntityRepresentative, m_tempPlayerReceivers);

            foreach (var receiver in m_tempPlayerReceivers)
            {
                output.UnionWith(receiver.RelayedBroadcasters);
            }
        }

        private void GridBroadcastersFromPlayer(MyCubeGrid grid, MyEntity interactedEntityRepresentative, long playerId, List<MyDataBroadcaster> output)
        {
            MyDebug.AssertDebug(output.Count == 0, "Output was not cleared before use!");

            var gridBroadcasters = MyRadioBroadcaster.GetGridRelayedBroadcasters(grid, playerId);
            foreach (var broadcaster in gridBroadcasters)
                if (GetBroadcasterParentEntity(broadcaster) == interactedEntityRepresentative)
                    output.Add(broadcaster);
        }

        public MyEntity GetBroadcasterParentEntity(MyDataBroadcaster broadcaster)
        {
            if (broadcaster.Entity is MyCubeBlock)
                return (broadcaster.Entity as MyCubeBlock).CubeGrid;

            return broadcaster.Entity as MyEntity;
        }

        //gets the grid with largest number of blocks in logical group
        public MyCubeGrid GetLogicalGroupRepresentative(MyCubeGrid grid)
        {
            var group = MyCubeGridGroups.Static.Logical.GetGroup(grid);

            if (group == null || group.Nodes.Count == 0)
                return grid;

            MyCubeGrid output = group.Nodes.First().NodeData;
            foreach (var node in group.Nodes)
            {
                if (node.NodeData.GetBlocks().Count > output.GetBlocks().Count)
                    output = node.NodeData;
            }
            return output;
        }

        public class BroadcasterInfo
        {
            public long EntityId;
            public string Name;
        }

        public class BroadcasterInfoComparer : IEqualityComparer<BroadcasterInfo>
        {
            public bool Equals(BroadcasterInfo x, BroadcasterInfo y)
            {
                return x.EntityId == y.EntityId && string.Equals(x.Name, y.Name);
            }

            public int GetHashCode(BroadcasterInfo obj)
            {
                unchecked
                {
                    int result = obj.EntityId.GetHashCode();
                    if (obj.Name != null)
                    {
                        result = (result * 397) ^ obj.Name.GetHashCode();
                    }
                    return result;
                }
            }
        }

        public bool CheckConnection(MyIdentity sender, MyIdentity receiver)
        {
            if (sender == receiver)
            {
                return true;
            }
            var senderRelayedBroadcasters = sender.Character.RadioReceiver.GetRelayedBroadcastersForPlayer(sender.IdentityId);
            var receiverRelayedBroadcasters = receiver.Character.RadioReceiver.GetRelayedBroadcastersForPlayer(receiver.IdentityId);

            foreach (var senderRelayedBroadcaster in senderRelayedBroadcasters)
            {
                //Is the broadcaster in range of sender broadcaster?
                if (VRageMath.Vector3D.Distance(senderRelayedBroadcaster.BroadcastPosition, sender.Character.PositionComp.GetPosition()) > sender.Character.RadioBroadcaster.BroadcastRadius)
                {
                    continue;
                }

                //Relayed broadcasters include antennas and characters 
                if (senderRelayedBroadcaster.Entity is IMyComponentOwner<MyIDModule>)
                {
                    MyIDModule broadcasterId;
                    if ((senderRelayedBroadcaster.Entity as IMyComponentOwner<MyIDModule>).GetComponent(out broadcasterId))
                    {
                        VRage.Game.MyRelationsBetweenPlayerAndBlock relation = broadcasterId.GetUserRelationToOwner(sender.IdentityId);
                        if (relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies || relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral || broadcasterId.Owner == 0)
                            continue;
                    }
                }
                if (senderRelayedBroadcaster.Entity is MyCharacter)
                {
                    var relation = (senderRelayedBroadcaster.Entity as MyCharacter).GetRelationTo(sender.IdentityId);
                    if (relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies || relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral)
                        continue;
                }

                if (receiverRelayedBroadcasters.Contains(senderRelayedBroadcaster))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsBroadcasterConnectedToCharacter(MyRadioBroadcaster broadcaster, MyCharacter character)
        {
            var broadcasterCharacter = broadcaster.Entity as MyCharacter;
            if (broadcasterCharacter == character)
            {
                return true;
            }

            var broadcasterBlock = broadcaster.Entity as MyCubeBlock;
            var characterBlock = character.Parent as MyCubeBlock;
            if (broadcasterBlock != null && characterBlock != null)
            {
                var broadcasterGrid = broadcasterBlock.CubeGrid;
                var characterGrid = characterBlock.CubeGrid;

                MyCubeGrid broadcasterGridRepresentative = GetLogicalGroupRepresentative(broadcasterGrid);
                MyCubeGrid characterGridRepresentative = GetLogicalGroupRepresentative(characterGrid);

                if (broadcasterGridRepresentative == characterGridRepresentative)
                {
                    return true;
                }
            }

            return false;
        }

        public bool CheckConnection(MyCharacter character, MyCubeGrid grid, MyPlayer player)
        {
            return CheckConnection((MyEntity)character, grid, player);
        }

        public bool CheckConnection(MyCubeGrid interactedGrid, MyCubeGrid grid, MyPlayer player)
        {
            return CheckConnection((MyEntity)GetLogicalGroupRepresentative(interactedGrid), grid, player);
        }

        private bool CheckConnection(MyEntity interactedEntityRepresentative, MyCubeGrid grid, MyPlayer player)
        {
            var connectedGrids = GetMutuallyConnectedGrids(interactedEntityRepresentative, player);
            var gridRepresentative = GetLogicalGroupRepresentative(grid);

            foreach (var connectedGrid in connectedGrids)
            {
                if (connectedGrid.EntityId == gridRepresentative.EntityId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
