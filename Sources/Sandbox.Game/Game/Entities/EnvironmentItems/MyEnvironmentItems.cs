#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Sandbox.Engine.Utils;

using Sandbox.Game;
using VRage;
using VRage.Library.Utils;
using Sandbox;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Weapons;
using Sandbox.Game.Components;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using VRage.ModAPI;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Entities.Debris;
using VRage.Network;
using VRage.Game.Models;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Profiler;

#endregion

namespace Sandbox.Game.Entities.EnvironmentItems
{
    /// <summary>
    /// Base class for collecting environment items (of one type) in entity. Useful for drawing of instanced data, or physical shapes instances.
    /// </summary>
    [MyEntityType(typeof(MyObjectBuilder_EnvironmentItems))]
    public class MyEnvironmentItems : MyEntity, IMyEventProxy
    {
        protected struct MyEnvironmentItemData
        {
            public int Id;
            public MyTransformD Transform;
            public MyStringHash SubtypeId;
            public bool Enabled;
            public int SectorInstanceId;
            public int UserData;
            public MyModel Model;
        }

        public class MyEnvironmentItemsSpawnData
        {
            // Baase entity object.
            public MyEnvironmentItems EnvironmentItems;

            // Physics shapes for subtypes.
            public Dictionary<MyStringHash, HkShape> SubtypeToShapes = new Dictionary<MyStringHash, HkShape>(MyStringHash.Comparer);
            // Root physics shapes per sector id.
            public HkStaticCompoundShape SectorRootShape;
            // Bounding box of all environment items transformed to world space.
            public BoundingBoxD AabbWorld = BoundingBoxD.CreateInvalid();
        }

        public struct ItemInfo
        {
            public int LocalId;
            public MyTransformD Transform;
            public MyStringHash SubtypeId;
            public int UserData;
        }

        private struct AddItemData
        {
            public Vector3D Position;
            public MyStringHash SubtypeId;
        }

        private struct ModifyItemData
        {
            public int LocalId;
            public MyStringHash SubtypeId;
        }

        private struct RemoveItemData
        {
            public int LocalId;
        }

        private readonly MyInstanceFlagsEnum m_instanceFlags;

        // Items data.
        protected readonly Dictionary<int, MyEnvironmentItemData> m_itemsData = new Dictionary<int, MyEnvironmentItemData>();
        // Map from Havok's instance identifier to key in items data.
        protected readonly Dictionary<int, int> m_physicsShapeInstanceIdToLocalId = new Dictionary<int, int>();

        // Map from key in items data to Havok's instance identifier.
        protected readonly Dictionary<int, int> m_localIdToPhysicsShapeInstanceId = new Dictionary<int, int>();
        // Map from environment item subtypes to their models
        protected static readonly Dictionary<MyStringHash, int> m_subtypeToModels = new Dictionary<MyStringHash, int>(MyStringHash.Comparer);

        // Sectors.
        protected readonly Dictionary<Vector3I, MyEnvironmentSector> m_sectors = new Dictionary<Vector3I, MyEnvironmentSector>(Vector3I.Comparer);
        public Dictionary<Vector3I, MyEnvironmentSector> Sectors { get { return m_sectors; } }

        protected List<HkdShapeInstanceInfo> m_childrenTmp = new List<HkdShapeInstanceInfo>();
        HashSet<Vector3I> m_updatedSectorsTmp = new HashSet<Vector3I>();
        List<HkdBreakableBodyInfo> m_tmpBodyInfos = new List<HkdBreakableBodyInfo>();
        protected static List<HkBodyCollision> m_tmpResults = new List<HkBodyCollision>();
        protected static List<MyEnvironmentSector> m_tmpSectors = new List<MyEnvironmentSector>();
        List<int> m_tmpToDisable = new List<int>();

        private MyEnvironmentItemsDefinition m_definition;
        public MyEnvironmentItemsDefinition Definition { get { return m_definition; } }

        public event Action<MyEnvironmentItems, ItemInfo> ItemAdded;
        public event Action<MyEnvironmentItems, ItemInfo> ItemRemoved;
        public event Action<MyEnvironmentItems, ItemInfo> ItemModified;

        private List<AddItemData> m_batchedAddItems = new List<AddItemData>();
        private List<ModifyItemData> m_batchedModifyItems = new List<ModifyItemData>();
        private List<RemoveItemData> m_batchedRemoveItems = new List<RemoveItemData>();

        private float m_batchTime = 0;
        private const float BATCH_DEFAULT_TIME = 10; // s
        public bool IsBatching { get { return m_batchTime > 0; } }
        public float BatchTime { get { return m_batchTime; } }

        public event Action<MyEnvironmentItems> BatchEnded;

        public Vector3 BaseColor;
        public Vector2 ColorSpread;

        public new MyPhysicsBody Physics
        {
            get { return base.Physics as MyPhysicsBody; }
            set { base.Physics = value; }
        }

        static MyEnvironmentItems()
        {
            var items = MyDefinitionManager.Static.GetEnvironmentItemDefinitions();
            foreach (var item in items)
            {
                CheckModelConsistency(item);
            }
        }

