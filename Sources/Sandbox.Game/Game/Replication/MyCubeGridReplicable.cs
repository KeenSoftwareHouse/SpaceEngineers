using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Replication
{
    class MyCubeGridReplicable : MyEntityReplicableBaseEvent<MyCubeGrid>, IMyStreamableReplicable
    {
        Action<MyCubeGrid> m_loadingDoneHandler;
        MyStreamingEntityStateGroup<MyCubeGridReplicable> m_streamingGroup;
        static List<MyCubeGrid> m_groups = new List<MyCubeGrid>();
        List<MyObjectBuilder_EntityBase> m_builders;
        List<MyEntity> m_foundEntities = null;

        private MyPropertySyncStateGroup m_propertySync;
        public MyCubeGrid Grid { get { return Instance; } }

        protected override IMyStateGroup CreatePhysicsGroup()
        {
            return new MyGridPhysicsStateGroup(Instance, this);
        }

        public override float GetPriority(MyClientInfo state)
        {
            if (Grid == null || Grid.Projector != null)
                return 0.0f;

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
                            var cubeblock = broadcaster.Entity as MyCubeBlock;
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

        public override bool OnSave(BitStream stream)
        {
            if (MyFakes.ENABLE_SENT_GROUP_AT_ONCE)
            {
                MyCubeGrid master = MyGridPhysicsStateGroup.GetMasterGrid(Grid);
                if (master != Grid)
                {
                    stream.WriteBool(true);
                    stream.WriteInt64(Grid.EntityId);
                    return true;
                }
            }

            if (Grid.IsSplit)
            {
                stream.WriteBool(true);
                stream.WriteInt64(Grid.EntityId);
                return true;
            }
            return false;
        }

        protected override void OnLoad(BitStream stream, Action<MyCubeGrid> loadingDoneHandler)
        {
            bool isSplit = stream.ReadBool();
            MyCubeGrid grid = null;
            if (isSplit)
            {
                long gridId = stream.ReadInt64();
                MyEntities.CallAsync(() => loadingDoneHandler(FindGrid(gridId)));
            }
            else 
            {
                grid = new MyCubeGrid();
                grid.DebugCreatedBy = DebugCreatedBy.FromServer;
                var builder = VRage.Serialization.MySerializer.CreateAndRead<MyObjectBuilder_EntityBase>(stream, MyObjectBuilderSerializer.Dynamic);
               
                Byte numSubGrids = stream.ReadByte();

                bool hasSubGrids = numSubGrids > 0;
                for (int i = 0; i < numSubGrids; ++i)
                {
                    var subGridBuilder = VRage.Serialization.MySerializer.CreateAndRead<MyObjectBuilder_EntityBase>(stream, MyObjectBuilderSerializer.Dynamic);
                    if(m_builders == null)
                    {
                        m_builders = new List<MyObjectBuilder_EntityBase>();
                    }
                    m_builders.Add(subGridBuilder);
                }

                if (hasSubGrids == false)
                {
                    MyEntities.InitAsync(grid, builder, true, (e) => loadingDoneHandler(grid));
                }
                else
                {
                    MyEntities.InitAsync(grid, builder, true, (e) => loadingDoneHandler(grid), m_builders);
                }
            }
        }

        MyCubeGrid FindGrid(long gridEntityId)
        {
            MyCubeGrid result;
            MyEntities.TryGetEntityById<MyCubeGrid>(gridEntityId, out result);

            if (result == null)
            {
                MyLog.Default.WriteLine("Failed to attach to gird after split");
                //   Debug.Fail("Grid for grid split not found");
            }
            return result;
        }

        public override void GetStateGroups(List<IMyStateGroup> resultList)
        {
            base.GetStateGroups(resultList);
            resultList.Add(m_propertySync);
        }

        protected override void OnHook()
        {
            Debug.Assert(MyMultiplayer.Static != null, "Should not get here without multiplayer");
            base.OnHook();
            m_propertySync = new MyPropertySyncStateGroup(this, Grid.SyncType);
            if (Sync.IsServer == false)
            {
                MyPlayerCollection.UpdateControl(Grid.EntityId);
            }
        }

        override protected void OnLoadBegin(BitStream stream, Action<MyCubeGrid> loadingDoneHandler)
        {
            m_loadingDoneHandler = loadingDoneHandler;
        }

        public IMyStateGroup GetStreamingStateGroup()
        {
            if (m_streamingGroup == null)
            {
                m_streamingGroup = new MyStreamingEntityStateGroup<MyCubeGridReplicable>(this);
            }
            return m_streamingGroup;
        }

        public void Serialize(BitStream stream)
        {
            stream.WriteBool(false);

            MyCubeGrid master = MyGridPhysicsStateGroup.GetMasterGrid(Grid);
            if (master == Grid || MyFakes.ENABLE_SENT_GROUP_AT_ONCE == false)
            {
                var builder = Grid.GetObjectBuilder();
                VRage.Serialization.MySerializer.Write(stream, ref builder, MyObjectBuilderSerializer.Dynamic);

                var g = MyCubeGridGroups.Static.PhysicalDynamic.GetGroup(Grid);
                if (g == null || MyFakes.ENABLE_SENT_GROUP_AT_ONCE == false)
                {
                    stream.WriteByte(0);
                }
                else
                {
                    m_groups.Clear();
                    foreach (var node in g.Nodes)
                    {
                        var target = MyMultiplayer.Static.ReplicationLayer.GetProxyTarget((IMyEventProxy)node.NodeData);
                        if (node.NodeData != Grid && !node.NodeData.IsStatic && target != null)
                        {
                            m_groups.Add(node.NodeData);
                        }
                    }

                    stream.WriteByte((byte)m_groups.Count); // Ignoring self
                    foreach (var node in m_groups)
                    {
                        builder = node.GetObjectBuilder();
                        VRage.Serialization.MySerializer.Write(stream, ref builder, MyObjectBuilderSerializer.Dynamic);
                    }
                }
            }
        }

        public void LoadDone(BitStream stream)
        {
            OnLoad(stream, m_loadingDoneHandler);
        }

        public float PriorityScale()
        {
            return 1.0f;
        }

        public bool NeedsToBeStreamed
        {
            get
            {
                if (Sync.IsServer)
                {
                    MyCubeGrid master = MyGridPhysicsStateGroup.GetMasterGrid(Grid);
                    if (master != Grid && MyFakes.ENABLE_SENT_GROUP_AT_ONCE)
                    {
                        return false;
                    }
                    return Grid.IsSplit == false;
                }
                return m_streamingGroup != null;
            }
        }

        public override IMyReplicable GetDependency()
        {
            if (m_physicsSync == null || Grid.IsStatic)
            {
                return null;
            }

            MyCubeGrid master = MyGridPhysicsStateGroup.GetMasterGrid(Grid);
            if (master != Grid)
            {
                return MyExternalReplicable.FindByObject(master);
            }

            BoundingBoxD box = Grid.PositionComp.WorldAABB;
            var group = MyCubeGridGroups.Static.PhysicalDynamic.GetGroup(Grid);
            if(group != null)
            {
                foreach (var node in group.Nodes)
                {
                   box.Include(node.NodeData.PositionComp.WorldAABB);
                }
            }

            if(m_foundEntities == null)
            {
                m_foundEntities = new List<MyEntity>();
            }

            m_foundEntities.Clear();

            MyGamePruningStructure.GetTopMostEntitiesInBox(ref box, m_foundEntities);

            float maxRadius = 0;
            MyCubeGrid biggestGrid = null;
            foreach (var entity in m_foundEntities)
            {
                MyCubeGrid grid = entity as MyCubeGrid;

                if (grid != null)
                {
                    // Dont check for projections
                    if (grid.Projector != null)
                        continue;

                    var rad = grid.PositionComp.LocalVolume.Radius;
                    if (rad > maxRadius || (rad == maxRadius && (biggestGrid == null || grid.EntityId > biggestGrid.EntityId)))
                    {
                        maxRadius = rad;
                        biggestGrid = grid;
                    }
                }
            }

            if (biggestGrid != null && biggestGrid != Grid)
            {
                return MyExternalReplicable.FindByObject(biggestGrid);
            }

            return null;
        }

        public void LoadCancel()
        {
            m_loadingDoneHandler(null);
        }
    }
}
