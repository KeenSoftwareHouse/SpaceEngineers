using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ParallelTasks;
using Sandbox.Game.WorldEnvironment.Definitions;
using VRage;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRage.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.Game;
using VRage.Network;
using VRage.Profiler;
using VRage.Serialization;
using VRageRender;

namespace Sandbox.Game.WorldEnvironment
{
    public class MyProceduralLogicalSector : MyLogicalEnvironmentSectorBase
    {
        #region Private

        private Dictionary<Type, MyObjectBuilder_EnvironmentModuleBase> m_moduleData = new Dictionary<Type, MyObjectBuilder_EnvironmentModuleBase>();

        // When we are scanning we do not have a sector yet so process items should not fire model update events.
        private bool m_scanning;

        // Weather this sector is kept by replication.
        private bool m_serverOwned;

        #endregion

        #region Public Members

        internal int X, Y, Lod;

        internal int[] ItemCountForLod = new int[MyEnvironmentSectorConstants.MaximumLod + 1];

        internal MyProceduralEnvironmentProvider Provider;

        internal HashSet<MyProceduralDataView> Viewers = new HashSet<MyProceduralDataView>();

        internal Vector3 BasisX, BasisY;

        internal bool Replicable = false;

        #endregion

        public MyProceduralLogicalSector(MyProceduralEnvironmentProvider provider, int x, int y, int localLod, MyObjectBuilder_ProceduralEnvironmentSector moduleData)
        {
            Provider = provider;

            Owner = provider.Owner;

            X = x;
            Y = y;
            Lod = localLod;

            provider.GeSectorWorldParameters(x, y, localLod * provider.LodFactor, out WorldPos, out BasisX, out BasisY);

            m_environment = (MyProceduralEnvironmentDefinition)provider.Owner.EnvironmentDefinition;

            m_seed = provider.GetSeed() ^ ((x * 377 + y) * 377 + Lod);
            m_itemPositionRng = new MyRandom(m_seed);

            // Area of the scanning surface:
            // We know that the norm of the cross product of two vectors is the are of the parallelogram delimited by them.
            // Since the basis surface is the union of the parallelograms delimited by basis and it's sign permutations
            // we have that the are is 4 times the norm of the cross product.

            double area = Vector3.Cross(BasisX, BasisY).Length() * 4;

            m_itemCountTotal = (int)(area * m_environment.ItemDensity);

            //if (localLod != 0) m_itemCountTotal = m_itemCountTotal;

            m_scanHelper = new ProgressiveScanHelper(m_itemCountTotal, localLod * provider.LodFactor);

            Bounds = Owner.GetBoundingShape(ref WorldPos, ref BasisX, ref BasisY);

            m_items = new List<ItemInfo>();

            m_totalSpawned = 0;

            UpdateModuleBuilders(moduleData);
        }

        private void UpdateModuleBuilders(MyObjectBuilder_ProceduralEnvironmentSector moduleData)
        {
            m_moduleData.Clear();

            if (moduleData == null) return;

            for (int i = 0; i < moduleData.SavedModules.Length; i++)
            {
                var mod = moduleData.SavedModules[i];

                var def = MyDefinitionManager.Static.GetDefinition<MyProceduralEnvironmentModuleDefinition>(mod.ModuleId);

                if (def != null)
                {
                    m_moduleData.Add(def.ModuleType, mod.Builder);

                    ModuleData module;
                    if (m_modules.TryGetValue(def.ModuleType, out module))
                    {
                        module.Module.Init(this, mod.Builder);
                    }
                }
            }
        }

        #region MyLogicalEnvironmentSectorBase

        public override unsafe void EnableItem(int itemId, bool enabled)
        {
            MyRuntimeEnvironmentItemInfo def;

            fixed (ItemInfo* items = m_items.GetInternalArray())
            {
                ItemInfo* it = items + itemId;

                if (it->DefinitionIndex == -1) return;

                GetItemDefinition((ushort)it->DefinitionIndex, out def);
            }
            var mod = GetModuleForDefinition(def);
            if (mod != null) mod.OnItemEnable(itemId, enabled);
        }

