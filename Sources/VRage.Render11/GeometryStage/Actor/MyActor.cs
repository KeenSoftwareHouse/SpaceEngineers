using System.Diagnostics;
using VRage.Library.Collections;
using VRageMath;
using Matrix = VRageMath.Matrix;
using BoundingBox = VRageMath.BoundingBox;
using VRage.Utils;
using VRage.Render11.GeometryStage2.Instancing;


namespace VRageRender
{
    internal class MyActor
    {
        #region Fields

        private MyIDTracker<MyActor> m_ID;
        internal MatrixD WorldMatrix;
        internal BoundingBoxD Aabb;
        internal Matrix? RelativeTransform;
        internal BoundingBox? LocalAabb;
        private bool m_renderProxyDirty;
        private bool m_visible;

        private readonly MyIndexedComponentContainer<MyActorComponent> m_components = new MyIndexedComponentContainer<MyActorComponent>();

        #endregion // Fields

        public bool IsVisible { get { return m_visible; } }

        public bool RenderDirty { get { return m_renderProxyDirty; } }

        internal Matrix LocalMatrix
        {
            get
            {
                var result = WorldMatrix;
                result.Translation = result.Translation - MyRender11.Environment.Matrices.CameraPosition;
                return (Matrix)result;
            }
        }

        internal void Construct()
        {
            m_components.Clear();

            m_visible = true;

            MyUtils.Init(ref m_ID);
            m_ID.Clear();
            LocalAabb = null;
            RelativeTransform = null;

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

        internal void SetLocalAabb(BoundingBox localAabb)
        {
            LocalAabb = localAabb;

            Aabb = LocalAabb.Value.Transform(ref WorldMatrix);

            for (int i = 0; i < m_components.Count; i++)
                m_components[i].OnAabbChange();
        }

        internal void SetRelativeTransform(Matrix? m)
        {
            RelativeTransform = m;
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
            if (LocalAabb.HasValue)
            {
                Aabb = LocalAabb.Value.Transform(ref matrix);
            }
            // figure out final matrix

            for (int i = 0; i < m_components.Count; i++)
                m_components[i].OnMatrixChange();

            if (LocalAabb.HasValue)
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
            return (float)Aabb.Distance(MyRender11.Environment.Matrices.CameraPosition);
        }

        internal void AddComponent<T>(MyActorComponent component) where T : MyActorComponent
        {
            // only flat hierarchy 
            Debug.Assert(component.Type != MyActorComponentEnum.GroupLeaf || GetComponent<MyGroupRootComponent>() == null);
            Debug.Assert(component.Type != MyActorComponentEnum.GroupRoot || GetComponent<MyGroupLeafComponent>() == null);

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

        // REMOVE-ME: Bevavior on rendering should not be controlled directly on actor, but on its component
        internal void MarkRenderDirty()
        {
            if (IsDestroyed)
                return;

            var renderableComponent = GetComponent<MyRenderableComponent>();
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
    }

    static class ActorExtensions
    {
        public static MyRenderableComponent GetRenderable(this MyActor actor)
        {
            return actor.GetComponent<MyRenderableComponent>();
        }

        public static MyInstanceComponent GetInstance(this MyActor actor)
        {
            return actor.GetComponent<MyInstanceComponent>();
        }

        public static MyFoliageComponent GetFoliage(this MyActor actor)
        {
            return actor.GetComponent<MyFoliageComponent>();
        }

        public static MySkinningComponent GetSkinning(this MyActor actor)
        {
            return actor.GetComponent<MySkinningComponent>();
        }

        public static MyGroupRootComponent GetGroupRoot(this MyActor actor)
        {
            return actor.GetComponent<MyGroupRootComponent>();
        }

        public static MyGroupLeafComponent GetGroupLeaf(this MyActor actor)
        {
            return actor.GetComponent<MyGroupLeafComponent>();
        }

        public static MyInstanceLodComponent GetInstanceLod(this MyActor actor)
        {
            return actor.GetComponent<MyInstanceLodComponent>();
        }
    }
}
