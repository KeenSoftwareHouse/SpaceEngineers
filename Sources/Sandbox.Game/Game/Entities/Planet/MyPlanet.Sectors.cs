using Sandbox.Engine.Voxels;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;
using Sandbox.Game.World;
using VRage;
using Sandbox.Game.Entities.EnvironmentItems;
using VRage.Generics;
using ParallelTasks;
using VRage.Game.Components;
using System.Diagnostics;
using System.ServiceModel.Security;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.WorldEnvironment;
using Sandbox.Game.WorldEnvironment.Definitions;
using VRage.Collections;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRage.Profiler;

namespace Sandbox.Game.Entities
{
    public partial class MyPlanet
    {
        private MyDynamicAABBTreeD m_children;

        private void PrepareSectors()
        {
            m_children = new MyDynamicAABBTreeD(Vector3D.Zero);

            Hierarchy.QueryAABBImpl = Hierarchy_QueryAABB;
            Hierarchy.QueryLineImpl = Hierarchy_QueryLine;
            Hierarchy.QuerySphereImpl = Hierarchy_QuerySphere;
        }

        #region Hierarchy implementation

        private void Hierarchy_QueryAABB(BoundingBoxD query, List<MyEntity> results)
        {
            m_children.OverlapAllBoundingBox<MyEntity>(ref query, results, clear: false);
        }

        private void Hierarchy_QuerySphere(BoundingSphereD query, List<MyEntity> results)
        {
            m_children.OverlapAllBoundingSphere<MyEntity>(ref query, results, clear: false);
        }

        private void Hierarchy_QueryLine(LineD query, List<MyLineSegmentOverlapResult<MyEntity>> results)
        {
            m_children.OverlapAllLineSegment<MyEntity>(ref query, results, clear: false);
        }

        public void AddChildEntity(MyEntity child)
        {
            if (MyFakes.ENABLE_PLANET_HIERARCHY)
            {
                var bbox = child.PositionComp.WorldAABB;

                ProfilerShort.Begin("Add sector to tree.");
                int proxyId = m_children.AddProxy(ref bbox, child, 0);
                ProfilerShort.BeginNextBlock("Add to child hierarchy.");
                Hierarchy.AddChild(child, true);
                ProfilerShort.End();

                MyHierarchyComponentBase childHierarchy = child.Components.Get<MyHierarchyComponentBase>();
                childHierarchy.ChildId = proxyId;
            }
            else
            {
                MyEntities.Add(child);
            }
        }

        public void RemoveChildEntity(MyEntity child)
        {
            if (MyFakes.ENABLE_PLANET_HIERARCHY)
            {
                if (child.Parent == this)
                {
                    MyHierarchyComponentBase childHierarchy = child.Components.Get<MyHierarchyComponentBase>();
                    m_children.RemoveProxy((int)childHierarchy.ChildId);
                    Hierarchy.RemoveChild(child, true);
                }
            }
        }

        internal void CloseChildEntity(MyEntity child)
        {
            RemoveChildEntity(child);
            child.Close();
        }

        #endregion
    }
}
