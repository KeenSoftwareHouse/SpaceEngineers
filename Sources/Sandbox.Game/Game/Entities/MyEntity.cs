#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Decals;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Utils;
using Sandbox.Game.World;
using Sandbox.Graphics;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using Sandbox.Common.Components;
using Sandbox.Game.Entities.Character;
using VRage;
using Sandbox.Game.Components;
using VRage;
using VRage.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

#endregion

namespace Sandbox.Game.Entities
{
    public partial class MyEntity
    {
        #region Fields

        public MyEntityComponentContainer Components { get; private set; }

        public string Name;

        //protected MySyncEntity SyncObject;

        protected List<MyHudEntityParams> m_hudParams;

        MyPositionComponentBase m_position;
        public MyPositionComponentBase PositionComp { get { return m_position; } set { Components.Add<MyPositionComponentBase>(value); } }

        int m_movementCooldown = 0; //number of frames after which we stop update world matrix with same values

        MyRenderComponentBase m_render;
        public MyRenderComponentBase Render { get { return m_render; } set { Components.Add<MyRenderComponentBase>(value); } }

        List<MyDebugRenderComponentBase> m_debugRenderers = new List<MyDebugRenderComponentBase>();

        public void DebugDraw()
        {
            foreach (var child in this.Hierarchy.Children)
            {
                child.Container.Entity.DebugDraw();
            }

            foreach (var render in m_debugRenderers)
            {
                render.DebugDraw();
            }
        }
        public void DebugDrawInvalidTriangles()
        {
            foreach (var render in m_debugRenderers)
            {
                render.DebugDrawInvalidTriangles();
            }
        }

        public void AddDebugRenderComponent(MyDebugRenderComponentBase render)
        {
            m_debugRenderers.Add(render);
        }

        public void ClearDebugRenderComponents()
        {
            m_debugRenderers.Clear();
        }

        //Rendering
        protected MyModel m_modelCollision;                       //  Collision model, used only for collisions

        //Space query structure
        public int GamePruningProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;
        public int TopMostPruningProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;

        #endregion

        #region Properties
        private MyGameLogicComponent m_gameLogic;
        public MyGameLogicComponent GameLogic
        {
            get { return m_gameLogic; }
            set { Components.Add<MyGameLogicComponent>(value); }
        }

        /// <summary>
        /// Entity id, can be set by subclasses (for example when using pool...)
        /// </summary>
        public long EntityId
        {
            get { return m_entityId ?? 0; }
            set
            {
                if (m_entityId.HasValue)
                {
                    var oldVal = m_entityId.Value;
                    if (value == 0)
                    {
                        m_entityId = null;
                        MyEntityIdentifier.RemoveEntity(oldVal);
                    }
                    else
                    {
                        m_entityId = value;
                        MyEntityIdentifier.SwapRegisteredEntityId(this, oldVal, m_entityId.Value);
                    }
                }
                else
                {
                    m_entityId = value;
                    MyEntityIdentifier.AddEntityWithId(this);
                }
            }
        }
        private long? m_entityId;

        private MySyncComponentBase m_syncObject;

        public MySyncComponentBase SyncObject { get { return m_syncObject; } protected set { Components.Add<MySyncComponentBase>(value); } }

        //Only debug property, use only for asserts, not for game logic.
        //Consider as being called after delete in C++
        public bool Closed { get; protected set; }
        public bool MarkedForClose { get; protected set; }

        public virtual float MaxGlassDistSq
        {
            get
            {
                return 0.01f * MySector.MainCamera.FarPlaneDistance * MySector.MainCamera.FarPlaneDistance;
            }
        }

        public bool Save
        {
            get
            {
                return (Flags & EntityFlags.Save) != 0;
            }
            set
            {
                if (value)
                    Flags |= EntityFlags.Save;
                else
                    Flags &= ~EntityFlags.Save;
            }
        }