        public override unsafe void UpdateItemModel(int itemId, short modelId)
        {
            if (!m_scanning)
            {
                ProfilerShort.Begin("Update Viewer");
                foreach (var view in Viewers)
                    if (view.Listener != null)
                    {
                        var sector = view.GetSectorIndex(X, Y);

                        var offset = view.SectorOffsets[sector];

                        if (itemId < ItemCountForLod[view.Lod])
                            view.Listener.OnItemChange(itemId + offset, modelId);
                    }
                ProfilerShort.End();
            }

            fixed (ItemInfo* items = m_items.GetInternalArray())
                items[itemId].ModelIndex = modelId;
        }

        public override unsafe void UpdateItemModelBatch(List<int> itemIds, short newModelId)
        {
            int count = itemIds.Count;

            if (!m_scanning)
            {
                ProfilerShort.Begin("Update Viewer");
                foreach (var view in Viewers)
                    if (view.Listener != null)
                    {
                        var sector = view.GetSectorIndex(X, Y);

                        view.Listener.OnItemsChange(sector, itemIds, newModelId);
                    }
                ProfilerShort.End();
            }

            ProfilerShort.Begin("Update storage");
            fixed (ItemInfo* items = m_items.GetInternalArray())
            fixed (int* ids = itemIds.GetInternalArray())
                for (int i = 0; i < count; ++i)
                    items[ids[i]].ModelIndex = newModelId;
            ProfilerShort.End();
        }

        public override List<ItemInfo> Items { get { return m_items; } }

        public override T GetModule<T>()
        {
            if (m_modules.ContainsKey(typeof(T)))
                return (T)m_modules[typeof(T)].Module;
            else
                return default(T);
        }

        public override IMyEnvironmentModule GetModuleForDefinition(MyRuntimeEnvironmentItemInfo def)
        {
            if (def.Type.StorageModule.Type == null) return null;

            ModuleData modData;
            if (m_modules.TryGetValue(def.Type.StorageModule.Type, out modData)) return modData.Module;

            return null;
        }

        #endregion

        #region Private

        private readonly List<ItemInfo> m_items;

        private readonly int m_itemCountTotal;

        private readonly MyProceduralEnvironmentDefinition m_environment;

        public int MinimumScannedLod = MyEnvironmentSectorConstants.MaximumLod + 1;
        private int m_totalSpawned = 0;

        private class ModuleData
        {
            public readonly Dictionary<short, MyLodEnvironmentItemSet> ItemsPerDefinition = new Dictionary<short, MyLodEnvironmentItemSet>();
            public readonly IMyEnvironmentModule Module;

            public readonly MyDefinitionId Definition;

            public ModuleData(Type type, MyDefinitionId definition)
            {
                Module = (IMyEnvironmentModule)Activator.CreateInstance(type);

                Definition = definition;
            }
        }

        private readonly Dictionary<Type, ModuleData> m_modules = new Dictionary<Type, ModuleData>();

        #endregion

        #region Sampling

        private int m_seed;

        private readonly MyRandom m_itemPositionRng;

        private Vector3 ComputeRandomItemPosition()
        {
            return BasisX * m_itemPositionRng.NextFloat(-1, 1) + BasisY * m_itemPositionRng.NextFloat(-1, 1);
        }

        public Vector3 GetRandomPerpendicularVector(ref Vector3 axis, int seed)
        {
            Vector3 tangent = Vector3.CalculatePerpendicularVector(axis);
            Vector3 bitangent; Vector3.Cross(ref axis, ref tangent, out bitangent);
            double angle = MyHashRandomUtils.UniformFloatFromSeed(seed) * 2 * MathHelper.Pi;
            return (float)Math.Cos(angle) * tangent + (float)Math.Sin(angle) * bitangent;
        }

