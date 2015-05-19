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
using VRage.Library.Utils;


#endregion

namespace Sandbox.Game.Entities
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MyPlaceAreas : MySessionComponentBase
    {
        #region Fields

        static MyDynamicAABBTreeD m_aabbTree = new MyDynamicAABBTreeD(MyConstants.GAME_PRUNING_STRUCTURE_AABB_EXTENSION);

        #endregion

        protected override void UnloadData()
        {
            base.UnloadData();

            List<MyPlaceArea> areas = new List<MyPlaceArea>();
            m_aabbTree.GetAll(areas, false);
            foreach (var area in areas)
            {
                area.PlaceAreaProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;
            }

            Clear();
        }

        public static void AddPlaceArea(MyPlaceArea area)
        {
            if (area.PlaceAreaProxyId == MyConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                BoundingBoxD box = area.WorldAABB;
                area.PlaceAreaProxyId = m_aabbTree.AddProxy(ref box, area, 0);
            }
        }

        public static void RemovePlaceArea(MyPlaceArea area)
        {
            if (area.PlaceAreaProxyId != MyConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                m_aabbTree.RemoveProxy(area.PlaceAreaProxyId);
                area.PlaceAreaProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;
            }
        }

        public static void MovePlaceArea(MyPlaceArea area)
        {
            if (area.PlaceAreaProxyId != MyConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                BoundingBoxD box = area.WorldAABB;
                m_aabbTree.MoveProxy(area.PlaceAreaProxyId, ref box, Vector3.Zero);
            }
        }

        public static void Clear()
        {
            m_aabbTree.Clear();
        }

        public static void GetAllAreasInSphere(BoundingSphereD sphere, List<MyPlaceArea> result)
        {
            m_aabbTree.OverlapAllBoundingSphere<MyPlaceArea>(ref sphere, result, false);
        }

        public static void GetAllAreas(List<MyPlaceArea> result)
        {
            m_aabbTree.GetAll<MyPlaceArea>(result, false);
        }

        public static void DebugDraw()
        {
            var result = new List<MyPlaceArea>();
            var resultAABBs = new List<BoundingBoxD>();
            m_aabbTree.GetAll(result, true, resultAABBs);
            for (int i = 0; i < result.Count; i++)
            {
                VRageRender.MyRenderProxy.DebugDrawAABB(resultAABBs[i], Vector3.One, 1, 1, false);
            }
        }
    }
}
