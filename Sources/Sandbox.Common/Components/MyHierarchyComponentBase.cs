using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.Components
{
    public class MyHierarchyComponentBase : MyComponentBase
    {
        private List<MyHierarchyComponentBase> m_children = new List<MyHierarchyComponentBase>();

        /// <summary>
        /// Return top most parent of this entity
        /// </summary>
        /// <returns></returns>
        public MyHierarchyComponentBase GetTopMostParent(Type type = null)
        {
            MyHierarchyComponentBase parent = this;

            while (parent.Parent != null && (type == null || !parent.CurrentContainer.Contains(type)))
            {
                parent = parent.Parent;
            }

            return parent;
        }

        /// <summary>
        /// Gets the childs collection.
        /// </summary>
        public List<MyHierarchyComponentBase> Children
        {
            get
            {
                return this.m_children;
            }
        }


        public MyHierarchyComponentBase Parent { get; set; }

        /// <summary>
        /// Adds the child.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <param name="preserveWorldPos">if set to <c>true</c> [preserve absolute position].</param>
        public void AddChild(Sandbox.ModAPI.IMyEntity child, bool preserveWorldPos = false, bool insertIntoSceneIfNeeded = true)
        {
            //MyEntities.Remove(child);  // if it's already in the world, remove it

            child.Hierarchy.Parent = this;

            if (preserveWorldPos)
            {
                var tmpWorldMatrix = child.WorldMatrix;

                this.Children.Add(child.Hierarchy);

                child.WorldMatrix = tmpWorldMatrix;
            }
            else
            {
                this.Children.Add(child.Hierarchy);
                var m = Entity.PositionComp.WorldMatrix;
                child.PositionComp.UpdateWorldMatrix(ref m);
            }

            if (Entity.InScene && !child.InScene && insertIntoSceneIfNeeded)
                child.OnAddedToScene(this.Entity);
        }

        public void AddChildWithMatrix(Sandbox.ModAPI.IMyEntity child, ref Matrix childLocalMatrix, bool insertIntoSceneIfNeeded = true)
        {
            child.Hierarchy.Parent = this;

            Children.Add(child.Hierarchy);
            child.WorldMatrix = (MatrixD)childLocalMatrix * Entity.PositionComp.WorldMatrix;

            if (Entity.InScene && !child.InScene && insertIntoSceneIfNeeded)
                child.OnAddedToScene(this);
        }

        /// <summary>
        /// Adds the child.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <param name="preserveWorldPos">if set to <c>true</c> [preserve absolute position].</param>
        public void RemoveChild(Sandbox.ModAPI.IMyEntity child, bool preserveWorldPos = false)
        {
            if (preserveWorldPos)
            {
                var tmpWorldMatrix = child.WorldMatrix;

                this.Children.Remove(child.Hierarchy);

                child.WorldMatrix = tmpWorldMatrix;
            }
            else
            {
                this.Children.Remove(child.Hierarchy);
            }

            child.Hierarchy.Parent = null;

            if (child.InScene)
                child.OnRemovedFromScene(this);
        }

        public void GetChildrenRecursive(HashSet<Sandbox.ModAPI.IMyEntity> result)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var entity = Children[i];
                result.Add(entity.Entity);
                entity.GetChildrenRecursive(result);
            }
        }
    }
}
