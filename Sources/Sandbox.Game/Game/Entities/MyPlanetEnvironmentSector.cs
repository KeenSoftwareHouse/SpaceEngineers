using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities
{
    class MyPlanetEnvironmentSector
    {
        const int NUM_PLACE_ITERATION = 4;

        int NumItemsInSector 
        {
            get 
            {
               switch (MySession.Static.Settings.FloraDensity)
               {
                   case 10:
                       return 250;
                       break;
                   case 20:
                       return 500;
                       break;
                   case 30:
                       return 750;
                       break;
               }
               return 0;
            }
        }

        bool m_saved = false;
        MyPlanet m_planet;
        int m_cellHashCode;
        public const int SECTOR_SIZE_METERS = 512;

        List<Vector3D> m_spawnPositions = new List<Vector3D>();

        Vector3I m_pos;
        Vector3D m_sectorCenter;

        public BoundingBoxD SectorBox;

        public bool HasGraphics = false;

        private Dictionary<MyStringHash, MyEnvironmentItems.MyEnvironmentItemsSpawnData> m_spawners;

        public MyPlanetEnvironmentSector()
        { 
        }

        public void Init(ref Vector3I pos, MyPlanet planet)
        {
            m_pos = pos;
            m_cellHashCode = pos.GetHashCode() + planet.PositionLeftBottomCorner.GetHashCode();
            m_planet = planet;
            SectorBox = new BoundingBoxD(m_planet.PositionLeftBottomCorner + pos * SECTOR_SIZE_METERS, m_planet.PositionLeftBottomCorner + pos * SECTOR_SIZE_METERS + SECTOR_SIZE_METERS);
            m_sectorCenter = SectorBox.Center;
        }

        public static Vector3D GetRandomPosition(ref BoundingBoxD box)
        {
            return box.Center + GetRandomVector3() * box.HalfExtents;
        }

        public static Vector3 GetRandomVector3()
        {
            return new Vector3(MyRandom.Instance.NextFloat(-1, 1), MyRandom.Instance.NextFloat(-1, 1), MyRandom.Instance.NextFloat(-1, 1));
        }

        public void PlaceItems()
        {
            ProfilerShort.Begin("flora :   spawn");
            Vector3 direction = -m_planet.GetWorldGravity(m_sectorCenter);
            direction.Normalize();

            var random = MyRandom.Instance;
            using (var stateToken = random.PushSeed(m_cellHashCode))
            {
                for (int i = 0; i < NumItemsInSector; ++i)
                {
                    Vector3D spawnPosition = GetRandomPosition(ref SectorBox);
                    Vector3D localPosition;
                    MyVoxelCoordSystems.WorldPositionToLocalPosition(m_planet.PositionLeftBottomCorner, ref spawnPosition, out localPosition);
                    Vector3D gravity = m_planet.GetWorldGravityNormalized(ref spawnPosition);

                    localPosition = m_planet.GetClosestSurfacePoint(ref localPosition, ref gravity, NUM_PLACE_ITERATION, 0);
                    MyVoxelCoordSystems.LocalPositionToWorldPosition(m_planet.PositionLeftBottomCorner, ref localPosition, out spawnPosition);
                    if (m_planet.Storage.DataProvider.IsMaterialAtPositionSpawningFlora(ref localPosition))
                    {
                        m_spawnPositions.Add(spawnPosition);
                    }
                }

                if (m_spawnPositions.Count > 0)
                {
                    m_spawners = new Dictionary<MyStringHash, MyEnvironmentItems.MyEnvironmentItemsSpawnData>();
                    var itemClasses = MyDefinitionManager.Static.GetEnvironmentItemClassDefinitions();
                    ProfilerShort.Begin("flora :  begin spawn");
                    foreach (var itemClass in itemClasses)
                    {
                        m_spawners[itemClass.Id.SubtypeId] = MyEnvironmentItems.BeginSpawn(itemClass);
                        m_spawners[itemClass.Id.SubtypeId].EnvironmentItems.Save = false;
                        m_spawners[itemClass.Id.SubtypeId].EnvironmentItems.CellsOffset = m_planet.PositionLeftBottomCorner;
                        m_spawners[itemClass.Id.SubtypeId].EnvironmentItems.OnElementRemoved += OnSectorItemRemoved;
                    }

                    ProfilerShort.End();
                    for (int i = 0; i < m_spawnPositions.Count; ++i)
                    {
                        int value = random.Next(1, 4);

                        var cl = MyDefinitionManager.Static.GetRandomEnvironmentClass(value);
                        if (cl != null)
                        {
                            var itemDef = cl.GetRandomItemDefinition();
                            if (itemDef != null)
                            {

                                var spawner = m_spawners[cl.Id.SubtypeId];
                                MyEnvironmentItems.SpawnItem(spawner, itemDef, m_spawnPositions[i], direction);
                            }
                        }

                    }
                    ProfilerShort.Begin("flora : end spawn");
                    foreach (var spawner in m_spawners)
                    {
                        MyEnvironmentItems.EndSpawn(spawner.Value, false);
                    }
                    ProfilerShort.End();

                    m_spawnPositions.Clear();
                }
            }
            ProfilerShort.End();
        }

        public void UpdateSectorGraphics()
        {
            HasGraphics = true;
            if (m_spawners == null)
            {
                return;
            }

            foreach (var spawner in m_spawners)
            {
                spawner.Value.EnvironmentItems.PrepareItemsGraphics();
            }
        }

        public void CloseSector()
        {
            m_spawnPositions.Clear();
            if (m_spawners == null)
            {
                return;
            }
            foreach (var spawner in m_spawners)
            {
                spawner.Value.EnvironmentItems.Close();
            }
            m_spawners.Clear();
            m_spawners = null;
            m_planet = null;
            HasGraphics = false;
        }

        void OnSectorItemRemoved(Vector3D pos)
        {
            
            foreach (var spawner in m_spawners)
            {
                spawner.Value.EnvironmentItems.Save = true;
            }

            if (m_saved == false)
            {
                m_planet.OnEnviromentSectorItemRemoved(m_pos);
                m_saved = true;
            }

            MyObjectBuilder_FloatingObject floatingBuilder = new MyObjectBuilder_FloatingObject();
            floatingBuilder.Item = new MyObjectBuilder_InventoryItem() { Amount = 1, Content = new MyObjectBuilder_PhysicalGunObject() {SubtypeName = "WelderItem" } };
            floatingBuilder.PersistentFlags = MyPersistentEntityFlags2.InScene; // Very important
            Vector3D gravity = -m_planet.GetWorldGravityNormalized(ref pos);
            floatingBuilder.PositionAndOrientation = new MyPositionAndOrientation()
            {
                Position = pos + 0.5*gravity,
                Up = (Vector3)gravity,
                Forward = (Vector3)MyUtils.GetRandomPerpendicularVector(ref gravity),
            };

            MyEntities.CreateFromObjectBuilderAndAdd(floatingBuilder);
        }
    }
}