        private ModuleData GetModule(MyRuntimeEnvironmentItemInfo info)
        {
            var type = info.Type.StorageModule.Type;
            if (type == null) return null;

            ModuleData moduled;
            if (!m_modules.TryGetValue(type, out moduled))
            {
                moduled = new ModuleData(type, info.Type.StorageModule.Definition);
                if (m_moduleData != null && m_moduleData.ContainsKey(type))
                    moduled.Module.Init(this, m_moduleData[type]);
                else
                    moduled.Module.Init(this, null);

                m_modules[type] = moduled;
            }

            return moduled;
        }

        private List<MyDiscreteSampler<MyRuntimeEnvironmentItemInfo>> m_candidates = new List<MyDiscreteSampler<MyRuntimeEnvironmentItemInfo>>();

        private MyRuntimeEnvironmentItemInfo GetItemForPosition(ref MySurfaceParams surface, int lod)
        {
            var key = new MyBiomeMaterial(surface.Biome, surface.Material);

            m_candidates.Clear();

            List<MyEnvironmentItemMapping> ruleset;
            if (m_environment.MaterialEnvironmentMappings.TryGetValue(key, out ruleset))
            {
                foreach (var rule in ruleset)
                {
                    var sampler = rule.Sampler(lod);
                    if (sampler != null && rule.Rule.Check(surface.HeightRatio, surface.Latitude, surface.Longitude, surface.Normal.Z))
                    {
                        m_candidates.Add(sampler);
                    }
                }
            }

            var seed = surface.Position.GetHashCode();

            float sample = MyHashRandomUtils.UniformFloatFromSeed(seed);

            switch (m_candidates.Count)
            {
                case 0:
                    return null;
                case 1:
                    return m_candidates[0].Sample(sample);

                default:
                    return m_candidates[(int) (MyHashRandomUtils.UniformFloatFromSeed(~seed) * m_candidates.Count)].Sample(sample);
            }
        }

