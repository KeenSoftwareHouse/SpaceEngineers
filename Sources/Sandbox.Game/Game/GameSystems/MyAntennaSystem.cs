using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Linq;
using VRage.Utils;


namespace Sandbox.Game.GameSystems
{
    static class MyAntennaSystem
    {
        static List<long> m_addedItems = new List<long>();
        static HashSet<BroadcasterInfo> m_output = new HashSet<BroadcasterInfo>(new BroadcasterInfoComparer());
        static HashSet<MyDataBroadcaster> m_tempPlayerRelayedBroadcasters = new HashSet<MyDataBroadcaster>();
        static List<MyDataBroadcaster> m_tempGridBroadcastersFromPlayer = new List<MyDataBroadcaster>();

        public static HashSet<BroadcasterInfo> GetMutuallyConnectedGrids(MyEntity interactedEntityRepresentative, MyPlayer player = null)
        {
            if (player == null)
            {
                player = MySession.LocalHumanPlayer;
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
                if (broadcaster.Parent == null || !(broadcaster.Parent is MyCubeBlock) || (broadcaster.Parent is MyBeacon))
                    continue;

                MyIDModule module;
                if ((broadcaster.Parent as IMyComponentOwner<MyIDModule>).GetComponent(out module))
                {
                    if (!((MyTerminalBlock)broadcaster.Parent).HasPlayerAccess(playerIdentity.IdentityId) || module.Owner == 0)
                        continue;
                }
                else
                    continue;

                var ownerCubeGrid = (broadcaster.Parent as MyCubeBlock).CubeGrid;

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

        private static void GetPlayerRadioReceivers(MyCharacter playerCharacter, MyEntity interactedEntityRepresentative, HashSet<MyDataReceiver> output)
        {
            MyDebug.AssertDebug(output.Count == 0, "Output was not cleared before use!");

            if (interactedEntityRepresentative is MyCubeGrid)
                output.UnionWith(MyDataReceiver.GetGridRadioReceivers(interactedEntityRepresentative as MyCubeGrid));

            if (playerCharacter != null)
            {
                output.Add(playerCharacter.RadioReceiver);
            }
        }

        private static HashSet<MyDataReceiver> m_tempPlayerReceivers = new HashSet<MyDataReceiver>();
        public static void GetPlayerRelayedBroadcasters(MyCharacter playerCharacter, MyEntity interactedEntityRepresentative, HashSet<MyDataBroadcaster> output)
        {
            MyDebug.AssertDebug(output.Count == 0, "Output was not cleared before use!");

            m_tempPlayerReceivers.Clear();
            GetPlayerRadioReceivers(playerCharacter, interactedEntityRepresentative, m_tempPlayerReceivers);

            foreach (var receiver in m_tempPlayerReceivers)
            {
                output.UnionWith(receiver.RelayedBroadcasters);
            }
        }

        private static void GridBroadcastersFromPlayer(MyCubeGrid grid, MyEntity interactedEntityRepresentative, long playerId, List<MyDataBroadcaster> output)
        {
            MyDebug.AssertDebug(output.Count == 0, "Output was not cleared before use!");

            var gridBroadcasters = MyRadioBroadcaster.GetGridRelayedBroadcasters(grid, playerId);
            var controlledObjectId = interactedEntityRepresentative.EntityId;
            foreach (var broadcaster in gridBroadcasters)
                if (GetBroadcasterParentEntityId(broadcaster) == controlledObjectId)
                    output.Add(broadcaster);
        }

        public static long GetBroadcasterParentEntityId(MyDataBroadcaster broadcaster)
        {
            if (broadcaster.Parent is MyCubeBlock)
                return (broadcaster.Parent as MyCubeBlock).CubeGrid.EntityId;

            else if (broadcaster.Parent is MyCharacter)
                return (broadcaster.Parent as MyCharacter).EntityId;

            return 0;
        }

        //gets the grid with largest number of blocks in logical group
        public static MyCubeGrid GetLogicalGroupRepresentative(MyCubeGrid grid)
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

        public static bool CheckConnection(MyIdentity sender, MyIdentity receiver)
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
                if (senderRelayedBroadcaster.Parent is IMyComponentOwner<MyIDModule>)
                {
                    MyIDModule broadcasterId;
                    if ((senderRelayedBroadcaster.Parent as IMyComponentOwner<MyIDModule>).GetComponent(out broadcasterId))
                    {
                        Sandbox.Common.MyRelationsBetweenPlayerAndBlock relation = broadcasterId.GetUserRelationToOwner(sender.IdentityId);
                        if (relation == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies || relation == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Neutral || broadcasterId.Owner == 0)
                            continue;
                    }
                }
                if (senderRelayedBroadcaster.Parent is MyCharacter)
                {
                    var relation = (senderRelayedBroadcaster.Parent as MyCharacter).GetRelationTo(sender.IdentityId);
                    if (relation == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Enemies || relation == Sandbox.Common.MyRelationsBetweenPlayerAndBlock.Neutral)
                        continue;
                }

                if (receiverRelayedBroadcasters.Contains(senderRelayedBroadcaster))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBroadcasterConnectedToCharacter(MyRadioBroadcaster broadcaster, MyCharacter character)
        {
            var broadcasterCharacter = broadcaster.Parent as MyCharacter;
            if (broadcasterCharacter == character)
            {
                return true;
            }

            var broadcasterBlock = broadcaster.Parent as MyCubeBlock;
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

        public static bool CheckConnection(MyCharacter character, MyCubeGrid grid, MyPlayer player)
        {
            return CheckConnection((MyEntity)character, grid, player);
        }

        public static bool CheckConnection(MyCubeGrid interactedGrid, MyCubeGrid grid, MyPlayer player)
        {
            return CheckConnection((MyEntity)MyAntennaSystem.GetLogicalGroupRepresentative(interactedGrid), grid, player);
        }

        private static bool CheckConnection(MyEntity interactedEntityRepresentative, MyCubeGrid grid, MyPlayer player)
        {
            var connectedGrids = MyAntennaSystem.GetMutuallyConnectedGrids(interactedEntityRepresentative, player);
            var gridRepresentative = MyAntennaSystem.GetLogicalGroupRepresentative(grid);

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