        public MyEnvironmentItems()
        {
            m_instanceFlags = MyInstanceFlagsEnum.ShowLod1 | MyInstanceFlagsEnum.CastShadows | MyInstanceFlagsEnum.EnableColorMask;
            m_definition = null;

            this.Render = new MyRenderComponentEnvironmentItems(this);
            AddDebugRenderComponent(new MyEnviromentItemsDebugDraw(this));
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            Init(null, null, null, null);

            BoundingBoxD aabbWorld = BoundingBoxD.CreateInvalid();
            Dictionary<MyStringHash, HkShape> subtypeIdToShape = new Dictionary<MyStringHash, HkShape>(MyStringHash.Comparer);
            HkStaticCompoundShape sectorRootShape = new HkStaticCompoundShape(HkReferencePolicy.None);
            var builder = (MyObjectBuilder_EnvironmentItems)objectBuilder;

            MyDefinitionId defId = new MyDefinitionId(builder.TypeId, builder.SubtypeId);
            CellsOffset = builder.CellsOffset;

            // Compatibility
            if (builder.SubtypeId == MyStringHash.NullOrEmpty)
            {
                if (objectBuilder is MyObjectBuilder_Bushes)
                {
                    defId = new MyDefinitionId(typeof(MyObjectBuilder_DestroyableItems), "Bushes");
                }
                else if (objectBuilder is MyObjectBuilder_TreesMedium)
                {
                    defId = new MyDefinitionId(typeof(MyObjectBuilder_Trees), "TreesMedium");
                }
                else if (objectBuilder is MyObjectBuilder_Trees)
                {
                    defId = new MyDefinitionId(typeof(MyObjectBuilder_Trees), "Trees");
                }
            }

            if (!MyDefinitionManager.Static.TryGetDefinition<MyEnvironmentItemsDefinition>(defId, out m_definition))
            {
                Debug.Assert(false, "Could not find definition " + defId.ToString() + " for environment items!");
                return;
            }

            if (builder.Items != null)
            {
                foreach (var item in builder.Items)
                {
                    var itemSubtype = MyStringHash.GetOrCompute(item.SubtypeName);
                    //Debug.Assert(m_definition.ContainsItemDefinition(itemSubtype));
                    if (!m_definition.ContainsItemDefinition(itemSubtype))
                    {
                        continue;
                    }

                    MatrixD worldMatrix = item.PositionAndOrientation.GetMatrix();
                    AddItem(m_definition.GetItemDefinition(itemSubtype), ref worldMatrix, ref aabbWorld);
                }
            }

            PrepareItemsPhysics(sectorRootShape, ref aabbWorld,subtypeIdToShape);
            PrepareItemsGraphics();

            foreach (var pair in subtypeIdToShape)
            {
                pair.Value.RemoveReference();
            }
            sectorRootShape.Base.RemoveReference();
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            var builder = (MyObjectBuilder_EnvironmentItems)base.GetObjectBuilder(copy);
            builder.SubtypeName = this.Definition.Id.SubtypeName;

            if (IsBatching)
                EndBatch(true);

            int numEnabled = 0;
            foreach (var itemsData in m_itemsData)
            {
                if (itemsData.Value.Enabled)
                    numEnabled++;
            }

            builder.Items = new MyObjectBuilder_EnvironmentItems.MyOBEnvironmentItemData[numEnabled];

            int insertIndex = 0;
            foreach (var itemsData in m_itemsData)
            {
                if (!itemsData.Value.Enabled)
                    continue;

                builder.Items[insertIndex].SubtypeName = itemsData.Value.SubtypeId.ToString();
                builder.Items[insertIndex].PositionAndOrientation = new MyPositionAndOrientation(itemsData.Value.Transform.TransformMatrix);
                insertIndex++;
            }

            builder.CellsOffset = CellsOffset;

            return builder;
        }

        /// <summary>
        /// Spawn Environment Items instance (e.g. forest) object which can be then used for spawning individual items (e.g. trees).
        /// </summary>
        public static MyEnvironmentItemsSpawnData BeginSpawn(MyEnvironmentItemsDefinition itemsDefinition, bool addToScene = true, long withEntityId = 0)
        {
            var builder = MyObjectBuilderSerializer.CreateNewObject(itemsDefinition.Id.TypeId, itemsDefinition.Id.SubtypeName) as MyObjectBuilder_EnvironmentItems;
            builder.EntityId = withEntityId;
            builder.PersistentFlags |= MyPersistentEntityFlags2.Enabled | (addToScene ? MyPersistentEntityFlags2.InScene : 0)| MyPersistentEntityFlags2.CastShadows;

            MyEnvironmentItems envItems;
            
            if(addToScene)
                envItems = MyEntities.CreateFromObjectBuilderAndAdd(builder) as MyEnvironmentItems;
            else
                envItems = MyEntities.CreateFromObjectBuilder(builder) as MyEnvironmentItems;


            MyEnvironmentItemsSpawnData spawnData = new MyEnvironmentItemsSpawnData();
            spawnData.EnvironmentItems = envItems;
            return spawnData;
        }

        /// <summary>
        /// Spawn environment item with the definition subtype on world position.
        /// </summary>
        public static bool SpawnItem(MyEnvironmentItemsSpawnData spawnData, MyEnvironmentItemDefinition itemDefinition, Vector3D position, Vector3D up, int userdata = -1, bool silentOverlaps = true)
        {
            if (!MyFakes.ENABLE_ENVIRONMENT_ITEMS)
                return true;

            Debug.Assert(spawnData != null && spawnData.EnvironmentItems != null);
            //Debug.Assert(itemDefinition != null);
            if (spawnData == null || spawnData.EnvironmentItems == null || itemDefinition == null)
            {
                return false;
            }

            Vector3D forward = MyUtils.GetRandomPerpendicularVector(ref up);
            MatrixD worldMatrix = MatrixD.CreateWorld(position, forward, up);


            return spawnData.EnvironmentItems.AddItem(itemDefinition, ref worldMatrix, ref spawnData.AabbWorld, userdata, silentOverlaps);
        }

        /// <summary>
        /// Ends spawning - finishes preparetion of items data.
        /// </summary>
        public static void EndSpawn(MyEnvironmentItemsSpawnData spawnData, bool updateGraphics = true, bool updatePhysics = true)
        {
            if (updatePhysics)
            {
                ProfilerShort.Begin("prepare physics");
                spawnData.EnvironmentItems.PrepareItemsPhysics(spawnData);
                spawnData.SubtypeToShapes.Clear();
                ProfilerShort.End();

                ProfilerShort.Begin("remove reference");
                foreach (var pair in spawnData.SubtypeToShapes)
                {
                    pair.Value.RemoveReference();
                }
                spawnData.SubtypeToShapes.Clear();
                
                ProfilerShort.End();
            }

            if (updateGraphics)
            {
                spawnData.EnvironmentItems.PrepareItemsGraphics();
            }
           
            ProfilerShort.Begin("prunning");
            
			spawnData.EnvironmentItems.UpdateGamePruningStructure();
            ProfilerShort.End();
        }

        public void UnloadGraphics()
        {
            foreach (var sector in m_sectors)
            {
                sector.Value.UnloadRenderObjects();
            }
        }

        public void ClosePhysics(MyEnvironmentItemsSpawnData data)
        {
            if (Physics != null)
            {
                Physics.Close();
                Physics = null;
            }
        }

        public static string GetModelName(MyStringHash itemSubtype)
        {
            int modelId = GetModelId(itemSubtype);
            return MyModel.GetById(modelId);
        }

        public static int GetModelId(MyStringHash subtypeId)
        {
            return m_subtypeToModels[subtypeId];
        }

