using System;
using System.Diagnostics;
using VRage.Library.Collections;
using VRageMath;
using Matrix = VRageMath.Matrix;
using BoundingBox = VRageMath.BoundingBox;
using VRage.Utils;


namespace VRageRender
{
    internal class MyActor
    {
        internal MatrixD WorldMatrix;

        internal Matrix LocalMatrix
        {
            get
            {
                var result = WorldMatrix;
                result.Translation = result.Translation - MyEnvironment.CameraPosition;
                return (Matrix)result;
            }
        }

        internal BoundingBoxD Aabb;
        internal Matrix? m_relativeTransform;
        internal BoundingBox? m_localAabb;

        private bool m_renderProxyDirty;
        private bool m_visible;

        internal bool IsVisible { get { return m_visible; } }

        internal bool RenderDirty { get { return m_renderProxyDirty; } }

        private MyIDTracker<MyActor> m_ID;

        internal void Construct()
        {
            m_components.Clear();

            m_visible = true;

            MyUtils.Init(ref m_ID);
            m_ID.Clear();
            m_localAabb = null;
            m_relativeTransform = null;

            Aabb = BoundingBoxD.CreateInvalid();
        }

        internal void Destruct()
        {
            if (m_ID == null)
                return;

            for (int i = 0; i < m_components.Count; i++)
            {
                m_components[i].OnRemove(this);
            }
            m_components.Clear();

            if (m_ID.Value != null)
            {
                m_ID.Deregister();
            }

            m_ID = null;
        }

        internal void SetID(uint id)
        {
            m_ID.Register(id, this);
        }

        internal uint ID { get { return m_ID.ID; } }

        internal bool IsDestroyed { get { return m_ID == null; } }

        internal void MarkRenderDirty()
        {
            if (IsDestroyed)
                return;

            var renderableComponent = GetRenderable();
            if (renderableComponent != null)
            {
                m_renderProxyDirty = true;
                MyRender11.PendingComponentsToUpdate.Add(renderableComponent);
            }
        }

        internal void MarkRenderClean()
        {
            m_renderProxyDirty = false;
        }

        internal void SetLocalAabb(BoundingBox localAabb)
        {
            m_localAabb = localAabb;

            Aabb = m_localAabb.Value.Transform(WorldMatrix);

            for (int i = 0; i < m_components.Count; i++)
                m_components[i].OnAabbChange();
        }

        internal void SetRelativeTransform(Matrix? m)
        {
            m_relativeTransform = m;
        }

        internal void SetVisibility(bool visibility)
        {
            if (m_visible != visibility)
            {
                m_visible = visibility;

                for (int i = 0; i < m_components.Count; i++)
                    m_components[i].OnVisibilityChange();
            }
        }

        internal void SetMatrix(ref MatrixD matrix)
        {
            WorldMatrix = matrix;
            if (m_localAabb.HasValue)
            {
                Aabb = (BoundingBoxD)m_localAabb.Value.Transform(WorldMatrix);
            }
            // figure out final matrix

            for (int i = 0; i < m_components.Count; i++)
                m_components[i].OnMatrixChange();

            if (m_localAabb.HasValue)
            {
                for (int i = 0; i < m_components.Count; i++)
                    m_components[i].OnAabbChange();
            }
        }

        internal void SetAabb(BoundingBoxD aabb)
        {
            Aabb = aabb;

            for (int i = 0; i < m_components.Count; i++)
                m_components[i].OnAabbChange();
        }

        internal float CalculateCameraDistance()
        {
            return (float)Aabb.Distance(MyEnvironment.CameraPosition);
        }

        private readonly MyIndexedComponentContainer<MyActorComponent> m_components = new MyIndexedComponentContainer<MyActorComponent>();

        internal void AddComponent<T>(MyActorComponent component) where T : MyActorComponent
        {
            // only flat hierarchy 
            Debug.Assert(component.Type != MyActorComponentEnum.GroupLeaf || GetGroupRoot() == null);
            Debug.Assert(component.Type != MyActorComponentEnum.GroupRoot || GetGroupLeaf() == null);

            component.Assign(this);
            m_components.Add(typeof(T), component);
        }

        internal void RemoveComponent<T>(MyActorComponent component) where T : MyActorComponent
        {
            component.OnRemove(this);
            m_components.Remove(typeof(T));
        }

        internal T GetComponent<T>() where T : MyActorComponent
        {
            return m_components.TryGetComponent<T>();
        }

        internal MyRenderableComponent GetRenderable()
        {
            return GetComponent<MyRenderableComponent>();
        }

        internal MyFoliageComponent GetFoliage()
        {
            return GetComponent<MyFoliageComponent>();
        }

        internal MySkinningComponent GetSkinning()
        {
            return GetComponent<MySkinningComponent>();
        }

        internal MyGroupRootComponent GetGroupRoot()
        {
            return GetComponent<MyGroupRootComponent>();
        }

        internal MyGroupLeafComponent GetGroupLeaf()
        {
            return GetComponent<MyGroupLeafComponent>();
        }

        internal MyInstanceLodComponent GetInstanceLod()
        {
            return GetComponent<MyInstanceLodComponent>();
        }
    }
}
