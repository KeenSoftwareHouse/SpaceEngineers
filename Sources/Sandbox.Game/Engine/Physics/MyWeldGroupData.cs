using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Entity;
using VRage.Groups;
using VRage.Profiler;

namespace Sandbox.Engine.Physics
{
    public class MyWeldGroupData : IGroupData<MyEntity>
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
            if (entity.MarkedForClose)
                return;
            //Debug.Assert(entity.Physics == null || !entity.Physics.IsWelded || entity.GetPhysicsBody().WeldInfo.Parent == m_weldParent.Physics);
            ProfilerShort.Begin("WeldGroup.OnNodeAdded");
            if (m_weldParent == null)
                m_weldParent = entity;
            else
            {
                var parentPhysicsBody = m_weldParent.Physics as MyPhysicsBody;
                Debug.Assert(parentPhysicsBody != null && entity.Physics != null);
                if(parentPhysicsBody.IsStatic)
                    parentPhysicsBody.Weld(entity.Physics as MyPhysicsBody);
                else if (entity.Physics.IsStatic || parentPhysicsBody.RigidBody2 == null && entity.Physics.RigidBody2 != null)
                    ReplaceParent(entity);
                else
                    parentPhysicsBody.Weld(entity.Physics as MyPhysicsBody);
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
            if (m_weldParent == null)
                return;
            ProfilerShort.Begin("WeldGroup.OnNodeRemoved");
            if (m_weldParent == entity)
            {
                if(m_group.Nodes.Count == 1 && m_group.Nodes.First().NodeData.MarkedForClose)
                {
                }
                else
                    if(m_group.Nodes.Count > 0)
                    ReplaceParent(null);
            }
            else if (m_weldParent.Physics != null)
            {
                if (!entity.MarkedForClose)
                    (m_weldParent.Physics as MyPhysicsBody).Unweld(entity.Physics as MyPhysicsBody);
            }
            if (m_weldParent != null && m_weldParent.Physics != null && m_weldParent.Physics.RigidBody != null)
            {
                Debug.Assert(m_weldParent.Physics.RigidBody.Layer > 0);
                m_weldParent.Physics.RigidBody.Activate();
                m_weldParent.RaisePhysicsChanged();
            }
            entity.RaisePhysicsChanged();
            Debug.Assert(entity.MarkedForClose || entity.Physics == null || !(entity.Physics as MyPhysicsBody).IsWelded);
            Debug.Assert(entity.MarkedForClose || entity.Physics == null || entity.Physics.RigidBody == null || entity.Physics.RigidBody.Layer > 0);
            ProfilerShort.End();
        }

        private void ReplaceParent(MyEntity newParent)
        {
            m_weldParent = MyWeldingGroups.ReplaceParent(m_group, m_weldParent, newParent);
        }

        public void OnCreate<TGroupData>(MyGroups<MyEntity, TGroupData>.Group group) where TGroupData : IGroupData<MyEntity>, new()
        {
            m_group = group as MyGroups<MyEntity, MyWeldGroupData>.Group;
            Debug.Assert(m_group != null, "Wrong group class");
        }

        /// <summary>
        /// Call when node body changes quality type (dynamic/static/doubled)
        /// Checks if there isnt more suitable parent in the group
        /// i.e. group parent is dynamic and there is node that is static
        /// </summary>
        /// <param name="oldParent">WeldInfo.Parent or self if WeldInfo.Parent is null</param>
        /// <returns>parent changed</returns>
        public bool UpdateParent(MyEntity oldParent)
        {
           // Debug.Assert(m_parents.Contains(oldParent));
            var parentBody = oldParent.GetPhysicsBody();
            if(parentBody.WeldedRigidBody.IsFixed)
                return false;

            MyPhysicsBody newParent = parentBody;
            foreach (var child in parentBody.WeldInfo.Children)
            {
                if (child.WeldedRigidBody.IsFixed)
                {
                    newParent = child;
                    break;
                }

                if (!newParent.Flags.HasFlag(VRage.Game.Components.RigidBodyFlag.RBF_DOUBLED_KINEMATIC) &&
                    child.Flags.HasFlag(VRage.Game.Components.RigidBodyFlag.RBF_DOUBLED_KINEMATIC))
                {
                    newParent = child;
                }
            }

            if (newParent == parentBody)
                return false;

            ReplaceParent((MyEntity)newParent.Entity);
            
            //old parent was excluded, weld it back
            newParent.Weld(parentBody);
            return true;
        }
    }
}