        /// <summary>
        /// Adds environment item to internal collections. Creates render and physics data. 
        /// </summary>
        /// <returns>True if successfully added, otherwise false.</returns>
        private bool AddItem(
            MyEnvironmentItemDefinition itemDefinition, 
            ref MatrixD worldMatrix, 
            ref BoundingBoxD aabbWorld, int userData = -1, bool silentOverlaps = false)
        {
            if (!MyFakes.ENABLE_ENVIRONMENT_ITEMS)
                return true;

            Debug.Assert(m_definition.ContainsItemDefinition(itemDefinition),
                String.Format("Environment item with definition '{0}' not found in class '{1}'", itemDefinition.Id, m_definition.Id));
            if (!m_definition.ContainsItemDefinition(itemDefinition))
            {
                return false;
            }

            if (itemDefinition.Model == null)
                return false;

            //MyDefinitionId defId = new MyDefinitionId(envItemObjectBuilderType, subtypeId.ToString());
            int modelId = MyEnvironmentItems.GetModelId(itemDefinition.Id.SubtypeId);
            string modelName = MyModel.GetById(modelId);

            MyModel model = VRage.Game.Models.MyModels.GetModelOnlyData(modelName);
            if (model == null)
            {
                //Debug.Fail(String.Format("Environment item model of '{0}' not found, skipping the item...", itemDefinition.Id));
                return false;
            }

            CheckModelConsistency(itemDefinition);

            int localId = worldMatrix.Translation.GetHashCode();

            if (m_itemsData.ContainsKey(localId))
            {
                if (!silentOverlaps)
                {
                    Debug.Fail("More items on same place! " + worldMatrix.Translation.ToString());
                    MyLog.Default.WriteLine("WARNING: items are on the same place.");
                }
                return false;
            }

            MyEnvironmentItemData data = new MyEnvironmentItemData()
            {
                Id = localId,
                SubtypeId = itemDefinition.Id.SubtypeId,
                Transform = new MyTransformD(ref worldMatrix),
                Enabled = true,
                SectorInstanceId = -1,
                Model = model,
                UserData = userData
            };

            //Preload split planes
            //VRageRender.MyRenderProxy.PreloadMaterials(model.AssetName); 

            aabbWorld.Include(model.BoundingBox.Transform(worldMatrix));

            MatrixD transform = data.Transform.TransformMatrix;
            float sectorSize = MyFakes.ENVIRONMENT_ITEMS_ONE_INSTANCEBUFFER ? 20000 : m_definition.SectorSize;

            Vector3I sectorId = MyEnvironmentSector.GetSectorId(transform.Translation - CellsOffset, sectorSize);
            MyEnvironmentSector sector;
            if (!m_sectors.TryGetValue(sectorId, out sector))
            {
                sector = new MyEnvironmentSector(sectorId, sectorId * sectorSize + CellsOffset);
                m_sectors.Add(sectorId, sector);
            }

            // Adds instance of the given model. Local matrix specified might be changed internally in renderer.

            MatrixD sectorOffsetInv = MatrixD.CreateTranslation(-sectorId * sectorSize - CellsOffset);
            Matrix transformL = (Matrix)(data.Transform.TransformMatrix * sectorOffsetInv);

            Color baseColor = BaseColor;
            if (ColorSpread.LengthSquared() > 0)
            {
                float amountLighten = MyUtils.GetRandomFloat(0.0f, ColorSpread.X);
                float amountDarker = MyUtils.GetRandomFloat(0.0f, ColorSpread.Y);
                baseColor = MyUtils.GetRandomSign() > 0 ? Color.Lighten(baseColor, amountLighten) : Color.Darken(baseColor, amountDarker);
            }

            Vector3 hsv = baseColor.ColorToHSVDX11();


            data.SectorInstanceId = sector.AddInstance(itemDefinition.Id.SubtypeId, modelId, localId, ref transformL, model.BoundingBox, m_instanceFlags, m_definition.MaxViewDistance, hsv);
            data.Transform = new MyTransformD(transform);
            m_itemsData.Add(localId, data);

            if (ItemAdded != null)
            {
                ItemAdded(this,
                    new ItemInfo()
                    {
                        LocalId = localId,
                        SubtypeId = data.SubtypeId,
                        Transform = data.Transform,
                    });
            }

            return true;
        }

        private static void CheckModelConsistency(MyEnvironmentItemDefinition itemDefinition)
        {
            int savedModelId;
            if (m_subtypeToModels.TryGetValue(itemDefinition.Id.SubtypeId, out savedModelId))
            {
                Debug.Assert(savedModelId == MyModel.GetId(itemDefinition.Model), "Environment item subtype id maps to a different model id than it used to!");
            }
            else
            {
                if (itemDefinition.Model != null)
                    m_subtypeToModels.Add(itemDefinition.Id.SubtypeId, MyModel.GetId(itemDefinition.Model));
            }
        }

        public void PrepareItemsGraphics()
        {
            foreach (var pair in m_sectors)
            {
                pair.Value.UpdateRenderInstanceData();
                pair.Value.UpdateRenderEntitiesData(WorldMatrix);
            }
        }

        public void PrepareItemsPhysics(MyEnvironmentItemsSpawnData spawnData)
        {
            spawnData.SectorRootShape = new HkStaticCompoundShape(HkReferencePolicy.None);
            spawnData.EnvironmentItems.PrepareItemsPhysics(spawnData.SectorRootShape, ref spawnData.AabbWorld, spawnData.SubtypeToShapes);
        }

        /// <summary>
        /// Prepares data for renderer and physics. Must be called after all items has been added.
        /// </summary>
      
        private void PrepareItemsPhysics(HkStaticCompoundShape sectorRootShape, ref BoundingBoxD aabbWorld, Dictionary<MyStringHash, HkShape> subtypeIdToShape)
        {
            foreach (var item in m_itemsData)
            {
                if (!item.Value.Enabled)
                    continue;

                int physicsShapeInstanceId;
                MatrixD transform = item.Value.Transform.TransformMatrix;
                if (AddPhysicsShape(item.Value.SubtypeId, item.Value.Model, ref transform, sectorRootShape, subtypeIdToShape, out physicsShapeInstanceId))
                {
                    // Map to data index - note that itemData is added after this to its list!
                    m_physicsShapeInstanceIdToLocalId[physicsShapeInstanceId] = item.Value.Id;
                    m_localIdToPhysicsShapeInstanceId[item.Value.Id] = physicsShapeInstanceId;

                }
            }

            PositionComp.WorldAABB = aabbWorld;

            if (sectorRootShape.InstanceCount > 0)
            {
                Debug.Assert(m_physicsShapeInstanceIdToLocalId.Count > 0);

                Physics = new Sandbox.Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_STATIC)
                {
                    MaterialType = m_definition.Material,
                    AngularDamping = MyPerGameSettings.DefaultAngularDamping,
                    LinearDamping = MyPerGameSettings.DefaultLinearDamping,
                    IsStaticForCluster = true,
                };

                sectorRootShape.Bake();
                HkMassProperties massProperties = new HkMassProperties();
                MatrixD matrix = MatrixD.CreateTranslation(CellsOffset);
                Physics.CreateFromCollisionObject((HkShape)sectorRootShape, Vector3.Zero, matrix, massProperties);

                // Only the server handles tree destructipon, the client will bounce off
                if (Sync.IsServer)
                {
                    Physics.ContactPointCallback += Physics_ContactPointCallback;
                    Physics.RigidBody.ContactPointCallbackEnabled = true;
                }

                Physics.Enabled = true;
            }           
        }

