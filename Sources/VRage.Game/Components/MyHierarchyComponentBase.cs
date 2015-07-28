using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRage.Components;
using VRage.ModAPI;

namespace VRage.Components
{
    public class MyHierarchyComponentBase : MyEntityComponentBase
    {
        private List<MyHierarchyComponentBase> m_children = new List<MyHierarchyComponentBase>();

        /// <summary>
        /// Return top most parent of this entity
        /// </summary>
        /// <returns></returns>
        public MyHierarchyComponentBase GetTopMostParent(Type type = null)
        {
            MyHierarchyComponentBase parent = this;

            while (parent.Parent != null && (type == null || !parent.Container.Contains(type)))
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
        public void AddChild(IMyEntity child, bool preserveWorldPos = false, bool insertIntoSceneIfNeeded = true)
        {
            //MyEntities.Remove(child);  // if it's already in the world, remove it

            MyHierarchyComponentBase childHierarchy = child.Components.Get<MyHierarchyComponentBase>();
            childHierarchy.Parent = this;

            if (preserveWorldPos)
            {
                var tmpWorldMatrix = child.WorldMatrix;

                this.Children.Add(childHierarchy);

                child.WorldMatrix = tmpWorldMatrix;
            }
            else
            {
                this.Children.Add(childHierarchy);

                MyPositionComponentBase positionComponent = Container.Get<MyPositionComponentBase>();
                MyPositionComponentBase childPositionComponent = child.Components.Get<MyPositionComponentBase>();

                var m = positionComponent.WorldMatrix;
                childPositionComponent.UpdateWorldMatrix(ref m);
            }

            if (Container.Entity.InScene && !child.InScene && insertIntoSceneIfNeeded)
                child.OnAddedToScene(Container.Entity);
        }

        public void AddChildWithMatrix(IMyEntity child, ref Matrix childLocalMatrix, bool insertIntoSceneIfNeeded = true)
        {
            MyHierarchyComponentBase childHierarchy = child.Components.Get<MyHierarchyComponentBase>();

            childHierarchy.Parent = this;

            Children.Add(childHierarchy);
            child.WorldMatrix = (MatrixD)childLocalMatrix * Container.Get<MyPositionComponentBase>().WorldMatrix;

            if (Container.Entity.InScene && !child.InScene && insertIntoSceneIfNeeded)
                child.OnAddedToScene(this);
        }

        /// <summary>
        /// Adds the child.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <param name="preserveWorldPos">if set to <c>true</c> [preserve absolute position].</param>
        public void RemoveChild(IMyEntity child, bool preserveWorldPos = false)
        {
            MyHierarchyComponentBase childHierarchy = child.Components.Get<MyHierarchyComponentBase>();

            if (preserveWorldPos)
            {
                var tmpWorldMatrix = child.WorldMatrix;

                this.Children.Remove(childHierarchy);

                child.WorldMatrix = tmpWorldMatrix;
            }
            else
            {
                this.Children.Remove(childHierarchy);
            }

            childHierarchy.Parent = null;

            if (child.InScene)
                child.OnRemovedFromScene(this);
        }

        public void GetChildrenRecursive(HashSet<IMyEntity> result)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var entity = Children[i];
                result.Add(entity.Container.Entity);
                entity.GetChildrenRecursive(result);
            }
        }

        public override string ComponentTypeDebugString
        {
            get { return "Hierarchy"; }
        }
    }
}