        public unsafe void ScanItems(int targetLod)
        {
            int lodDiff = MinimumScannedLod - targetLod;

            if (lodDiff < 1) return;

            int startingLod = MinimumScannedLod - 1;

            int totalItems = m_itemCountTotal;
            int[] localLodOffsets = new int[lodDiff];
            for (int lod = startingLod; lod >= targetLod; --lod)
            {
                int count = m_scanHelper.GetItemsForLod(lod);
                localLodOffsets[lod - targetLod] = totalItems; // Using for cumulative sum
                totalItems += count;
            }

            List<MySurfaceParams> surfaceParams = new List<MySurfaceParams>(totalItems);
            // Step 1: Generate Points
            List<Vector3> points = new List<Vector3>(totalItems);

            /*if (m_environment.ScanningMethod == MyProceduralScanningMethod.Grid)
            {
                // compute non-centric basis for the grid method.
                Vector3 start = -BasisX - BasisY;

                // has to be per lod otherwise we will have gaps.
                for (int lod = startingLod; lod >= targetLod; --lod)
                {
                    int relativeLod = lod - targetLod;
                    int count = lod > targetLod ? localLodOffsets[relativeLod - 1] - localLodOffsets[relativeLod] : totalItems - localLodOffsets[relativeLod];
                    float root = (float)Math.Sqrt(count);

                    int width = (int)Math.Ceiling(root);
                    int height = (int)Math.Floor(root);

                    float hrecip = .5f / height;
                    float wrecip = .5f / width;

                    Vector3 newBasisX = 2 * BasisX + wrecip;
                    Vector3 newBasisY = 2 * BasisY + hrecip;

                    for (int x = 0; x < width; ++x)
                        for (int y = 0; y < height; ++y)
                        {
                            Vector3 pos = start + newBasisX * ((float)x / width) + newBasisY * ((float)y / height);
                            pos += newBasisX * m_itemPositionRng.NextFloat(-wrecip, wrecip) + newBasisY * m_itemPositionRng.NextFloat(-hrecip, hrecip);
                            points.Add(pos);
                        }
                }
            }
            else*/
            {
                // Random is default
                for (int i = 0; i < m_itemCountTotal; ++i)
                    points.Add(ComputeRandomItemPosition());
            }

            BoundingBoxD box = BoundingBoxD.CreateFromPoints(Bounds);

            // Step 2: Query surface
            Provider.Owner.QuerySurfaceParameters(WorldPos, ref box, points, surfaceParams);

            // Step 3: Select items to spawn
            Items.Capacity = Items.Count + points.Count;

            int locationOffset = 0;
            int itemOffset = 0;

            fixed (ItemInfo* items = Items.GetInternalArray())
            fixed (MySurfaceParams* parms = surfaceParams.GetInternalArray())
                for (int lod = startingLod; lod >= targetLod; --lod)
                {
                    int relativeLod = lod - targetLod;

                    // Count of items in this lod
                    int count = lod > targetLod ? localLodOffsets[relativeLod - 1] - localLodOffsets[relativeLod] : totalItems - localLodOffsets[relativeLod];

                    // Scan those items.
                    for (int i = 0; i < count; ++i)
                    {
                        //Debug.Assert(itemOffset < points.Count);

                        var definition = GetItemForPosition(ref parms[itemOffset], lod);
                        if (definition != null)
                        {
                            Vector3 up = -parms[itemOffset].Gravity;

                            items[m_totalSpawned].Position = parms[itemOffset].Position;
                            items[m_totalSpawned].ModelIndex = -1;
                            items[m_totalSpawned].Rotation = Quaternion.CreateFromForwardUp(GetRandomPerpendicularVector(ref up, parms[itemOffset].Position.GetHashCode()), up);
                            parms[locationOffset++] = parms[itemOffset];

                            items[m_totalSpawned].DefinitionIndex = definition.Index;

                            var moduled = GetModule(definition);

                            if (moduled != null)
                            {
                                MyLodEnvironmentItemSet set;

                                if (!moduled.ItemsPerDefinition.TryGetValue(definition.Index, out set))
                                {
                                    moduled.ItemsPerDefinition[definition.Index] = set = new MyLodEnvironmentItemSet() { Items = new List<int>() };
                                }

                                set.Items.Add(m_totalSpawned);
                            }

                            m_totalSpawned++;
                            Debug.Assert(m_totalSpawned <= m_itemCountTotal);
                        }
                        itemOffset++;
                    }

                    ItemCountForLod[lod] = m_totalSpawned;

                    foreach (var module in m_modules.Values)
                    {
                        if (module == null) continue;
                        //if (module.Module.MaximumLodIndex >= relativeLod)
                        foreach (var def in module.ItemsPerDefinition.Keys.ToArray())
                        {
                            //Debug.Assert(lod <= 15 && lod >= 0);
                            var set = module.ItemsPerDefinition[def];
                            //set.LodOffsets[lod] = set.Items.Count;
                            module.ItemsPerDefinition[def] = set;
                        }
                    }
                }

            Items.SetSize(m_totalSpawned);


            // Update modules
            m_scanning = true;
            foreach (var moduleData in m_modules.Values)
            {
                if (moduleData == null) continue;

                moduleData.Module.ProcessItems(moduleData.ItemsPerDefinition, surfaceParams, new[] { 0, 0, 0, 0 }, startingLod, targetLod);
            }
            m_scanning = false;

            MinimumScannedLod = targetLod;
        }

        public override void Init(MyObjectBuilder_EnvironmentSector sectorBuilder)
        {
            UpdateModuleBuilders((MyObjectBuilder_ProceduralEnvironmentSector)sectorBuilder);
        }