        public MyEntityUpdateEnum NeedsUpdate
        {
            get
            {
                MyEntityUpdateEnum needsUpdate = MyEntityUpdateEnum.NONE;

                if ((Flags & EntityFlags.NeedsUpdate) != 0)
                    needsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

                if ((Flags & EntityFlags.NeedsUpdate10) != 0)
                    needsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

                if ((Flags & EntityFlags.NeedsUpdate100) != 0)
                    needsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

                if ((Flags & EntityFlags.NeedsUpdateBeforeNextFrame) != 0)
                    needsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

                return needsUpdate;
            }
            set
            {
                bool hasChanged = value != NeedsUpdate;

                if (hasChanged)
                {
                    if (InScene)
                        MyEntities.UnregisterForUpdate(this);

                    Flags &= ~EntityFlags.NeedsUpdateBeforeNextFrame;
                    Flags &= ~EntityFlags.NeedsUpdate;
                    Flags &= ~EntityFlags.NeedsUpdate10;
                    Flags &= ~EntityFlags.NeedsUpdate100;

                    if ((value & MyEntityUpdateEnum.BEFORE_NEXT_FRAME) != 0)
                        Flags |= EntityFlags.NeedsUpdateBeforeNextFrame;
                    if ((value & MyEntityUpdateEnum.EACH_FRAME) != 0)
                        Flags |= EntityFlags.NeedsUpdate;
                    if ((value & MyEntityUpdateEnum.EACH_10TH_FRAME) != 0)
                        Flags |= EntityFlags.NeedsUpdate10;
                    if ((value & MyEntityUpdateEnum.EACH_100TH_FRAME) != 0)
                        Flags |= EntityFlags.NeedsUpdate100;

                    if (InScene)
                        MyEntities.RegisterForUpdate(this);
                }
            }
        }


        public MatrixD WorldMatrix
        {
            get { return PositionComp.WorldMatrix; }
            set { PositionComp.SetWorldMatrix(value); }
        }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        public MyEntity Parent { get { return m_hierarchy != null && m_hierarchy.Parent != null ? m_hierarchy.Parent.Container.Entity as MyEntity : null; } private set { m_hierarchy.Parent = value.Components.Get<MyHierarchyComponentBase>(); } }

        /// <summary>
        /// Return top most parent of this entity
        /// </summary>
        /// <returns></returns>
        public MyEntity GetTopMostParent(Type type = null)
        {
            MyEntity parent = this;

            while (parent.Parent != null && (type == null || !parent.GetType().IsSubclassOf(type)))
            {
                parent = parent.Parent;
            }

            return parent;
        }


        private MyHierarchyComponentBase m_hierarchy;
        public MyHierarchyComponentBase Hierarchy { get { return m_hierarchy; } set { Components.Add<MyHierarchyComponentBase>(value); } }

        private MyPhysicsBody m_physics;

        /// <summary>
        /// Gets the physic body representation of the entity.
        /// </summary>
        public MyPhysicsBody Physics { get { return m_physics; } set { Components.Add<MyPhysicsComponentBase>(value); } }

        MyPhysicsComponentBase IMyEntity.Physics { get { return Physics; } set { Physics = (MyPhysicsBody)value; } }

        public bool InvalidateOnMove
        {
            get
            {
                return (Flags & EntityFlags.InvalidateOnMove) != 0;
            }

            set
            {
                EntityFlags oldValue = Flags;

                if (value)
                {
                    Flags = Flags | EntityFlags.InvalidateOnMove;
                }
                else
                {
                    Flags = Flags & (~EntityFlags.InvalidateOnMove);
                }
            }
        }

        public bool SyncFlag
        {
            get { return (Flags & EntityFlags.Sync) != 0; }
            set
            {
                Flags = value ? (Flags | EntityFlags.Sync) : (Flags & (~EntityFlags.Sync));
            }
        }

        public bool InScene
        {
            get
            {
                bool inScene = ((Render.PersistentFlags & MyPersistentEntityFlags2.InScene) > 0);
                return inScene;
            }
            set
            {
                if (value)
                    Render.PersistentFlags |= MyPersistentEntityFlags2.InScene;
                else
                    Render.PersistentFlags &= ~MyPersistentEntityFlags2.InScene;
            }
        }

        public virtual bool IsVolumetric
        {
            get { return false; }
        }

        public virtual Vector3D LocationForHudMarker
        {
            get { return PositionComp.GetPosition(); }
        }

        public virtual List<MyHudEntityParams> GetHudParams(bool allowBlink)
        {
            return m_hudParams;
        }

        public void UpdateGamePruningStructure()
        {
            MyGamePruningStructure.Move(this);
            foreach (var child in Hierarchy.Children) child.Container.Entity.UpdateGamePruningStructure();
        }

        public void AddToGamePruningStructure()
        {
            MyGamePruningStructure.Add(this);
            foreach (var child in Hierarchy.Children)
                child.Container.Entity.AddToGamePruningStructure();
        }

        public void RemoveFromGamePruningStructure()
        {
            MyGamePruningStructure.Remove(this);

            if (Hierarchy != null)
            {
                foreach (var child in Hierarchy.Children)
                    child.Container.Entity.RemoveFromGamePruningStructure();
            }
        }

