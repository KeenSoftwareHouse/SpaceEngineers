#region Using

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage.Utils;
using VRageMath;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game.Models;
using VRage.Game.Gui;
using VRage.Game.Utils;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Library.Collections;
using VRage.Profiler;
using VRage.Network;

#endregion

namespace VRage.Game.Entity
{
    // IMyInventoryOwner is kept for backward's compatibility for modders
    [MyEntityType(typeof(MyObjectBuilder_EntityBase))]
    public partial class MyEntity
    {
        #region Fields

        //TODO: This should be set only inside entity
        public MyDefinitionId? DefinitionId = null; //{ get; private set; }

        public MyEntityComponentContainer Components { get; private set; }

        public string Name;

        public bool DebugAsyncLoading; // Will be eventually removed
        public DebugCreatedBy DebugCreatedBy; // Will be eventually removed

        private List<MyEntity> m_tmpOnPhysicsChanged = new List<MyEntity>();
        public float m_massChangeForCollisions = 1f;

        protected List<MyHudEntityParams> m_hudParams;

        MyPositionComponentBase m_position;
        public MyPositionComponentBase PositionComp
        {
            get
            {
                //Debug.Assert(!(m_position is MyNullPositionComponent));
                return m_position;
            }
            set
            {
                Components.Add<MyPositionComponentBase>(value);
            }
        }

        // server position
        public Vector3D m_serverPosition = new Vector3D();
        public Quaternion m_serverOrientation = new Quaternion();
        public MatrixD m_serverWorldMatrix = new MatrixD();
        // server velocities
        public Vector3 m_serverLinearVelocity;
        public Vector3 m_serverAngularVelocity;

        public bool m_positionResetFromServer;
        public bool SentFromServer;

        MyRenderComponentBase m_render;
        public MyRenderComponentBase Render
        {
            get
            {
                //Debug.Assert(!(m_render is MyNullRenderComponent));
                return m_render;
            }
            set
            {
                Components.Add<MyRenderComponentBase>(value);
            }
        }

        #region Debug rendering
        List<MyDebugRenderComponentBase> m_debugRenderers = new List<MyDebugRenderComponentBase>();

