using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.SessionComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using VRage;
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
        const int ANTENNA_UPDATE_TIME = 50;
        Action<MyCubeGrid> m_loadingDoneHandler;
        StateGroups.MyStreamingEntityStateGroup<MyCubeGridReplicable> m_streamingGroup;
        static List<MyCubeGrid> m_groups = new List<MyCubeGrid>();
        List<MyObjectBuilder_EntityBase> m_builders;
        //List<MyEntity> m_foundEntities = null;
        Dictionary<ulong, int> m_clientState = null;
        HashSet<Sandbox.ModAPI.Ingame.IMyBeacon> m_visibilityAffectors = new HashSet<ModAPI.Ingame.IMyBeacon>();

        //MyEntityPositionVerificationStateGroup m_posVerGroup;

        private StateGroups.MyPropertySyncStateGroup m_propertySync;
        public MyCubeGrid Grid { get { return Instance; } }

        protected override IMyStateGroup CreatePhysicsGroup()
        {
            //m_posVerGroup = new MyGridPositionVerificationStateGroup(Instance);
            return new StateGroups.MyEntityPhysicsStateGroup(Instance, this);
        }

        protected override float BaseVisibility
        {
            get
            {
                float radius = 0;

                foreach (var affector in m_visibilityAffectors)
                {
                    if (affector.IsWorking && affector.Radius > radius)
                    {
                        radius = affector.Radius;
                    }
                }

                return Math.Max(base.BaseVisibility, radius);
            }
        }

        public override float GetPriority(MyClientInfo state, bool cached)
        {
            if (Grid == null || Grid.Projector != null || Grid.IsPreview)
                return 0.0f;

            if (MyFakes.MP_ISLANDS)
            {
                var parent = Instance.GetTopMostParent();

                BoundingBoxD aabb;

                if (MyIslandSyncComponent.Static.GetIslandAABBForEntity(parent, out aabb))
                {
                    var ipriority = GetBasePriority(aabb.Center, aabb.Size, state);

                    MyIslandSyncComponent.Static.SetPriorityForIsland(Instance, state.EndpointId.Value, ipriority);

                    return ipriority;
                }
        
            }


            ulong clientEndpoint = state.EndpointId.Value;
            if (cached)
            {
                if (m_cachedPriorityForClient != null && m_cachedPriorityForClient.ContainsKey(clientEndpoint))
                {
                    return m_cachedPriorityForClient[clientEndpoint];
                }
            }

            if (m_cachedPriorityForClient == null)
            {
                m_cachedPriorityForClient = new Dictionary<ulong, float>();
            }

            float priority = base.GetPriority(state,cached);
            m_cachedPriorityForClient[clientEndpoint] = priority;

            if (priority < 0.1f)
            {
                if (m_clientState == null)
                {
                    m_clientState = new Dictionary<ulong, int>();
                }

                if (m_clientState.ContainsKey(clientEndpoint))
                {
                    m_clientState[state.EndpointId.Value]++;
                }
                else
                {
                    m_clientState[state.EndpointId.Value] = 0;
                }

                if (m_clientState[state.EndpointId.Value] % ANTENNA_UPDATE_TIME == 0)
                {
                    m_clientState[state.EndpointId.Value] = 0;
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
                                    m_cachedPriorityForClient[clientEndpoint] = 0.1f;
                                    return m_cachedPriorityForClient[clientEndpoint]; // Minimal priority, update, but not that often
                                }
                            }

                        }
                    }
                }
                else
                {
                    return m_cachedPriorityForClient[clientEndpoint];
                }
            }

            return priority;
        }

        public override bool OnSave(BitStream stream)
        {
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
            //resultList.Add(m_posVerGroup);
        }

        protected override void OnHook()
        {
            Debug.Assert(MyMultiplayer.Static != null, "Should not get here without multiplayer");
            base.OnHook();
            m_propertySync = new StateGroups.MyPropertySyncStateGroup(this, Grid.SyncType);
            if (Sync.IsServer == false)
            {
                MyPlayerCollection.UpdateControl(Grid.EntityId);

                foreach (var block in Instance.CubeBlocks)  // CubeBlocks is getter of readonly field m_cubeBlocks, should never be null (but check it!)
                {
                    if (block.FatBlock is MyCockpit)
                    {
                        // block.FatBlock is not null
                        MyPlayerCollection.UpdateControl(block.FatBlock.EntityId);

                        if (MySession.Static.LocalHumanPlayer != null 
                            && MySession.Static.LocalHumanPlayer.Controller != null 
                            && MySession.Static.LocalHumanPlayer.Controller.ControlledEntity == block.FatBlock)
                        {
                            MySession.Static.SetCameraController(VRage.Game.MyCameraControllerEnum.Entity, block.FatBlock);
                            break;
                        }
                    }
                }
            }

            Grid.OnBlockAdded += Grid_OnBlockAdded;
            Grid.OnBlockRemoved += Grid_OnBlockRemoved;
        }

        void Grid_OnBlockRemoved(MySlimBlock obj)
        {
            if (obj.FatBlock is IMyBeacon)
                m_visibilityAffectors.Remove(obj.FatBlock as IMyBeacon);
        }

        void Grid_OnBlockAdded(MySlimBlock obj)
        {
            if (obj.FatBlock is IMyBeacon)
                m_visibilityAffectors.Add(obj.FatBlock as IMyBeacon);
        }

        override protected void OnLoadBegin(BitStream stream, Action<MyCubeGrid> loadingDoneHandler)
        {
            m_loadingDoneHandler = loadingDoneHandler;
        }

        public IMyStateGroup GetStreamingStateGroup()
        {
            if (m_streamingGroup == null)
            {
                m_streamingGroup = new StateGroups.MyStreamingEntityStateGroup<MyCubeGridReplicable>(this, this);
            }
            return m_streamingGroup;
        }

        public void Serialize(BitStream stream)
        {
            stream.WriteBool(false);

            var builder = Grid.GetObjectBuilder();
            try
            {
                VRage.Serialization.MySerializer.Write(stream, ref builder, MyObjectBuilderSerializer.Dynamic);
            }
            catch (Exception e)
            {
                XmlSerializer serializer = MyXmlSerializerManager.GetSerializer(builder.GetType());
                MyLog.Default.WriteLine("Grid data - START");
                try
                {
                    serializer.Serialize(MyLog.Default.GetTextWriter(), builder);
                }
                catch
                {
                    MyLog.Default.WriteLine("Failed");
                }
                MyLog.Default.WriteLine("Grid data - END");
                throw;
            }
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
                    var target = MyMultiplayer.Static.ReplicationLayer.GetProxyTarget((IMyEventProxy) node.NodeData);
                    if (node.NodeData != Grid && !node.NodeData.IsStatic && target != null)
                    {
                        m_groups.Add(node.NodeData);
                    }
                }

                stream.WriteByte((byte) m_groups.Count); // Ignoring self
                foreach (var node in m_groups)
                {
                    builder = node.GetObjectBuilder();
                    try
                    {
                        VRage.Serialization.MySerializer.Write(stream, ref builder, MyObjectBuilderSerializer.Dynamic);
                    }
                    catch (Exception e)
                    {
                        XmlSerializer serializer = MyXmlSerializerManager.GetSerializer(builder.GetType());
                        MyLog.Default.WriteLine("Grid data - START");
                        try
                        {
                            serializer.Serialize(MyLog.Default.GetTextWriter(), builder);
                        }
                        catch
                        {
                            MyLog.Default.WriteLine("Failed");
                        }
                        MyLog.Default.WriteLine("Grid data - END");
                        throw;
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
                    return Grid.IsSplit == false;
                }
                return m_streamingGroup != null;
            }
        }

        public override IMyReplicable GetParent()
        {
            if (m_physicsSync == null || Grid == null || Grid.IsStatic)
            {
                return null;
            }

            /*MyCubeGrid master = MyGridPhysicsStateGroup.GetMasterGrid(Grid);
            if (master != Grid)
            {
                if (master != null)
                {
                    return MyExternalReplicable.FindByObject(master);
                }
                return null;
            }*/

            float maxRadius = 0;
            MyCubeGrid biggestGrid = null;

            BoundingBoxD box = Grid.PositionComp.WorldAABB;
            var group = MyCubeGridGroups.Static.PhysicalDynamic.GetGroup(Grid);
            if(group != null)
            {
                foreach (var node in group.Nodes)
                {
                  // box.Include(node.NodeData.PositionComp.WorldAABB);

                    MyCubeGrid grid = node.NodeData as MyCubeGrid;

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
            }


            if (biggestGrid != null && biggestGrid != Grid)
            {
                return MyExternalReplicable.FindByObject(biggestGrid);
            }

            return null;

            //if(m_foundEntities == null)
            //{
            //    m_foundEntities = new List<MyEntity>();
            //}

            //m_foundEntities.Clear();

            //MyGamePruningStructure.GetTopMostEntitiesInBox(ref box, m_foundEntities);

          
        }

        public void LoadCancel()
        {
            m_loadingDoneHandler(null);
        }

    }
}