        protected virtual bool CanBeAddedToRender()
        {
            return true;
        }

        public MyModel Model
        {
            get { return Render.GetModel(); }
        }

        public MyModel ModelCollision
        {
            get
            {
                if (m_modelCollision != null)
                {
                    return m_modelCollision;
                }
                else
                {
                    return Render.GetModel();
                }
            }
        }

        private string m_displayName;
        public string DisplayName
        {
            get
            {
                return m_displayName;
            }
            set
            {
                m_displayName = value;
            }
        }

        public Dictionary<string, MyEntitySubpart> Subparts
        {
            get;
            private set;
        }

        public virtual bool IsCCDForProjectiles
        {
            get { return false; }
        }

        #endregion

        #region Methods

        //public StackTrace CreationStack = new StackTrace(true);

        public MyEntity()
            : this(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MyEntity"/> class.
        /// </summary>
        public MyEntity(bool initComponents = true)
        {
            Components = new MyEntityComponentContainer(this);
            Components.ComponentAdded += Components_ComponentAdded;
            Components.ComponentRemoved += Components_ComponentRemoved;

            this.Flags = EntityFlags.Visible |
                         EntityFlags.SkipIfTooSmall |
                         EntityFlags.Save |
                         EntityFlags.NeedsResolveCastShadow |
                         EntityFlags.InvalidateOnMove;


            if (initComponents)
            {
                this.m_hudParams = new List<MyHudEntityParams>(1);
                this.Hierarchy = new MyHierarchyComponentBase();
                this.GameLogic = new MyNullGameLogicComponent();
                this.PositionComp = new MyPositionComponent();

                PositionComp.LocalMatrix = Matrix.Identity;

                this.Render = new MyRenderComponent();
                AddDebugRenderComponent(new MyDebugRenderComponent(this));
            }
        }

        void Components_ComponentAdded(Type t, MyEntityComponentBase c)
        {
            if (t == typeof(MyPhysicsComponentBase))
                m_physics = c as MyPhysicsBody;
            else if (t == typeof(MySyncComponentBase))
                m_syncObject = c as MySyncComponentBase;
            else if (t == typeof(MyGameLogicComponent))
                m_gameLogic = c as MyGameLogicComponent;
            else if (t == typeof(MyPositionComponentBase))
                m_position = c as MyPositionComponentBase;
            else if (t == typeof(MyHierarchyComponentBase))
                m_hierarchy = c as MyHierarchyComponentBase;
            else if (t == typeof(MyRenderComponentBase))
            {
                m_render = c as MyRenderComponentBase;
            }
        }

        void Components_ComponentRemoved(Type t, MyEntityComponentBase c)
        {
            if (t == typeof(MyPhysicsComponentBase))
                m_physics = null;
            else if (t == typeof(MySyncComponentBase))
                m_syncObject = null;
            else if (t == typeof(MyGameLogicComponent))
                m_gameLogic = null;
            else if (t == typeof(MyPositionComponentBase))
                m_position = null;
            else if (t == typeof(MyHierarchyComponentBase))
                m_hierarchy = null;
            else if (t == typeof(MyRenderComponentBase))
            {
                m_render = null;
            }
        }

        protected virtual MySyncEntity OnCreateSync()
        {
            return new MySyncEntity(this);
        }

        public void CreateSync()
        {
            SyncObject = OnCreateSync();
        }

        #region Update
        public virtual void UpdateOnceBeforeFrame()
        {
            m_gameLogic.UpdateOnceBeforeFrame();
            Debug.Assert(!Closed, "Cannot update entity, entity is closed");
        }

        public virtual void UpdateBeforeSimulation()
        {
            m_gameLogic.UpdateBeforeSimulation();
            Debug.Assert(!Closed, "Cannot update entity, entity is closed");
        }
        public virtual void UpdateAfterSimulation()
        {
            m_gameLogic.UpdateAfterSimulation();
            Debug.Assert(!Closed, "Cannot update entity, entity is closed");
            //if(m_syncObject != null) m_syncObject.Update();
        }

        public virtual void UpdatingStopped()
        {
            Debug.Assert(!Closed, "Cannot update entity, entity is closed");
            //if(m_syncObject != null) m_syncObject.Update();
        }

        /// <summary>
        /// Called each 10th frame if registered for update10
        /// </summary>
        public virtual void UpdateBeforeSimulation10()
        {
            m_gameLogic.UpdateBeforeSimulation10();
            Debug.Assert(!Closed, "Cannot update entity, entity is closed");
        }
        public virtual void UpdateAfterSimulation10()
        {
            m_gameLogic.UpdateAfterSimulation10();
            Debug.Assert(!Closed, "Cannot update entity, entity is closed");
            //if (m_syncObject != null) m_syncObject.Update10();
        }


        /// <summary>
        /// Called each 100th frame if registered for update100
        /// </summary>
        public virtual void UpdateBeforeSimulation100()
        {
            m_gameLogic.UpdateBeforeSimulation100();
            Debug.Assert(!Closed, "Cannot update entity, entity is closed");
        }
        public virtual void UpdateAfterSimulation100()
        {
            m_gameLogic.UpdateAfterSimulation100();
            Debug.Assert(!Closed, "Cannot update entity, entity is closed");
            //if (m_syncObject != null) m_syncObject.Update100();
        }
        #endregion

        public virtual string GetFriendlyName()
        {
            return string.Empty;
        }
        #endregion

        #region Position And Movement Methods

        protected virtual bool ShouldSync
        {
            get
            {
                return m_syncObject != null && MyMultiplayer.Static != null;
            }
        }

        /// <summary>
        /// Scaling of this object's model. 
        /// Actualy used only for drawing and JLX collision skins. 
        /// I don't use it for line-triangleVertexes intersections in octree (but it can be added).
        /// </summary>

        float? m_scale;
        public float? Scale
        {
            get { return m_scale; }
            set
            {
                if (m_scale != value)
                {
                    m_scale = value;

                    var localMatrix = PositionComp.LocalMatrix;
                    if (m_scale != null)
                    {
                        System.Diagnostics.Debug.Assert(!MyUtils.IsZero(m_scale.Value));
                        var worldMatrix = PositionComp.WorldMatrix;
                        var localAABB = PositionComp.LocalAABB;
                        localAABB.Min *= m_scale.Value;
                        localAABB.Max *= m_scale.Value;
                        PositionComp.LocalAABB = localAABB;

                        if (Parent == null)
                        {
                            MyUtils.Normalize(ref worldMatrix, out worldMatrix);
                            PositionComp.WorldMatrix = MatrixD.CreateScale(m_scale.Value) * worldMatrix;
                        }
                        else
                        {
                            MyUtils.Normalize(ref localMatrix, out localMatrix);
                            PositionComp.LocalMatrix = Matrix.CreateScale(m_scale.Value) * localMatrix;
                        }
                    }
                    else
                    {
                        MyUtils.Normalize(ref localMatrix, out localMatrix);
                        PositionComp.LocalMatrix = localMatrix;
                    }

                    PositionComp.UpdateWorldMatrix();
                }
            }
        }

        public virtual MatrixD GetViewMatrix()
        {
            return PositionComp.WorldMatrixNormalizedInv;
        }

        /// <summary>
        /// Updates the world matrix (change caused by parent)
        /// </summary>
        public virtual void UpdateWorldMatrix(ref MatrixD parentWorldMatrix, object source = null)
        {
            PositionComp.UpdateWorldMatrix(ref parentWorldMatrix, source);
        }
        #endregion

        #region Draw Methods

        /// <summary>
        /// Draw physical representation of entity
        /// </summary>
        public virtual void DebugDrawPhysics()
        {
            foreach (var child in Hierarchy.Children)
            {
                (child.Container.Entity as MyEntity).DebugDrawPhysics();
            }

            if (this.m_physics == null)
            {
                return;
            }

            const float maxDrawDistance = 200;

            if (GetDistanceBetweenCameraAndBoundingSphere() > maxDrawDistance)
            {
                return;
            }

            this.m_physics.DebugDraw();
        }

        #endregion

        #region Intersection Methods

        //  Calculates intersection of line with object.
        public virtual bool GetIntersectionWithLine(ref LineD line, out Vector3D? v, bool useCollisionModel = true, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            v = null;
            MyModel collisionModel = Model;
            if (useCollisionModel)
                collisionModel = ModelCollision;

            if (collisionModel != null)
            {
                MyIntersectionResultLineTriangleEx? result = collisionModel.GetTrianglePruningStructure().GetIntersectionWithLine(this, ref line, flags);
                if (result != null)
                {
                    v = result.Value.IntersectionPointInWorldSpace;
                    return true;
                }
            }
            else
                Debug.Assert(false);//this should be overriden by child class if object has no model by default
            return false;
        }

        //  Calculates intersection of line with any triangleVertexes in this model instance. Closest intersection and intersected triangleVertexes will be returned.
        internal virtual bool GetIntersectionWithLine(ref LineD line, out MyIntersectionResultLineTriangleEx? t, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            bool ret = false;

            t = null;
            MyModel collisionModel = Model;

            if (collisionModel != null)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyEntity.GetIntersectionWithLine on model");
                MyIntersectionResultLineTriangleEx? result = collisionModel.GetTrianglePruningStructure().GetIntersectionWithLine(this, ref line, flags);
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                if (result != null)
                {
                    t = result.Value;
                    ret = true;
                }
            }

            return ret;

        }