        public void DebugDraw()
        {
            if (this.Hierarchy != null)
            {
                foreach (var child in this.Hierarchy.Children)
                {
                    child.Container.Entity.DebugDraw();
                }
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

        #endregion 
        
        //Rendering
        protected MyModel m_modelCollision;                       //  Collision model, used only for collisions

        //Space query structure (don't change these directly).
        public int GamePruningProxyId = MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED;
        public int TopMostPruningProxyId = MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED;
        public bool StaticForPruningStructure = false;
        public int TargetPruningProxyId = MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED;

        bool m_raisePhysicsCalled = false;

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
            get { return m_entityId; }
            set
            {
                if (m_entityId != 0)
                {
                    var oldVal = m_entityId;
                    if (value == 0)
                    {
                        m_entityId = 0;
                        MyEntityIdentifier.RemoveEntity(oldVal);
                    }
                    else
                    {
                        m_entityId = value;
                        MyEntityIdentifier.SwapRegisteredEntityId(this, oldVal, m_entityId);
                    }
                }
                else
                {
                    if (value != 0)
                    {
                        m_entityId = value;
                        MyEntityIdentifier.AddEntityWithId(this);
                    }
                    else
                        System.Diagnostics.Debug.Fail("Useless setting of 0 as ID");
                }
            }
        }
        private long m_entityId;

        private MySyncComponentBase m_syncObject;

        public MySyncComponentBase SyncObject { get { return m_syncObject; } protected set { Components.Add<MySyncComponentBase>(value); } }

        private MyModStorageComponentBase m_storage;

        public MyModStorageComponentBase Storage { get { return m_storage; } set { Components.Add<MyModStorageComponentBase>(value); } }

        //Only debug property, use only for asserts, not for game logic.
        //Consider as being called after delete in C++
        public bool Closed { get; protected set; }
        public bool MarkedForClose { get; protected set; }

        public virtual float MaxGlassDistSq
        {
            get
            {
                IMyCamera currentCamera = MyAPIGatewayShortcuts.GetMainCamera != null ? MyAPIGatewayShortcuts.GetMainCamera() : null;
                if (currentCamera != null)
                    return 0.01f * currentCamera.FarPlaneDistance * currentCamera.FarPlaneDistance;
                else
                    return 0.01f * MyCamera.DefaultFarPlaneDistance * MyCamera.DefaultFarPlaneDistance;
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

        bool m_isPreview = false;
        public bool IsPreview
        {
            get
            {
                return m_isPreview;
            }
            set
            {
                m_isPreview = value;
            }
        }

        bool m_isreadyForReplication = false;
        public Dictionary<IMyReplicable, Action> ReadyForReplicationAction = new Dictionary<IMyReplicable, Action>();

        // Indicates whether the entity finished initialization and can be replicated for clients
        public bool IsReadyForReplication
        {
            get { return m_isreadyForReplication; }
            set 
            {
                m_isreadyForReplication = value;

                // Add your replicable to priority updates once done. Kind of hacky implementation. Should be remade when possible
                if (m_isreadyForReplication && ReadyForReplicationAction.Count > 0)
                {
                    foreach (var action in ReadyForReplicationAction.Values)
                    {
                        action();
                    }
                    ReadyForReplicationAction.Clear();
                }
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
                        MyEntitiesInterface.UnregisterUpdate(this, false);

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
                        MyEntitiesInterface.RegisterUpdate(this);
                }
            }
        }


        public MatrixD WorldMatrix
        {
            get { return PositionComp != null ? PositionComp.WorldMatrix : MatrixD.Zero; }
            set { if (PositionComp != null) PositionComp.SetWorldMatrix(value); }
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


        private MyHierarchyComponent<MyEntity> m_hierarchy;

        public MyHierarchyComponent<MyEntity> Hierarchy { get { return m_hierarchy; } set { Components.Add<MyHierarchyComponentBase>(value); } }

        MyHierarchyComponentBase IMyEntity.Hierarchy
        {
            get { return m_hierarchy; }
            set
            {
                if (!(value is MyHierarchyComponent<MyEntity>)) return;
                Components.Add<MyHierarchyComponentBase>(value);
            }
        }

        /// Optimized link to physics component.
        private MyPhysicsComponentBase m_physics;
        /// Implementing interface IMyEntity - get/set physics component.
        MyPhysicsComponentBase IMyEntity.Physics { get { return m_physics; } set { Components.Add<MyPhysicsComponentBase>(value); } }
        /// Gets the physic component of the entity.
        public MyPhysicsComponentBase Physics
        {
            get { return m_physics; }
            set { Components.Add<MyPhysicsComponentBase>(value); }
        }

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
                return Render != null && ((Render.PersistentFlags & MyPersistentEntityFlags2.InScene) > 0);
            }
            set
            {
                if (Render == null)
                    return;
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
            get { return PositionComp != null ? PositionComp.GetPosition() : Vector3D.Zero; }
        }

        public virtual List<MyHudEntityParams> GetHudParams(bool allowBlink)
        {
            return m_hudParams;
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

        public string DebugName
        {
            get
            {
                string name = m_displayName ?? Name;
                if (name == null)
                    name = "";
                return name + " (" + GetType().Name + ", " + EntityId.ToString() + ")";
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

            this.Flags = EntityFlags.Default;

            if (initComponents)
            {
                this.m_hudParams = new List<MyHudEntityParams>(1);
                this.Hierarchy = new MyHierarchyComponent<MyEntity>();
                this.GameLogic = new MyNullGameLogicComponent();
                this.PositionComp = new MyPositionComponent();

                PositionComp.LocalMatrix = Matrix.Identity;

                CreateStandardRenderComponentsExtCallback(this);
            }
        }

        void Components_ComponentAdded(Type t, MyEntityComponentBase c)
        {
            // TODO: this has to be refactored because it is very dangerous - each component member set here can be overwritten with new one of the same base type
            // (assume more than one GameLogicComponent), components should support aggregates (parameter can be added to MyComponentTypeAttribute).
            if ((typeof(MyPhysicsComponentBase)).IsAssignableFrom(t))
                m_physics = c as MyPhysicsComponentBase;
            else if ((typeof(MySyncComponentBase)).IsAssignableFrom(t))
                m_syncObject = c as MySyncComponentBase;
            else if ((typeof(MyGameLogicComponent)).IsAssignableFrom(t))
                m_gameLogic = c as MyGameLogicComponent;
            else if ((typeof(MyPositionComponentBase)).IsAssignableFrom(t))
            {
                m_position = c as MyPositionComponentBase;
                if (m_position == null)
                    PositionComp = new VRage.Game.Components.MyNullPositionComponent();
            }
            else if ((typeof(MyHierarchyComponentBase)).IsAssignableFrom(t))
                m_hierarchy = c as MyHierarchyComponent<MyEntity>;
            else if ((typeof(MyRenderComponentBase)).IsAssignableFrom(t))
            {
                m_render = c as MyRenderComponentBase;
                if (m_render == null)
                    Render = new VRage.Game.Components.MyNullRenderComponent();
            }
            else if ((typeof(MyInventoryBase)).IsAssignableFrom(t))
            {
                OnInventoryComponentAdded(c as MyInventoryBase);
            }
            else if ((typeof(MyModStorageComponentBase)).IsAssignableFrom(t))
            {
                m_storage = c as MyModStorageComponentBase;
            }
        }

        void Components_ComponentRemoved(Type t, MyEntityComponentBase c)
        {
            // TODO: see comment at Components_ComponentAdded
            if ((typeof(MyPhysicsComponentBase)).IsAssignableFrom(t))
                m_physics = null;
            else if ((typeof(MySyncComponentBase)).IsAssignableFrom(t))
                m_syncObject = null;
            else if ((typeof(MyGameLogicComponent)).IsAssignableFrom(t))
                m_gameLogic = null;
            else if ((typeof(MyPositionComponentBase)).IsAssignableFrom(t))
                PositionComp = new VRage.Game.Components.MyNullPositionComponent();
            else if ((typeof(MyHierarchyComponentBase)).IsAssignableFrom(t))
                m_hierarchy = null;
            else if ((typeof(MyRenderComponentBase)).IsAssignableFrom(t))
            {
                Render = new VRage.Game.Components.MyNullRenderComponent();
            }
            else if ((typeof(MyInventoryBase)).IsAssignableFrom(t))
            {
                OnInventoryComponentRemoved(c as MyInventoryBase);
            }
            else if ((typeof(MyModStorageComponentBase)).IsAssignableFrom(t))
            {
                m_storage = null;
            }
        }

        protected virtual MySyncComponentBase OnCreateSync()
        {
            //return new MySyncEntity(this); // removing dependency on sandbox
            return CreateDefaultSyncEntityExtCallback(this);
        }

        public void CreateSync()
        {
            SyncObject = OnCreateSync();
        }

        public MyEntitySubpart GetSubpart(string name)
        {
            return Subparts[name];
        }

        public bool TryGetSubpart(string name, out MyEntitySubpart subpart)
        {
            return Subparts.TryGetValue(name, out subpart);
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
            ProfilerShort.Begin(m_gameLogic.GetType().Name);
            m_gameLogic.UpdateBeforeSimulation10();
            ProfilerShort.End();
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

        public virtual MatrixD GetViewMatrix()
        {
            return PositionComp.WorldMatrixNormalizedInv;
        }

        public void SetSpeedsAccordingToServerValues()
        {
            if (Physics != null)
            {
                Physics.SetSpeeds(m_serverLinearVelocity, m_serverAngularVelocity);
            }
        }

        public virtual void SetWorldMatrix(MatrixD worldMatrix, bool forceUpdate = false, bool updateChildren = true)
        {
            if (PositionComp != null) PositionComp.SetWorldMatrix(worldMatrix, null, forceUpdate, updateChildren );
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
                VRage.Game.Models.MyIntersectionResultLineTriangleEx? result = collisionModel.GetTrianglePruningStructure().GetIntersectionWithLine(this, ref line, flags);
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
        public virtual bool GetIntersectionWithLine(ref LineD line, out VRage.Game.Models.MyIntersectionResultLineTriangleEx? t, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            bool ret = false;

            t = null;
            MyModel collisionModel = Model;

            if (collisionModel != null)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyEntity.GetIntersectionWithLine on model");
                VRage.Game.Models.MyIntersectionResultLineTriangleEx? result = collisionModel.GetTrianglePruningStructure().GetIntersectionWithLine(this, ref line, flags);
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
            IMyCamera currentCamera = MyAPIGatewayShortcuts.GetMainCamera();
            Vector3D campos = currentCamera.Position;
            var v = PositionComp.WorldVolume;
            return MyUtils.GetSmallestDistanceToSphereAlwaysPositive(ref campos, ref v);
        }

        //  Largest distance from camera to bounding sphere of this phys object. Result is always positive, even if camera is inside the sphere.
        //  It's actualy distance between camera and opposite side of the sphere
        public double GetLargestDistanceBetweenCameraAndBoundingSphere()
        {
            IMyCamera currentCamera = MyAPIGatewayShortcuts.GetMainCamera();
            Vector3D campos = currentCamera.Position;
            var v = PositionComp.WorldVolume;
            return MyUtils.GetLargestDistanceToSphere(ref campos, ref v);
        }

        //  Distance from camera to bounding sphere of this phys object. Result is always positive, even if camera is inside the sphere.
        public double GetDistanceBetweenCameraAndBoundingSphere()
        {
            IMyCamera currentCamera = MyAPIGatewayShortcuts.GetMainCamera();
            Vector3D campos = currentCamera.Position;
            var v = PositionComp.WorldVolume;
            return MyUtils.GetSmallestDistanceToSphereAlwaysPositive(ref campos, ref v);
        }

        // Distance from current player position to bounding sphere of this phys object. Result is always positive, even if player is inside the sphere.
        public double GetDistanceBetweenPlayerPositionAndBoundingSphere()
        {
            Vector3D playerPos = MyAPIGatewayShortcuts.GetLocalPlayerPosition();
            var v = PositionComp.WorldVolume;
            return MyUtils.GetSmallestDistanceToSphereAlwaysPositive(ref playerPos, ref v);
        }

        //  Distance from camera to position of entity.
        public double GetDistanceBetweenCameraAndPosition()
        {
            IMyCamera currentCamera = MyAPIGatewayShortcuts.GetMainCamera();
            return Vector3D.Distance(currentCamera.Position, this.PositionComp.GetPosition());
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
                MyEntitiesInterface.RegisterUpdate(this);

            if (Render.NeedsDraw)
                MyEntitiesInterface.RegisterDraw(this);

            if (this.m_physics != null)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("m_physics.Activate");
                this.m_physics.Activate();
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

            // If this is just an Entity
            if (GetType() == typeof(MyEntity))
            {
                Flags |= EntityFlags.Save | EntityFlags.IsGamePrunningStructureObject;
                PositionComp.LocalVolume = new BoundingSphere(Vector3.Zero, 0.5f);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("AddToGamePruningStructure");
            if (Parent == null || (Flags & EntityFlags.IsGamePrunningStructureObject) != 0)
                AddToGamePruningStructureExtCallBack(this);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            Components.OnAddedToScene();

            foreach (var child in Hierarchy.Children)
            {
                if (!child.Container.Entity.InScene)
                {
                    child.Container.Entity.OnAddedToScene(source);
                }
            }

            MyProceduralWorldGeneratorTrackEntityExtCallback(this);

            MyWeldingGroupsAddNodeExtCallback(this);
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

            MyEntitiesInterface.UnregisterUpdate(this, false);
            MyEntitiesInterface.UnregisterDraw(this);

            if (MyWeldingGroupsGroupExistsExtCallback(this) == true) //because of weird handling of weapons
                MyWeldingGroupsRemoveNodeExtCallback(this);

            if (this.m_physics != null && this.m_physics.Enabled)
            {
                this.m_physics.Deactivate();
            }

            Render.RemoveRenderObjects();

            RemoveFromGamePruningStructureExtCallBack(this);
        }

        /// <summary>
        /// This event may not be invoked at all, when calling MyEntities.CloseAll, marking is bypassed
        /// </summary>
        public event Action<MyEntity> OnMarkForClose;
        public event Action<MyEntity> OnClose;
        public event Action<MyEntity> OnClosing;

        public event Action<MyEntity> OnPhysicsChanged;

        public void AddToGamePruningStructure()
        {
            AddToGamePruningStructureExtCallBack(this);
        }

        public void RemoveFromGamePruningStructure()
        {
            RemoveFromGamePruningStructureExtCallBack(this);
        }

        public void UpdateGamePruningStructure()
        {
            UpdateGamePruningStructureExtCallBack(this);
        }

        #endregion

        #region Implementation of IMyNotifyEntityChanged

        // Because for now entity = script resource entity also implements this interface for inheritors.

        #endregion

        //jn:TODO this should be on Physics component
        public void RaisePhysicsChanged()
        {
            if (m_raisePhysicsCalled)
            {
                return;
            }
            m_raisePhysicsCalled = true;
            // TODO: JanN, this should be done cleaner imho
            if (!InScene)
            {
                var handler = OnPhysicsChanged;
                if (handler != null)
                    handler(this);
            }
            else
            {
                MyWeldingGroupsGetGroupNodesExtCallback(this, m_tmpOnPhysicsChanged);
                foreach (var entity in m_tmpOnPhysicsChanged)
                {
                    var handler = entity.OnPhysicsChanged;
                    if (handler != null)
                        handler(entity);
                }
                m_tmpOnPhysicsChanged.Clear();
            }
            m_raisePhysicsCalled = false;
        }

        #region Drawing, objectbuilder, init & close

        /// <summary>
        /// DONT USE THIS METHOD, EVER!
        /// </summary>
        /// <param name="id"></param>
        public void HackyComponentInitByMiroPleaseDontUseEver(MyDefinitionId id)
        {
            InitComponentsExtCallback(Components, id.TypeId, id.SubtypeId, null);
        }

        public virtual void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            ProfilerShort.Begin("MyEntity.Init(objectBuilder)");
            MarkedForClose = false;
            Closed = false;
            this.Render.PersistentFlags = MyPersistentEntityFlags2.CastShadows;

            if (objectBuilder != null)
            {
                if (objectBuilder.EntityId != 0)
                    this.EntityId = objectBuilder.EntityId;
                else
                    AllocateEntityID();

                DefinitionId = objectBuilder.GetId();

                if (objectBuilder.EntityDefinitionId != null) // backward compatibility
                {
                    Debug.Assert(objectBuilder.SubtypeName == null || objectBuilder.SubtypeName == objectBuilder.EntityDefinitionId.Value.SubtypeName);
                    DefinitionId = objectBuilder.EntityDefinitionId.Value;
                }

                if (objectBuilder.PositionAndOrientation.HasValue)
                {
                    var posAndOrient = objectBuilder.PositionAndOrientation.Value;

                    //GR: Check for NaN values and remove them (otherwise there will be problems wilth clusters)
                    if (posAndOrient.Position.x.IsValid() == false)
                    {
                        posAndOrient.Position.x = 0.0f;
                    }
                    if (posAndOrient.Position.y.IsValid() == false)
                    {
                        posAndOrient.Position.y = 0.0f;
                    }
                    if (posAndOrient.Position.z.IsValid() == false)
                    {
                        posAndOrient.Position.z = 0.0f;
                    }

                    MatrixD matrix = MatrixD.CreateWorld(posAndOrient.Position, posAndOrient.Forward, posAndOrient.Up);
                    //if (matrix.IsValid())
                    //    MatrixD.Rescale(ref matrix, scale);
                    MyUtils.AssertIsValid(matrix);
                    PositionComp.SetWorldMatrix((MatrixD)matrix);
                    ClampToWorld();
                }

                this.Name = objectBuilder.Name;
                this.Render.PersistentFlags = objectBuilder.PersistentFlags & ~VRage.ObjectBuilders.MyPersistentEntityFlags2.InScene;

                // This needs to be called after Entity has it's valid EntityID so components when are initiliazed or added to container, they get valid EntityID
                InitComponentsExtCallback(this.Components, DefinitionId.Value.TypeId, DefinitionId.Value.SubtypeId, objectBuilder.ComponentContainer);
            }
            else
            {
                AllocateEntityID();
            }

            Debug.Assert(!this.InScene, "Entity is in scene after creation!");

            MyEntitiesInterface.SetEntityName(this, false);

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
            // MZ: hotfixed crashing game
            BoundingBoxD bbWorld = MyAPIGatewayShortcuts.GetWorldBoundaries != null ? MyAPIGatewayShortcuts.GetWorldBoundaries() : default(BoundingBoxD);
            // clam only if AABB is valid
            if (bbWorld.Max.X > bbWorld.Min.X && bbWorld.Max.Y > bbWorld.Min.Y && bbWorld.Max.Z > bbWorld.Min.Z)
            {
                if (resPosition.X > bbWorld.Max.X)
                    resPosition.X = bbWorld.Max.X - offset;
                else if (resPosition.X < bbWorld.Min.X)
                    resPosition.X = bbWorld.Min.X + offset;
                if (resPosition.Y > bbWorld.Max.Y)
                    resPosition.Y = bbWorld.Max.Y - offset;
                else if (resPosition.Y < bbWorld.Min.Y)
                    resPosition.Y = bbWorld.Min.Y + offset;
                if (resPosition.Z > bbWorld.Max.Z)
                    resPosition.Z = bbWorld.Max.Z - offset;
                else if (resPosition.Z < bbWorld.Min.Z)
                    resPosition.Z = bbWorld.Min.Z + offset;
                PositionComp.SetPosition(resPosition);
            }
        }

        private void AllocateEntityID()
        {
            if (this.EntityId == 0 && MyEntityIdentifier.AllocationSuspended == false)
            {
                this.EntityId = MyEntityIdentifier.AllocateId();
            }
        }

        //  This is real initialization of this class!!! Instead of constructor.
        public virtual void Init(StringBuilder displayName,
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

            if (PositionComp.Scale == null)
            PositionComp.Scale = scale;

            AllocateEntityID();
            ProfilerShort.End();
        }

        public virtual void RefreshModels(string model, string modelCollision)
        {
            float scale = PositionComp.Scale.GetValueOrDefault(1.0f);
            if (model != null)
            {
                Render.ModelStorage = VRage.Game.Models.MyModels.GetModelOnlyData(model);
                var renderModel = Render.GetModel();
                PositionComp.LocalVolumeOffset = renderModel == null ? Vector3.Zero : renderModel.BoundingSphere.Center * scale;
             }

            if (modelCollision != null)
                m_modelCollision = VRage.Game.Models.MyModels.GetModelOnlyData(modelCollision);

            if (Render.ModelStorage != null)
            {
                var localAABB = Render.GetModel().BoundingBox;
                localAABB.Min = localAABB.Min * scale;
                localAABB.Max = localAABB.Max * scale;
                this.PositionComp.LocalAABB = localAABB;

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
                        // VRAGE TODO: Reenable this check after moving/splitting MyFakes?
                        //if (MyFakes.ENABLE_DUMMY_MIRROR_MATRIX_CHECK)
                        //{
                        //    // This should not be here but if you want to check bad matrices of other types
                        //    if (!(this is MyCharacter))
                        //    {
                        //        Debug.Assert(!dummy.Value.Matrix.IsMirrored());
                        //    }
                        //}

                        if (!MyEntitySubpart.GetSubpartFromDummy(model, dummy.Key, dummy.Value, ref data))
                            continue;

                        MyEntitySubpart subpart = new MyEntitySubpart();
                        subpart.Render.EnableColorMaskHsv = Render.EnableColorMaskHsv;
                        subpart.Render.ColorMaskHsv = Render.ColorMaskHsv;
                        // First rescale model
                        var subPartModel = MyModels.GetModelOnlyData(data.File);
                        if (subPartModel != null && Model != null)
                            subPartModel.Rescale(Model.ScaleFactor);

                        subpart.Init(null, data.File, this, PositionComp.Scale);

                        // Set this to false becase no one else is responsible for rendering subparts
                        subpart.Render.NeedsDrawFromParent = false;
                        subpart.Render.PersistentFlags = Render.PersistentFlags & ~MyPersistentEntityFlags2.InScene;

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
            if(Closed)
                return;

            Close();
            BeforeDelete();
            if(GameLogic != null)
            {
                GameLogic.Close();
            }

            //doesnt work in parallel update
            //Debug.Assert(MySandboxGame.IsMainThread(), "Entity.Close() called not from Main Thread!");
            Debug.Assert(MyEntitiesInterface.IsUpdateInProgress() == false, "Do not close entities directly in Update*, use MarkForClose() instead");
            Debug.Assert(MyEntitiesInterface.IsCloseAllowed() == true, "Use MarkForClose()");
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

            MyEntitiesInterface.RemoveName(this);
            MyEntitiesInterface.RemoveFromClosedEntities(this);

            if (m_physics != null)
            {
                m_physics.Close();
                Physics = null;

                RaisePhysicsChanged();
            }

            MyEntitiesInterface.UnregisterUpdate(this, true);


            if (Parent == null) //only root objects are in entities list
            {
                // Commented out - causes assertion when pasting grids
                //Debug.Assert(MyEntitiesInterface.Exist(this), "Entity does not have parent and is not in MyEntities");
                MyEntitiesInterface.Remove(this);
            }
            else
            {
                Parent.Hierarchy.Children.Remove(this.Hierarchy);

                //remove children first
                if (Parent.InScene)
                {
                    OnRemovedFromScene(this);
                    MyEntitiesInterface.RaiseEntityRemove(this);
                }
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

                MyEntitiesInterface.Close(this);
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
            var objBuilder = MyEntityFactoryCreateObjectBuilderExtCallback(this);

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

                objBuilder.ComponentContainer = Components.Serialize();

                if (this.DefinitionId.HasValue)
                {
                    objBuilder.SubtypeName = DefinitionId.Value.SubtypeName;
                }
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

        public virtual void BeforePaste()
        { 
        }

        public virtual void AfterPaste()
        {

        }

        public void SetEmissiveParts(string emissiveName, Color emissivePartColor, float emissivity)
        {
            UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], emissiveName, emissivePartColor, emissivity);
        }

        public void SetEmissivePartsForSubparts(string emissiveName, Color emissivePartColor, float emissivity)
        {
            if (Subparts != null)
            {
                foreach (var subPart in Subparts)
                {
                    subPart.Value.SetEmissiveParts(emissiveName, emissivePartColor, emissivity);
                }
            }
        }

        protected static void UpdateNamedEmissiveParts(uint renderObjectId, string emissiveName, Color emissivePartColor, float emissivity)
        {
            if (renderObjectId != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                VRageRender.MyRenderProxy.UpdateColorEmissivity(renderObjectId, 0, emissiveName, emissivePartColor, emissivity);
            }
        }

        #endregion

        public override string ToString()
        {
            return this.GetType().Name + " {" + EntityId.ToString("X8") + "}";
        }

        #region Inventory

        /// <summary>
        /// Search for inventory component with maching index.
        /// </summary>
        public virtual MyInventoryBase GetInventoryBase(int index)
        {
            MyInventoryBase baseInventory = null;
            if (!Components.TryGet(out baseInventory))
                return null;
            return baseInventory.IterateInventory(index);
        }

        /// <summary>
        /// Simply get the MyInventoryBase component stored in this entity.
        /// </summary>
        /// <returns></returns>
        public MyInventoryBase GetInventoryBase()
        {
            MyInventoryBase inventoryBase = null;
            Components.TryGet<MyInventoryBase>(out inventoryBase);
            return inventoryBase;
        }

        /// <summary>
        /// Iterate through inventories and return their count.
        /// </summary>
        public int InventoryCount
        {
            get
            {
                MyInventoryBase inventory = null;
                if (Components.TryGet<MyInventoryBase>(out inventory))
                {
                    return inventory.GetInventoryCount();
                }
                return 0;
            }
        }

        /// <summary>
        /// Returns true if this entity has got at least one inventory. 
        /// Note that one aggregate inventory can contain zero simple inventories => zero will be returned even if GetInventoryBase() != null.
        /// </summary>
        public bool HasInventory
        {
            get
            {
                return InventoryCount > 0;
            }
        }

        /// <summary>
        /// Display Name for GUI etc. Override in descendant classes. Usually used to display in terminal or inventory controls.
        /// </summary>
        public virtual String DisplayNameText
        {
            get;
            set;
        }

        protected virtual void OnInventoryComponentAdded(MyInventoryBase inventory) { }

        protected virtual void OnInventoryComponentRemoved(MyInventoryBase inventory) { }

        #endregion

        // ------------------ PLEASE READ -------------------------
        // VRAGE TODO: Delegates in MyEntity help us to get rid of sandbox. There are too many dependencies and this was the easy way to cut MyEntity out of sandbox.
        //             These delegates should not last here forever, after complete deletion of sandbox, there should be no reason for them to stay.

        // VRAGE TODO: Delegates helping us to move MyEntity to VRage.Game,
        // original AddToGamePruningStructure/RemoveFromGamePruningStructure/UpdateGamePruningStructureExtCallBack contained references to sandbox.
        // We cannot use extension or virtual function at this point.
        // This is probably temporary helper before we get rid of sandbox totally (pruning structure will be probably in vrage too).
        public static Action<MyEntity> AddToGamePruningStructureExtCallBack = null;
        public static Action<MyEntity> RemoveFromGamePruningStructureExtCallBack = null;
        public static Action<MyEntity> UpdateGamePruningStructureExtCallBack = null;

        // VRAGE TODO: Delegates helping us to move MyEntity to VRage.Game,
        // MyEntityFactory contains dozens of references to sandbox.
        // We cannot use extension or virtual function at this point.
        // This is probably temporary helper before we get rid of sandbox totally (MyEntityFactory will be probably in vrage too).
        public delegate MyObjectBuilder_EntityBase MyEntityFactoryCreateObjectBuilderDelegate(MyEntity entity);
        public static MyEntityFactoryCreateObjectBuilderDelegate MyEntityFactoryCreateObjectBuilderExtCallback = null;

        // VRAGE TODO: Delegates helping us to move MyEntity to VRage.Game,
        // MySyncEntity contains dozens of references to sandbox.
        // We cannot use extension or virtual function at this point.
        // This is probably temporary helper before we get rid of sandbox totally (MySyncEntity will be probably in vrage too).
        public delegate MySyncComponentBase CreateDefaultSyncEntityDelegate(MyEntity thisEntity);
        public static CreateDefaultSyncEntityDelegate CreateDefaultSyncEntityExtCallback;

        // VRAGE TODO: Delegates helping us to move MyEntity to VRage.Game,
        // MyWeldingGroups contains references to sandbox.
        // We cannot use extension or virtual function at this point.
        // This is probably temporary helper before we get rid of sandbox totally (MyWeldingGroups will be probably in vrage too).
        public static Action<MyEntity> MyWeldingGroupsAddNodeExtCallback = null;
        public static Action<MyEntity> MyWeldingGroupsRemoveNodeExtCallback = null;
        public static Action<MyEntity, List<MyEntity>> MyWeldingGroupsGetGroupNodesExtCallback = null;
        public delegate bool MyWeldingGroupsGroupExistsDelegate(MyEntity entity);
        public static MyWeldingGroupsGroupExistsDelegate MyWeldingGroupsGroupExistsExtCallback = null;

        // VRAGE TODO: Delegates helping us to move MyEntity to VRage.Game. See above.
        public static Action<MyEntity> MyProceduralWorldGeneratorTrackEntityExtCallback = null;

        // VRAGE TODO: Delegates helping us to move MyEntity to VRage.Game. See above.
        public static Action<MyEntity> CreateStandardRenderComponentsExtCallback = null;

        // VRAGE TODO: Delegates helping us to move MyEntity to VRage.Game. See above.
        public static Action<MyComponentContainer, MyObjectBuilderType, MyStringHash, MyObjectBuilder_ComponentContainer> InitComponentsExtCallback = null;

        // VRAGE TODO: Delegates helping us to move MyEntity to VRage.Game. See above.
        public static Func<MyObjectBuilder_EntityBase, bool, MyEntity> MyEntitiesCreateFromObjectBuilderExtCallback = null; 

        public virtual void SerializeControls(BitStream stream)
        {
            stream.WriteBool(false);
        }
        public virtual void DeserializeControls(BitStream stream, bool outOfOrder)
        {
            var valid = stream.ReadBool();
            Debug.Assert(!valid);
        }
        public virtual void ApplyLastControls()
        {
        }
    }
}