        public bool IsValidPosition(Vector3D position)
        {
            return !m_itemsData.ContainsKey(position.GetHashCode());
        }

        public void BeginBatch(bool sync)
        {
            Debug.Assert(!IsBatching);
            m_batchTime = BATCH_DEFAULT_TIME;
            if (sync)
                MySyncEnvironmentItems.SendBeginBatchAddMessage(EntityId);
        }

        public void BatchAddItem(Vector3D position, MyStringHash subtypeId, bool sync)
        {
            Debug.Assert(IsBatching);
            Debug.Assert(m_definition.ContainsItemDefinition(subtypeId));
            if (!m_definition.ContainsItemDefinition(subtypeId)) return;

            m_batchedAddItems.Add(new AddItemData() { Position = position, SubtypeId = subtypeId });

            if (sync)
                MySyncEnvironmentItems.SendBatchAddItemMessage(EntityId, position, subtypeId);
        }

        public void BatchModifyItem(int localId, MyStringHash subtypeId, bool sync)
        {
            Debug.Assert(IsBatching);
            Debug.Assert(m_itemsData.ContainsKey(localId));
            if (!m_itemsData.ContainsKey(localId)) return;

            m_batchedModifyItems.Add(new ModifyItemData() { LocalId = localId, SubtypeId = subtypeId });

            if (sync)
                MySyncEnvironmentItems.SendBatchModifyItemMessage(EntityId, localId, subtypeId);
        }

        public void BatchRemoveItem(int localId, bool sync)
        {
            Debug.Assert(IsBatching);
            Debug.Assert(m_itemsData.ContainsKey(localId));
            if (!m_itemsData.ContainsKey(localId)) return;

            m_batchedRemoveItems.Add(new RemoveItemData() { LocalId = localId });

            if (sync)
                MySyncEnvironmentItems.SendBatchRemoveItemMessage(EntityId, localId);
        }

        public void EndBatch(bool sync)
        {
            m_batchTime = 0;

            if (m_batchedAddItems.Count > 0 || m_batchedModifyItems.Count > 0 || m_batchedRemoveItems.Count > 0)
                ProcessBatch();

            m_batchedAddItems.Clear();
            m_batchedModifyItems.Clear();
            m_batchedRemoveItems.Clear();

            if (sync)
                MySyncEnvironmentItems.SendEndBatchAddMessage(EntityId);
        }

        private void ProcessBatch()
        {
            foreach (var removedItem in m_batchedRemoveItems)
                RemoveItem(removedItem.LocalId, false, false);

            foreach (var modifyModel in m_batchedModifyItems)
                ModifyItemModel(modifyModel.LocalId, modifyModel.SubtypeId, false, false);

            if (Physics != null)
            {
                if(Sync.IsServer)
                    Physics.ContactPointCallback -= Physics_ContactPointCallback;
                Physics.Close();
                Physics = null;
            }

            BoundingBoxD aabbWorld = BoundingBoxD.CreateInvalid();
            Dictionary<MyStringHash, HkShape> subtypeIdToShape = new Dictionary<MyStringHash, HkShape>(MyStringHash.Comparer);
            HkStaticCompoundShape sectorRootShape = new HkStaticCompoundShape(HkReferencePolicy.None);

            m_physicsShapeInstanceIdToLocalId.Clear();
            m_localIdToPhysicsShapeInstanceId.Clear();

            foreach (var item in m_itemsData)
            {
                if (!item.Value.Enabled)
                    continue;
                int modelId = m_subtypeToModels[item.Value.SubtypeId];
                MyModel model = VRage.Game.Models.MyModels.GetModelOnlyData(MyModel.GetById(modelId));
                var matrix = item.Value.Transform.TransformMatrix;
                aabbWorld.Include(model.BoundingBox.Transform(matrix));
            }

            foreach (var item in m_batchedAddItems)
            {
                var matrix = MatrixD.CreateWorld(item.Position, Vector3D.Forward, Vector3D.Up);
                var definition = m_definition.GetItemDefinition(item.SubtypeId);
                AddItem(definition, ref matrix, ref aabbWorld);
            }

            PrepareItemsPhysics(sectorRootShape, ref aabbWorld,subtypeIdToShape);
            PrepareItemsGraphics();

            foreach (var pair in subtypeIdToShape)
            {
                pair.Value.RemoveReference();
            }

            subtypeIdToShape.Clear();
        }

        public bool ModifyItemModel(int itemInstanceId, MyStringHash newSubtypeId, bool updateSector, bool sync)
        {
            MyEnvironmentItemData data;
            if (!m_itemsData.TryGetValue(itemInstanceId, out data))
            {
                Debug.Assert(false, "Item instance not found.");
                return false;
            }

            int modelId = GetModelId(data.SubtypeId);
            int newModelId = GetModelId(newSubtypeId);
            if (data.Enabled)
            {
                Matrix matrix = data.Transform.TransformMatrix;

                var sectorId = MyEnvironmentSector.GetSectorId(matrix.Translation - CellsOffset, Definition.SectorSize);
                MyModel modelData = VRage.Game.Models.MyModels.GetModelOnlyData(MyModel.GetById(modelId));
                var sector = Sectors[sectorId];

                Matrix invOffset = Matrix.Invert(sector.SectorMatrix);
                matrix = matrix * invOffset;

                sector.DisableInstance(data.SectorInstanceId, modelId);
                int newSectorInstanceId = sector.AddInstance(newSubtypeId, newModelId, itemInstanceId, ref matrix, modelData.BoundingBox, m_instanceFlags, m_definition.MaxViewDistance);

                data.SubtypeId = newSubtypeId;
                data.SectorInstanceId = newSectorInstanceId;
                m_itemsData[itemInstanceId] = data;

                if (updateSector)
                {
                    sector.UpdateRenderInstanceData();
                    sector.UpdateRenderEntitiesData(WorldMatrix);
                }

                if (ItemModified != null)
                {
                    ItemModified(this,
                        new ItemInfo()
                        {
                            LocalId = data.Id,
                            SubtypeId = data.SubtypeId,
                            Transform = data.Transform
                        });
                }

                if (sync)
                {
                    MySyncEnvironmentItems.SendModifyModelMessage(EntityId, itemInstanceId, newSubtypeId);
                }
            }

            return true;
        }