        //  Calculates intersection of line with any triangleVertexes in this model instance. All intersections and intersected triangleVertexes will be returned.
        internal virtual bool GetIntersectionsWithLine(ref LineD line, List<MyIntersectionResultLineTriangleEx> result, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            MyModel collisionModel = Model;

            if (collisionModel != null)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyEntity.GetIntersectionWithLine on model");
                collisionModel.GetTrianglePruningStructure().GetTrianglesIntersectingLine(this, ref line, flags, result);
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

            return result.Count > 0;
        }

        //  Calculates intersection of line with any bounding sphere in this model instance. Center of the bounding sphere will be returned.
        //  It takes boundingSphereRadiusMultiplier argument which serves for extending the influence (radius) for interaction with line.
        public virtual Vector3D? GetIntersectionWithLineAndBoundingSphere(ref LineD line, float boundingSphereRadiusMultiplier)
        {
            if (Render.GetModel() == null)
                return null;

            BoundingSphereD vol = PositionComp.WorldVolume;
            vol.Radius *= boundingSphereRadiusMultiplier;

            //  Check if line intersects phys object's current bounding sphere, and if not, return 'no intersection'
            if (!MyUtils.IsLineIntersectingBoundingSphere(ref line, ref vol))
                return null;

            return vol.Center;
        }

