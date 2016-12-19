using System;
using System.Collections.Generic;
using VRageMath;
using VRage.ModAPI;
using System.Diagnostics;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace VRage.Game.Components
{
    [MyComponentBuilder(typeof(MyObjectBuilder_HierarchyComponentBase))]
    public class MyHierarchyComponentBase : MyEntityComponentBase
    {
        protected List<MyHierarchyComponentBase> m_children = new List<MyHierarchyComponentBase>();
        protected readonly List<MyEntity> m_deserializedEntities = new List<MyEntity>();

        public event Action<IMyEntity> OnChildRemoved;

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

        /**
         * Identifier for the parent hierarchy.
         * 
         * This is should be reliably unique within a hierarchy level but only usable by the parent.
         */
        public long ChildId;

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

        MyEntityComponentContainer m_parentContainer;
        MyHierarchyComponentBase m_parent;
        public MyHierarchyComponentBase Parent
        {
            get { return m_parent; }
            set
            {
                if (m_parentContainer != null)
                {
                    m_parentContainer.ComponentAdded -= Container_ComponentAdded;
                    m_parentContainer.ComponentRemoved -= Container_ComponentRemoved;
                    m_parentContainer = null;
                }

                m_parent = value;

                if (m_parent != null)
                {
                    Debug.Assert(m_parent.Container != null);
                    m_parentContainer = m_parent.Container;

                    m_parentContainer.ComponentAdded += Container_ComponentAdded;
                    m_parentContainer.ComponentRemoved += Container_ComponentRemoved;
                }
            }
        }

        void Container_ComponentRemoved(Type arg1, MyEntityComponentBase arg2)
        {
            if (arg2 == m_parent)
                m_parent = null;
        }

        void Container_ComponentAdded(Type arg1, MyEntityComponentBase arg2)
        {
            if (typeof(MyHierarchyComponentBase).IsAssignableFrom(arg1))
            {
                m_parent = arg2 as MyHierarchyComponentBase;
            }
        }

        /// <summary>
        /// Adds the child.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <param name="preserveWorldPos">if set to <c>true</c> [preserve absolute position].</param>
        public void AddChild(IMyEntity child, bool preserveWorldPos = false, bool insertIntoSceneIfNeeded = true)
        {
            //MyEntities.Remove(child);  // if it's already in the world, remove it

            MyHierarchyComponentBase childHierarchy = child.Components.Get<MyHierarchyComponentBase>();

            if (m_children.Contains(childHierarchy))
            {
                Debug.Fail("The child is already in the hierarchy.");
                return;
            }

            childHierarchy.Parent = this;

            if (preserveWorldPos)
            {
                var tmpWorldMatrix = child.WorldMatrix;

                this.Children.Add(childHierarchy);

                child.PositionComp.SetWorldMatrix(tmpWorldMatrix, Entity, true);
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

            if (OnChildRemoved != null)
                OnChildRemoved(child);
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

        public override void OnBeforeRemovedFromContainer()
        {
            if (m_parentContainer != null)
            {
                m_parentContainer.ComponentAdded -= Container_ComponentAdded;
                m_parentContainer.ComponentRemoved -= Container_ComponentRemoved;
                m_parentContainer = null;
            }

            m_parent = null;

            base.OnBeforeRemovedFromContainer();
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            foreach (var child in m_children)
            {
                if (!child.Entity.InScene)
                {
                    child.Entity.OnAddedToScene(Container.Entity);
                }
            }
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var ob = new MyObjectBuilder_HierarchyComponentBase();

            foreach (var child in Children)
            {
                //IMPORTANT - entities that are supposed to be saved in hierarchy should be saved ONLY in hierarchy
                if (child.Entity.Save)
                {
                    ob.Children.Add(child.Entity.GetObjectBuilder(copy));
                }
            }
            // Dont serialize when empty
            return ob.Children.Count > 0 ? ob : null;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);
            var ob = builder as MyObjectBuilder_HierarchyComponentBase;

            if (ob != null)
            {
                m_deserializedEntities.Clear();
                foreach (var child in ob.Children)
                {
                    //IMPORTANT - entities that are supposed to be saved in hierarchy should be saved ONLY in hierarchy
                    if (!MyEntityIdentifier.ExistsById(child.EntityId))
                    {
                        var childEntity = MyEntity.MyEntitiesCreateFromObjectBuilderExtCallback(child, true);
                        m_deserializedEntities.Add(childEntity);
                    }
                }

                foreach (var deserializedEntity in m_deserializedEntities)
                {
                    AddChild(deserializedEntity, true, false);
                }
            }
        }
    }
}
