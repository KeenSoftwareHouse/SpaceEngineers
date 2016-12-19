using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.World;
using Sandbox.Game.WorldEnvironment.Definitions;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using Sandbox.ModAPI;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.Models;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.WorldEnvironment
{
    public class MyEnvironmentSector : MyEntity
    {
        #region IMyEnvironmentSector

        #region Public

        public Vector3D SectorCenter
        {
            get { return m_sectorCenter; }
            private set { m_sectorCenter = value; }
        }

        public Vector3D[] Bounds { get; private set; }

        public MyWorldEnvironmentDefinition EnvironmentDefinition { get; private set; }

        public void Init(IMyEnvironmentOwner owner, ref MyEnvironmentSectorParameters parameters)
        {
            // Copy parameters in
            SectorCenter = parameters.Center;

            Bounds = parameters.Bounds;

            m_dataRange = parameters.DataRange;

            m_environment = (MyProceduralEnvironmentDefinition)parameters.Environment;

            EnvironmentDefinition = parameters.Environment;

            m_owner = owner;

            m_provider = parameters.Provider;

            // Compute appropriate render origin.
            Vector3D center = parameters.Center;

            owner.ProjectPointToSurface(ref center);

            if (!Engine.Platform.Game.IsDedicated)
                m_render = new MyInstancedRenderSector(string.Format("{0}:Sector({1:X})", owner, parameters.SectorId), MatrixD.CreateTranslation(center));

            SectorId = parameters.SectorId;

            BoundingBoxD worldBounds = BoundingBoxD.CreateInvalid();
            for (int i = 0; i < 8; ++i)
                worldBounds.Include(Bounds[i]);

            // Entity stuff
            PositionComp.SetPosition(parameters.Center);
            PositionComp.WorldAABB = worldBounds;

            // Add missing stuff
            AddDebugRenderComponent(new MyDebugRenderComponentEnvironmentSector(this));
            GameLogic = new MyNullGameLogicComponent();
            Save = false;

            IsClosed = false;
        }

        protected override void Closing()
        {
            CloseInternal(true);
        }

        public new void Close()
        {
            CloseInternal(false);
        }

        private void CloseInternal(bool entityClosing)
        {
            if (m_render != null)
                m_render.DetachEnvironment(this);

            Debug.Assert(LodLevel == -1 || entityClosing);
            Debug.Assert(!HasPhysics || entityClosing);

            if (DataView != null)
            {
                DataView.Close();
                DataView = null;
            }

            foreach (var module in m_modules.Values)
            {
                module.Proxy.Close();
            }

            HasPhysics = false;
            m_currentLod = -1;

            base.Close(); // Close the entity. Should close physics and all that good stuff as well.

            IsClosed = true;
        }

        public void SetLod(int lod)
        {
            if (Closed) return;

            Debug.Assert(!Engine.Platform.Game.IsDedicated);
            if (lod != m_currentLod || lod != m_lodToSwitch)
            {
                RecordHistory(lod, true);

                if (Interlocked.Exchange(ref m_hasParallelWorkPending, 1) == 0) // if not queued
                    Owner.ScheduleWork(this, true);
                m_lodToSwitch = lod;
                m_render.Lod = m_lodToSwitch;
            }
            else
            {
                RecordHistory(lod, false);
            }
        }

        public void EnablePhysics(bool physics)
        {
            if (Closed) return;

            var toggle = HasPhysics != physics;

            if (toggle != m_togglePhysics && toggle)
            {
                if (m_activeShape == null || m_recalculateShape)
                {
                    if (!HasParallelWorkPending)
                        Owner.ScheduleWork(this, true);

                    HasParallelWorkPending = true;
                }
                else
                {
                    if (Physics != null)
                        Physics.Enabled = physics;
                    toggle = false;
                    HasPhysics = physics;

                    if (!physics)
                    {
                        var handler = OnPhysicsClose;
                        if (handler != null)
                            handler();
                    }
                }
            }

            m_togglePhysics = toggle;
        }

        public bool IsLoaded
        {
            get { return true; }
        }

        public bool IsClosed { get; private set; }

        public int LodLevel
        {
            get { return m_currentLod; }
        }

        public bool HasPhysics { get; private set; }
        public bool IsPinned { get; internal set; }

        public bool IsPendingLodSwitch
        {
            get { return m_currentLod != m_lodToSwitch; }
        }
        public bool IsPendingPhysicsToggle { get { return m_togglePhysics; } }

        public void CancelParallel()
        {
            // TODO: Implement me! D:
        }

        public bool HasSerialWorkPending { get; private set; }

        public bool HasParallelWorkPending
        {
            get { return m_hasParallelWorkPending == 1; }
            private set { m_hasParallelWorkPending = value ? 1 : 0; }
        }

        public long SectorId { get; private set; }

        public void DoParallelWork()
        {
            m_hasParallelWorkPending = 0;

            if (Closed)
            {
                m_lodToSwitch = m_currentLod;
                m_togglePhysics = false;
                return;
            }

            bool work = false;

            if (m_lodToSwitch != m_currentLod)
            {
                work = true;

                if (m_lodToSwitch == -1)
                {
                    m_render.Close();
                }
                else
                {
                    FetchData(m_lodToSwitch);

                    BuildInstanceBuffers(m_lodToSwitch);
                }

                m_lodSwitchedFrom = m_currentLod;
            }

            if ((m_togglePhysics && !HasPhysics) || (HasPhysics && m_recalculateShape))
            {
                work = true;
                BuildShape();
            }

            HasSerialWorkPending = true;

            if (work) Owner.ScheduleWork(this, false);
        }

        public bool DoSerialWork()
        {
            if (Closed) return false;
            if (HasParallelWorkPending) return false;

            bool work = false;

            ProfilerShort.Begin("Update Modules");
            if (m_togglePhysics || m_lodSwitchedFrom != m_lodToSwitch)
            {
                foreach (var module in m_modules)
                {
                    ProfilerShort.Begin(module.Key.Name);
                    if (m_lodSwitchedFrom != m_lodToSwitch)
                        module.Value.Proxy.CommitLodChange(m_lodSwitchedFrom, m_lodToSwitch);
                    if (m_togglePhysics)
                        module.Value.Proxy.CommitPhysicsChange(!HasPhysics);
                    ProfilerShort.End();
                }
                work = true;

            }
            ProfilerShort.End();

            m_currentLod = m_lodToSwitch;

            ProfilerShort.Begin("Event Callbacks");
            if (m_lodSwitchedFrom != m_currentLod && m_lodToSwitch == m_currentLod)
                RaiseOnLodCommitEvent(m_currentLod);
            if (m_togglePhysics)
                RaiseOnPhysicsCommitEvent(HasPhysics);
            ProfilerShort.End();

            ProfilerShort.Begin("Update Renderer");
            if (m_render != null && m_render.HasChanges() && m_lodToSwitch == m_currentLod)
            {
                m_render.CommitChangesToRenderer();
                work = true;
                m_lodSwitchedFrom = m_currentLod;
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Prepare Rigid Body");
            if (m_togglePhysics)
            {
                if (HasPhysics)
                {
                    Physics.Enabled = false;
                    HasPhysics = false;
                    m_togglePhysics = false;
                }
                else if (m_newShape != null)
                {
                    PreparePhysicsBody();
                    work = true;
                    HasPhysics = true;
                    m_togglePhysics = false;
                }
            }
            if (m_recalculateShape)
            {
                m_recalculateShape = false;
                if (HasPhysics && m_newShape != null)
                {
                    PreparePhysicsBody();
                }
            }

            ProfilerShort.End();

            HasSerialWorkPending = false;

            return work;
        }

        public void OnItemChange(int index, short newModelIndex)
        {
            ProfilerShort.Begin("OnItemChange");
            if (m_currentLod == -1 && !HasPhysics)
            {
                ProfilerShort.End(); return;
            }

            foreach (var module in m_modules.Values)
            {
                ProfilerShort.Begin(module.Definition.SubtypeName + " Module");
                module.Proxy.OnItemChange(index, newModelIndex);
                ProfilerShort.End();
            }

            if (m_currentLod != -1)
            {
                UpdateItemModel(index, newModelIndex);

                m_render.CommitChangesToRenderer();
            }

            if (HasPhysics)
                UpdateItemShape(index, newModelIndex);
            else if (newModelIndex >= 0)
                m_recalculateShape = true;
            ProfilerShort.End();
        }

        public void OnItemsChange(int sector, List<int> indices, short newModelIndex)
        {
            if (m_currentLod == -1 && !HasPhysics) return;

            var offset = DataView.SectorOffsets[sector];
            int count = (sector < DataView.SectorOffsets.Count - 1 ? DataView.SectorOffsets[sector + 1] : DataView.Items.Count) - offset;

            for (int i = 0; i < indices.Count; ++i)
            {
                if (indices[i] >= count)
                {
                    indices.RemoveAtFast(i);
                    --i;
                }
            }

            foreach (var module in m_modules.Values)
            {
                module.Proxy.OnItemChangeBatch(indices, offset, newModelIndex);
            }


            if (m_currentLod != -1)
            {
                foreach (var item in indices)
                {
                    var it = item + offset;
                    UpdateItemModel(it, newModelIndex);
                }

                m_render.CommitChangesToRenderer();
            }

            if (HasPhysics)
                foreach (var item in indices)
                {
                    var it = item + offset;
                    UpdateItemShape(it, newModelIndex);
                }
            else if (newModelIndex > 0)
                m_recalculateShape = true;
        }

        public MyEnvironmentDataView DataView { get; private set; }
        #endregion

        #region Private

        private MyProceduralEnvironmentDefinition m_environment;

        private MyInstancedRenderSector m_render;

        private IMyEnvironmentOwner m_owner;

        private IMyEnvironmentDataProvider m_provider;

        private struct LodHEntry
        {
            public int Lod;
            public bool Set;
            public StackTrace Trace;

            public override string ToString()
            {
#if !XB1
                return String.Format("{0} {1} @ {2}", Set ? "Set" : "Requested", Lod, Trace.GetFrame(1));
#else // XB1
                System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
                return String.Format("{0} {1}", Set ? "Set" : "Requested", Lod);
#endif // XB1
            }
        }

        private MyConcurrentQueue<LodHEntry> m_lodHistory = new MyConcurrentQueue<LodHEntry>();

        [Conditional("DEBUG")]
        private void RecordHistory(int lod, bool set)
        {
            if (m_lodHistory.Count > 10)
                m_lodHistory.Dequeue();

            m_lodHistory.Enqueue(new LodHEntry
            {
                Lod = lod,
                Set = set,
                Trace = new StackTrace(),
            });
        }

        // Center of this sector (in world).
        private Vector3D m_sectorCenter;

        // Storage sector range.
        private BoundingBox2I m_dataRange;

        private unsafe void FetchData(int lodToSwitch)
        {
            ProfilerShort.Begin(string.Format("FetchData({0})", lodToSwitch));
            var oldData = DataView;

            if (oldData != null && oldData.Lod == lodToSwitch)
            {
                ProfilerShort.End();
                return;
            }

            DataView = m_provider.GetItemView(lodToSwitch, ref m_dataRange.Min, ref m_dataRange.Max, ref m_sectorCenter);

            DataView.Listener = this;

            if (oldData != null) oldData.Close();

            foreach (var module in m_modules.Values)
            {
                module.Proxy.Close();
            }

            m_modules.Clear();

            int totalItems = DataView.Items.Count;

            fixed (ItemInfo* items = DataView.Items.GetInternalArray())
                for (int i = 0; i < totalItems; ++i)
                {
                    if (items[i].DefinitionIndex == -1)
                        continue;

                    var def = m_environment.Items[items[i].DefinitionIndex];

                    var proxies = def.Type.ProxyModules;

                    if (proxies != null)
                    {
                        foreach (var proxy in proxies)
                        {
                            Module mod;
                            if (!m_modules.TryGetValue(proxy.Type, out mod))
                            {
                                mod = new Module((IMyEnvironmentModuleProxy)Activator.CreateInstance(proxy.Type));
                                mod.Definition = proxy.Definition;
                                m_modules[proxy.Type] = mod;
                            }

                            mod.Items.Add(i);
                        }
                    }
                }

            ProfilerShort.End();

            ProfilerShort.Begin("InitModuleProxies()");
            foreach (var module in m_modules)
            {
                ProfilerShort.Begin(module.Key.Name);
                module.Value.Proxy.Init(this, module.Value.Items);
                module.Value.Items = null;
                ProfilerShort.End();
            }
            ProfilerShort.End();
        }

        #endregion

        #endregion

        #region Physics

        public event Action OnPhysicsClose;

        private Dictionary<int, HkShape> m_modelsToShapes;

        private class CompoundInstancedShape
        {
            public HkStaticCompoundShape Shape = new HkStaticCompoundShape(HkReferencePolicy.TakeOwnership);
            private readonly Dictionary<int, int> m_itemToShapeInstance = new Dictionary<int, int>();
            private readonly Dictionary<int, int> m_shapeInstanceToItem = new Dictionary<int, int>();

            private bool m_baked;

            public void AddInstance(int itemId, ref ItemInfo item, HkShape shape)
            {
                if (!shape.IsZero)
                {
                    Matrix m;
                    Matrix.CreateFromQuaternion(ref item.Rotation, out m);
                    m.Translation = item.Position;

                    var instance = Shape.AddInstance(shape, m);
                    m_itemToShapeInstance[itemId] = instance;
                    m_shapeInstanceToItem[instance] = itemId;
                }
            }

            public void Bake()
            {
                Debug.Assert(!m_baked);
                Shape.Bake();
                m_baked = true;
            }

            public bool TryGetInstance(int itemId, out int shapeInstance)
            {
                return m_itemToShapeInstance.TryGetValue(itemId, out shapeInstance);
            }

            public bool TryGetItemId(int shapeInstance, out int itemId)
            {
                return m_shapeInstanceToItem.TryGetValue(shapeInstance, out itemId);
            }

            public int GetItemId(int shapeInstance)
            {
                return m_shapeInstanceToItem[shapeInstance];
            }
        }

        private CompoundInstancedShape m_activeShape;
        private CompoundInstancedShape m_newShape;

        private bool m_togglePhysics;

        private bool m_recalculateShape;

        private unsafe void BuildShape()
        {
            ProfilerShort.Begin("BuildPhysicsShape()");
            FetchData(0);

            ProfilerShort.Begin("CollectInstances");

            var shape = new CompoundInstancedShape();

            if (m_modelsToShapes == null)
                m_modelsToShapes = new Dictionary<int, HkShape>();

            int totalItems = DataView.Items.Count;

            fixed (ItemInfo* items = DataView.Items.GetInternalArray())
            {
                for (int i = 0; i < totalItems; ++i)
                {
                    var modelId = items[i].ModelIndex;
                    if (modelId < 0) continue;
                    if (Owner.GetModelForId(modelId) == null) continue;

                    HkShape modelShape;

                    if (!m_modelsToShapes.TryGetValue(modelId, out modelShape))
                    {
                        var modelData = MyModels.GetModelOnlyData(Owner.GetModelForId(modelId).Model);

                        var shapes = modelData.HavokCollisionShapes;
                        if (shapes != null)
                        {
                            if (shapes.Length == 0)
                                MyLog.Default.Warning("Model {0} has an empty list of shapes, something wrong with export?", modelData.AssetName);
                            else
                            {
                                if (shapes.Length > 1)
                                    MyLog.Default.Warning("Model {0} has multiple shapes, only the first will be used.", modelData.AssetName);

                                modelShape = shapes[0];
                            }
                        }
                        m_modelsToShapes[modelId] = modelShape;
                    }

                    shape.AddInstance(i, ref items[i], modelShape);
                }
            }

            ProfilerShort.BeginNextBlock("Bake()");
            shape.Bake();
            ProfilerShort.End();

            m_newShape = shape;

            ProfilerShort.End();
        }

        private void UpdateItemShape(int index, short newModelIndex)
        {
            int shapeId;
            if (m_activeShape != null && m_activeShape.TryGetInstance(index, out shapeId))
                m_activeShape.Shape.EnableInstance(shapeId, newModelIndex >= 0);
            else if (!m_recalculateShape)
            {
                m_recalculateShape = true;
                if (!HasParallelWorkPending)
                    Owner.ScheduleWork(this, true);
            }
        }

        private void PreparePhysicsBody()
        {
            m_activeShape = m_newShape;
            m_newShape = null;

            Debug.Assert(m_activeShape != null);

            if (Physics != null)
                Physics.Close();

            Physics = new MyPhysicsBody(this, RigidBodyFlag.RBF_STATIC);

            var physics = (MyPhysicsBody)Physics;

            HkMassProperties massProperties = new HkMassProperties();
            //MatrixD matrix = MatrixD.CreateTranslation();
            physics.CreateFromCollisionObject(m_activeShape.Shape, Vector3.Zero, PositionComp.WorldMatrix, massProperties);
            physics.ContactPointCallback += Physics_onContactPoint;
            physics.IsStaticForCluster = true;

            if (m_contactListeners != null && m_contactListeners.Count != 0)
                Physics.RigidBody.ContactPointCallbackEnabled = true;

            //shape.RemoveReference();

            Physics.Enabled = true;
        }

        private void Physics_onContactPoint(ref MyPhysics.MyContactPointEvent evt)
        {
            var bodyA = evt.ContactPointEvent.GetPhysicsBody(0);
            if (bodyA == null)
                return;

            int bodyIndex = bodyA.Entity == this ? 0 : 1;

            uint myShapekey = evt.ContactPointEvent.GetShapeKey(bodyIndex);

            if (myShapekey == uint.MaxValue) return;

            var bodyB = evt.ContactPointEvent.GetPhysicsBody(1 ^ bodyIndex);
            if (bodyB == null)
                return;

            var other = bodyB.Entity;

            int item = GetItemFromShapeKey(myShapekey);

            foreach (var callback in m_contactListeners)
            {
                callback.Invoke(item, (MyEntity)other, ref evt);
            }
        }

        #endregion

        #region Graphics

        private int m_lodSwitchedFrom = -1;
        private volatile int m_currentLod = -1;
        private volatile int m_lodToSwitch = -1;

        private List<short> m_modelToItem;

        public unsafe void BuildInstanceBuffers(int lod)
        {
            ProfilerShort.Begin("Build Instance Buffers");
            Dictionary<short, List<MyInstanceData>> instances = new Dictionary<short, List<MyInstanceData>>();

            m_modelToItem = new List<short>(DataView.Items.Count);

            Vector3D offset = SectorCenter - m_render.WorldMatrix.Translation;

            int maxItems = DataView.Items.Count;

            fixed (short* instanceIndex = m_modelToItem.GetInternalArray())
            fixed (ItemInfo* items = DataView.Items.GetInternalArray())
                for (int i = 0; i < maxItems; ++i)
                {
                    if (items[i].ModelIndex < 0) continue;

                    List<MyInstanceData> instanceData;

                    if (!instances.TryGetValue(items[i].ModelIndex, out instanceData))
                    {
                        instanceData = new List<MyInstanceData>();
                        instances[items[i].ModelIndex] = instanceData;
                    }

                    Matrix m;
                    Matrix.CreateFromQuaternion(ref items[i].Rotation, out m);
                    m.Translation = items[i].Position + offset;

                    Debug.Assert(instanceData.Count < short.MaxValue);

                    instanceIndex[i] = (short)instanceData.Count;
                    instanceData.Add(new MyInstanceData(m));
                }

            m_modelToItem.SetSize(maxItems);

            foreach (var modelData in instances)
            {
                var model = m_owner.GetModelForId(modelData.Key);
                if (model != null)
                {
                    int modelId = MyModel.GetId(model.Model);
                    m_render.AddInstances(modelId, modelData.Value);
                }
                else
                    System.Diagnostics.Debug.Fail("Model shouldnt be null");
            }
            ProfilerShort.End();
        }

        private void UpdateItemModel(int index, short newModelIndex)
        {
            ProfilerShort.Begin("Update Render Model");
            var item = DataView.Items[index];
            if (item.ModelIndex != newModelIndex)
            {
                if (m_currentLod == m_lodToSwitch)
                {
                    if (item.ModelIndex >= 0 && m_owner.GetModelForId(item.ModelIndex) != null)
                    {
                        int modelId = MyModel.GetId(m_owner.GetModelForId(item.ModelIndex).Model);
                        m_render.RemoveInstance(modelId, m_modelToItem[index]);
                        m_modelToItem[index] = -1;
                    }

                    if (newModelIndex >= 0 && m_owner.GetModelForId(newModelIndex) != null)
                    {
                        int modelId = MyModel.GetId(m_owner.GetModelForId(newModelIndex).Model);

                        Vector3D offset = SectorCenter - m_render.WorldMatrix.Translation;
                        Matrix m;
                        Matrix.CreateFromQuaternion(ref item.Rotation, out m);
                        m.Translation = item.Position + offset;

                        var data = new MyInstanceData(m);

                        m_modelToItem[index] = m_render.AddInstance(modelId, ref data);
                    }
                }

                item.ModelIndex = newModelIndex;
                DataView.Items[index] = item;
            }
            ProfilerShort.End();
        }

        public void GetItemInfo(int itemId, out uint renderObjectId, out int instanceIndex)
        {
            var item = DataView.Items[itemId];
            int modelId = MyModel.GetId(m_owner.GetModelForId(item.ModelIndex).Model);

            renderObjectId = m_render.GetRenderEntity(modelId);

            instanceIndex = m_modelToItem[itemId];
        }

        #endregion

        #region Module API

        #region Public

        public IMyEnvironmentOwner Owner { get { return m_owner; } }

        public void EnableItem(int itemId, bool enabled)
        {
            MyLogicalEnvironmentSectorBase logicalSector;
            int logicalItem;
            DataView.GetLogicalSector(itemId, out logicalItem, out logicalSector);

            logicalSector.EnableItem(logicalItem, enabled);
        }

        /**
         * Get the item that corresponds to a given shape key.
         */
        public int GetItemFromShapeKey(uint shapekey)
        {
            uint child;
            int instance;
            m_activeShape.Shape.DecomposeShapeKey(shapekey, out instance, out child);
            return m_activeShape.GetItemId(instance);
        }

        public event MySectorContactEvent OnContactPoint
        {
            add
            {
                if (m_contactListeners == null) m_contactListeners = new HashSet<MySectorContactEvent>();
                if (m_contactListeners.Count == 0 && Physics != null && Physics.RigidBody != null)
                    Physics.RigidBody.ContactPointCallbackEnabled = true;
                m_contactListeners.Add(value);
            }
            remove
            {
                if (m_contactListeners != null)
                {
                    m_contactListeners.Remove(value);

                    if (m_contactListeners.Count == 0 && Physics != null && Physics.RigidBody != null)
                        Physics.RigidBody.ContactPointCallbackEnabled = false;
                }
            }
        }

        public T GetModuleForDefinition<T>(MyRuntimeEnvironmentItemInfo itemEnvDefinition) where T : class, IMyEnvironmentModuleProxy
        {
            var proxyTypes = itemEnvDefinition.Type.ProxyModules;
            if (proxyTypes == null || !proxyTypes.Any(x => typeof(T).IsAssignableFrom(x.Type))) return null;

            Module mod;
            m_modules.TryGetValue(typeof(T), out mod);

            return (T)(mod != null ? mod.Proxy : null);
        }

        public T GetModule<T>() where T : class, IMyEnvironmentModuleProxy
        {
            Module mod;
            m_modules.TryGetValue(typeof(T), out mod);

            return (T)(mod != null ? mod.Proxy : null);
        }

        public IMyEnvironmentModuleProxy GetModule(Type moduleType)
        {
            Module mod;
            m_modules.TryGetValue(moduleType, out mod);

            return mod != null ? mod.Proxy : null;

        }

        public void RaiseItemEvent<TModule>(TModule module, int item, bool fromClient = false) where TModule : IMyEnvironmentModuleProxy
        {
            RaiseItemEvent<TModule, object>(module, item, null, fromClient);
        }

        public void RaiseItemEvent<TModule, TArgument>(TModule module, int item, TArgument eventData, bool fromClient = false) where TModule : IMyEnvironmentModuleProxy
        {
            Debug.Assert(m_modules[typeof(TModule)].Proxy == (IMyEnvironmentModuleProxy)module);

            var modDef = m_modules[typeof(TModule)].Definition;

            int logicalItem;
            MyLogicalEnvironmentSectorBase sector;
            DataView.GetLogicalSector(item, out logicalItem, out sector);

            sector.RaiseItemEvent(logicalItem, ref modDef, eventData, fromClient);
        }

        #endregion

        #region Private

        private class Module
        {
            public readonly IMyEnvironmentModuleProxy Proxy;
            public List<int> Items = new List<int>();

            public MyDefinitionId Definition;

            public Module(IMyEnvironmentModuleProxy proxy)
            {
                Proxy = proxy;
            }
        }

        private readonly Dictionary<Type, Module> m_modules = new Dictionary<Type, Module>();

        private bool m_modulesPendingUpdate = false;

        private HashSet<MySectorContactEvent> m_contactListeners;
        private int m_hasParallelWorkPending; // Has to be int because interlocked does not work on bools.

        #endregion

        #endregion

        public new void DebugDraw()
        {
            if (LodLevel < 0 && !HasPhysics) return;

            Color color = Color.Red;
            if (MyPlanetEnvironmentSessionComponent.ActiveSector == this)
            {
                color = Color.LimeGreen;

                if (DataView != null)
                {
                    if (MyPlanetEnvironmentSessionComponent.DebugDrawActiveSectorItems)
                        for (int index = 0; index < DataView.Items.Count; index++)
                        {
                            var item = DataView.Items[index];
                            var pos = item.Position + SectorCenter;
                            MyRuntimeEnvironmentItemInfo def;
                            Owner.GetDefinition((ushort)item.DefinitionIndex, out def);

                            MyRenderProxy.DebugDrawText3D(pos, string.Format("{0} i{1} m{2} d{3}", def.Type.Name, index, item.ModelIndex, item.DefinitionIndex), color, 0.7f, true);
                        }

                    if (MyPlanetEnvironmentSessionComponent.DebugDrawActiveSectorProvider)
                    {
                        foreach (var log in DataView.LogicalSectors)
                        {
                            log.DebugDraw(DataView.Lod);
                        }
                    }
                }
            }
            else if (HasPhysics && LodLevel == -1)
            {
                color = Color.RoyalBlue;
            }

            var center = (Bounds[4] + Bounds[7]) / 2;

            if (MyPlanetEnvironmentSessionComponent.ActiveSector == this
                || Vector3D.DistanceSquared(center, MySector.MainCamera.Position) < MyPlanetEnvironmentSessionComponent.DebugDrawDistance * MyPlanetEnvironmentSessionComponent.DebugDrawDistance)
            {
                var label = ToString();
                MyRenderProxy.DebugDrawText3D(center, label, color, 1, true);
            }

            MyRenderProxy.DebugDraw6FaceConvex(Bounds, color, 1, true, false);
        }

        public override string ToString()
        {
            long id = SectorId;

            int x = (int)(id & 0xFFFFFF);
            id >>= 24;
            int y = (int)(id & 0xFFFFFF);
            id >>= 24;
            int face = (int)(id & 0x7);
            id >>= 3;
            int lod = (int)(id & 0xFF);

            return string.Format("S(x{0} y{1} f{2} l{3}({4}) c{6} {5})", x, y, face, lod, LodLevel, HasPhysics ? " p" : "", DataView != null ? DataView.Items.Count : 0);
        }

        public override int GetHashCode()
        {
            return SectorId.GetHashCode();
        }

        public event Action<MyEnvironmentSector, int> OnLodCommit;

        public void RaiseOnLodCommitEvent(int lod)
        {
            if (OnLodCommit != null)
                OnLodCommit.Invoke(this, lod);
        }

        public event Action<MyEnvironmentSector, bool> OnPhysicsCommit;

        public void RaiseOnPhysicsCommitEvent(bool enabled)
        {
            if (OnPhysicsCommit != null)
                OnPhysicsCommit(this, enabled);
        }
    }
}
