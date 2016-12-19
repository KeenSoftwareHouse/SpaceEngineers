using Sandbox.Common;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyIslandSyncComponent : MySessionComponentBase
    {
        protected static Color[] m_colors = { new Color(0,192,192), Color.Orange, Color.BlueViolet * 1.5f, Color.BurlyWood, Color.Chartreuse,
                                  Color.CornflowerBlue, Color.Cyan, Color.ForestGreen, Color.Fuchsia,
                                  Color.Gold, Color.GreenYellow, Color.LightBlue, Color.LightGreen, Color.LimeGreen,
                                  Color.Magenta, Color.MintCream, Color.Orchid, Color.PeachPuff, Color.Purple };

        public static MyIslandSyncComponent Static = null;

        List<Havok.HkRigidBody> m_rigidBodies = new List<Havok.HkRigidBody>();


        public struct IslandData
        {
            public HashSet<IMyEntity> RootEntities;
            public BoundingBoxD AABB;
            public Dictionary<ulong, float> ClientPriority;
        }

        List<IslandData> m_rootIslands = new List<IslandData>();
        Dictionary<IMyEntity, int> m_rootEntityIslandIndex = new Dictionary<IMyEntity, int>();

        public override void LoadData()
        {
            base.LoadData();

            Static = this;

            VRage.Game.Components.MyPositionComponent.SynchronizationEnabled = false;
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            Static = null;
        }

        public override bool IsRequiredByGame
        {
            get
            {
                return MyFakes.MP_ISLANDS;
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            m_rootIslands.Clear();
            m_rootEntityIslandIndex.Clear();

            var clusterList = MyPhysics.GetClusterList();

            if (clusterList != null)
            {
                foreach (Havok.HkWorld havokWorld in MyPhysics.GetClusterList())
                {
                    var islandCount = havokWorld.GetActiveSimulationIslandsCount();

                    for (int i = 0; i < islandCount; i++)
                    {
                        havokWorld.GetActiveSimulationIslandRigidBodies(i, m_rigidBodies);

                        HashSet<IMyEntity> island = null;

                        foreach (var rigidBody in m_rigidBodies)
                        {
                            var ents = rigidBody.GetAllEntities();
                            foreach (var entity in ents)
                            {
                                var topParent = entity.GetTopMostParent();

                                foreach (var rootIsland in m_rootIslands)
                                {
                                    if (rootIsland.RootEntities.Contains(topParent))
                                    {
                                        island = rootIsland.RootEntities;
                                        break;
                                    }
                                }
                            }
                            ents.Clear();
                        }

                        if (island == null)
                        {
                            IslandData islandData = new IslandData()
                            {
                                AABB = BoundingBoxD.CreateInvalid(),
                                RootEntities = new HashSet<IMyEntity>(),
                                ClientPriority = new Dictionary<ulong,float>()
                            };
                            island = islandData.RootEntities;
                            m_rootIslands.Add(islandData);
                        }

                        foreach (var rigidBody in m_rigidBodies)
                        {
                            var ents = rigidBody.GetAllEntities();
                            foreach (var entity in ents)
                            {
                                var topParent = entity.GetTopMostParent();
                                island.Add(topParent);
                            }
                            ents.Clear();
                        }

                        m_rigidBodies.Clear();
                    }
                }

                for (int i = 0; i < m_rootIslands.Count; i++)
                {
                    var islandData = m_rootIslands[i];
                    islandData.AABB = BoundingBoxD.CreateInvalid();

                    foreach (var entity in islandData.RootEntities)
                    {
                        islandData.AABB.Include(entity.PositionComp.WorldAABB);

                        m_rootEntityIslandIndex[entity] = i;
                    }

                    m_rootIslands[i] = islandData;
                }
            }
        }



        protected static Color IndexToColor(int index)
        {
            return m_colors[index % m_colors.Length];
        }

        public override void Draw()
        {
            base.Draw();

            int isl = 0;

            foreach (var island in m_rootIslands)
            {
                VRageRender.MyRenderProxy.DebugDrawAABB(island.AABB, IndexToColor(isl));

                string islandInfo = "Island " + isl + " : " + island.RootEntities.Count + " root entities. Priorities: ";

                int c = 0;
                foreach (var client in island.ClientPriority)
                {
                    islandInfo += "Client" + c + ": " + client.Value;
                    c++;
                }

                VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(100, isl * 15), islandInfo, IndexToColor(isl), 0.7f);

                isl++;
            }
        }

        public bool GetIslandAABBForEntity(IMyEntity entity, out BoundingBoxD aabb)
        {
            int islandIndex;
            aabb = BoundingBoxD.CreateInvalid();
            if (m_rootEntityIslandIndex.TryGetValue(entity, out islandIndex))
            {
                aabb = m_rootIslands[islandIndex].AABB;
                return true;
            }

            return false;
        }

        public void SetPriorityForIsland(IMyEntity entity, ulong client, float priority)
        {
            int islandIndex;
            if (m_rootEntityIslandIndex.TryGetValue(entity, out islandIndex))
            {
                m_rootIslands[islandIndex].ClientPriority[client] = priority;
            }
        }
    }
}