        public override MyObjectBuilder_EnvironmentSector GetObjectBuilder()
        {
            List<MyObjectBuilder_ProceduralEnvironmentSector.Module> modulesToSave = new List<MyObjectBuilder_ProceduralEnvironmentSector.Module>(m_modules.Count);
            foreach (var module in m_modules.Values)
            {
                var builder = module.Module.GetObjectBuilder();
                if (builder != null)
                    modulesToSave.Add(new MyObjectBuilder_ProceduralEnvironmentSector.Module()
                    {
                        ModuleId = module.Definition,
                        Builder = builder
                    });
            }

            if (modulesToSave.Count > 0)
            {
                modulesToSave.Capacity = modulesToSave.Count;
                var sector = new MyObjectBuilder_ProceduralEnvironmentSector
                {
                    SavedModules = modulesToSave.GetInternalArray(),
                    SectorId = Id
                };

                return sector;
            }

            return null;
        }

        private class ProgressiveScanHelper
        {
            private int m_itemsTotal;

            private int m_offset;

            private const bool EXAGERATE = true;

            public ProgressiveScanHelper(int finalCount, int offset)
            {
                m_itemsTotal = finalCount;

                int maxLod = 4;
                int lastLodRateRecip = 10;

                m_logMaxLodRecip = 1 / Math.Log(maxLod);

                m_base = Math.Log(10) * m_logMaxLodRecip;

                m_offset = offset;
            }

            private double m_base;
            private double m_logMaxLodRecip;

            private double F(double x)
            {
                return -Math.Pow(m_base, -x) * m_logMaxLodRecip;
            }

            public int GetItemsForLod(int lod)
            {
                lod += m_offset;

                if (EXAGERATE)
                {
                    return (int)(m_itemsTotal * (F(lod + 1) - F(lod)));
                }
                else
                {
                    switch (lod)
                    {
                        case 1:
                            return (int)(m_itemsTotal * .5f);
                        case 3:
                            return (int)(m_itemsTotal * .25f);
                        case 6:
                            return (int)(m_itemsTotal * .25f);
                        default:
                            return 0;
                    }
                }
            }
        }

        private ProgressiveScanHelper m_scanHelper;

        #endregion

        public override void GetItemDefinition(ushort key, out MyRuntimeEnvironmentItemInfo it)
        {
            it = m_environment.Items[key];
        }

        public override void Close()
        {
            foreach (var module in m_modules.Values)
            {
                module.Module.Close();
            }

            m_modules.Clear();
            m_items.Clear();

            base.Close();
        }

        public override void DebugDraw(int lod)
        {
            var offset = WorldPos + MySector.MainCamera.UpVector * 1;

            for (int index = 0; index < m_items.Count; index++)
            {
                var item = m_items[index];
                var pos = item.Position + offset;
                MyRuntimeEnvironmentItemInfo def;
                Owner.GetDefinition((ushort)item.DefinitionIndex, out def);

                MyRenderProxy.DebugDrawText3D(pos, string.Format("{0} i{1} m{2} d{3}", def.Type.Name, index, item.ModelIndex, item.DefinitionIndex), Color.Purple, 0.7f, true);
            }

            foreach (var module in m_modules.Values)
            {
                module.Module.DebugDraw();
            }
        }

        #region Multiplayer

        public override bool ServerOwned
        {
            get { return m_serverOwned; }

            internal set
            {
                m_serverOwned = value;

                if (!Sync.IsServer && !value && Viewers.Count == 0)
                {
                    Provider.CloseSector(this);
                }
            }
        }

        /// <summary>
        /// Raise event from a storage module.
        /// 
        /// Can be either a client event to server (fromClient = true)
        /// or a broadcast of a server event to all clients with this logical sector (fromClient = false). 
        /// </summary>
        /// <typeparam name="TModule">Type of the storage module to notify</typeparam>
        /// <param name="logicalItem">Logical item Id</param>
        /// <param name="eventData">Data to send along with the event.</param>
        /// <param name="fromClient">Weather this event comes from client to server or server to all clients.</param>
        public void RaiseItemEvent<TModule>(int logicalItem, object eventData, bool fromClient = false) where TModule : IMyEnvironmentModule
        {
            var modDef = m_modules[typeof(TModule)].Definition;

            RaiseItemEvent(logicalItem, ref modDef, eventData, fromClient);
        }

