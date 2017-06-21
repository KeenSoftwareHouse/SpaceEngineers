#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using VRage.Utils;
using VRageMath;

using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Entities.Cube;
using VRage.Game;


#endregion

namespace Sandbox.Game.Entities
{
    static class MyRadioBroadcasters
    {
        #region Fields

        static MyDynamicAABBTreeD m_aabbTree = new MyDynamicAABBTreeD(MyConstants.GAME_PRUNING_STRUCTURE_AABB_EXTENSION);

        #endregion

        public static void AddBroadcaster(MyRadioBroadcaster broadcaster)
        {
            if (broadcaster.Entity is MyCubeBlock)
            {
                MyCubeGrid grid = (broadcaster.Entity as MyCubeBlock).CubeGrid;
                Debug.Assert(grid.InScene, "adding broadcaster when grid is not in scene");
            }
            if (broadcaster.RadioProxyID == MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                BoundingBoxD box = BoundingBoxD.CreateFromSphere(new BoundingSphereD(broadcaster.BroadcastPosition, broadcaster.BroadcastRadius));
                broadcaster.RadioProxyID = m_aabbTree.AddProxy(ref box, broadcaster, 0);
            }
        }

        public static void RemoveBroadcaster(MyRadioBroadcaster broadcaster)
        {
            if (broadcaster.RadioProxyID != MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                m_aabbTree.RemoveProxy(broadcaster.RadioProxyID);
                broadcaster.RadioProxyID = MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED;
            }
        }

        public static void MoveBroadcaster(MyRadioBroadcaster broadcaster)
        {
            if (broadcaster.RadioProxyID != MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                BoundingBoxD box = BoundingBoxD.CreateFromSphere(new BoundingSphereD(broadcaster.BroadcastPosition, broadcaster.BroadcastRadius));
                m_aabbTree.MoveProxy(broadcaster.RadioProxyID, ref box, Vector3.Zero);
            }
        }

        public static void Clear()
        {
            m_aabbTree.Clear();
        }

        public static void GetAllBroadcastersInSphere(BoundingSphereD sphere, List<MyDataBroadcaster> result)
        {
            m_aabbTree.OverlapAllBoundingSphere<MyDataBroadcaster>(ref sphere, result, false);
            for(int i = result.Count -1; i >= 0; i--)
            {
                var x = result[i];
                var dst = (sphere.Radius + ((MyRadioBroadcaster)x).BroadcastRadius);
                dst*=dst;
                if (Vector3D.DistanceSquared(sphere.Center, x.BroadcastPosition) > dst)
                    result.RemoveAtFast(i);
            }
        }

        public static void DebugDraw()
        {
            var result = new List<MyRadioBroadcaster>();
            var resultAABBs = new List<BoundingBoxD>();
            m_aabbTree.GetAll(result, true, resultAABBs);
            for (int i = 0; i < result.Count; i++)
            {
                //VRageRender.MyRenderProxy.DebugDrawAABB(resultAABBs[i], Vector3.One, 1, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(result[i].BroadcastPosition, result[i].BroadcastRadius, Color.White, 1, false);
            }
        }
    }
}
