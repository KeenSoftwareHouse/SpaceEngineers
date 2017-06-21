using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.Game.Entities.Cube;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_SpawnGroupDefinition))]
    public class MySpawnGroupDefinition : MyDefinitionBase
    {
        public struct SpawnGroupPrefab {
            public Vector3 Position;
            public String SubtypeId;
            public String BeaconText;
            public float Speed;
            public bool ResetOwnership;
            public bool PlaceToGridOrigin;
        }

        public struct SpawnGroupVoxel
        {
            public Vector3 Offset;
            public String StorageName;
        }

        public float Frequency;

        private float m_spawnRadius;
        public float SpawnRadius
        {
            get 
            {
                if(m_initialized == false)
                {
                    ReloadPrefabs();
                }

                return m_spawnRadius;
            }
            private set
            {
                m_spawnRadius = value;
            }
        }

        bool m_initialized = false;

        public bool IsEncounter;
        public bool IsPirate;
        public bool ReactorsOn;
        public List<SpawnGroupPrefab> Prefabs = new List<SpawnGroupPrefab>();
        public List<SpawnGroupVoxel> Voxels = new List<SpawnGroupVoxel>();

        public bool IsValid
        {
            get
            {
                return Frequency != 0.0f && m_spawnRadius != 0.0f && Prefabs.Count != 0;
            }
        }

        public MySpawnGroupDefinition()
        { }

        protected override void Init(MyObjectBuilder_DefinitionBase baseBuilder)
        {
            base.Init(baseBuilder);

            var builder = baseBuilder as MyObjectBuilder_SpawnGroupDefinition;

            Frequency = builder.Frequency;
            if (Frequency == 0.0f)
            {
                MySandboxGame.Log.WriteLine("Spawn group initialization: spawn group has zero frequency");
                return;
            }

            SpawnRadius = 0.0f;
            BoundingSphere sphere = new BoundingSphere(Vector3.Zero, float.MinValue);

            Prefabs.Clear();
            foreach (var prefab in builder.Prefabs)
            {
                SpawnGroupPrefab spawnPrefab = new SpawnGroupPrefab();
                spawnPrefab.Position = prefab.Position;
                spawnPrefab.SubtypeId = prefab.SubtypeId;
                spawnPrefab.BeaconText = prefab.BeaconText;
                spawnPrefab.Speed = prefab.Speed;
                spawnPrefab.ResetOwnership = prefab.ResetOwnership;
                spawnPrefab.PlaceToGridOrigin = prefab.PlaceToGridOrigin;

                var prefabDef = MyDefinitionManager.Static.GetPrefabDefinition(spawnPrefab.SubtypeId);
                if (prefabDef == null)
                {
                    System.Diagnostics.Debug.Assert(false, "Spawn group initialization: Could not get prefab " + spawnPrefab.SubtypeId);
                    MySandboxGame.Log.WriteLine("Spawn group initialization: Could not get prefab " + spawnPrefab.SubtypeId);
                    return;
                }
                Prefabs.Add(spawnPrefab);
            }

            Voxels.Clear();
            if (builder.Voxels != null)
            {
                foreach (var prefab in builder.Voxels)
                {
                    SpawnGroupVoxel spawnPrefab = new SpawnGroupVoxel();
                    spawnPrefab.Offset = prefab.Offset;
                    spawnPrefab.StorageName = prefab.StorageName;

                    Voxels.Add(spawnPrefab);
                }
            }

            SpawnRadius = sphere.Radius + 5.0f; // Add 5m just to be sure
            IsEncounter = builder.IsEncounter;
            IsPirate = builder.IsPirate;
            ReactorsOn = builder.ReactorsOn;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var spawnGroupBuilder = base.GetObjectBuilder() as MyObjectBuilder_SpawnGroupDefinition;

            spawnGroupBuilder.Frequency = Frequency;
            spawnGroupBuilder.Prefabs = new MyObjectBuilder_SpawnGroupDefinition.SpawnGroupPrefab[Prefabs.Count];

            int i = 0;
            foreach (var prefab in Prefabs)
            {
                spawnGroupBuilder.Prefabs[i].BeaconText = prefab.BeaconText;
                spawnGroupBuilder.Prefabs[i].SubtypeId = prefab.SubtypeId;
                spawnGroupBuilder.Prefabs[i].Position = prefab.Position;
                spawnGroupBuilder.Prefabs[i].Speed = prefab.Speed;
                spawnGroupBuilder.Prefabs[i].ResetOwnership = prefab.ResetOwnership;
                spawnGroupBuilder.Prefabs[i].PlaceToGridOrigin = prefab.PlaceToGridOrigin;

                i++;
            }

            spawnGroupBuilder.Voxels = new MyObjectBuilder_SpawnGroupDefinition.SpawnGroupVoxel[Voxels.Count];
            i = 0;
            foreach (var prefab in Voxels)
            {
                spawnGroupBuilder.Voxels[i].Offset = prefab.Offset;
                spawnGroupBuilder.Voxels[i].StorageName = prefab.StorageName;

                i++;
            }
            spawnGroupBuilder.IsEncounter = IsEncounter;
            spawnGroupBuilder.IsPirate = IsPirate;
            spawnGroupBuilder.ReactorsOn = ReactorsOn;
            return spawnGroupBuilder;
        }

        public void ReloadPrefabs()
        {
            BoundingSphere sphere = new BoundingSphere(Vector3.Zero, float.MinValue);
            float radiusAddition = 0;
            foreach (var prefab in Prefabs)
            {
                var prefabDef = MyDefinitionManager.Static.GetPrefabDefinition(prefab.SubtypeId);
                if (prefabDef == null)
                {
                    System.Diagnostics.Debug.Assert(false, "Spawn group initialization: Could not get prefab " + prefab.SubtypeId);
                    MySandboxGame.Log.WriteLine("Spawn group initialization: Could not get prefab " + prefab.SubtypeId);
                    return;
                }

                BoundingSphere prefabSphere = prefabDef.BoundingSphere;
                prefabSphere.Center += prefab.Position;

                sphere.Include(prefabSphere);

                if (prefabDef.CubeGrids != null)
                {
                    foreach (var grid in prefabDef.CubeGrids)
                    {
                        float gridSize = MyDefinitionManager.Static.GetCubeSize(grid.GridSizeEnum);
                        radiusAddition = Math.Max(radiusAddition, 2 * gridSize);
                    }
                }
            }
            SpawnRadius = sphere.Radius + radiusAddition;
            m_initialized = true;
        }
    }
}