        // Override of parent class event dispatcher
        public override void RaiseItemEvent<T>(int logicalItem, ref MyDefinitionId modDef, T eventData, bool fromClient)
        {
            // Client must raise event to server always
            Debug.Assert(Sync.IsServer != fromClient);

            if (fromClient)
                MyMultiplayer.RaiseEvent(this, x => x.HandleItemEventClient, logicalItem, (SerializableDefinitionId)modDef, (object)eventData);
            else
                MyMultiplayer.RaiseEvent(this, x => x.HandleItemEventServer, logicalItem, (SerializableDefinitionId)modDef, (object)eventData);
        }

        // From server to clients
        [Broadcast, Event, Reliable]
        public void HandleItemEventServer(int logicalItem, SerializableDefinitionId def,
            [Serialize(MyObjectFlags.Dynamic | MyObjectFlags.Nullable, DynamicSerializerType = typeof(MyDynamicObjectResolver))] object data)
        {
            HandleItemEvent(logicalItem, def, data, false);
        }

        // From a client to server
        [Event, Reliable, Server]
        public void HandleItemEventClient(int logicalItem, SerializableDefinitionId def,
            [Serialize(MyObjectFlags.Dynamic | MyObjectFlags.Nullable, DynamicSerializerType = typeof(MyDynamicObjectResolver))] object data)
        {
            HandleItemEvent(logicalItem, def, data, true);
        }

        /**
         * Handler for multiplayer events.
         * 
         * Depending on the provided module definition the event may be coming from server to clients or from a clientto the server.
         */
        public void HandleItemEvent(int logicalItem, SerializableDefinitionId def, object data, bool fromClient)
        {
            if (typeof(MyObjectBuilder_ProceduralEnvironmentModuleDefinition).IsAssignableFrom(def.TypeId))
            {
                // If our object builder is for a storage module we must deliver the event to the related module in this end.

                var modDef = MyDefinitionManager.Static.GetDefinition<MyProceduralEnvironmentModuleDefinition>(def);

                if (modDef == null)
                {
                    MyLog.Default.Error("Received message about unknown logical module {0}", def);
                    return;
                }

                ModuleData mod;
                if (m_modules.TryGetValue(modDef.ModuleType, out mod))
                {
                    mod.Module.HandleSyncEvent(logicalItem, data, fromClient);
                }
            }
            else
            {
                // otherwise we must notify all proxy modules in our views if they contain the same item.

                var modDef = MyDefinitionManager.Static.GetDefinition<MyEnvironmentModuleProxyDefinition>(def);

                if (modDef == null)
                {
                    MyLog.Default.Error("Received message about unknown module proxy {0}", def);
                    return;
                }

                foreach (var view in Viewers)
                {
                    if (view.Listener != null)
                    {
                        var mod = view.Listener.GetModule(modDef.ModuleType);

                        // Compute view offset
                        var sector = view.GetSectorIndex(X, Y);

                        var offset = view.SectorOffsets[sector];

                        if (logicalItem < ItemCountForLod[view.Lod])
                        {
                            if (mod != null)
                            {
                                mod.HandleSyncEvent(logicalItem + offset, data, fromClient);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        public void UpdateMinLod()
        {
            MinLod = int.MaxValue;

            foreach (var view in Viewers)
            {
                MinLod = Math.Min(view.Lod, MinLod);
            }

            var rep = MinLod <= Provider.SyncLod;

            if (rep != Replicable)
            {
                if (rep)
                    Provider.MarkReplicable(this);
                else
                    Provider.UnmarkReplicable(this);

                Replicable = rep;
            }
        }

        public override string ToString()
        {
            return string.Format("x{0} y{1} l{2} : {3}", X, Y, Lod, Items.Count);
        }

        public override string DebugData
        {
            get { return string.Format("x:{0} y:{1} highLod:{2} localLod:{3} seed:{4:X} count:{5} ", X, Y, Lod, MinimumScannedLod, m_seed, Items.Count); }
        }
    }
}