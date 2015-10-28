using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// Responsible for synchronizing entity physics over network
    /// </summary>
    class MyGridPhysicsStateGroup : MyEntityPhysicsStateGroup
    {
        public MyGridPhysicsStateGroup(MyEntity entity, IMyReplicable ownerReplicable)
            : base(entity, ownerReplicable)
        {
        }

        /// <summary>
        /// Returns true when grid is part of group and is not the master (who does the sync)
        /// </summary>
        bool IsSlaveGrid()
        {
            Debug.Assert(Entity is MyCubeGrid, "MyGridPhysicsStateGroup should work only on grids");

            // Biggest grid does the sync
            var group = MyCubeGridGroups.Static.Physical.GetGroup((MyCubeGrid)Entity);
            float maxRadius = 0;
            MyCubeGrid biggestGrid = null;
            foreach (var node in group.Nodes)
            {
                // Sort by radius, then by EntityId (to make stable sort of two same-size grids)
                var rad = node.NodeData.PositionComp.LocalVolume.Radius;
                if (rad > maxRadius || (rad == maxRadius && Entity.EntityId > biggestGrid.EntityId))
                {
                    maxRadius = rad;
                    biggestGrid = node.NodeData;
                }
            }

            return biggestGrid != Entity; // Only biggest grid does the sync
        }

        protected override float GetGroupPriority(int frameCountWithoutSync, MyClientStateBase client, PrioritySettings settings)
        {
            if (IsSlaveGrid())
                return 0;

            return base.GetGroupPriority(frameCountWithoutSync, client, settings);
        }

        // Physics state
        //void SerializePhysics(BitStream stream)
        //{
        //    if (stream.Reading)
        //    {
        //        Matrix world = Matrix.Identity;
        //        bool moving = false;
        //        Vector3 linearVelocity = Vector3.Zero;
        //        Vector3 angularVelocity = Vector3.Zero;

        //        stream.SerializePositionOrientation(ref world);
        //        stream.Serialize(ref moving);
        //        if (moving)
        //        {
        //            stream.Serialize(ref linearVelocity);
        //            stream.Serialize(ref angularVelocity);
        //        }

        //        PositionComp.SetWorldMatrix(world);
        //        Physics.LinearVelocity = linearVelocity;
        //        Physics.AngularVelocity = angularVelocity;
        //    }
        //    else
        //    {
        //        Matrix world = PositionComp.WorldMatrix; // TODO:SK use just position + orientation
        //        bool moving = Physics.LinearVelocity != Vector3.Zero || Physics.AngularVelocity != Vector3.Zero;
        //        Vector3 linearVelocity = Physics.LinearVelocity;
        //        Vector3 angularVelocity = Physics.AngularVelocity;

        //        stream.SerializePositionOrientation(ref world);
        //        stream.Serialize(ref moving);
        //        if (moving)
        //        {
        //            stream.Serialize(ref linearVelocity);
        //            stream.Serialize(ref angularVelocity);
        //        }
        //    }
        //}
    }
}
