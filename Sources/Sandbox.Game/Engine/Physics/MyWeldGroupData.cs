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
            ProfilerShort.Begin("WeldGroup.OnNodeAdded");
            if (m_weldParent == null)
                m_weldParent = entity;
            else
            {
                Debug.Assert(m_weldParent.Physics != null && entity.Physics != null);
                if ((m_weldParent.Physics.RigidBody2 == null && entity.Physics.RigidBody2 != null)
                    || (!m_weldParent.Physics.IsStatic && entity.Physics.IsStatic))
                {
                    ReplaceParent(entity);
                }
                else
                    m_weldParent.Physics.Weld(entity.Physics);
            }
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
            else
                m_weldParent.Physics.Unweld(entity.Physics);
            ProfilerShort.End();
        }

        private void ReplaceParent(MyEntity newParent)
        {
            ProfilerShort.Begin("WeldGroup.ReplaceParent");
            if(m_weldParent.Physics != null)
                m_weldParent.Physics.UnweldAll(false);
            else //parent physics was closed
            {
                foreach(var node in m_group.Nodes)
                    node.NodeData.Physics.Unweld(false);
            }
            m_weldParent = newParent;
            if (newParent == null)
            {
                foreach (var node in m_group.Nodes)
                {
                    if (node.NodeData.Physics.RigidBody2 != null)
                    {
                        m_weldParent = node.NodeData;
                        break;
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
            m_weldParent.Physics.RecreateWeldedShape();
            if (!m_weldParent.Physics.IsInWorld)
                m_weldParent.Physics.Activate();
            ProfilerShort.End();
        }

        public void OnCreate<TGroupData>(MyGroups<MyEntity, TGroupData>.Group group) where TGroupData : IGroupData<MyEntity>, new()
        {
            m_group = group as MyGroups<MyEntity, MyWeldGroupData>.Group;
            Debug.Assert(m_group != null, "Wrong group class");
        }
    }
}