		public bool TryGetItemInfoById(int itemId, out ItemInfo result)
		{
			result = new ItemInfo();
			MyEnvironmentItemData data;
			if (m_itemsData.TryGetValue(itemId, out data))
			{
				if (data.Enabled)
				{
					result = new ItemInfo() { LocalId = itemId, SubtypeId = data.SubtypeId, Transform = data.Transform };
					return true;
				}
			}
			return false;
		}

        public void GetPhysicalItemsInRadius(Vector3D position, float radius, List<ItemInfo> result)
        {
            double radiusSq = radius * radius;
            if (this.Physics != null && this.Physics.RigidBody != null)
            {
                // CH: This iterates through all the child shapes and test their position, but there's currently no better way.
                HkStaticCompoundShape shape = (HkStaticCompoundShape)Physics.RigidBody.GetShape();
                HkShapeContainerIterator it = shape.GetIterator();
                while (it.IsValid)
                {
                    uint shapeKey = it.CurrentShapeKey;
                    int physicsInstanceId, localId;
                    uint childKey;
                    shape.DecomposeShapeKey(shapeKey, out physicsInstanceId, out childKey);
                    if (m_physicsShapeInstanceIdToLocalId.TryGetValue(physicsInstanceId, out localId))
                    {
                        MyEnvironmentItemData data;
                        if (m_itemsData.TryGetValue(localId, out data))
                        {
                            if (data.Enabled && Vector3D.DistanceSquared(data.Transform.Position, position) < radiusSq)
                            {
                                result.Add(new ItemInfo() { LocalId = localId, SubtypeId = data.SubtypeId, Transform = data.Transform });
                            }
                        }
                    }

                    it.Next();
                }
            }
        }

        public void GetAllItemsInRadius(Vector3D point, float radius, List<ItemInfo> output)
        {
            GetSectorsInRadius(point, radius, m_tmpSectors);
            foreach (var sector in m_tmpSectors)
            {
                sector.GetItemsInRadius(point, radius, output);
            }
            m_tmpSectors.Clear();
        }

        public void GetItemsInSector(Vector3I sectorId, List<ItemInfo> output)
        {
            if (!m_sectors.ContainsKey(sectorId))
                return;

            m_sectors[sectorId].GetItems(output);
        }

        public int GetItemsCount(MyStringHash id)
        {
            int counter = 0;
            foreach (var item in m_itemsData)
            {
                if (item.Value.SubtypeId == id)
                    ++counter;
            }
            return counter;
        }

        public void GetSectorsInRadius(Vector3D position, float radius, List<MyEnvironmentSector> sectors)
        {
            foreach (var sector in m_sectors)
            {
                if (sector.Value.IsValid)
                {
                    var sectorBox = sector.Value.SectorWorldBox;
                    sectorBox.Inflate(radius);
                    if (sectorBox.Contains(position) == ContainmentType.Contains)
                        sectors.Add(sector.Value);
                }
            }
        }

        public void GetSectorIdsInRadius(Vector3D position, float radius, List<Vector3I> sectorIds)
        {
            foreach (var sector in m_sectors)
            {
                if (sector.Value.IsValid)
                {
                    var sectorBox = sector.Value.SectorWorldBox;
                    sectorBox.Inflate(radius);
                    if (sectorBox.Contains(position) == ContainmentType.Contains)
                        sectorIds.Add(sector.Key);
                }
            }
        }

        public void RemoveItemsAroundPoint(Vector3D point, double radius)
        {
            double radiusSq = radius * radius;
            if (this.Physics != null && this.Physics.RigidBody != null)
            {
                // CH: This iterates through all the child shapes and test their position, but there's currently no better way.
                HkStaticCompoundShape shape = (HkStaticCompoundShape)Physics.RigidBody.GetShape();
                HkShapeContainerIterator it = shape.GetIterator();
                while (it.IsValid)
                {
                    uint shapeKey = it.CurrentShapeKey;
                    int physicsInstanceId;
                    uint childKey;
                    shape.DecomposeShapeKey(shapeKey, out physicsInstanceId, out childKey);
                    int instanceId;
                    if (m_physicsShapeInstanceIdToLocalId.TryGetValue(physicsInstanceId, out instanceId))
                    {
                        if (DisableRenderInstanceIfInRadius(point, radiusSq, instanceId, hasPhysics: true))
                        {
                            shape.EnableInstance(physicsInstanceId, false);
                            m_tmpToDisable.Add(instanceId);
                        }
                    }
                    it.Next();
                }
            }
            else
            {
                foreach (var itemsData in m_itemsData)
                {
                    if (itemsData.Value.Enabled == false)
                        continue;

                    //If itemsData has physical representation
                    //if (m_localIdToPhysicsShapeInstanceId.ContainsKey(itemsData.Key))
                    if (DisableRenderInstanceIfInRadius(point, radiusSq, itemsData.Key))
                        m_tmpToDisable.Add(itemsData.Key);
                }
            }

            foreach(var key in m_tmpToDisable)
            {
                var data = m_itemsData[key];
                data.Enabled = false;
                m_itemsData[key] = data;
            }

            m_tmpToDisable.Clear();

            foreach (var sector in m_updatedSectorsTmp)
            {
                Sectors[sector].UpdateRenderInstanceData();
            }

            m_updatedSectorsTmp.Clear();
        }

        public bool RemoveItem(int itemInstanceId, bool sync, bool immediateUpdate = true)
        {
            int physicsInstanceId;

            if (m_localIdToPhysicsShapeInstanceId.TryGetValue(itemInstanceId, out physicsInstanceId))
            {
                return RemoveItem(itemInstanceId, physicsInstanceId, sync, immediateUpdate);
            }
            else if (m_itemsData.ContainsKey(itemInstanceId))
            {
                return RemoveNonPhysicalItem(itemInstanceId, sync, immediateUpdate);
            }

            return false;
        }