        //  Return true if object intersects specified sphere.
        //  This method doesn't return exact point of intersection or any additional data.
        //  We don't look for closest intersection - so we stop on first intersection found.
        public virtual bool GetIntersectionWithSphere(ref BoundingSphereD sphere)
        {
            MyModel collisionModel = Model;

            if (collisionModel != null)
                return collisionModel.GetTrianglePruningStructure().GetIntersectionWithSphere(this, ref sphere);
            return false;
        }

        //  Return list of triangles intersecting specified sphere. 
        public void GetTrianglesIntersectingSphere(ref BoundingSphereD sphere, Vector3? referenceNormalVector, float? maxAngle,
                                                   List<MyTriangle_Vertex_Normals> retTriangles, int maxNeighbourTriangles)
        {
            MyModel collisionModel = Model;

            if (collisionModel != null)
            {
                BoundingSphereD sph = (BoundingSphereD)sphere;
                collisionModel.GetTrianglePruningStructure().GetTrianglesIntersectingSphere(ref sph, referenceNormalVector, maxAngle, retTriangles, maxNeighbourTriangles);
            }
        }

        public virtual bool DoOverlapSphereTest(float sphereRadius, Vector3D spherePos)
        {
            return false;
        }


        private Vector3[] m_frustumIntersectionCorners = new Vector3[8];

        //  Smalles distance between camera and bounding sphere of this phys object. Result is always positive, even if camera is inside the sphere.
        public double GetSmallestDistanceBetweenCameraAndBoundingSphere()
        {
            Vector3D campos = MySector.MainCamera.Position;
            var v = PositionComp.WorldVolume;
            return MyUtils.GetSmallestDistanceToSphereAlwaysPositive(ref campos, ref v);
        }

        //  Largest distance from camera to bounding sphere of this phys object. Result is always positive, even if camera is inside the sphere.
        //  It's actualy distance between camera and opposite side of the sphere
        public double GetLargestDistanceBetweenCameraAndBoundingSphere()
        {
            Vector3D campos = MySector.MainCamera.Position;
            var v = PositionComp.WorldVolume;
            return MyUtils.GetLargestDistanceToSphere(ref campos, ref v);
        }

        //  Distance from camera to bounding sphere of this phys object. Result is always positive, even if camera is inside the sphere.
        public double GetDistanceBetweenCameraAndBoundingSphere()
        {
            Vector3D campos = MySector.MainCamera.Position;
            var v = PositionComp.WorldVolume;
            return MyUtils.GetSmallestDistanceToSphereAlwaysPositive(ref campos, ref v);
        }

        //  Distance from camera to position of entity.
        public double GetDistanceBetweenCameraAndPosition()
        {
            return Vector3D.Distance(MySector.MainCamera.Position, this.PositionComp.GetPosition());
        }

