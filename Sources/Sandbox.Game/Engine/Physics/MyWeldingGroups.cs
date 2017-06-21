using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.Game.Entity;
using VRage.Groups;
using VRage.Profiler;

namespace Sandbox.Engine.Physics
{
    public class MyWeldingGroups : MyGroups<MyEntity, MyWeldGroupData>, IMySceneComponent
    {
        static MyWeldingGroups m_static;

        public static MyWeldingGroups Static
        {
            get
            {
                Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread, "Welding groups can be accessed only from main thread.");
                return m_static;
            }
        }


        public void Load()
        {
            m_static = this;
            SupportsOphrans = true;
        }

        public void Unload()
        {
            m_static = null;
        }

        /// <summary>
        /// Replace common parent in weld group
        /// old parent is not considered part of weld group anymore (isnt welded to new parent)
        /// </summary>
        /// <param name="group">weld group</param>
        /// <param name="oldParent">old parent</param>
        /// <param name="newParent">new parent (can be null)</param>
        /// <returns>chosen new parent</returns>
        public static MyEntity ReplaceParent(Group group, MyEntity oldParent, MyEntity newParent)
        {
            ProfilerShort.Begin("WeldGroup.ReplaceParent");

            if (oldParent.Physics != null)
                oldParent.GetPhysicsBody().UnweldAll(false);
            else //parent physics was closed
            {
                if (group == null)
                    return oldParent;
                foreach (var node in group.Nodes)
                {
                    if (node.NodeData.MarkedForClose)
                        continue;
                    node.NodeData.GetPhysicsBody().Unweld(false);
                }
            }
            if (group == null)
                return oldParent;
            if (newParent == null)
            {
                foreach (var node in group.Nodes)
                {
                    if (node.NodeData.MarkedForClose)
                        continue;
                    if (node.NodeData == oldParent)
                        continue;
                    if (node.NodeData.Physics.IsStatic)
                    {
                        newParent = node.NodeData;
                        break;
                    }
                    if (node.NodeData.Physics.RigidBody2 != null)
                    {
                        newParent = node.NodeData;
                    }
                }
            }
            foreach (var node in group.Nodes)
            {
                if (node.NodeData.MarkedForClose)
                    continue;
                if (newParent == node.NodeData)
                    continue;
                if (newParent == null)
                    newParent = node.NodeData;
                else
                    newParent.GetPhysicsBody().Weld(node.NodeData.Physics, false);
            }
            if (newParent != null && !newParent.Physics.IsInWorld)
            {
                newParent.Physics.Activate();
            }
            ProfilerShort.End();
            return newParent;
        }

        public override void CreateLink(long linkId, MyEntity parentNode, MyEntity childNode)
        {
            if (MySandboxGame.Static.UpdateThread == Thread.CurrentThread)
                base.CreateLink(linkId, parentNode, childNode);
            else
                Debug.Fail("Creating link from paralel thread!");
        }
    }
}
