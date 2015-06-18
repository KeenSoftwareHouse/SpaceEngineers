using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VRage.Utils;
using VRageMath;
using VRage;
using VRage.Voxels;
using Sandbox.Game.World.Generator;
using Sandbox.ModAPI;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.ObjectBuilders;
using VRage.Collections;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace Sandbox.Game.World
{
    public partial class MyWorldGenerator
    {
        public struct Args
        {
            public MyScenarioDefinition Scenario;
            public int AsteroidAmount;
        }

        private static List<MyCubeGrid> m_tmpSpawnedGridList = new List<MyCubeGrid>();

        public static event ActionRef<Args> OnAfterGenerate;

        static MyWorldGenerator()
        {
            if (MyFakes.TEST_PREFABS_FOR_INCONSISTENCIES)
            {
                string prefabDir = Path.Combine(MyFileSystem.ContentPath, "Data", "Prefabs");
                var prefabFiles = Directory.GetFiles(prefabDir);
                foreach (var prefabFile in prefabFiles)
                {
                    if (Path.GetExtension(prefabFile) != ".sbc")
                        continue;

                    MyObjectBuilder_CubeGrid result = null;
                    var fsPath = Path.Combine(MyFileSystem.ContentPath, prefabFile);
                    MyObjectBuilderSerializer.DeserializeXML<MyObjectBuilder_CubeGrid>(fsPath, out result);

                    if (result == null) continue;

                    foreach (var block in result.CubeBlocks)
                    {
                        if (block.IntegrityPercent == 0.0f)
                        {
                            Debug.Assert(false, "Inconsistent block in prefab file " + prefabFile);
                            break;
                        }
                    }
                }

                string worldDir = Path.Combine(MyFileSystem.ContentPath, "Worlds");
                var worldDirs = Directory.GetDirectories(worldDir);
                foreach (var dir in worldDirs)
                {
                    var files = Directory.GetFiles(dir);
                    foreach (var file in files)
                    {
                        if (Path.GetExtension(file) != ".sbs")
                            continue;

                        MyObjectBuilder_Sector result = null;

                        var fsPath = Path.Combine(MyFileSystem.ContentPath, file);
                        MyObjectBuilderSerializer.DeserializeXML<MyObjectBuilder_Sector>(fsPath, out result);

                        Debug.Assert(result != null, "Unloadable world: " + file);
                        foreach (var obj in result.SectorObjects)
                        {
                            if (obj.TypeId == typeof(MyObjectBuilder_CubeGrid))
                            {
                                var grid = (MyObjectBuilder_CubeGrid)obj;
                                foreach (var block in grid.CubeBlocks)
                                {
                                    if (block.IntegrityPercent == 0.0f)
                                    {
                                        Debug.Assert(false, "Inconsistent block in save " + file);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static string GetPrefabTypeName(MyObjectBuilder_EntityBase entity)
        {
            if (entity is MyObjectBuilder_VoxelMap)
                return "Asteroid";
            if (entity is MyObjectBuilder_CubeGrid)
            {
                var g = (MyObjectBuilder_CubeGrid)entity;
                if (g.IsStatic)
                    return "Station";
                else if (g.GridSizeEnum == MyCubeSize.Large)
                    return "LargeShip";
                else
                    return "SmallShip";
            }
            if (entity is MyObjectBuilder_Character)
                return "Character";
            return "Unknown";
        }

        public static void GenerateWorld(Args args)
        {
            MySandboxGame.Log.WriteLine("MyWorldGenerator.GenerateWorld - START");
            ProfilerShort.Begin("MyWorldGenerator.GenerateWorld");

            using (var indent = MySandboxGame.Log.IndentUsing(LoggingOptions.NONE))
            {
                RunGeneratorOperations(ref args);

                if (!MySandboxGame.IsDedicated)
                {
                    SetupPlayer(ref args);
                }

                CallOnAfterGenerate(ref args);
            }

            ProfilerShort.End();
            MySandboxGame.Log.WriteLine("MyWorldGenerator.GenerateWorld - END");
        }

        public static void CallOnAfterGenerate(ref Args args)
        {
            if (OnAfterGenerate != null)
                OnAfterGenerate(ref args);
        }

        public static void InitInventoryWithDefaults(MyInventory inventory)
        {
            var inventoryOb = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Inventory>();
            FillInventoryWithDefaults(inventoryOb, MySession.Static.Scenario);
            inventory.Init(inventoryOb);
        }

        private static void SetupPlayer(ref Args args)
        {
            var identity = Sync.Players.CreateNewIdentity(Sync.Clients.LocalClient.DisplayName);
            var player = Sync.Players.CreateNewPlayer(identity, Sync.Clients.LocalClient, Sync.MyName);
            var playerStarts = args.Scenario.PossiblePlayerStarts;
            if (playerStarts == null || playerStarts.Length == 0)
            {
                Sync.Players.RespawnComponent.SetupCharacterDefault(player, args);
            }
            else
            {
                var randomStart = playerStarts[MyUtils.GetRandomInt(playerStarts.Length)];
                randomStart.SetupCharacter(args);
            }

            // Setup toolbar
            if (args.Scenario.DefaultToolbar != null)
            {
                // TODO: JakubD fix this
                MyToolbar toolbar = new MyToolbar(MyToolbarType.Character);
                toolbar.Init(args.Scenario.DefaultToolbar, player.Character, true);

                MySession.Static.Toolbars.RemovePlayerToolbar(player.Id);
                MySession.Static.Toolbars.AddPlayerToolbar(player.Id, toolbar);
                MyToolbarComponent.InitToolbar(MyToolbarType.Character, args.Scenario.DefaultToolbar);
                MyToolbarComponent.InitCharacterToolbar(args.Scenario.DefaultToolbar);
            }
        }

        public static void FillInventoryWithDefaults(MyObjectBuilder_Inventory inventory, MyScenarioDefinition scenario)
        {
            if (inventory.Items == null)
                inventory.Items = new List<MyObjectBuilder_InventoryItem>(15);
            else
                inventory.Items.Clear();

            if (scenario != null)
            {
                MyStringId[] guns;
                if (MySession.Static.CreativeMode)
                    guns = scenario.CreativeModeWeapons;// new string[] { "AngleGrinderItem", "AutomaticRifleItem", "HandDrillItem", "WelderItem" };
                else
                    guns = scenario.SurvivalModeWeapons;// new string[] { "AngleGrinderItem", "HandDrillItem", "WelderItem" };

                if (guns != null)
                {
                    foreach (var gun in guns)
                    {
                        var inventoryItem = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_InventoryItem>();
                        inventoryItem.Amount = 1;
                        inventoryItem.Content = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>(gun.ToString());
                        inventory.Items.Add(inventoryItem);
                    }
                }
            }
        }

        #region Generator operations

        private static void RunGeneratorOperations(ref Args args)
        {
            var operations = args.Scenario.WorldGeneratorOperations;
            if (operations != null && operations.Length > 0)
            {
                foreach (var op in operations)
                {
                    op.Apply();
                }
            }
        }

        public static void AddAsteroidPrefab(string prefabName, Vector3 position, string name)
        {
            var fileName = GetVoxelPrefabPath(prefabName);
            var storage = LoadRandomizedVoxelMapPrefab(fileName);
            AddVoxelMap(name, storage, position);
        }

        public static MyVoxelMap AddVoxelMap(string storageName, MyStorageBase storage, Vector3 positionMinCorner, long entityId = 0)
        {
            var voxelMap = new MyVoxelMap();
            voxelMap.EntityId = entityId;
            voxelMap.Init(storageName, storage, positionMinCorner);
            MyEntities.Add(voxelMap);
            return voxelMap;
        }

      
        private static Vector3I FindBestOctreeSize(float radius)
        {
            int nodeRadius = MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS;
            while (nodeRadius < radius)
                nodeRadius *= 2;
            //nodeRadius *= 2;
            return new Vector3I(nodeRadius, nodeRadius, nodeRadius);
        }

        public static void AddEntity(MyObjectBuilder_EntityBase entityBuilder)
        {
            MyEntities.CreateFromObjectBuilderAndAdd(entityBuilder);
        }

        private static void AddObjectsPrefab(string prefabName)
        {
            var objects = LoadObjectsPrefab(prefabName);
            foreach (var obj in objects)
            {
                MyEntities.CreateFromObjectBuilderAndAdd(obj);
            }
        }

        private static void SetupBase(string basePrefabName, Vector3 offset, string voxelFilename, string beaconName = null)
        {
            // Add one inital asteroid underneath the base. The base and the asteroid right now are hardcoded.
            // Maybe we can add some base+asteroid combinations later and select one randomly
            MyPrefabManager.Static.SpawnPrefab(
                prefabName: basePrefabName,
                position: new Vector3(-3, 11, 15) + offset,
                forward: Vector3.Forward,
                up: Vector3.Up,
                beaconName: beaconName,
                updateSync: false);

            // Small red block on landing gears.
            MyPrefabManager.Static.AddShipPrefab("SmallShip_SingleBlock", Matrix.CreateTranslation(new Vector3(-5.20818424f, -0.442984432f, -8.31522751f) + offset));

            var filePath = GetVoxelPrefabPath("VerticalIsland_128x128x128");
            var storage = LoadRandomizedVoxelMapPrefab(filePath);
            AddVoxelMap(voxelFilename, storage, new Vector3(-20, -110, -60) + offset);
        }

        private static MyStorageBase LoadRandomizedVoxelMapPrefab(string prefabFilePath)
        {
            var storage = MyStorageBase.LoadFromFile(prefabFilePath);
            storage.DataProvider = MyCompositeShapeProvider.CreateAsteroidShape(
                MyUtils.GetRandomInt(int.MaxValue - 1) + 1,
                storage.Size.AbsMax() * MyVoxelConstants.VOXEL_SIZE_IN_METRES,
                MySession.Static.Settings.VoxelGeneratorVersion);
            storage.Reset(MyStorageDataTypeFlags.Material);
            return storage;
        }

        #endregion

        #region Prefab file loading

        private static string GetObjectsPrefabPath(string prefabName)
        {
            return Path.Combine("Data", "Prefabs", prefabName + ".sbs");
        }

        public static string GetVoxelPrefabPath(string prefabName)
        {
            MyVoxelMapStorageDefinition definition;
            if (MyDefinitionManager.Static.TryGetVoxelMapStorageDefinition(prefabName, out definition))
            {
                if (definition.Context.IsBaseGame)
                {
                    return Path.Combine(MyFileSystem.ContentPath, definition.StorageFile);
                }
                else
                {
                    return definition.StorageFile;
                }
            }

            return Path.Combine(MyFileSystem.ContentPath, "VoxelMaps", prefabName + MyVoxelConstants.FILE_EXTENSION);
        }

        private static List<MyObjectBuilder_EntityBase> LoadObjectsPrefab(string file)
        {
            MyObjectBuilder_Sector sector;
            var fsPath = Path.Combine(MyFileSystem.ContentPath, GetObjectsPrefabPath(file));
            MyObjectBuilderSerializer.DeserializeXML<MyObjectBuilder_Sector>(fsPath, out sector);
            foreach (var sectorObject in sector.SectorObjects)
                sectorObject.EntityId = 0;
            return sector.SectorObjects;
        }

        #endregion

        #region Helpers

        public static void SetProceduralSettings(int? asteroidAmount, MyObjectBuilder_SessionSettings sessionSettings)
        {
            sessionSettings.ProceduralSeed = MyRandom.Instance.Next();
            switch ((MyGuiScreenWorldSettings.AsteroidAmountEnum)asteroidAmount)
            {
                case MyGuiScreenWorldSettings.AsteroidAmountEnum.ProceduralLow:
                    sessionSettings.ProceduralDensity = 0.25f;
                    break;
                case MyGuiScreenWorldSettings.AsteroidAmountEnum.ProceduralNormal:
                    sessionSettings.ProceduralDensity = 0.35f;
                    break;
                case MyGuiScreenWorldSettings.AsteroidAmountEnum.ProceduralHigh:
                    sessionSettings.ProceduralDensity = 0.50f;
                    break;
                default:
                    throw new InvalidBranchException();
                    break;
            }
        }

        #endregion
    }
}
