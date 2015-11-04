using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Collections;
using System.Collections.Specialized;
using System.Threading;


namespace VRageRender
{
    class MyActor
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

        internal bool m_renderProxyDirty;
        internal bool m_visible;

        internal bool RenderDirty { get { return m_renderProxyDirty; } }

        MyIDTracker<MyActor> m_ID;

        internal void Construct()
        {
            m_components.Clear();

            m_visible = true;
            m_renderProxyDirty = true;

            m_ID = new MyIDTracker<MyActor>();
            m_localAabb = null;
            m_relativeTransform = null;

            Aabb = BoundingBoxD.CreateInvalid();
        }

        internal void Destruct()
        {
            for (int i = 0; i < m_components.Count; i++)
            {
                m_components[i].OnRemove(this);
            }
            m_components.Clear();

            if(m_ID.Value != null)
            {
                m_ID.Deregister();
            }
        }

        internal void SetID(uint id)
        {
            m_ID.Register(id, this);
        }

        internal uint ID { get { return m_ID.ID; } }

        internal bool IsDestroyed { get { return m_ID.Value == null; } }

        internal void MarkRenderDirty()
        {
            m_renderProxyDirty = true;
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
            if(m_visible != visibility)
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

            if(m_localAabb.HasValue)
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

        List<MyActorComponent> m_components = new List<MyActorComponent>();

        internal void AddComponent(MyActorComponent component) 
        {
            // only flat hierarchy 
            Debug.Assert(component.Type != MyActorComponentEnum.GroupLeaf || GetGroupRoot() == null);
            Debug.Assert(component.Type != MyActorComponentEnum.GroupRoot || GetGroupLeaf() == null);

            component.Assign(this);
            m_components.Add(component);
        }

        internal void RemoveComponent(MyActorComponent component)
        {
            component.OnRemove(this);
            m_components.Remove(component);
        }

        internal MyActorComponent GetComponent(MyActorComponentEnum type)
        {
            for (int i = 0; i < m_components.Count; i++ )
            {
                if (m_components[i].Type == type)
                    return m_components[i];
            }
            return null;
        }

        internal bool IsVisible { get { return m_visible && GetRenderable() != null; } }

        internal MyRenderableComponent GetRenderable()
        {
            return GetComponent(MyActorComponentEnum.Renderable) as MyRenderableComponent;
        }

        internal MySkinningComponent GetSkinning()
        {
            return GetComponent(MyActorComponentEnum.Skinning) as MySkinningComponent;
        }

        internal MyGroupRootComponent GetGroupRoot()
        {
            return GetComponent(MyActorComponentEnum.GroupRoot) as MyGroupRootComponent;
        }

        internal MyGroupLeafComponent GetGroupLeaf()
        {
            return GetComponent(MyActorComponentEnum.GroupLeaf) as MyGroupLeafComponent;
        }
        
        internal MyInstanceLodComponent GetInstanceLod()
        {
            return GetComponent(MyActorComponentEnum.InstanceLod) as MyInstanceLodComponent;
        }
    };
}