        protected bool RemoveItem(int itemInstanceId, int physicsInstanceId, bool sync, bool immediateUpdate)
        {
            //Debug.Assert(sync == false || Sync.IsServer, "Synchronizing env. item removal from the client is forbidden!"); let it sync for planets
            Debug.Assert(physicsInstanceId == -1 || m_physicsShapeInstanceIdToLocalId.ContainsKey(physicsInstanceId), "Could not find env. item shape!");
            Debug.Assert(physicsInstanceId == -1 || m_localIdToPhysicsShapeInstanceId.ContainsKey(itemInstanceId), "Could not find env. item instance!");

            m_physicsShapeInstanceIdToLocalId.Remove(physicsInstanceId);
            m_localIdToPhysicsShapeInstanceId.Remove(itemInstanceId);

            if (!m_itemsData.ContainsKey(itemInstanceId)) return false;
            
            MyEnvironmentItemData itemData = m_itemsData[itemInstanceId];
            m_itemsData.Remove(itemInstanceId);

            if (Physics != null)
            {
                HkStaticCompoundShape shape = (HkStaticCompoundShape) Physics.RigidBody.GetShape();
                shape.EnableInstance(physicsInstanceId, false);
            }

            Matrix matrix = itemData.Transform.TransformMatrix;
            var sectorId = MyEnvironmentSector.GetSectorId(matrix.Translation - m_cellsOffset, Definition.SectorSize);

            var modelId = GetModelId(itemData.SubtypeId);

            Debug.Assert(Sectors.ContainsKey(sectorId));

            bool disabled = false;

            MyEnvironmentSector sector;
            if (Sectors.TryGetValue(sectorId, out sector))
            {
                disabled = sector.DisableInstance(itemData.SectorInstanceId, modelId);
            }

            //TODO: find a better way to fix the instanceId
            foreach(var item in m_itemsData)
            {
                if(item.Value.SectorInstanceId == Sectors[sectorId].SectorItemCount)
                {
                    var data = item.Value;
                    data.SectorInstanceId = itemData.SectorInstanceId;
                    m_itemsData[item.Key] = data;
                    break;
                }
            }

            Debug.Assert(disabled, "Env. item instance render not disabled");

            if (immediateUpdate && sector != null)
                sector.UpdateRenderInstanceData(modelId);

            OnRemoveItem(itemInstanceId, ref matrix, itemData.SubtypeId, itemData.UserData);

            if (sync)
            {
                MySyncEnvironmentItems.RemoveEnvironmentItem(EntityId, itemInstanceId);
            }
            return true;
        }

        protected bool RemoveNonPhysicalItem(int itemInstanceId, bool sync, bool immediateUpdate)
        {
            Debug.Assert(sync == false || Sync.IsServer, "Synchronizing env. item removal from the client is forbidden!");
            Debug.Assert(m_itemsData.ContainsKey(itemInstanceId), "Could not find env. item shape!");

            MyEnvironmentItemData itemData = m_itemsData[itemInstanceId];
            itemData.Enabled = false;
            m_itemsData[itemInstanceId] = itemData;

            Matrix matrix = itemData.Transform.TransformMatrix;
            var sectorId = MyEnvironmentSector.GetSectorId(matrix.Translation, Definition.SectorSize);

            var modelId = GetModelId(itemData.SubtypeId);

            var disabled = Sectors[sectorId].DisableInstance(itemData.SectorInstanceId, modelId);
            //Debug.Assert(disabled, "Env. item instance render not disabled");

            if (immediateUpdate)
                Sectors[sectorId].UpdateRenderInstanceData(modelId);

            OnRemoveItem(itemInstanceId, ref matrix, itemData.SubtypeId, itemData.UserData);

            if (sync)
            {
                MySyncEnvironmentItems.RemoveEnvironmentItem(EntityId, itemInstanceId);
            }

            return true;
        }

        public void RemoveItemsOfSubtype(HashSet<MyStringHash> subtypes)
        {
            BeginBatch(true);

            List<int> keys = new List<int>(m_itemsData.Keys);
            foreach (var key in keys)
            {
                var itemData = m_itemsData[key];
                if (!itemData.Enabled)
                    continue;
                if (subtypes.Contains(itemData.SubtypeId))
                {
                    BatchRemoveItem(key, true);
                }
            }

            EndBatch(true);
        }

        protected virtual void OnRemoveItem(int localId, ref Matrix matrix, MyStringHash myStringId, int userData)
        {
            if (ItemRemoved != null)
            {
                ItemRemoved(this,
                    new ItemInfo()
                    {
                        LocalId = localId,
                        SubtypeId = myStringId,
                        Transform = new MyTransformD(matrix),
                        UserData = userData
                    });
            }
        }

        private bool DisableRenderInstanceIfInRadius(Vector3D center, double radiusSq, int itemInstanceId, bool hasPhysics = false)
        {
            MyEnvironmentItemData itemData = m_itemsData[itemInstanceId];
            Vector3 translation = itemData.Transform.Position;
            if (Vector3D.DistanceSquared(new Vector3D(translation), center) <= radiusSq)
            {
                int physicsInstanceId;
                bool itemRemoved = false;
                if (m_localIdToPhysicsShapeInstanceId.TryGetValue(itemInstanceId, out physicsInstanceId))
                {
                    m_physicsShapeInstanceIdToLocalId.Remove(physicsInstanceId);
                    m_localIdToPhysicsShapeInstanceId.Remove(itemInstanceId);
                    itemRemoved = true;
                }

                if (!hasPhysics || itemRemoved)
                {
                    Matrix matrix = itemData.Transform.TransformMatrix;
                    Vector3I sectorId = MyEnvironmentSector.GetSectorId(matrix.Translation - m_cellsOffset, m_definition.SectorSize);
                    MyEnvironmentSector sector;
                    if (Sectors.TryGetValue(sectorId, out sector))
                    {
                        bool disabled = Sectors[sectorId].DisableInstance(itemData.SectorInstanceId, GetModelId(itemData.SubtypeId));
                        Debug.Assert(disabled, "Env. item render not disabled");

                        if (disabled)
                        {
                            m_updatedSectorsTmp.Add(sectorId);
                            //itemData.Enabled = false;
                            //m_itemsData[itemInstanceId] = itemData;
                        }
                    }
                    else
                        Debug.Fail("Missing sector!");
                    return true;
                }
            }
            return false;
        }

        /// Default implementation does nothing. If you want env. items to react to damage, subclass this
        public virtual void DoDamage(float damage, int instanceId, Vector3D position, Vector3 normal, MyStringHash type) { }