        // When ie. Large Weapon is hit, all parts must be ignored in aoe damage
        public virtual MyEntity GetBaseEntity()
        {
            return this;
        }
        #endregion

        #region Entity events

        /// <summary>
        /// Called when [activated] which for entity means that was added to scene.
        /// </summary>
        /// <param name="source">The source of activation.</param>
        public virtual void OnAddedToScene(object source)
        {
            System.Diagnostics.Debug.Assert(InScene == false, "Object was inserted twice into the scene");
            System.Diagnostics.Debug.Assert((EntityId != 0 && Save) || !Save);

            InScene = true;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("OnActivated");

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("UpdateRenderobject");
            Render.UpdateRenderObject(true);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if (NeedsUpdate != MyEntityUpdateEnum.NONE)
                MyEntities.RegisterForUpdate(this);

            if (Render.NeedsDraw)
                MyEntities.RegisterForDraw(this);

            if (this.m_physics != null)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("m_physics.Activate");
                this.m_physics.Activate();
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("AddToGamePruningStructure");
            AddToGamePruningStructure();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            Components.OnAddedToScene();

            foreach (var child in Hierarchy.Children)
            {
                child.Container.Entity.OnAddedToScene(source);
            }

            if (MyFakes.ENABLE_ASTEROID_FIELDS)
            {
                if (Sandbox.Game.World.Generator.MyProceduralWorldGenerator.Static != null)
                {
                    Sandbox.Game.World.Generator.MyProceduralWorldGenerator.Static.TrackEntity(this);
                }
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }


        public virtual void OnRemovedFromScene(object source)
        {
            InScene = false;

            if (Hierarchy != null)
            {
                foreach (var child in Hierarchy.Children)
                {
                    child.Container.Entity.OnRemovedFromScene(source);
                }
            }

            Components.OnRemovedFromScene();

            MyEntities.UnregisterForUpdate(this);
            MyEntities.UnregisterForDraw(this);

            if (this.m_physics != null && this.m_physics.Enabled)
            {
                this.m_physics.Deactivate();
            }

            Render.RemoveRenderObjects();

            RemoveFromGamePruningStructure();
        }

        /// <summary>
        /// This event may not be invoked at all, when calling MyEntities.CloseAll, marking is bypassed
        /// </summary>
        public event Action<MyEntity> OnMarkForClose;
        public event Action<MyEntity> OnClose;
        public event Action<MyEntity> OnClosing;

        public event Action<MyEntity> OnPhysicsChanged;

        //public event Action<MyEntity> OnContactEvent;

        #endregion

        #region Implementation of IMyNotifyEntityChanged

        // Because for now entity = script resource entity also implements this interface for inheritors.

        #endregion

        public void RaisePhysicsChanged()
        {
            var handler = OnPhysicsChanged;
            if (handler != null)
                handler(this);
        }

        #region Drawing, objectbuilder, init & close

        public virtual void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            ProfilerShort.Begin("MyEntity.Init(objectBuilder)");
            MarkedForClose = false;
            Closed = false;
            this.Render.PersistentFlags = MyPersistentEntityFlags2.CastShadows;
            if (objectBuilder != null)
            {
                if (objectBuilder.PositionAndOrientation.HasValue)
                {
                    var posAndOrient = objectBuilder.PositionAndOrientation.Value;
                    MatrixD matrix = MatrixD.CreateWorld(posAndOrient.Position, posAndOrient.Forward, posAndOrient.Up);
                    MyUtils.AssertIsValid(matrix);

                    PositionComp.SetWorldMatrix((MatrixD)matrix);
                    if(MyPerGameSettings.LimitedWorld)
                    {
                        ClampToWorld();                    
                    }
                }
                // Do not copy EntityID if it gets overwritten later. It might
                // belong to some existing entity that we're making copy of.
                if (objectBuilder.EntityId != 0)
                    this.EntityId = objectBuilder.EntityId;
                this.Name = objectBuilder.Name;
                this.Render.PersistentFlags = objectBuilder.PersistentFlags;
            }

            AllocateEntityID();

            this.InScene = false;

            MyEntities.SetEntityName(this, false);

            if (SyncFlag)
            {
                CreateSync();
            }
            GameLogic.Init(objectBuilder);
            ProfilerShort.End();
        }

