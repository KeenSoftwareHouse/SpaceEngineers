using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Groups;

namespace Sandbox.Engine.Physics
{
    class MyWeldGroupData : IGroupData<MyEntity>
    {
        MyGroups<MyEntity, MyWeldGroupData>.Group m_group;
        MyEntity m_weldParent;

        public void OnRelease()
        {
            m_group = null;
            m_weldParent = null;
        }

        public void OnNodeAdded(MyEntity entity)
        {
            Debug.Assert(entity.Physics == null || !entity.Physics.IsWelded || entity.Physics.WeldInfo.Parent == m_weldParent.Physics);
            ProfilerShort.Begin("WeldGroup.OnNodeAdded");
            if (m_weldParent == null)
                m_weldParent = entity;
            else
            {
                Debug.Assert(m_weldParent.Physics != null && entity.Physics != null);
                if(m_weldParent.Physics.IsStatic)
                    m_weldParent.Physics.Weld(entity.Physics);
                else if (entity.Physics.IsStatic || m_weldParent.Physics.RigidBody2 == null && entity.Physics.RigidBody2 != null)
                    ReplaceParent(entity);
                else
                    m_weldParent.Physics.Weld(entity.Physics);
            }
            if (m_weldParent.Physics != null && m_weldParent.Physics.RigidBody != null)
            {
                m_weldParent.Physics.RigidBody.Activate();
                Debug.Assert(m_weldParent.Physics.RigidBody.Layer > 0);
            }
            m_weldParent.RaisePhysicsChanged();
            ProfilerShort.End();
        }

        public void OnNodeRemoved(MyEntity entity)
        {
            ProfilerShort.Begin("WeldGroup.OnNodeRemoved");
            if (m_weldParent == entity)
            {
                if(m_group.Nodes.Count > 0)
                    ReplaceParent(null);
            }
            else if (m_weldParent.Physics != null)
            {
                m_weldParent.Physics.Unweld(entity.Physics);
            }
            else
            {
                System.Diagnostics.StackTrace st = new StackTrace();
                var entitiesClosing = false;
                foreach (var frame in st.GetFrames())
                    entitiesClosing |= frame.GetMethod().Name.Contains("CloseAll");
                VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.Analytics, "Welding: Welded parent null physics.", string.Format("Unwelding from stored body. {0}", entitiesClosing ? "MyEntities.CloseAll()" : ""));
                entity.Physics.WeldInfo.Parent.Unweld(entity.Physics);
            }
            if (m_weldParent.Physics != null && m_weldParent.Physics.RigidBody != null)
            {
                Debug.Assert(m_weldParent.Physics.RigidBody.Layer > 0);
                m_weldParent.Physics.RigidBody.Activate();
            }
            m_weldParent.RaisePhysicsChanged();
            entity.RaisePhysicsChanged();
            Debug.Assert(entity.Physics == null || !entity.Physics.IsWelded);
            Debug.Assert(entity.Physics == null || entity.Physics.RigidBody == null || entity.Physics.RigidBody.Layer > 0);
            ProfilerShort.End();
        }

        private void ReplaceParent(MyEntity newParent)
        {
            ProfilerShort.Begin("WeldGroup.ReplaceParent");
            //foreach (var node in m_group.Nodes)
            //    Debug.Assert(m_weldParent == node.NodeData 
            //        || (m_weldParent.Physics == null || m_weldParent.Physics.WeldInfo.Children.Contains(node.NodeData.Physics)));

            if(m_weldParent.Physics != null)
                m_weldParent.Physics.UnweldAll(false);
            else //parent physics was closed
            {
                foreach(var node in m_group.Nodes)
                    node.NodeData.Physics.Unweld(false);
            }
            foreach (var node in m_group.Nodes)
                Debug.Assert(!node.NodeData.Physics.IsWelded);

            m_weldParent = newParent;
            if (newParent == null)
            {
                foreach (var node in m_group.Nodes)
                {
                    if(node.NodeData.Physics.IsStatic)
                    {
                        m_weldParent = node.NodeData;
                        break;
                    }
                    if (node.NodeData.Physics.RigidBody2 != null)
                    {
                        m_weldParent = node.NodeData;
                    }
                }
            }

            foreach (var node in m_group.Nodes)
            {
                if (m_weldParent == node.NodeData)
                    continue;

                if (m_weldParent == null)
                    m_weldParent = node.NodeData;
                else
                    m_weldParent.Physics.Weld(node.NodeData.Physics, false);
            }
            if (!m_weldParent.Physics.IsInWorld)
            {
                m_weldParent.Physics.Activate();
            }
            ProfilerShort.End();
        }

        public void OnCreate<TGroupData>(MyGroups<MyEntity, TGroupData>.Group group) where TGroupData : IGroupData<MyEntity>, new()
        {
            m_group = group as MyGroups<MyEntity, MyWeldGroupData>.Group;
            Debug.Assert(m_group != null, "Wrong group class");
        }
    }
}
