using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Utils;
using VRage.Voxels;
using VRage.Serialization;
using VRageMath;
using Sandbox.Game.World.Generator;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.Components;

namespace Sandbox.Game.World.Generator
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    class MyEncounterGenerator : MySessionComponentBase
    {
        private const double m_minDistanceToRecognizeMovement = 100.0; 
        private static Dictionary<IMyEntity, MyEncounterId> m_entityToEncounterConversion = new Dictionary<IMyEntity, MyEncounterId>();
        private static HashSet<MyEncounterId> m_savedEncounters  = new HashSet<MyEncounterId>();
        private static List<MyCubeGrid> m_createdGrids = new List<MyCubeGrid>();
        private static SerializableDictionary<MyEncounterId, Vector3D> m_movedOnlyEncounters = new SerializableDictionary<MyEncounterId, Vector3D>();
        private static List<MySpawnGroupDefinition> m_spawnGroups = new List<MySpawnGroupDefinition>();
        private static List<MySpawnGroupDefinition> m_spawnGroupsNoVoxels = new List<MySpawnGroupDefinition>();
        private static List<int> m_randomEncounters = new List<int>();
        private static List<Vector3D> m_placePositions = new List<Vector3D>();
        private static List<MyEncounterId> m_encountersId = new List<MyEncounterId>();


        private static List<float> m_spawnGroupCumulativeFrequencies = new List<float>();
        private static float m_spawnGroupTotalFrequencies = 0.0f;
        private static MyRandom m_random = new MyRandom();

        public static SerializableDictionary<MyEncounterId, Vector3D> MovedEncounters()
        {
            return m_movedOnlyEncounters;
        }

        public static bool RemoveEncounter(BoundingBoxD boundingVolume, int seed)
        {
            bool wasFound = false;
            for (int i = 0; i < 2; ++i)
            {
                MyEncounterId encounter = new MyEncounterId(boundingVolume, seed,i);
                if (true == m_savedEncounters.Contains(encounter))
                {
                    wasFound = false;
                    continue;
                }

                List<IMyEntity> entitiesToRemove = new List<IMyEntity>();

                foreach (var entity in m_entityToEncounterConversion)
                {
                    if (entity.Value.BoundingBox == encounter.BoundingBox && entity.Value.Seed == encounter.Seed && entity.Value.EncounterId == encounter.EncounterId)
                    {
                        entity.Key.Close();
                        entitiesToRemove.Add(entity.Key);
                        wasFound = true;
                    }
                }

                foreach (var entity in entitiesToRemove)
                {
                    m_entityToEncounterConversion.Remove(entity);
                }
            }
            return wasFound;
        }

        public static bool PlaceEncounterToWorld(BoundingBoxD boundingVolume, int seed, MyObjectSeedType seedType)
        {
            if (MySession.Static.Settings.EnableEncounters == false)
            {
                return false;
            }

            Vector3D placePosition = boundingVolume.Center;
            m_random.SetSeed(seed);

            if (m_spawnGroups.Count == 0)
            {
                m_spawnGroupsNoVoxels.Clear();
                var allSpawnGroups = MyDefinitionManager.Static.GetSpawnGroupDefinitions();
                foreach (var spawnGroup in allSpawnGroups)
                {
                    if (spawnGroup.IsEncounter)
                    {
                        m_spawnGroups.Add(spawnGroup);
                        if (spawnGroup.Voxels.Count == 0)
                        {
                            m_spawnGroupsNoVoxels.Add(spawnGroup);
                        }
                    }
                }
            }

            if (m_spawnGroups.Count > 0)
            {
                m_randomEncounters.Clear();
                m_placePositions.Clear();
                m_encountersId.Clear();
                int numEncoutersToPlace = seedType == MyObjectSeedType.EncounterMulti ? 2 : 1;
                List<MySpawnGroupDefinition> currentSpawnGroup = seedType == MyObjectSeedType.EncounterMulti ? m_spawnGroupsNoVoxels : m_spawnGroups;

                for (int i = 0; i < numEncoutersToPlace; ++i)
                {
                    MyEncounterId encounterPosition = new MyEncounterId(boundingVolume, seed, i);
                    if (true == m_savedEncounters.Contains(encounterPosition))
                    {
                        continue;
                    }
                    m_randomEncounters.Add(PickRandomEncounter(currentSpawnGroup));
                    Vector3D newPosition = placePosition + (i == 0 ? -1 : 1) * GetEncounterBoundingBox(currentSpawnGroup[m_randomEncounters[m_randomEncounters.Count - 1]]).HalfExtents;
                    Vector3D savedPosition = Vector3D.Zero;
                    if (true == m_movedOnlyEncounters.Dictionary.TryGetValue(encounterPosition, out savedPosition))
                    {
                        newPosition = savedPosition;
                    }
                    encounterPosition.PlacePosition = newPosition;

                    m_encountersId.Add(encounterPosition);

                    m_placePositions.Add(newPosition);
                }

                //first place voxels becaose voxel needs to be created even on client and if grids were created first
                //entity ids woudn't match
                for (int i = 0; i < m_randomEncounters.Count; ++i)
                {
                    foreach (var selectedVoxel in currentSpawnGroup[m_randomEncounters[i]].Voxels)
                    {
                        var filePath = MyWorldGenerator.GetVoxelPrefabPath(selectedVoxel.StorageName);

                        var storage = MyStorageBase.LoadFromFile(filePath);
                        storage.DataProvider = MyCompositeShapeProvider.CreateAsteroidShape(0, 1.0f, MySession.Static.Settings.VoxelGeneratorVersion);
                        IMyEntity voxel = MyWorldGenerator.AddVoxelMap(String.Format("Asteroid_{0}_{1}_{2}", m_entityToEncounterConversion.Count, seed, m_random.Next()), storage, m_placePositions[i] + selectedVoxel.Offset);
                        voxel.Save = false;
                        voxel.OnPhysicsChanged += OnCreatedEntityChanged;
                        m_entityToEncounterConversion[voxel] = m_encountersId[i];
                    }
                }

                if (Sync.IsServer == true)
                {
                    for (int i = 0; i < m_randomEncounters.Count; ++i)
                    {
                        SpawnEncouter(m_encountersId[i], m_placePositions[i], currentSpawnGroup, m_randomEncounters[i]);
                    }
                }
            }

            return true;
        }

        private static BoundingBox GetEncounterBoundingBox(MySpawnGroupDefinition selectedEncounter)
        {
            BoundingBox encouterBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
            selectedEncounter.ReloadPrefabs();
            foreach (var selectedPrefab in selectedEncounter.Prefabs)
            {
                var prefabDefinition = MyDefinitionManager.Static.GetPrefabDefinition(selectedPrefab.SubtypeId);
                encouterBoundingBox.Include(prefabDefinition.BoundingSphere);
            }
            return encouterBoundingBox;
        }

        private static void SpawnEncouter(MyEncounterId encounterPosition, Vector3D placePosition, List<MySpawnGroupDefinition> candidates, int selectedEncounter)
        {
            foreach (var selectedPrefab in candidates[selectedEncounter].Prefabs)
            {
                m_createdGrids.Clear();
                Vector3D direction = Vector3D.Forward;
                Vector3D upVector = Vector3D.Up;

                var spawningOptions = Sandbox.ModAPI.SpawningOptions.TurnOffReactors;
                if (selectedPrefab.Speed > 0.0f)
                {
                    spawningOptions = Sandbox.ModAPI.SpawningOptions.RotateFirstCockpitTowardsDirection |
                                     Sandbox.ModAPI.SpawningOptions.SpawnRandomCargo |
                                     Sandbox.ModAPI.SpawningOptions.DisableDampeners;



                    float centerArcRadius = (float)Math.Atan(MyNeutralShipSpawner.NEUTRAL_SHIP_FORBIDDEN_RADIUS / placePosition.Length());
                    direction = -Vector3D.Normalize(placePosition);
                    float theta = m_random.NextFloat(centerArcRadius, centerArcRadius + MyNeutralShipSpawner.NEUTRAL_SHIP_DIRECTION_SPREAD);
                    float phi = m_random.NextFloat(0, 2 * MathHelper.Pi);
                    Vector3D cosVec = Vector3D.CalculatePerpendicularVector(direction);
                    Vector3D sinVec = Vector3D.Cross(direction, cosVec);
                    cosVec *= (Math.Sin(theta) * Math.Cos(phi));
                    sinVec *= (Math.Sin(theta) * Math.Sin(phi));
                    direction = direction * Math.Cos(theta) + cosVec + sinVec;

                    upVector = Vector3D.CalculatePerpendicularVector(direction);
                }
                spawningOptions |= Sandbox.ModAPI.SpawningOptions.DisableSave;

                var prefabDefinition = MyDefinitionManager.Static.GetPrefabDefinition(selectedPrefab.SubtypeId);
                Vector3D prefabPosDeltaValue = Vector3D.Zero;
                if (prefabDefinition.CubeGrids.Length > 0)
                {
                    if (prefabDefinition.CubeGrids[0].PositionAndOrientation.HasValue)
                    {
                        prefabPosDeltaValue = prefabDefinition.CubeGrids[0].PositionAndOrientation.Value.Position;
                    }
                }
                prefabPosDeltaValue -= prefabDefinition.BoundingSphere.Center;

                MyPrefabManager.Static.SpawnPrefab(
                   resultList: m_createdGrids,
                   prefabName: selectedPrefab.SubtypeId,
                   position: placePosition + selectedPrefab.Position - prefabPosDeltaValue,
                   forward: direction,
                   up:upVector,
                   beaconName: selectedPrefab.BeaconText,
                   initialLinearVelocity: direction * selectedPrefab.Speed,
                   spawningOptions: spawningOptions,
                   updateSync: true);

                ProcessCreatedGrids(ref encounterPosition, selectedPrefab.Speed);
            }
        }

        private static int PickRandomEncounter(List<MySpawnGroupDefinition> candidates)
        {
            m_spawnGroupTotalFrequencies = 0.0f;
            m_spawnGroupCumulativeFrequencies.Clear();

            foreach (var spawnGroup in candidates)
            {              
                m_spawnGroupTotalFrequencies += spawnGroup.Frequency;
                m_spawnGroupCumulativeFrequencies.Add(m_spawnGroupTotalFrequencies);
            }

            float rnd = m_random.NextFloat(0.0f, m_spawnGroupTotalFrequencies);
            int selectedEncounter = 0;
            while (selectedEncounter < m_spawnGroupCumulativeFrequencies.Count())
            {
                if (rnd <= m_spawnGroupCumulativeFrequencies[selectedEncounter])
                    break;

                ++selectedEncounter;
            }

            if (selectedEncounter >= m_spawnGroupCumulativeFrequencies.Count())
                selectedEncounter = m_spawnGroupCumulativeFrequencies.Count() - 1;
            return selectedEncounter;
        }

        private static void ProcessCreatedGrids(ref MyEncounterId encounterPosition,  float prefabSpeed)
        {
            foreach (var grid in m_createdGrids)
            {              
                grid.OnGridChanged += OnCreatedEntityChanged;
                grid.OnPhysicsChanged += OnCreatedEntityChanged;
                grid.PositionComp.OnPositionChanged += OnCreatedEntityPositionChanged;
                m_entityToEncounterConversion[grid] = encounterPosition;
            }
        }      

        private static void OnCreatedEntityPositionChanged(MyPositionComponentBase obj)
        {
            if (obj.Container.Entity.Save == false)
            {
                MyEncounterId id;
                if (m_entityToEncounterConversion.TryGetValue(obj.Container.Entity, out id))
                {
                    Vector3D newPosition = obj.GetPosition();
                    if (Vector3D.Distance(id.PlacePosition, newPosition) > m_minDistanceToRecognizeMovement)
                    {
                        m_movedOnlyEncounters[id] = obj.GetPosition();
                    }
                }
            }
        }

        private static void OnCreatedEntityChanged(IMyEntity obj)
        {
            MyEncounterId id;
            if (m_entityToEncounterConversion.TryGetValue(obj, out id))
            {
               
                if (obj.MarkedForClose == false)
                {
                    m_savedEncounters.Add(id);
                    //if encouter has multiple grids (e.g. it has motors,pistons...)
                    foreach (var entity in m_entityToEncounterConversion)
                    {
                        if (entity.Value == id)
                        {
                            m_movedOnlyEncounters.Dictionary.Remove(id);

                            entity.Key.Save = true;
                            entity.Key.OnPhysicsChanged -= OnCreatedEntityChanged;

                            var cubeGrid = (entity.Key as MyCubeGrid);
                            if (cubeGrid == null)
                            {
                                continue;
                            }
                            cubeGrid.OnGridChanged -= OnCreatedEntityChanged;
                            cubeGrid.PositionComp.OnPositionChanged -= OnCreatedEntityPositionChanged;
                        }
                    }
                }
            }
        }


        public static void Load(MyObjectBuilder_Encounters encountersObjectBuilder)
        {
            if (encountersObjectBuilder != null)
            {
                m_savedEncounters = encountersObjectBuilder.SavedEcounters ?? m_savedEncounters;
                m_movedOnlyEncounters = encountersObjectBuilder.MovedOnlyEncounters ?? m_movedOnlyEncounters;
            }
        }
        public static MyObjectBuilder_Encounters Save()
        {
            MyObjectBuilder_Encounters saveData = new MyObjectBuilder_Encounters();
            saveData.SavedEcounters = new HashSet<MyEncounterId>(m_savedEncounters);
            saveData.MovedOnlyEncounters =  new SerializableDictionary<MyEncounterId,Vector3D>(new Dictionary<MyEncounterId,Vector3D> (m_movedOnlyEncounters.Dictionary));
            return saveData;
        }

        private void ClearCollections()
        {
            m_createdGrids.Clear();
            m_entityToEncounterConversion.Clear();
            m_savedEncounters.Clear();
            m_movedOnlyEncounters.Dictionary.Clear();
            m_spawnGroupsNoVoxels.Clear();
            m_spawnGroups.Clear();
        }

        public override void LoadData()
        {
            ClearCollections();
            var allSpawnGroups = MyDefinitionManager.Static.GetSpawnGroupDefinitions();
            foreach (var spawnGroup in allSpawnGroups)
            {
                if (spawnGroup.IsEncounter)
                { 
                    m_spawnGroups.Add(spawnGroup);
                    if (spawnGroup.Voxels.Count == 0)
                    {
                        m_spawnGroupsNoVoxels.Add(spawnGroup);
                    }
                }
            }
        }

        protected override void UnloadData()
        {
            ClearCollections();
        }

        public static void DrawEncounters()
        {
            foreach (var encounter in m_entityToEncounterConversion)
            {
                Vector3D encounterPos = Vector3D.Zero;
                if(m_movedOnlyEncounters.Dictionary.TryGetValue(encounter.Value, out encounterPos))
                {
                    VRageRender.MyRenderProxy.DebugDrawSphere(encounterPos, (float)encounter.Value.BoundingBox.HalfExtents.X / 2, Color.Yellow.ToVector3(), 1.0f, true);
                }
                else if(m_savedEncounters.Contains(encounter.Value))
                {
                    VRageRender.MyRenderProxy.DebugDrawSphere(encounter.Key.PositionComp.GetPosition(), (float)encounter.Value.BoundingBox.HalfExtents.X / 2, Color.Green.ToVector3(), 1.0f, true);
                }
                else
                {
                    VRageRender.MyRenderProxy.DebugDrawSphere(encounter.Key.PositionComp.GetPosition(), (float)encounter.Value.BoundingBox.HalfExtents.X / 2, Color.Blue.ToVector3(), 1.0f, true);
                }
            }
        }
    }
}