        void Physics_ContactPointCallback(ref MyPhysics.MyContactPointEvent e)
        {
            var vel = Math.Abs(e.ContactPointEvent.SeparatingVelocity);
            var other = e.ContactPointEvent.GetOtherEntity(this);

            if (other == null || other.Physics == null || other is MyFloatingObject) return;

            if (other is IMyHandheldGunObject<MyDeviceBase>) return;

            // Prevent debris from breaking trees.
            // Debris flies in unpredictable ways and this could cause out of sync tree destruction which would is bad.
            if (other.Physics.RigidBody != null && other.Physics.RigidBody.Layer == MyPhysics.CollisionLayers.DebrisCollisionLayer) return;

            // On objects held in manipulation tool, Havok returns high velocities, after this contact is fired by contraint solver..
            // Therefore we disable damage from objects connected by constraint to character
            if (MyManipulationTool.IsEntityManipulated(other as MyEntity))
            {
                return;
            }

            float otherMass = MyDestructionHelper.MassFromHavok(other.Physics.Mass);

            if (other is Character.MyCharacter)
                otherMass = other.Physics.Mass;

            double impactEnergy = vel*vel*otherMass;

            // TODO: per item max impact energy
            if (impactEnergy > 200000)
            {
                int bodyId = 0;
                var normal = e.ContactPointEvent.ContactPoint.Normal;
                if (e.ContactPointEvent.Base.BodyA.GetEntity(0) != this)
                {
                    bodyId = 1;
                    normal *= -1;
                }
                var shapeKey = e.ContactPointEvent.GetShapeKey(bodyId);
                if (shapeKey == uint.MaxValue) //jn: TODO find out why this happens, there is ticket for it https://app.asana.com/0/9887996365574/26645443970236
                {
                    return;
                }

                HkStaticCompoundShape shape = (HkStaticCompoundShape) Physics.RigidBody.GetShape();
                int physicsInstanceId;
                uint childKey;

                shape.DecomposeShapeKey(shapeKey, out physicsInstanceId, out childKey);
                int itemInstanceId;
                if (m_physicsShapeInstanceIdToLocalId.TryGetValue(physicsInstanceId, out itemInstanceId))
                {
                    var position = Physics.ClusterToWorld(e.ContactPointEvent.ContactPoint.Position);

                    DestroyItemAndCreateDebris(position, normal, impactEnergy, itemInstanceId);
                }
            }
        }

