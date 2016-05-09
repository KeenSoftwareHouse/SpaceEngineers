using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
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
using VRage.Collections;
using VRage.Groups;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Cube;
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Replication
{
    /// <summary>
    /// Responsible for synchronizing entity physics over network
    /// </summary>
    class MyGridPhysicsStateGroup : MyEntityPhysicsStateGroup
    {
        static Func<MyEntity, Vector3D, bool> m_positionValidation = ValidatePosition;

        static List<MyCubeGrid> m_groups = new List<MyCubeGrid>();

        static HashSet<MyCubeGrid> m_tmpRopeGrids = new HashSet<MyCubeGrid>();
        static HashSet<MyRope> m_tmpRopes = new HashSet<MyRope>();

        public new MyCubeGrid Entity { get { return (MyCubeGrid)base.Entity; } }

        int m_currentSentPosition = 0;

        public MyGridPhysicsStateGroup(MyEntity entity, IMyReplicable ownerReplicable)
            : base(entity, ownerReplicable)
        {
        }

        static bool ValidatePosition(MyEntity entity, Vector3D position)
        {
            // TODO: Jump hack, ideally remove (depends on position which comes from server, need to read it first, then check, then apply)
            var grid = entity as MyCubeGrid;
            if (grid != null && grid.PositionComp != null && grid.GridSystems.JumpSystem != null)
            {
                return grid.GridSystems.JumpSystem.CheckReceivedCoordinates(ref position);
            }
            return true;
        }

        /// <summary>
        /// Returns master grid
        /// </summary>
        public static MyCubeGrid GetMasterGrid(MyCubeGrid grid)
        {
            // Biggest grid does the sync
            var group = MyCubeGridGroups.Static.PhysicalDynamic.GetGroup(grid);

            if (group == null)
            {
                return grid;
            }

            float maxRadius = 0;
            MyCubeGrid biggestGrid = null;
            foreach (var node in group.Nodes)
            {
                // Static in never master
                if (node.NodeData.IsStatic)
                    continue;

                // Sort by radius, then by EntityId (to make stable sort of two same-size grids)
                var rad = node.NodeData.PositionComp.LocalVolume.Radius;
                if (rad > maxRadius || (rad == maxRadius && (biggestGrid == null || grid.EntityId > biggestGrid.EntityId)))
                {
                    maxRadius = rad;
                    biggestGrid = node.NodeData;
                }
            }

            return biggestGrid; // Only biggest grid does the sync
        }

        protected override float GetGroupPriority(int frameCountWithoutSync, MyClientInfo client, PrioritySettings settings)
        {
            if (Entity.MarkedForClose)
            {
                return 0.0f;
            }

            if (Entity != GetMasterGrid(Entity))
                return 0;

            return base.GetGroupPriority(frameCountWithoutSync, client, settings);
        }
        
        private void UpdateGridMaxSpeed(MyCubeGrid grid,bool fromServer = true)
        {
            if (Sync.IsServer == false && MyPerGameSettings.EnableMultiplayerVelocityCompensation && grid != null && grid.Physics != null && grid.Physics.RigidBody != null)
            {
                float maxSpeed = grid.GridSizeEnum == MyCubeSize.Large ? MyGridPhysics.LargeShipMaxLinearVelocity() : MyGridPhysics.SmallShipMaxLinearVelocity();
                
                maxSpeed *= Sync.RelativeSimulationRatio;
               
                if (fromServer && grid.Physics.RigidBody.LinearVelocity.LengthSquared() > maxSpeed * maxSpeed)
                {
                    grid.Physics.RigidBody.MaxLinearVelocity = grid.Physics.RigidBody.LinearVelocity.Length();
                }
                else
                {
                    grid.Physics.RigidBody.MaxLinearVelocity = maxSpeed;
                }
            }
        }

        public override bool Serialize(BitStream stream, EndpointId forClient,uint timestamp, byte packetId, int maxBitPosition)
        {
            // Client does not care about slave grids, he always synced group through controlled object
            Debug.Assert(stream.Reading || !Sync.IsServer || Entity == GetMasterGrid(Entity), "Writing data from SlaveGrid!");

            bool apply = !IsControlledLocally;

            bool moving = false;
            if (stream.Writing)
            {
                moving = IsMoving(Entity);
                stream.WriteBool(moving);
            }
            else
            {
                moving = stream.ReadBool();
            }


            // Serialize this grid
            apply = SerializeTransform(stream, Entity, null, m_lowPrecisionOrientation, apply,moving, timestamp, m_positionValidation, MoveHandler);
            SerializeVelocities(stream, Entity, EffectiveSimulationRatio, apply, moving,VelocityHandler);

     
            // Serialize other grids in group
            Vector3D basePos = Entity.WorldMatrix.Translation;
            if (stream.Writing)
            {
                bool fullyWritten = true;
                UpdateGridMaxSpeed(Entity, Sync.IsServer);
                var g = MyCubeGridGroups.Static.PhysicalDynamic.GetGroup(Entity);
                if (g == null)
                {
                    stream.WriteBool(false);
                }
                else
                {
                    m_groups.Clear();
                    int i = 0;
                    foreach (var node in g.Nodes)
                    {                        
                        i++;
                        if (ResponsibleForUpdate(node.NodeData, forClient))
                        {
                            continue;
                        } 

                        if(i < m_currentSentPosition)
                        {
                            continue;
                        }


                        var target = MyMultiplayer.Static.ReplicationLayer.GetProxyTarget((IMyEventProxy)node.NodeData);
                        
                        int pos = stream.BitPosition;

                        if (node.NodeData != Entity && !node.NodeData.IsStatic && target != null)
                        {                 
                            stream.WriteBool(true);
                            // ~26.5 bytes per grid, not bad
                            NetworkId networkId = MyMultiplayer.Static.ReplicationLayer.GetNetworkIdByObject(target);
                            stream.WriteNetworkId(networkId); // ~2 bytes
             
                            moving = IsMoving(node.NodeData);
                            stream.WriteBool(moving);

                            SerializeTransform(stream, node.NodeData, basePos, m_lowPrecisionOrientation, apply, moving, timestamp, null, null); // 12.5 bytes
                            SerializeVelocities(stream, node.NodeData, EffectiveSimulationRatio, apply, moving); // 12 byte
                            UpdateGridMaxSpeed(node.NodeData, Sync.IsServer);
                            m_groups.Add(node.NodeData);

                            m_currentSentPosition++;
                        }

                        if (stream.BitPosition > maxBitPosition)
                        {
                            stream.SetBitPositionWrite(pos);
                            fullyWritten = false;
                            m_currentSentPosition--;
                            break;
                        }

                        if (i == g.Nodes.Count)
                        {
                            m_currentSentPosition = 0;
                        }
                    }

                    stream.WriteBool(false);
                }

                stream.WriteBool(fullyWritten);

                if (fullyWritten)
                {
                    SerializeRopeData(stream, apply, gridsGroup: m_groups);
                }
                return fullyWritten;

            }
            else
            {
                UpdateGridMaxSpeed(Entity, !Sync.IsServer);

                while (stream.ReadBool())
                {               
                    NetworkId networkId = stream.ReadNetworkId(); // ~2 bytes
                    MyCubeGridReplicable replicable = MyMultiplayer.Static.ReplicationLayer.GetObjectByNetworkId(networkId) as MyCubeGridReplicable;
                    MyCubeGrid grid = replicable != null ? replicable.Grid : null;

                    moving = stream.ReadBool();
                    SerializeTransform(stream, grid, basePos, m_lowPrecisionOrientation, apply && grid != null, moving, timestamp, null, null); // 12.5 bytes
                    SerializeVelocities(stream, grid, EffectiveSimulationRatio, apply && grid != null, moving); // 12 bytes
                   
                    UpdateGridMaxSpeed(grid,!Sync.IsServer);
                }

                if (stream.ReadBool())
                {
                    SerializeRopeData(stream, apply);
                }
            }
            return true;
        }

        private void SerializeRopeData(BitStream stream, bool applyWhenReading, List<MyCubeGrid> gridsGroup = null)
        {
            if (MyRopeComponent.Static == null)
                return;

            if (stream.Writing)
            {
                m_tmpRopes.Clear();
                m_tmpRopeGrids.Clear();

                m_tmpRopeGrids.Add(Entity);
                Debug.Assert(gridsGroup != null);
                if (gridsGroup != null) 
                {
                    foreach (var grid in gridsGroup)
                        m_tmpRopeGrids.Add(grid);
                }

                MyRopeComponent.Static.GetRopesForGrids(m_tmpRopeGrids, m_tmpRopes);

                MyRopeData ropeData;

                stream.WriteUInt16((ushort)m_tmpRopes.Count);
                foreach (var rope in m_tmpRopes)
                {
                    var ropeProxyTarget = MyMultiplayer.Static.ReplicationLayer.GetProxyTarget((IMyEventProxy)rope);
                    NetworkId ropeNetworkId = MyMultiplayer.Static.ReplicationLayer.GetNetworkIdByObject(ropeProxyTarget);
                    stream.WriteNetworkId(ropeNetworkId);

                    //TODO - MyRopeComponent should be rewritten to singleton
                    MyRopeComponent.GetRopeData(rope.EntityId, out ropeData);
                    stream.WriteFloat(ropeData.CurrentRopeLength);
                }

                m_tmpRopes.Clear();
                m_tmpRopeGrids.Clear();
            }
            else
            {
                uint ropesCount = stream.ReadUInt16();
                for (uint i = 0; i < ropesCount; ++i)
                {
                    NetworkId ropeNetworkId = stream.ReadNetworkId();
                    float ropeLength = stream.ReadFloat();

                    MyRopeReplicable replicable = MyMultiplayer.Static.ReplicationLayer.GetObjectByNetworkId(ropeNetworkId) as MyRopeReplicable;
                    MyRope rope = replicable != null ? replicable.Instance : null;

                    if (rope != null && applyWhenReading)
                        MyRopeComponent.Static.SetRopeLengthSynced(rope.EntityId, ropeLength);
                }
            }

        }

        private bool IsGroupControlledByAnyPlayer(List<MyCubeGrid> gridsGroup)
        {
            MyPlayer.PlayerId playerId;
            bool controlled = Sync.Players.ControlledEntities.TryGetValue(Entity.EntityId, out playerId);
            if (controlled)
                return true;

            if (gridsGroup != null)
            {
                foreach (var grid in gridsGroup)
                {
                    controlled = Sync.Players.ControlledEntities.TryGetValue(Entity.EntityId, out playerId);
                    if (controlled)
                        return true;
                }
            }

            return false;
        }
    }
}
