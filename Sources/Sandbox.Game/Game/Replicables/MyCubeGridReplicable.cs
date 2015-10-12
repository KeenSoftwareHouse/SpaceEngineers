using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replicables;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;


namespace Sandbox.Game.Replicables
{
    class MyCubeGridReplicable : MyEntityReplicableBaseEvent<MyCubeGrid>
    {
        public MyCubeGrid Grid { get { return Instance; } }

        protected override IMyStateGroup CreatePhysicsGroup()
        {
            return new MyGridPhysicsStateGroup(Instance, this);
        }

        public override float GetPriority(MyClientStateBase state)
        {
            float priority = base.GetPriority(state);
            if (priority == 0.0f)
            {
                MyPlayerCollection playerCollection = MySession.Static.Players;

                var connectedPlayers = playerCollection.GetOnlinePlayers();

                foreach (var player in connectedPlayers)
                {
                    if (player.Client.SteamUserId == state.EndpointId.Value && player.Character != null)
                    {
                        var broadcasters = player.Character.RadioReceiver.GetRelayedBroadcastersForPlayer(player.Identity.IdentityId);
                        foreach (var broadcaster in broadcasters)
                        {
                            var cubeblock = broadcaster.Parent as MyCubeBlock;
                            if (cubeblock != null && cubeblock.CubeGrid == Grid)
                            {
                                return 0.1f; // Minimal priority, update, but not that often
                            }
                        }

                    }
                }
            }
            return priority;
        }

        public override void OnSave(BitStream stream)
        {
            stream.WriteBool(Grid.IsSplit);
            if (Grid.IsSplit)
            {
                stream.WriteInt64(Grid.EntityId);
            }
            else
            {
                base.OnSave(stream);
            }
        }

        protected override void OnLoad(BitStream stream, Action<MyCubeGrid> loadingDoneHandler)
        {
            bool isSplit = stream.ReadBool();
            MyCubeGrid grid = null;
            if (isSplit)
            {
                long gridId = stream.ReadInt64();
                MyEntity entity;
                MyEntities.TryGetEntityById(gridId, out entity);
                grid = entity as MyCubeGrid;
                loadingDoneHandler(grid);
            }
            else
            {
                base.OnLoad(stream, loadingDoneHandler);
                //grid = new MyCubeGrid();
                //var builder = VRage.Serialization.MySerializer.CreateAndRead<MyObjectBuilder_EntityBase>(stream, MyObjectBuilderSerializer.Dynamic);
                //MyEntities.InitAsync(grid, builder, true, (e) => loadingDoneHandler(grid));
            }
        }
    }
}