        protected virtual void ClampToWorld()
        {
            var resPosition = PositionComp.GetPosition();
            float offset = 10;
            if (resPosition.X > MySession.Static.WorldBoundaries.Max.X)
                resPosition.X = MySession.Static.WorldBoundaries.Max.X - offset;
            else if (resPosition.X < MySession.Static.WorldBoundaries.Min.X)
                resPosition.X = MySession.Static.WorldBoundaries.Min.X + offset;
            if (resPosition.Y > MySession.Static.WorldBoundaries.Max.Y)
                resPosition.Y = MySession.Static.WorldBoundaries.Max.Y - offset;
            else if (resPosition.Y < MySession.Static.WorldBoundaries.Min.Y)
                resPosition.Y = MySession.Static.WorldBoundaries.Min.Y + offset;
            if (resPosition.Z > MySession.Static.WorldBoundaries.Max.Z)
                resPosition.Z = MySession.Static.WorldBoundaries.Max.Z - offset;
            else if (resPosition.Z < MySession.Static.WorldBoundaries.Min.Z)
                resPosition.Z = MySession.Static.WorldBoundaries.Min.Z + offset;
            PositionComp.SetPosition(resPosition);
        }

        private void AllocateEntityID()
        {
            if (this.EntityId == 0 && MyEntityIdentifier.AllocationSuspended == false)
            {
                this.EntityId = MyEntityIdentifier.AllocateId();
            }
        }

        //  This is real initialization of this class!!! Instead of constructor.
        public void Init(StringBuilder displayName,
                         string model,
                         MyEntity parentObject,
                         float? scale,
                         string modelCollision = null)
        {
            ProfilerShort.Begin("MyEntity.Init(...models...)");
            MarkedForClose = false;
            Closed = false;
            this.Render.PersistentFlags = MyPersistentEntityFlags2.CastShadows;
            this.DisplayName = displayName != null ? displayName.ToString() : null;

            RefreshModels(model, modelCollision);

            if (parentObject != null)
            {
                parentObject.Hierarchy.AddChild(this, false, false);
            }

            PositionComp.Scale = scale;

            AllocateEntityID();
            ProfilerShort.End();
        }

        public void RefreshModels(string model, string modelCollision)
        {
            if (model != null)
            {
                Render.ModelStorage = MyModels.GetModelOnlyData(model);
                PositionComp.LocalVolumeOffset = Render.GetModel().BoundingSphere.Center;
            }
            if (modelCollision != null)
                m_modelCollision = MyModels.GetModelOnlyData(modelCollision);

            if (Render.ModelStorage != null)
            {
                this.PositionComp.LocalAABB = Render.GetModel().BoundingBox;

                bool idAllocationState = MyEntityIdentifier.AllocationSuspended;
                try
                {
                    MyEntityIdentifier.AllocationSuspended = false;

                    if (Subparts == null)
                        Subparts = new Dictionary<string, MyEntitySubpart>();
                    else
                    {
                        foreach (var existingSubpart in Subparts)
                        {
                            Hierarchy.RemoveChild(existingSubpart.Value);
                            existingSubpart.Value.Close();
                        }
                        Subparts.Clear();
                    }

                    MyEntitySubpart.Data data = new MyEntitySubpart.Data();
                    foreach (var dummy in Render.GetModel().Dummies)
                    {
                        // Check of mirrored matrix of dummy object is under fake, because characters have mirrored dummies
                        if (MyFakes.ENABLE_DUMMY_MIRROR_MATRIX_CHECK)
                        {
                            // This should not be here but if you want to check bad matrices of other types
                            if (!(this is MyCharacter))
                            {
                                Debug.Assert(!dummy.Value.Matrix.IsMirrored());
                            }
                        }

                        if (!MyEntitySubpart.GetSubpartFromDummy(model, dummy.Key, dummy.Value, ref data))
                            continue;

                        MyEntitySubpart subpart = new MyEntitySubpart();
                        subpart.Render.EnableColorMaskHsv = Render.EnableColorMaskHsv;
                        subpart.Render.ColorMaskHsv = Render.ColorMaskHsv;
                        subpart.Init(null, data.File, this, null);
                        subpart.PositionComp.LocalMatrix = data.InitialTransform;
                        Subparts[data.Name] = subpart;

                        if (InScene)
                            subpart.OnAddedToScene(this);
                    }
                }
                finally
                {
                    MyEntityIdentifier.AllocationSuspended = idAllocationState;
                }

                if (Render.GetModel().GlassData != null)
                {
                    Render.NeedsDraw = true;
                    Render.NeedsDrawFromParent = true;
                }

            }
            else
            {   //entities without model has box with side length = 1 by default
                float defaultBoxHalfSize = 0.5f;
                this.PositionComp.LocalAABB = new BoundingBox(new Vector3(-defaultBoxHalfSize), new Vector3(defaultBoxHalfSize));
            }
        }