        public void DestroyItemAndCreateDebris(Vector3D position, Vector3 normal, double energy, int itemId)
        {
            if (MyPerGameSettings.Destruction)
                DoDamage(100.0f, itemId, position, normal, MyStringHash.NullOrEmpty);
            else
            {
                var debri = DestroyItem(itemId);
                if (debri != null && debri.Physics != null)
                {
                    MyParticleEffect effect;
                    if (MyParticlesManager.TryCreateParticleEffect((int) MyParticleEffectsIDEnum.DestructionTree, out effect))
                    {
                        effect.WorldMatrix = MatrixD.CreateTranslation(position); //, (Vector3D)normal, Vector3D.CalculatePerpendicularVector(normal));
                    }

                    var treeMass = debri.Physics.Mass;

                    const float ENERGY_PRESERVATION = .8f;

                    // Tree final velocity
                    float velTree = (float) Math.Sqrt(energy/treeMass);
                    float accell = velTree/(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS*MyFakes.SIMULATION_SPEED)*ENERGY_PRESERVATION;
                    var force = accell*normal;

                    var pos = debri.Physics.CenterOfMassWorld + 0.5f*Vector3D.Dot(position - debri.Physics.CenterOfMassWorld, debri.WorldMatrix.Up)*debri.WorldMatrix.Up;
                    debri.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, force, pos, null);
                    Debug.Assert(debri.GetPhysicsBody().HavokWorld.ActiveRigidBodies.Contains(debri.Physics.RigidBody));
                }
            }
        }

        protected virtual MyEntity DestroyItem(int itemInstanceId)
        {
            return null;
        }


        void DestructionBody_AfterReplaceBody(ref HkdReplaceBodyEvent e)
        {
            e.GetNewBodies(m_tmpBodyInfos);
            foreach (var b in m_tmpBodyInfos)
            {
                var m = b.Body.GetRigidBody().GetRigidBodyMatrix();
                var t = m.Translation;
                var o = Quaternion.CreateFromRotationMatrix(m.GetOrientation());
                Physics.HavokWorld.GetPenetrationsShape(b.Body.BreakableShape.GetShape(), ref t, ref o, m_tmpResults, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                foreach (var res in m_tmpResults)
                {
                    if (res.GetCollisionEntity() is MyVoxelMap)
                    {
                        b.Body.GetRigidBody().Quality = HkCollidableQualityType.Fixed;
                        break;
                    }
                }

                m_tmpResults.Clear();
                b.Body.GetRigidBody();
                b.Body.Dispose();
            }
        }

        /// <summary>
        /// Adds item physics shape to rootShape and returns instance id of added shape instance.
        /// </summary>
        /// <returns>true if ite physics shape has been added, otherwise false.</returns>
        private bool AddPhysicsShape(MyStringHash subtypeId, MyModel model, ref MatrixD worldMatrix, HkStaticCompoundShape sectorRootShape,
            Dictionary<MyStringHash, HkShape> subtypeIdToShape, out int physicsShapeInstanceId)
        {
            physicsShapeInstanceId = 0;

            HkShape physicsShape;
            if (!subtypeIdToShape.TryGetValue(subtypeId, out physicsShape))
            {
                HkShape[] shapes = model.HavokCollisionShapes;
                if (shapes == null || shapes.Length == 0)
                    return false;

                Debug.Assert(shapes.Length == 1);

                //List<HkShape> listShapes = new List<HkShape>();
                //for (int i = 0; i < shapes.Length; i++)
                //{
                //    listShapes.Add(shapes[i]);
                //    HkShape.SetUserData(shapes[i], shapes[i].UserData | (int)HkShapeUserDataFlags.EnvironmentItem);
                //}

                //physicsShape = new HkListShape(listShapes.GetInternalArray(), listShapes.Count, HkReferencePolicy.None);
                //HkShape.SetUserData(physicsShape, physicsShape.UserData | (int)HkShapeUserDataFlags.EnvironmentItem);
                physicsShape = shapes[0];
                physicsShape.AddReference();
                subtypeIdToShape[subtypeId] = physicsShape;
            }

            if (physicsShape.ReferenceCount != 0)
            {
                Matrix localMatrix = worldMatrix * MatrixD.CreateTranslation(-CellsOffset);
                physicsShapeInstanceId = sectorRootShape.AddInstance(physicsShape, localMatrix);
                Debug.Assert(physicsShapeInstanceId >= 0 && physicsShapeInstanceId < int.MaxValue, "Shape key space overflow");
                return true;
            }
            else
            {
                return false;
            }
        }

        public void GetItems(ref Vector3D point, List<Vector3D> output)
        {
            Vector3I sectorId = MyEnvironmentSector.GetSectorId(point, m_definition.SectorSize);
            MyEnvironmentSector sector = null;
            if (m_sectors.TryGetValue(sectorId, out sector))
            {
                sector.GetItems(output);
            }
        }

        public void GetItemsInRadius(ref Vector3D point, float radius, List<Vector3D> output)
        {
            Vector3I sectorId = MyEnvironmentSector.GetSectorId(point, m_definition.SectorSize);
            MyEnvironmentSector sector = null;
            if (m_sectors.TryGetValue(sectorId, out sector))
            {
                sector.GetItemsInRadius(point, radius, output);
            }
        }

        public bool HasItem(int localId)
        {
            return m_itemsData.ContainsKey(localId) && m_itemsData[localId].Enabled;
        }

        public void GetAllItems(List<ItemInfo> output)
        {
            foreach (var sector in m_sectors)
            {
                sector.Value.GetItems(output);
            }
        }

        public void GetItemsInSector(ref Vector3D point, List<ItemInfo> output)
        {
            Vector3I sectorId = MyEnvironmentSector.GetSectorId(point, m_definition.SectorSize);
            MyEnvironmentSector sector = null;
            if (m_sectors.TryGetValue(sectorId, out sector))
            {
                sector.GetItems(output);
            }
        }

        public MyEnvironmentSector GetSector(ref Vector3D worldPosition)
        {
            Vector3I sectorId = MyEnvironmentSector.GetSectorId(worldPosition, m_definition.SectorSize);
            MyEnvironmentSector sector = null;
            if (m_sectors.TryGetValue(sectorId, out sector))
            {
                return sector;
            }

            return null;
        }

        public MyEnvironmentSector GetSector(ref Vector3I sectorId)
        {
            MyEnvironmentSector sector = null;
            if (m_sectors.TryGetValue(sectorId, out sector))
            {
                return sector;
            }

            return null;
        }

        public Vector3I GetSectorId(ref Vector3D worldPosition)
        {
            return MyEnvironmentSector.GetSectorId(worldPosition, m_definition.SectorSize);
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            if (Sync.IsServer && IsBatching)
            {
                m_batchTime -= 100 * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (m_batchTime <= 0)
                {
                    EndBatch(true);
                }

                var handler = BatchEnded;
                if (handler != null)
                    BatchEnded(this);
            }
        }

        protected override void ClampToWorld()
        {
            return;
        }

        public int GetItemInstanceId(uint shapeKey)
        {
            HkStaticCompoundShape shape = (HkStaticCompoundShape)Physics.RigidBody.GetShape();
            int physicsInstanceId;
            uint childKey;
            if (shapeKey == uint.MaxValue)
                return -1;

            shape.DecomposeShapeKey(shapeKey, out physicsInstanceId, out childKey);
            // Item instance id 
            int itemInstanceId;
            if (!m_physicsShapeInstanceIdToLocalId.TryGetValue(physicsInstanceId, out itemInstanceId))
                return -1;

            return itemInstanceId;
        }

        public bool IsItemEnabled(int localId)
        {
            return m_itemsData[localId].Enabled;
        }

        public MyStringHash GetItemSubtype(int localId)
        {
            return m_itemsData[localId].SubtypeId;
        }

        public MyEnvironmentItemDefinition GetItemDefinition(int itemInstanceId)
        {
            MyEnvironmentItemData itemData = m_itemsData[itemInstanceId];

            MyDefinitionId defId = new MyDefinitionId(m_definition.ItemDefinitionType, itemData.SubtypeId);
            return MyDefinitionManager.Static.GetEnvironmentItemDefinition(defId) as MyEnvironmentItemDefinition;
        }

        public MyEnvironmentItemDefinition GetItemDefinitionFromShapeKey(uint shapeKey)
        {
            int itemInstanceId = GetItemInstanceId(shapeKey);
            if (itemInstanceId == -1)
                return null;

            MyEnvironmentItemData itemData = m_itemsData[itemInstanceId];

            MyDefinitionId defId = new MyDefinitionId(m_definition.ItemDefinitionType, itemData.SubtypeId);
            return MyDefinitionManager.Static.GetEnvironmentItemDefinition(defId) as MyEnvironmentItemDefinition;
        }

        public bool GetItemWorldMatrix(int itemInstanceId, out MatrixD worldMatrix)
        {
            worldMatrix = MatrixD.Identity;

            MyEnvironmentItemData itemData = m_itemsData[itemInstanceId];

            worldMatrix = itemData.Transform.TransformMatrix;
            return true;
        }

        Vector3D m_cellsOffset;
        public Vector3D CellsOffset
        {
            set
            {
                m_cellsOffset = value;
                PositionComp.SetPosition(m_cellsOffset);
            }
            get
            {
                return m_cellsOffset;
            }
        }

        class MyEnviromentItemsDebugDraw : MyDebugRenderComponentBase
        {
            private MyEnvironmentItems m_items;
            public MyEnviromentItemsDebugDraw(MyEnvironmentItems items)
            {
                m_items = items;
            }

            public override void DebugDraw()
            {
                if (!MyDebugDrawSettings.DEBUG_DRAW_ENVIRONMENT_ITEMS)
                    return;

                foreach (var sec in m_items.Sectors)
                {
                    sec.Value.DebugDraw(sec.Key, m_items.m_definition.SectorSize);

                    if (sec.Value.IsValid)
                    {
                        var box = sec.Value.SectorBox;

                        var point = box.Center + sec.Value.SectorMatrix.Translation;

                        var dist = Vector3D.Distance(World.MySector.MainCamera.Position, point);
                        if (dist < 1000)
                            MyRenderProxy.DebugDrawText3D(point, m_items.Definition.Id.SubtypeName + " Sector: " + sec.Key, Color.SaddleBrown, 1.0f, true);
                    }
                }
            }

            public override void DebugDrawInvalidTriangles()
            {
            }
        }
    }
}
