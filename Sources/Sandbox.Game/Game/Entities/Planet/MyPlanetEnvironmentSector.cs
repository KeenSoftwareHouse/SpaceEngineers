using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.AI;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.World;
using VRage;
using VRage.Collections;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRage.Network;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;
using Sandbox.Game.Multiplayer;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Entities
{
    /**
     * Identifier of a sector;
     */
    public struct MyPlanetSectorId
    {
        public Vector3S Position;
        public Vector3B Direction;

        // Invalid ID.
        public static MyPlanetSectorId Invalid = new MyPlanetSectorId();

        public override int GetHashCode()
        {
            return (Position.GetHashCode() * 773) ^ Direction.GetHashCode();
        }

        public override string ToString()
        {
            Vector3I id = Position - Direction;
            return String.Format("({0}, {1}, {2})", id.X, id.Y, id.Z);
        }

        public static bool operator ==(MyPlanetSectorId a, MyPlanetSectorId b)
        {
            return a.Position == b.Position && a.Direction == b.Direction;
        }

        public static bool operator !=(MyPlanetSectorId a, MyPlanetSectorId b)
        {
            return a.Position != b.Position || a.Direction != b.Direction;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MyPlanetSectorId)) return false;
            return this == (MyPlanetSectorId)obj;
        }

        public string ToSerializedString()
        {
            return String.Format("{0};{1};{2}:{3};{4};{5}", Position.X, Position.Y, Position.Z, Direction.X, Direction.Y, Direction.Z);
        }

        public static MyPlanetSectorId FromSerializedString(string value)
        {
            var vecs = value.Split(':');

            if (vecs.Length != 2)
            {
                Debug.Fail("Bad sector ID");
                return Invalid;
            }

            MyPlanetSectorId id;
            Vector3I val;

            if (!Vector3I.TryParseFromString(vecs[0], out val))
            {
                return Invalid;
            }
            id.Position = new Vector3S(val);

            if (!Vector3I.TryParseFromString(vecs[1], out val))
            {
                return Invalid;
            }
            id.Direction = new Vector3B(val);

            return id;
        }

        public static void Unpack64(long value, out MyPlanetSectorId id)
        {
            id.Position.X = (short)(value & 0xFFFF);
            id.Position.Y = (short)((value >> 16) & 0xFFFF);
            id.Position.Z = (short)((value >> 32) & 0xFFFF);

            value >>= 48;

            id.Direction = new Vector3B(Base6Directions.GetIntVector((Base6Directions.Direction)value));
        }

        public long Pack64()
        {
            long value = Position.X
                         | ((long)Position.Y << 16)
                         | ((long)Position.Z << 32);

            Vector3I dirv = Direction;
            var dir = (long)Base6Directions.GetDirection(ref dirv);

            return value | (dir << 48);
        }
    }

    [Flags]
    public enum MyPlanetSectorOperation
    {
        Spawn = 1 << 0, // Prepare placement
        SpawnGraphics = 1 << 1, // Spawn lod1 graphics
        SpawnDetails = 1 << 2, // Spawn lod0 graphics
        AddEntities = 1 << 3, // Add entities for sectors
        SpawnPhysics = 1 << 4, // Spawn Physics for all items
        CloseGraphics = 1 << 5, // Close all graphics
        CloseDetails = 1 << 6, // Close only lod0 graphics
        ClosePhysics = 1 << 7, // Close only the physics for items in the sector.
        Close = 1 << 8, // Close and cleanup the sector
        Reset = 1 << 9 // Forces the sector to stop all current activity and reset.
    }

    public static class MyPlanetSectorOperationExtensions
    {
        public static bool HasFlags(this MyPlanetSectorOperation self, MyPlanetSectorOperation operation)
        {
            return operation == (self & operation);
        }
    }

    /**
     * Planet environment sectors controll environmnetal features in planets such as trees, bushes, small voxel features and AI.
     */
    public class MyPlanetEnvironmentSector : MyEntity, IMyEventProxy
    {
        // Operations that cannot be done in parallel.
        internal const MyPlanetSectorOperation SERIAL_OPERATIONS_MASK = MyPlanetSectorOperation.SpawnPhysics | MyPlanetSectorOperation.SpawnGraphics | MyPlanetSectorOperation.ClosePhysics | MyPlanetSectorOperation.Close | MyPlanetSectorOperation.CloseGraphics | MyPlanetSectorOperation.CloseDetails | MyPlanetSectorOperation.AddEntities | MyPlanetSectorOperation.Reset;

        #region Public
        /**
         * Operations that are pending on this sector.
         */
#if DEBUG
        private const int MAX_HISTORY = 10;
        private MyConcurrentQueue<MyPlanetSectorOperation> m_opHistory = new MyConcurrentQueue<MyPlanetSectorOperation>();
        private MyConcurrentQueue<Work> m_workHistory = new MyConcurrentQueue<Work>();

        internal struct Work
        {
            public MyPlanetSectorOperation Op;
            public bool Parallel;

            public override string ToString()
            {
                return String.Format("{0}({1})", Parallel ? "Parallel" : "Serial", Op);
            }
        }

        private void RememberWork(bool parallel, MyPlanetSectorOperation op)
        {
            m_planet.SectorOperations.Hit();

            if (m_workHistory.Count == MAX_HISTORY) m_workHistory.Dequeue();
            m_workHistory.Enqueue(new Work
            {
                Parallel = parallel,
                Op = op
            });
        }

        private MyPlanetSectorOperation m_opsPending;
        public MyPlanetSectorOperation PendingOperations
        {
            get { return m_opsPending; }
            set
            {
                m_opsPending = value;

                if (m_opHistory.Count == MAX_HISTORY << 1) m_opHistory.Dequeue();
                m_opHistory.Enqueue(m_opsPending);
            }
        }
#else
        public MyPlanetSectorOperation PendingOperations;

        [Conditional("DEBUG")]
        private void RememberWork(bool b, MyPlanetSectorOperation op) {
        }
#endif

        public bool ParallelPending
        {
            get { return (PendingOperations & ~SERIAL_OPERATIONS_MASK) != 0; }
        }


        public bool SerialPending
        {
            get { return (PendingOperations & SERIAL_OPERATIONS_MASK) != 0; }
        }

        public bool ShouldClose
        {
            get { return (IsClosed || (PendingOperations.HasFlags(MyPlanetSectorOperation.Close) && !PendingOperations.HasFlags(MyPlanetSectorOperation.Spawn))) && !IsQueuedParallel; }
        }

        public MyPlanet Planet { get { return m_planet; } }
        public MyPlanetSectorId SectorId { get { return m_sectorId; } }

        public List<int> SavedItems
        {
            get
            {
                List<int> items;
                m_planet.SavedSectors.TryGetValue(m_sectorId, out items);

                return items;
            }
        }

        #endregion

        #region Meta

        // Identifyer of this sector.
        MyPlanetSectorId m_sectorId;

        // Planet that contains the sector,.
        MyPlanet m_planet;

        // Semi-unique identifier of the sector.
        int m_cellHashCode;

        // Bounding box for the sector.
        public BoundingBoxD SectorBox;

        // Center projected to surface
        public Vector3D LocalSurfaceCenter { get; private set; }

        public Vector3D SectorCenter
        {
            get
            {
                return m_sectorCenter;
            }
        }

        // Vertices of the bounding convex for the sector. This is not a frustum because it's possible that no face is parallel.
        Vector3D[] m_sectorFrustum;

        // Center of the sector face that points to the planet.
        private Vector3D m_sectorCenter;

        // Weather this sector is to have it's state recorded
        bool m_saved = false;
        #endregion

        class SpawnInfo
        {
            // Environment definition for this material.
            public List<MyPlanetEnvironmentMapping> EnvironmentMaps;

            // Used when checking for valid maps;
            [ThreadStatic]
            private static int[] m_tmpMaps;

            // Spawn location that have this material.
            public List<Vector3D> Locations;

            public bool Valid { get { return EnvironmentMaps != null && EnvironmentMaps.Count > 0; } }

            public SpawnInfo(List<MyPlanetEnvironmentMapping> envs)
            {
                EnvironmentMaps = envs;
                Locations = new List<Vector3D>();

                if (Valid)
                    m_tmpMaps = new int[EnvironmentMaps.Count];
            }

            /**
             * Scan all environment maps, select the ones that match the surface params and out of those, choose one at random.
             */
            public bool TryGetRandomValid(ref MyPlanetStorageProvider.SurfaceProperties props, out MyPlanetEnvironmentMapping mapping, MyRandom random)
            {
                if (MyFakes.SKIP_ENVIRONMENT_ITEM_RULES)
                {
                    if (!Valid)
                    {
                        mapping = null;
                        return false;
                    }

                    var index = random.Next(0, EnvironmentMaps.Count - 1);
                    mapping = EnvironmentMaps[index];
                    return true;
                }

                // Prepare thread static local buffer.
                if (m_tmpMaps == null || m_tmpMaps.Length < EnvironmentMaps.Count) m_tmpMaps = new int[EnvironmentMaps.Count];

                int validParams = 0;

                for (int i = 0; i < EnvironmentMaps.Count; i++)
                {
                    var rule = EnvironmentMaps[i].Rule;
                    if (rule.Check(props.HeightRatio, props.Latitude, props.Longitude, props.Slope))
                    {
                        m_tmpMaps[validParams++] = i;
                    }
                }

                switch (validParams)
                {
                    case 0:
                        mapping = null;
                        return false;
                    case 1:
                        mapping = EnvironmentMaps[m_tmpMaps[0]];

                        return true;
                    default:
                        var index = random.Next(0, validParams - 1);
                        mapping = EnvironmentMaps[m_tmpMaps[index]];
                        return true;
                }

            }
        }

        struct VoxelMapInfo
        {
            public string Name;
            public Vector3 Position;
            public string Storage;
            public Matrix Matrix;
            public int Id;
            public int Modifier;
            public long EntityId;
        }

        // List of points classified by (material << 8 | biomeValue);
        Dictionary<ushort, SpawnInfo> m_spawnInfo = new Dictionary<ushort, SpawnInfo>();

        private static int m_totalBots;

        struct MyDetailSpawnInfo
        {
            public Vector3D Position;
            public MyMaterialEnvironmentItem Item;
        }

        // Metadata about detail items.
        Dictionary<int, MyDetailSpawnInfo> m_detailItems;

        #region Status Fields

        FastResourceLock m_statusLock = new FastResourceLock();

        public IDisposable AcquireStatusLock()
        {
            return m_statusLock.AcquireExclusiveUsing();
        }

        // Weather the sector is queued to be updated.
        public bool IsQueuedParallel { get; set; }

        // Weather the sector is queued for serial.
        public bool IsQueuedSerial { get; set; }

        // Weather the sector contains some entity in it.
        // Used to optimize sector updates when multiple entities are in the same sector.
        public bool HasEntity { get; set; }

        public bool HasGraphics
        {
            get;
            protected set;
        }

        public bool HasPhysics
        {
            get;
            set;
        }

        public bool HasDetails
        {
            get;
            protected set;
        }

        public bool IsClosed { get; protected set; }

        public bool Loaded { get; protected set; }

        public bool ServerOwned { get; set; }

        private bool EntityRaised { get; set; }

        /**
         * Clear all transient status values such as having an entity or being marked to sleep.
         */
        public void PrepareForUpdate()
        {
            HasEntity = false;
            // Leave closed sectors mostly clean so they can be removed
            if (!IsClosed)
            {
                if (Sync.IsServer || !ServerOwned)
                    PendingOperations |= MyPlanetSectorOperation.Close;
                PendingOperations |= MyPlanetSectorOperation.CloseGraphics;
                PendingOperations |= MyPlanetSectorOperation.CloseDetails;
                PendingOperations |= MyPlanetSectorOperation.ClosePhysics;
            }
        }

        public void Reset()
        {
            using (m_statusLock.AcquireExclusiveUsing())
            {
                PendingOperations |= MyPlanetSectorOperation.Reset;
            }
        }

        #endregion

        // Spawners for items
        private Dictionary<MyStringHash, MyEnvironmentItems.MyEnvironmentItemsSpawnData> m_spawners;
        private bool m_itemsSpawned;

        // Spawners for details
        private Dictionary<MyStringHash, MyEnvironmentItems.MyEnvironmentItemsSpawnData> m_detailSpawners;
        private bool m_detailsSpawned;

        // Bots
        private List<int> m_myBots = new List<int>();

        // Voxel maps.
        private Dictionary<MyVoxelMap, int> m_voxelMaps = new Dictionary<MyVoxelMap, int>();

        private List<VoxelMapInfo> m_voxelMapsToAdd = new List<VoxelMapInfo>();

        // Basis Vectors por placing items.
        private Vector3 m_basisX, m_basisY;

        // Number of items that have actually been placed.
        int m_numPlacedItems;

        // Using this random with the same has every time ensure sectors spawn predictably.
        MyRandom m_random = new MyRandom();

        // Items added to the list of candidate spawn locations
        int m_itemsAdded;

        public MyPlanetEnvironmentSector()
        {
            // Preset everything to default.
            HasPhysics = false;
            HasDetails = false;
            Loaded = false;
            IsClosed = true;

            m_voxelMap_RangeChangedDelegate = VoxelMap_RangeChanged;
        }

        public event Action OnSectorClose;

        public event Action OnPhysicsClose;

        public void Init(ref MyPlanetSectorId id, MyPlanet planet)
        {
            long eid = String.Format("{0}:{1}", planet.EntityId, id).GetHashCode64();
            eid = (eid >> 8) + eid + (eid << 13);
            EntityId = MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.PLANET_ENVIRONMENT_SECTOR, 0x00FFFFFFFFFFFFFF & eid);

            // Make sure this sector was properly cleaned up.
            Debug.Assert(Loaded == false && IsClosed && !EntityRaised);

            HasPhysics = false;
            HasDetails = false;
            HasEntity = false;
            Loaded = false;
            IsClosed = false;
            ServerOwned = false;

            m_itemsSpawned = false;
            m_detailsSpawned = false;


            m_sectorId = id;
            m_cellHashCode = id.GetHashCode() + planet.GetInstanceHash();
            m_planet = planet;

            planet.SectorIdToWorldBoundingBox(ref id, out SectorBox);

            // Prepare basis vectors for item placement.
            ComputeBasisPlacement();

            m_random.SetSeed(m_cellHashCode);

            Vector3 cent = SectorBox.Center - m_planet.WorldMatrix.Translation;

            LocalSurfaceCenter = m_planet.GetClosestSurfacePointLocal(ref cent);

            UpdateSectorFrustrum(); // This is used for debug draw and for PrepareItems (effective sector bounding box).
        }

        private void ComputeBasisPlacement()
        {
            // Push center to face
            m_sectorCenter = SectorBox.Center + new Vector3(m_sectorId.Direction) * SectorBox.HalfExtents;

            if (m_sectorId.Direction.X == 0)
            {
                m_basisX = new Vector3(1f, 0f, 0f);
            }
            else
            {
                m_basisX = new Vector3(0f, 0f, 1f);
            }

            if (m_sectorId.Direction.Y == 0)
            {
                m_basisY = new Vector3(0f, 1f, 0f);
            }
            else
            {
                m_basisY = new Vector3(0f, 0f, 1f);
            }

            m_basisX *= SectorBox.HalfExtents;
            m_basisY *= SectorBox.HalfExtents;
        }

        // Get a random position for an item.
        public Vector3D GetRandomItemPosition()
        {
            Vector3 facePosition = m_random.NextFloat(-1, 1) * m_basisX + m_random.NextFloat(-1, 1) * m_basisY;
            return m_sectorCenter + facePosition;
        }

        public Vector3D GetRandomPerpendicularVector(ref Vector3D axis, int seed)
        {
            using (m_random.PushSeed(seed))
            {
                Vector3D tangent = Vector3D.CalculatePerpendicularVector(axis);
                Vector3D bitangent; Vector3D.Cross(ref axis, ref tangent, out bitangent);
                double angle = m_random.NextFloat(0, 2 * MathHelper.Pi);
                return Math.Cos(angle) * tangent + Math.Sin(angle) * bitangent;
            }
        }

        // This method will run on different thread
        public void PrepareItems()
        {
            if (!MyFakes.ENABLE_ENVIRONMENT_ITEMS)
            {
                return;
            }

            if (m_planet.Storage == null || m_planet.Storage.Closed) return;

            ProfilerShort.Begin("PlaceItems");
            var provider = m_planet.Provider;

            using (m_planet.Storage.Pin())
            {
                int itemsInSector = m_planet.SectorTotalItems;

                if (itemsInSector <= 0) return;

                m_spawners = new Dictionary<MyStringHash, MyEnvironmentItems.MyEnvironmentItemsSpawnData>();
                m_detailItems = new Dictionary<int, MyDetailSpawnInfo>();

                // Setup the coefficient cache for the planet shape, this should speed up material queries.
                provider.Shape.PrepareCache();

                // Prepare list of rules for efficient material computation.
                BoundingBox surfaceBox = (BoundingBox)BoundingBoxD.CreateFromPoints(m_sectorFrustum);
                surfaceBox.Translate(-m_planet.PositionLeftBottomCorner);
                provider.Material.PrepareRulesForBox(ref surfaceBox);

                int itemId = 0;
                int sc = 0; // saved sector cursor

                List<int> saved;
                m_planet.SavedSectors.TryGetValue(m_sectorId, out saved);

                for (int i = 0; i < itemsInSector; ++i)
                {
                    itemId++;
                    //if() TODO(DI): handle cancelling here

                    if (saved != null && sc < saved.Count && saved[sc] == itemId)
                    {
                        // simulate computing position.
                        m_random.NextDouble();
                        m_random.NextDouble();

                        if (sc < saved.Count)
                            sc++;
                        continue;
                    }

                    ProfilerShort.Begin("AcquirePosition");

                    Vector3D spawnPosition = GetRandomItemPosition();
                    Vector3D localPosition = spawnPosition - m_planet.PositionLeftBottomCorner;

                    MyPlanetStorageProvider.SurfaceProperties props;
                    provider.ComputeCombinedMaterialAndSurface(localPosition, true, out props);

                    spawnPosition = props.Position + m_planet.PositionLeftBottomCorner;

                    string matId = props.Material.Id.SubtypeName;
                    ushort key = (ushort)((props.Material.Index << 8) + props.Biome); // combine the biomevalue in the key.
                    ProfilerShort.End();

                    // Seed from current position so we do not interfere with the parent random instance,
                    // This way we can preserve determinism reliably.
                    using (m_random.PushSeed(props.Position.GetHashCode()))
                    {
                        if (!m_spawnInfo.ContainsKey(key))
                        {
                            List<MyPlanetEnvironmentMapping> envs = null;

                            // Get environment for biome/material
                            if (m_planet.Generator.MaterialEnvironmentMappings.ContainsKey(props.Biome) &&
                                m_planet.Generator.MaterialEnvironmentMappings[props.Biome].ContainsKey(matId))
                            {
                                envs = m_planet.Generator.MaterialEnvironmentMappings[props.Biome][matId];
                            }

                            if (envs == null || envs.Count == 0) continue;

                            SpawnInfo info = new SpawnInfo(envs);

                            ProfilerShort.Begin("PrepareSpawners");
                            // Prepare the spawners for each item.
                            foreach (var maps in info.EnvironmentMaps)
                            {
                                foreach (var item in maps.Items)
                                {
                                    if (MyDefinitionManager.Static.GetVoxelMapGroups() != null)
                                    {
                                        //check if it is group of objects
                                        if (item.GroupId != null && item.GroupId.Length > 0)
                                        {
                                            for (int j = 0; j < MyDefinitionManager.Static.GetVoxelMapGroups().Count; j++)
                                            {
                                                if (item.GroupId.Equals(MyDefinitionManager.Static.GetVoxelMapGroups()[j].GroupId))
                                                {
                                                    item.GroupIndex = j;
                                                    break;
                                                }
                                            }
                                        }
                                        //check for modifiers
                                        if (item.ModifierId != null && item.ModifierId.Length > 0)
                                        {
                                            for (int j = 0; j < MyDefinitionManager.Static.GetVoxelMapModifiers().Count; j++)
                                            {
                                                if (item.ModifierId.Equals(MyDefinitionManager.Static.GetVoxelMapModifiers()[j].ModifierId))
                                                {
                                                    item.ModifierIndex = j;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    var definitionId = item.Definition;

                                    if (m_spawners.ContainsKey(definitionId.SubtypeId) || item.IsDetail)
                                    {
                                        continue;
                                    }

                                    MyEnvironmentItemsDefinition definition;
                                    MyDefinitionManager.Static.TryGetDefinition(definitionId, out definition);
                                    Debug.Assert(definition != null, String.Format("There is no definition for item {0}", definitionId));

                                    if (definition != null)
                                    {
                                        int index = m_spawners.Count + 1;

                                        var items = MyEnvironmentItems.BeginSpawn(definition, false, ComputeItemEntityId(index));
                                        items.EnvironmentItems.Save = false;
                                        items.EnvironmentItems.PlanetSpawnerDefinition = definitionId;
                                        items.EnvironmentItems.CellsOffset = m_planet.PositionLeftBottomCorner;
                                        items.EnvironmentItems.BaseColor = item.BaseColor;
                                        items.EnvironmentItems.ColorSpread = item.ColorSpread;
                                        items.EnvironmentItems.PlanetSector = this;

                                        m_spawners[definitionId.SubtypeId] = items;
                                    }
                                }
                            }
                            ProfilerShort.End();

                            m_spawnInfo.Add(key, info);
                        }

                        var nfo = m_spawnInfo[key];

                        ProfilerShort.Begin("SpawnItem");
                        try
                        {
                            // Grab the parameters of the surface
                            Vector3 localPos = spawnPosition - m_planet.PositionLeftBottomCorner;

                            // Find the mappings that are satisfiable.
                            MyPlanetEnvironmentMapping map;
                            if (nfo.TryGetRandomValid(ref props, out map, m_random))
                            {
                                float kk = m_random.NextFloat(0, 1);
                                float densityKey = m_random.NextFloat(0, 1);

                                // this will help preserve density, when total density is over 1 it will never continue
                                if (densityKey > map.TotalFrequency) continue;

                                var item = map.GetItemRated(kk);

                                var definitionId = map.Items[item].Definition;

                                if (map.Items[item].IsDetail)
                                {
                                    MyDetailSpawnInfo detailSpawnInfo = new MyDetailSpawnInfo();
                                    detailSpawnInfo.Position = spawnPosition;
                                    detailSpawnInfo.Item = map.Items[item];
                                    m_detailItems.Add(itemId, detailSpawnInfo);
                                }
                                else
                                {
                                    MyEnvironmentItemsDefinition definition;
                                    MyDefinitionManager.Static.TryGetDefinition(definitionId, out definition);

                                    var spawner = m_spawners[definitionId.SubtypeId];

                                    Vector3 direction = Vector3.Normalize(spawnPosition - m_planet.WorldMatrix.Translation);
                                    spawnPosition += direction * map.Items[item].Offset;

                                    MyEnvironmentItems.SpawnItem(spawner, definition.GetRandomItemDefinition(), spawnPosition, direction, itemId, true);
                                }
                                m_numPlacedItems++;
                            }
                        }
                        finally
                        {
                            ProfilerShort.End();
                        }
                    }
                    m_itemsAdded++;
                }
            }

            ProfilerShort.End();
        }

        private long ComputeItemEntityId(int index)
        {
            long id = (m_planet.GetInstanceHash() * 337) ^ m_sectorId.GetHashCode() ^ (index * 2137);
            id = MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.PLANET_ENVIRONMENT_ITEM, id & 0x00FFFFFFFFFFFFFF);
            return id;
        }

        public void EndPlacement()
        {
            m_itemsSpawned = true;
            if (m_numPlacedItems == 0 || m_spawners == null)
            {
                return;
            }

            ProfilerShort.Begin("PlanetSectors::EndSpawn()");
            foreach (var spawner in m_spawners)
            {
                m_planet.AddChildEntity(spawner.Value.EnvironmentItems);
                MyEnvironmentItems.EndSpawn(spawner.Value, true, false);
            }
            ProfilerShort.End();
        }

        public void UpdateSectorGraphics()
        {
            if (!m_itemsSpawned) return;
            Debug.Assert(Loaded && !HasGraphics);

            HasGraphics = true;

            if (m_numPlacedItems == 0)
                return;

            if (m_spawners != null)
                foreach (var spawner in m_spawners)
                {
                    spawner.Value.EnvironmentItems.PrepareItemsGraphics();
                }

            if (m_detailSpawners != null)
                foreach (var spawner in m_detailSpawners)
                {
                    spawner.Value.EnvironmentItems.PrepareItemsGraphics();
                }
        }

        public void CloseGraphics()
        {
            if (m_numPlacedItems != 0)
            {
                if (m_spawners != null)
                {
                    foreach (var spawner in m_spawners)
                    {
                        spawner.Value.EnvironmentItems.UnloadGraphics();
                    }
                }

                if (m_detailSpawners != null)
                {
                    foreach (var spawner in m_detailSpawners)
                    {
                        spawner.Value.EnvironmentItems.UnloadGraphics();
                    }
                }
            }
            HasGraphics = false;
        }

        public void UpdateSectorPhysics()
        {
            if (!m_itemsSpawned) return;
            if (!Loaded) return;

            // Do not spawn physics before the server tells us to, this prevents ships from getting murdered by trees that should no longer be there.
            // Later I think we should ideally disable all those collisions on clients to only the server would handle them.
            if (!Sync.IsServer && !ServerOwned)
            {
                HasPhysics = true;
                return;
            }

            if (Sync.IsServer && !EntityRaised)
            {
                EntityRaised = true;
                MyEntities.RaiseEntityCreated(this);
            }

            Debug.Assert(!HasPhysics);
            Debug.Assert(m_itemsSpawned);

            if (m_numPlacedItems != 0)
            {
                if (m_spawners != null)
                {
                    foreach (var spawner in m_spawners)
                    {
                        spawner.Value.EnvironmentItems.PrepareItemsPhysics(spawner.Value);
                        spawner.Value.EnvironmentItems.ItemRemoved += OnSectorItemRemoved;
                    }
                }

                if (m_detailSpawners != null)
                {
                    foreach (var spawner in m_detailSpawners)
                    {
                        spawner.Value.EnvironmentItems.PrepareItemsPhysics(spawner.Value);
                        spawner.Value.EnvironmentItems.ItemRemoved += OnSectorItemRemoved;
                    }
                }
            }
            HasPhysics = true;
        }

        public void CloseSectorPhysics(bool removeEntity = true)
        {
            Debug.Assert(HasPhysics);

            if(OnPhysicsClose != null)
                OnPhysicsClose();

            if (m_spawners != null)
            {
                foreach (var spawner in m_spawners)
                {
                    spawner.Value.EnvironmentItems.ItemRemoved -= OnSectorItemRemoved;
                    spawner.Value.EnvironmentItems.ClosePhysics(spawner.Value);
                }
            }

            if (m_detailSpawners != null)
            {
                foreach (var spawner in m_detailSpawners)
                {
                    spawner.Value.EnvironmentItems.ItemRemoved -= OnSectorItemRemoved;
                    spawner.Value.EnvironmentItems.ClosePhysics(spawner.Value);
                }
            }

            HasPhysics = false;
        }

        public void CloseSector()
        {
            if (OnSectorClose != null)
            {
                OnSectorClose();
                var actList = OnSectorClose.GetInvocationList();
                foreach (var act in actList)
                {
                    OnSectorClose -= (Action)act;
                }
            }

            if (HasPhysics)
                CloseSectorPhysics();
            
            // Unraise entity, maybe someone is tracking us.
            if (Sync.IsServer && EntityRaised)
            {
                MyEntities.RaiseEntityRemove(this);
                EntityRaised = false;
            }

            EntityId = 0;

            IsQueuedParallel = false;

            HasGraphics = false;
            Loaded = false;
            Debug.Assert(!ServerOwned || !MySession.Static.Ready);
            m_numPlacedItems = 0;

            if (m_spawners != null)
            {
                foreach (var spawner in m_spawners)
                {
                    m_planet.CloseChildEntity(spawner.Value.EnvironmentItems);
                }
            }

            CloseDetails();

            m_spawnInfo.Clear();

            if (m_spawners != null) m_spawners.Clear();
            if (m_detailSpawners != null) m_detailSpawners.Clear();

            if (m_detailItems != null) m_detailItems.Clear();
            m_detailItems = null;

            m_spawners = null;
            m_detailSpawners = null;
            m_planet = null;
            PendingOperations = 0;

            m_itemsSpawned = false;
            m_detailsSpawned = false;
            m_itemsAdded = 0;
            IsClosed = true;
        }

        public void ResetSector()
        {
            if (HasPhysics)
                CloseSectorPhysics(false);

            IsQueuedParallel = false;

            HasGraphics = false;
            Loaded = false;
            m_numPlacedItems = 0;

            m_random.SetSeed(m_cellHashCode);

            if (m_spawners != null)
            {
                foreach (var spawner in m_spawners)
                {
                    m_planet.CloseChildEntity(spawner.Value.EnvironmentItems);
                }
            }

            CloseDetails();

            m_spawnInfo.Clear();

            if (m_spawners != null) m_spawners.Clear();
            if (m_detailSpawners != null) m_detailSpawners.Clear();

            if (m_detailItems != null) m_detailItems.Clear();
            m_detailItems = null;

            m_spawners = null;
            m_detailSpawners = null;
            PendingOperations = MyPlanetSectorOperation.Spawn;

            m_itemsSpawned = false;
            m_detailsSpawned = false;
            m_itemsAdded = 0;
        }

        public void PlaceDetailItems()
        {
            if (Loaded == false)
            {
                return;
            }

            Debug.Assert(!HasDetails);

            ProfilerShort.Begin("PlanetSector::SpawnDetails()");
            try
            {
                HasDetails = true;

                if (m_detailItems == null)
                {
                    return;
                }
                m_detailSpawners = new Dictionary<MyStringHash, MyEnvironmentItems.MyEnvironmentItemsSpawnData>();

                foreach (var detailSpawnInfo in m_detailItems)
                {
                    var item = detailSpawnInfo.Value.Item;

                    Vector3 direction = Vector3.Normalize(detailSpawnInfo.Value.Position - m_planet.WorldMatrix.Translation);

                    Vector3 position = detailSpawnInfo.Value.Position + direction * item.Offset;

                    var definitionId = item.Definition;
                    //if in group than pick one object based on chance
                    if (item.GroupIndex > -1 && MyDefinitionManager.Static.GetVoxelMapGroups() == null) return;
                    if (item.GroupIndex > -1 && item.GroupIndex < MyDefinitionManager.Static.GetVoxelMapGroups().Count)
                    {
                        MyVoxelMapGroup group = MyDefinitionManager.Static.GetVoxelMapGroups()[item.GroupIndex];
                        if (@group.Items.Length > 1)
                        {
                            float groupItemChooser = ((float)Math.Abs(position.GetHashCode()) / int.MaxValue) * group.ChanceTotal;
                            for (int i = 0; i < @group.Items.Length; i++)
                            {
                                if (groupItemChooser <= group.Items[i].Chance || i == @group.Items.Length - 1)
                                {
                                    item.Definition = new MyDefinitionId(typeof(MyObjectBuilder_VoxelMapStorageDefinition), MyStringHash.GetOrCompute(group.Items[i].SubtypeId));
                                    break;
                                }
                                groupItemChooser -= group.Items[i].Chance;
                            }
                        }
                        else
                        {
                            item.Definition = new MyDefinitionId(typeof(MyObjectBuilder_VoxelMapStorageDefinition), MyStringHash.GetOrCompute(group.Items[0].SubtypeId));
                        }
                    }
                    if (m_detailSpawners.ContainsKey(definitionId.SubtypeId) == false && item.IsEnvironemntItem)
                    {
                        MyEnvironmentItemsDefinition definition;
                        MyDefinitionManager.Static.TryGetDefinition(definitionId, out definition);

                        int index = m_detailSpawners.Count + 1 + m_spawners.Count;
                        var items = MyEnvironmentItems.BeginSpawn(definition, false, ComputeItemEntityId(index));
                        items.EnvironmentItems.Save = false;
                        items.EnvironmentItems.PlanetSpawnerDefinition = definitionId;
                        items.EnvironmentItems.CellsOffset = m_planet.PositionLeftBottomCorner;
                        items.EnvironmentItems.BaseColor = item.BaseColor;
                        items.EnvironmentItems.ColorSpread = item.ColorSpread;
                        items.EnvironmentItems.PlanetSector = this;

                        m_detailSpawners[definitionId.SubtypeId] = items;
                    }

                    if (item.IsBot && MyPerGameSettings.EnableAi && m_totalBots < 10)
                    {
                        ProfilerShort.Begin("AddBot()");
                        var botid = MyAIComponent.Static.SpawnNewBot((MyAgentDefinition)MyDefinitionManager.Static.GetBotDefinition(item.Definition), position);
                        m_myBots.Add(botid);
                        m_totalBots++;
                        ProfilerShort.End();
                    }
                    else if (item.IsVoxel)
                    {
                        ProfilerShort.Begin("AddVoxelMap()");
                        if (MyFakes.ENABLE_VOXEL_ENVIRONEMNT_ITEMS && m_voxelMaps.Count == 0)
                        {
                            Vector3D surfaceUp = direction;
                            Vector3D forward = GetRandomPerpendicularVector(ref surfaceUp, position.GetHashCode());
                            var rotation = Matrix.CreateFromDir(forward, direction);

                            string storage = item.Definition.SubtypeName;
                            string name = String.Format("P({0})S({1})A({2}__{3})", m_planet.StorageName, m_sectorId, storage, detailSpawnInfo.Key);

                            long id = name.GetHashCode64();
                            id = MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.PLANET_VOXEL_DETAIL, id & 0x00FFFFFFFFFFFFFF);

                            m_voxelMapsToAdd.Add(new VoxelMapInfo
                            {
                                Name = name,
                                Storage = storage,
                                Position = position,
                                Matrix = rotation,
                                Id = detailSpawnInfo.Key,
                                Modifier = item.ModifierIndex,
                                EntityId = id
                            });
                        }
                        ProfilerShort.End();
                    }
                    else if (item.IsEnvironemntItem)
                    {
                        ProfilerShort.Begin("AddEnvironmentItem()");
                        MyEnvironmentItemsDefinition definition;
                        MyDefinitionManager.Static.TryGetDefinition(item.Definition, out definition);

                        var spawner = m_detailSpawners[item.Definition.SubtypeId];

                        MyEnvironmentItems.SpawnItem(spawner, definition.GetRandomItemDefinition(), position, direction, detailSpawnInfo.Key, true);
                        ProfilerShort.End();
                    }
                    else
                    {
                        Debug.Fail("Items that are not supported should not make it this far.");
                    }
                }
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        private void AddVoxelMap(string prefabName, Vector3D position, MatrixD rotation, string name, int id, long entityId, byte[] modifierFrom = null, byte[] modifierTo = null)
        {
            var fileName = MyWorldGenerator.GetVoxelPrefabPath(prefabName);
            var storage = MyStorageBase.LoadFromFile(fileName, modifierFrom, modifierTo);

            rotation.Translation = position;

            var voxelMap = MyWorldGenerator.AddVoxelMap(name, storage, rotation, entityId, true);
            voxelMap.Save = false;
            voxelMap.RangeChanged += m_voxelMap_RangeChangedDelegate;
            m_voxelMaps[voxelMap] = id;
        }

        private readonly MyVoxelBase.StorageChanged m_voxelMap_RangeChangedDelegate;
        private void VoxelMap_RangeChanged(MyVoxelBase voxel, Vector3I minVoxelChanged, Vector3I maxVoxelChanged, MyStorageDataTypeFlags changedData)
        {
            ReleaseVoxelMap((MyVoxelMap)voxel);
        }


        public void EndDetailPlacement()
        {
            if (Loaded == false)
            {
                return;
            }

            m_detailsSpawned = true;
            if (m_detailSpawners == null)
            {
                return;
            }

            ProfilerShort.Begin("PlanetSector::AddDetailEntities");
            foreach (var spawner in m_detailSpawners)
            {
                m_planet.AddChildEntity(spawner.Value.EnvironmentItems);
                MyEnvironmentItems.EndSpawn(spawner.Value, true, false);
            }
            ProfilerShort.End();

            if (m_voxelMapsToAdd.Count > 0)
            {
                ProfilerShort.Begin("PlanetSector::AddVoxelMaps()");
                foreach (var map in m_voxelMapsToAdd)
                {
                    // Handle when the stone was already modified and is sent by the server.
                    MyVoxelMap existingStone;
                    if (MyEntities.TryGetEntityById(map.EntityId, out existingStone))
                    {
                        if (existingStone != null && existingStone.StorageName == map.Name)
                        {
                            Debug.Assert(false, "Storage was already sent but sector did not know about the modified item.");
                            RecordModifiedItem(map.Id);
                        }
                        else
                        {
                            Debug.Assert(false, "Voxel stone storage collision.");
                        }
                        continue;
                    }

                    //if there is modifier pick one based on chance
                    if (map.Modifier >= 0 && MyDefinitionManager.Static.GetVoxelMapModifiers() != null && MyDefinitionManager.Static.GetVoxelMapModifiers().Count > map.Modifier)
                    {
                        float modifierSelection = Math.Abs((float)map.Position.GetHashCode() / int.MaxValue) * MyDefinitionManager.Static.GetVoxelMapModifiers()[map.Modifier].ChanceTotal;
                        int selectedModifier = -1;
                        for (int i = 0; i < MyDefinitionManager.Static.GetVoxelMapModifiers()[map.Modifier].Options.Length; i++)
                        {
                            if (modifierSelection <= MyDefinitionManager.Static.GetVoxelMapModifiers()[map.Modifier].Options[i].Chance || i == MyDefinitionManager.Static.GetVoxelMapModifiers()[map.Modifier].Options.Length - 1)
                            {
                                selectedModifier = i;
                                break;
                            }
                            modifierSelection -= MyDefinitionManager.Static.GetVoxelMapModifiers()[map.Modifier].Options[i].Chance;
                        }
                        if (selectedModifier == -1 || MyDefinitionManager.Static.GetVoxelMapModifiers()[map.Modifier].Options[selectedModifier].Changes == null || MyDefinitionManager.Static.GetVoxelMapModifiers()[map.Modifier].Options[selectedModifier].Changes.Length == 0)
                        {
                            AddVoxelMap(map.Storage, map.Position, map.Matrix, map.Name, map.Id, map.EntityId);
                        }
                        else
                        {
                            byte[] from = new byte[MyDefinitionManager.Static.GetVoxelMapModifiers()[map.Modifier].Options[selectedModifier].Changes.Length];
                            byte[] to = new byte[MyDefinitionManager.Static.GetVoxelMapModifiers()[map.Modifier].Options[selectedModifier].Changes.Length];
                            for (int i = 0; i < MyDefinitionManager.Static.GetVoxelMapModifiers()[map.Modifier].Options[selectedModifier].Changes.Length; i++)
                            {
                                from[i] = MyDefinitionManager.Static.GetVoxelMapModifiers()[map.Modifier].Options[selectedModifier].Changes[i].FromIndex;
                                to[i] = MyDefinitionManager.Static.GetVoxelMapModifiers()[map.Modifier].Options[selectedModifier].Changes[i].ToIndex;
                            }
                            AddVoxelMap(map.Storage, map.Position, map.Matrix, map.Name, map.Id, map.EntityId, from, to);
                        }
                    }
                    else
                    {
                        AddVoxelMap(map.Storage, map.Position, map.Matrix, map.Name, map.Id, map.EntityId);
                    }
                }
                m_voxelMapsToAdd.Clear();
                ProfilerShort.End();
            }
        }

        public void CloseDetails()
        {
            ProfilerShort.Begin("PlanetSector::RemoveBots");
            foreach (var id in m_myBots)
            {
                MyAIComponent.Static.RemoveBot(id, true);
                m_totalBots--;
            }
            m_myBots.Clear();
            ProfilerShort.End();

            ProfilerShort.Begin("PlanetSector::RemoveVoxelMaps()");
            foreach (var vmap in m_voxelMaps)
            {
                if (vmap.Key.Storage != null && vmap.Key.Storage.Shared)
                {
                    vmap.Key.RangeChanged -= m_voxelMap_RangeChangedDelegate;
                    vmap.Key.Close();
                }
            }
            m_voxelMapsToAdd.Clear();
            m_voxelMaps.Clear();
            ProfilerShort.End();

            ProfilerShort.Begin("PlanetSector::CloseSpawners()");
            if (m_detailSpawners != null)
            {
                foreach (var spawner in m_detailSpawners)
                {
                    m_planet.CloseChildEntity(spawner.Value.EnvironmentItems);
                }
            }
            ProfilerShort.End();

            HasDetails = false;
            m_detailsSpawned = false;
        }

        public void EvaluateOperations()
        {
            if (PendingOperations.HasFlags(MyPlanetSectorOperation.Reset))
            {
                PendingOperations = MyPlanetSectorOperation.Reset;
                return;
            }

            if (PendingOperations.HasFlags(MyPlanetSectorOperation.Close))
            {
                PendingOperations = MyPlanetSectorOperation.Close;
                return;
            }

            if (PendingOperations.HasFlags(MyPlanetSectorOperation.Spawn) && Loaded)
            {
                PendingOperations &= ~MyPlanetSectorOperation.Spawn;
            }

            if (PendingOperations.HasFlags(MyPlanetSectorOperation.SpawnDetails))
            {
                PendingOperations &= ~MyPlanetSectorOperation.CloseDetails;
                if (HasDetails) PendingOperations &= ~MyPlanetSectorOperation.SpawnDetails;
                else { PendingOperations |= MyPlanetSectorOperation.AddEntities; }
            }
            else if (PendingOperations.HasFlags(MyPlanetSectorOperation.CloseDetails))
            {
                if (!HasDetails)
                    PendingOperations &= ~MyPlanetSectorOperation.CloseDetails;
            }

            if (PendingOperations.HasFlags(MyPlanetSectorOperation.SpawnGraphics))
            {
                PendingOperations &= ~MyPlanetSectorOperation.CloseGraphics;
                if (HasGraphics) PendingOperations &= ~MyPlanetSectorOperation.SpawnGraphics;
                else PendingOperations |= MyPlanetSectorOperation.AddEntities;
            }
            else if (PendingOperations.HasFlags(MyPlanetSectorOperation.CloseGraphics) && !HasGraphics)
            {
                PendingOperations &= ~MyPlanetSectorOperation.CloseGraphics;
            }

            if (PendingOperations.HasFlags(MyPlanetSectorOperation.SpawnPhysics))
            {
                PendingOperations &= ~MyPlanetSectorOperation.ClosePhysics;
                if (HasPhysics) PendingOperations &= ~MyPlanetSectorOperation.SpawnPhysics;
                else PendingOperations |= MyPlanetSectorOperation.AddEntities;
            }
            else if (PendingOperations.HasFlags(MyPlanetSectorOperation.ClosePhysics) && !HasPhysics)
            {
                PendingOperations &= ~MyPlanetSectorOperation.ClosePhysics;
            }
        }

        internal bool DoParallelWork()
        {
            bool worked = false;
            MyPlanetSectorOperation toDo;

            //TODO: Use lazy init, after we have support to merge created entities to main list
            MyEntityIdentifier.InitPerThreadStorage(128);

            using (m_statusLock.AcquireExclusiveUsing())
            {
                toDo = PendingOperations & ~SERIAL_OPERATIONS_MASK;
                PendingOperations = PendingOperations & SERIAL_OPERATIONS_MASK;

                if (PendingOperations != 0 && !PendingOperations.HasFlags(MyPlanetSectorOperation.Close) && !IsQueuedSerial)
                {
                    m_planet.SectorsToWorkSerial.Enqueue(this);
                    IsQueuedSerial = true;
                }
            }

            RememberWork(true, toDo);

            ProfilerShort.Begin("PlanetSector::Spawn()");
            if (toDo.HasFlags(MyPlanetSectorOperation.Spawn))
            {
                Debug.Assert(!Loaded);
                PrepareItems();
                worked = true;
                Loaded = true;
            }
            ProfilerShort.End();

            if (toDo.HasFlags(MyPlanetSectorOperation.SpawnDetails))
            {
                if (!HasDetails)
                {
                    ProfilerShort.Begin("PlanetSector::SpawnDetails()");
                    PlaceDetailItems();
                    worked = true;
                    ProfilerShort.End();
                }
            }

            MyEntityIdentifier.DestroyPerThreadStorage();

            return worked;
        }

        internal void DoSerialWork(bool queuedWork = true)
        {
            IsQueuedSerial = false;

            // Don' t run anything if closed and ran by queue processor.
            if (IsClosed && queuedWork) return;

            if (ParallelPending && !PendingOperations.HasFlags(MyPlanetSectorOperation.Close)) return;

            MyPlanetSectorOperation toDo;

            toDo = PendingOperations & SERIAL_OPERATIONS_MASK;
            PendingOperations = PendingOperations & ~SERIAL_OPERATIONS_MASK;

            RememberWork(false, toDo);

            if (toDo.HasFlags(MyPlanetSectorOperation.Reset))
            {
                ProfilerShort.Begin("PlanetSector::Reset()");
                m_planet.SectorsClosed.Hit();
                ResetSector();
                IsClosed = false;
                ProfilerShort.End();
                return;
            }

            if (toDo.HasFlags(MyPlanetSectorOperation.Close))
            {
                ProfilerShort.Begin("PlanetSector::Close()");
                m_planet.SectorsClosed.Hit();
                CloseSector();
                ProfilerShort.End();
                return;
            }

            if (toDo.HasFlags(MyPlanetSectorOperation.CloseDetails))
            {
                ProfilerShort.Begin("PlanetSector::CloseDetails()");
                CloseDetails();
                HasDetails = false;
                ProfilerShort.End();
            }

            if (toDo.HasFlags(MyPlanetSectorOperation.AddEntities))
            {
                ProfilerShort.Begin("PlanetSector::AddEntities()");
                if (!m_itemsSpawned)
                {
                    EndPlacement();
                }

                if (HasDetails && !m_detailsSpawned)
                {
                    EndDetailPlacement();
                }
                ProfilerShort.End();
            }

            if (toDo.HasFlags(MyPlanetSectorOperation.SpawnPhysics) && !IsClosed)
            {
                if (!HasPhysics)
                {
                    ProfilerShort.Begin("PlanetSector::SpawnPhysics()");
                    UpdateSectorPhysics();
                    ProfilerShort.End();
                }
            }
            else if (toDo.HasFlags(MyPlanetSectorOperation.ClosePhysics))
            {
                ProfilerShort.Begin("PlanetSector::ClosePhysics()");
                CloseSectorPhysics();
                ProfilerShort.End();
            }

            if (toDo.HasFlags(MyPlanetSectorOperation.SpawnGraphics))
            {
                ProfilerShort.Begin("PlanetSector::SpawnGraphics()");
                UpdateSectorGraphics();
                ProfilerShort.End();
            }
            else if (toDo.HasFlags(MyPlanetSectorOperation.CloseGraphics))
            {
                ProfilerShort.Begin("PlanetSector::CloseGraphics()");
                CloseGraphics();
                ProfilerShort.End();
            }
        }

        public override string ToString()
        {
            return String.Format("PlanetSector {0}", m_sectorId);
        }

        private void UpdateSectorFrustrum()
        {
            BoundingBox box = (BoundingBox)SectorBox;
            box.Translate(-m_planet.WorldMatrix.Translation);

            m_planet.Provider.Shape.GetBounds(ref box);

            box.Min.Z--;
            box.Max.Z++;

            // Draw sector frustrum.
            Vector3D[] v = new Vector3D[8];

            v[0] = m_sectorCenter - m_basisX - m_basisY;
            v[1] = m_sectorCenter + m_basisX - m_basisY;
            v[2] = m_sectorCenter - m_basisX + m_basisY;
            v[3] = m_sectorCenter + m_basisX + m_basisY;

            for (int i = 0; i < 4; ++i)
            {
                v[i] -= m_planet.WorldMatrix.Translation;
                v[i].Normalize();
                v[i + 4] = v[i] * box.Max.Z;
                v[i] *= box.Min.Z;

                v[i] += m_planet.WorldMatrix.Translation;
                v[i + 4] += m_planet.WorldMatrix.Translation;
            }

            m_sectorFrustum = v;
        }

        public void DebugDraw()
        {
            if (IsClosed) return;

            Vector3D[] v = m_sectorFrustum;

            StringBuilder sb = new StringBuilder();

            sb.Append(m_sectorId);
            sb.Append(' ');

            if (Loaded)
                sb.Append('L');
            if (HasGraphics)
                sb.Append('G');
            if (HasDetails)
                sb.Append('D');
            if (HasPhysics)
                sb.Append('P');

            Color col = Loaded ? (HasGraphics ? Color.Red : Color.Blue) : Color.Green;

            MyRenderProxy.DebugDraw6FaceConvex(v, col, .6f, true, false);

            Vector3D topCenter = (v[7] + v[4]) * .5f;

            MyRenderProxy.DebugDrawText3D(topCenter, sb.ToString(), col, .66f, true);
        }

        #region Item removal & Sync

        /**
         * Record that an item in this sector has been modified.
         */
        [Event, Reliable, Broadcast]
        public void RecordModifiedItem(int itemId)
        {
            if (!m_planet.SavedSectors.ContainsKey(m_sectorId))
            {
                m_planet.SavedSectors.Add(m_sectorId, new List<int>());
            }

            m_planet.SavedSectors[m_sectorId].InsertInOrder(itemId);

            // Remove detail so so it does not get re-spawned
            m_detailItems.Remove(itemId);

            m_planet.OnEnviromentSectorItemRemoved(ref m_sectorId);
        }

        [Event, Broadcast, Reliable]
        public void BreakEnvironmentItem(SerializableDefinitionId myDefinitionId, Vector3D position, Vector3 normal, double energy, int itemId)
        {
            MyEnvironmentItems.MyEnvironmentItemsSpawnData items;
            if (m_spawners.TryGetValue(MyStringHash.GetOrCompute(myDefinitionId.SubtypeId), out items))
            {
                items.EnvironmentItems.DestroyItemAndCreateDebris(position, normal, energy, itemId);
            }
        }

        private void OnSectorItemRemoved(MyEnvironmentItems item, MyEnvironmentItems.ItemInfo value)
        {
            BroadcastRemoveItem(value.UserData);
        }

        private void BroadcastRemoveItem(int itemId)
        {
            if (Sync.IsServer)
            {
                MyMultiplayer.RaiseEvent(this, x => x.RecordModifiedItem, itemId);

                RecordModifiedItem(itemId);
            }
        }

        private void ReleaseVoxelMap(MyVoxelMap vmap)
        {
            vmap.Save = true;
            vmap.RangeChanged -= m_voxelMap_RangeChangedDelegate;

            if (m_voxelMaps.ContainsKey(vmap))
            {
                var id = m_voxelMaps[vmap];

                RecordModifiedItem(id);

                m_voxelMaps.Remove(vmap);
                m_detailItems.Remove(id);
            }
        }

        #endregion

        #region Item Enumerator

        class EnumerableItems : IEnumerable<MyEnvironmentItems>
        {
            MyPlanetEnvironmentSector sector;

            public EnumerableItems(MyPlanetEnvironmentSector sector)
            {
                this.sector = sector;
            }

            public IEnumerator<MyEnvironmentItems> GetEnumerator()
            {
                return new ItemsIterator(sector);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        class ItemsIterator : IEnumerator<MyEnvironmentItems>
        {
            IEnumerator<MyEnvironmentItems.MyEnvironmentItemsSpawnData> m_normal, m_details;

            bool m_inDetails;

            public ItemsIterator(MyPlanetEnvironmentSector sector)
            {
                if (sector.m_spawners != null)
                    m_normal = sector.m_spawners.Values.GetEnumerator();
                else
                    m_inDetails = true;

                if (sector.m_detailSpawners != null)
                    m_details = sector.m_detailSpawners.Values.GetEnumerator();
            }

            public MyEnvironmentItems Current
            {
                get
                {
                    if (m_inDetails)
                        return m_details != null && m_details.Current != null ? m_details.Current.EnvironmentItems : null;
                    return m_normal != null && m_normal.Current != null ? m_normal.Current.EnvironmentItems : null;
                }
            }

            public void Dispose()
            {
                if (m_normal != null) m_normal.Dispose();
                if (m_details != null) m_details.Dispose();
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (m_inDetails)
                    return m_details != null ? m_details.MoveNext() : false;
                if (!m_normal.MoveNext())
                    m_inDetails = true;
                else
                    return true;
                if (m_details != null)
                    return m_details.MoveNext();
                return false;
            }

            public void Reset()
            {
                if (m_normal != null)
                {
                    m_normal.Reset();
                    m_inDetails = false;
                }
                if (m_details != null) m_details.Reset();
            }
        }

        public IEnumerable<MyEnvironmentItems> GetItems()
        {
            if (!Loaded)
                return Enumerable.Empty<MyEnvironmentItems>();
            return new EnumerableItems(this);
        }

        #endregion
    }
}