        /// <summary>
        /// Every object must have this method, but not every phys object must necessarily have something to cleanup
        /// <remarks>
        /// </remarks>
        /// </summary>
        public void Delete()
        {
            Close();
            BeforeDelete();
            GameLogic.Close();
            //doesnt work in parallel update
            //Debug.Assert(MySandboxGame.IsMainThread(), "Entity.Close() called not from Main Thread!");
            Debug.Assert(MyEntities.UpdateInProgress == false, "Do not close entities directly in Update*, use MarkForClose() instead");
            Debug.Assert(MyEntities.CloseAllowed == true, "Use MarkForClose()");
            Debug.Assert(!Closed, "Close() called twice!");

            //Children has to be cleared after close notification is send
            while (Hierarchy.Children.Count > 0)
            {
                MyHierarchyComponentBase compToRemove = Hierarchy.Children[Hierarchy.Children.Count - 1];
                Debug.Assert(compToRemove.Parent != null, "Entity has no parent but is part of children collection");

                compToRemove.Container.Entity.Delete();

                Hierarchy.Children.Remove(compToRemove);
            }

            //OnPositionChanged = null;

            CallAndClearOnClosing();

            MyDecals.RemoveModelDecals(this);
            MyEntities.RemoveName(this);
            MyEntities.RemoveFromClosedEntities(this);

            if (m_physics != null)
            {
                m_physics.Close();
                Physics = null;

                RaisePhysicsChanged();
            }

            MyEntities.UnregisterForUpdate(this, true);


            if (Parent == null) //only root objects are in entities list
                MyEntities.Remove(this);
            else
            {
                Parent.Hierarchy.Children.Remove(this.Hierarchy);

                //remove children first
                if (Parent.InScene)
                    OnRemovedFromScene(this);

                MyEntities.RaiseEntityRemove(this);
            }

            if (this.EntityId != 0)
            {
                MyEntityIdentifier.RemoveEntity(this.EntityId);
            }

            //this.EntityId = 0;
            Debug.Assert(this.Hierarchy.Children.Count == 0);

            CallAndClearOnClose();

			Components.Clear();

            ClearDebugRenderComponents();

            Closed = true;
        }

        protected virtual void BeforeDelete() 
        {
        }

        protected virtual void Closing()
        { 
        }

        /// <summary>
        /// This method marks this entity for close which means, that Close
        /// will be called after all entities are updated
        /// </summary>
        public void Close()
        {
            // TODO: Make synchronized
            if (!MarkedForClose)
            {
                // Needs update = false, added, because entities was updated once before closed
                //NeedsUpdate = MyEntityUpdateEnum.NONE;
                MarkedForClose = true;
                Closing();
                MyEntities.Close(this);
                GameLogic.MarkForClose();
                ProfilerShort.Begin("MarkForCloseHandler");
                var handler = OnMarkForClose;
                if (handler != null) handler(this);
                ProfilerShort.End();
            }
        }

        private void CallAndClearOnClose()
        {
            if (OnClose != null)
                OnClose(this);

            OnClose = null;
        }

        private void CallAndClearOnClosing()
        {
            if (OnClosing != null)
                OnClosing(this);

            OnClosing = null;
        }

        /// <summary>
        /// Gets object builder from object.
        /// </summary>
        /// <returns></returns>
        public virtual MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            var objBuilder = MyEntityFactory.CreateObjectBuilder(this);

            if (objBuilder != null)
            {
                objBuilder.PositionAndOrientation = new MyPositionAndOrientation()
                {
                    Position = this.PositionComp.GetPosition(),
                    Up = (Vector3)this.WorldMatrix.Up,
                    Forward = (Vector3)this.WorldMatrix.Forward
                };

                objBuilder.EntityId = this.EntityId;
                Debug.Assert(objBuilder.EntityId != 0);

                objBuilder.Name = this.Name;
                objBuilder.PersistentFlags = Render.PersistentFlags;
            }
            return objBuilder;
        }

        /// <summary>
        /// Called before method GetObjectBuilder, when saving sector
        /// </summary>
        public virtual void BeforeSave()
        {

        }

        /// <summary>
        /// Method is called defacto from Update, preparation fo Draw
        /// </summary>
        public virtual void PrepareForDraw()
        {
            foreach (var render in m_debugRenderers)
            {
                render.PrepareForDraw();
            }
        }

        #endregion

        public override string ToString()
        {
            return this.GetType().Name + " {" + EntityId.ToString("X8") + "}";
        }

    }
}