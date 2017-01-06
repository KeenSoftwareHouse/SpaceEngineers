using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.World.Generator;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Profiler;
using VRage.Voxels;

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
#if XB1
                System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
#else // !XB1
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
#endif // !XB1
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
                Sync.Players.RespawnComponent.SetupCharacterFromStarts(player, playerStarts, args);
            }

            // Setup toolbar
            var defaultToolbar = args.Scenario.DefaultToolbar;
            if (defaultToolbar != null)
            {
                // TODO: JakubD fix this
                MyToolbar toolbar = new MyToolbar(MyToolbarType.Character);
                toolbar.Init(defaultToolbar, player.Character, true);

                MySession.Static.Toolbars.RemovePlayerToolbar(player.Id);
                MySession.Static.Toolbars.AddPlayerToolbar(player.Id, toolbar);
                MyToolbarComponent.InitToolbar(MyToolbarType.Character, defaultToolbar);
                MyToolbarComponent.InitCharacterToolbar(defaultToolbar);
            }
        }

        public static void FillInventoryWithDefaults(MyObjectBuilder_Inventory inventory, MyScenarioDefinition scenario)
        {
            if (inventory.Items == null)
                inventory.Items = new List<MyObjectBuilder_InventoryItem>();
            else
                inventory.Items.Clear();

            if (scenario != null && MySession.Static.Settings.SpawnWithTools)
            {
                MyStringId[] guns;
                if (MySession.Static.CreativeMode)
                    guns = scenario.CreativeModeWeapons;// new string[] { "AngleGrinderItem", "AutomaticRifleItem", "HandDrillItem", "WelderItem" };
                else
                    guns = scenario.SurvivalModeWeapons;// new string[] { "AngleGrinderItem", "HandDrillItem", "WelderItem" };

                uint itemId = 0;
                if (guns != null)
                {
                    foreach (var gun in guns)
                    {
                        var inventoryItem = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_InventoryItem>();
                        inventoryItem.Amount = 1;
                        inventoryItem.PhysicalContent = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>(gun.ToString());
                        inventoryItem.ItemId = itemId++;
                        inventory.Items.Add(inventoryItem);
                    }
                    inventory.nextItemId = itemId;
                }

                MyScenarioDefinition.StartingItem[] items;
                if (MySession.Static.CreativeMode)
                    items = scenario.CreativeModeComponents;
                else
                    items = scenario.SurvivalModeComponents;

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var inventoryItem = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_InventoryItem>();
                        inventoryItem.Amount = item.amount;
                        inventoryItem.PhysicalContent = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Component>(item.itemName.ToString());
                        inventoryItem.ItemId = itemId++;
                        inventory.Items.Add(inventoryItem);
                    }
                    inventory.nextItemId = itemId;
                }

                MyScenarioDefinition.StartingPhysicalItem[] physicalItems;
                if (MySession.Static.CreativeMode)
                    physicalItems = scenario.CreativeModePhysicalItems;
                else
                    physicalItems = scenario.SurvivalModePhysicalItems;

                if (physicalItems != null)
                {
                    foreach (var item in physicalItems)
                    {
                        var inventoryItem = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_InventoryItem>();
                        inventoryItem.Amount = item.amount;
                        if (item.itemType.ToString().Equals("Ore"))
                        {
                            inventoryItem.PhysicalContent = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(item.itemName.ToString());
                        }
                        else if (item.itemType.ToString().Equals("Ingot"))
                        {
                            inventoryItem.PhysicalContent = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ingot>(item.itemName.ToString());
                        }
                        else if (item.itemType.ToString().Equals("OxygenBottle"))
                        {
                            inventoryItem.Amount = 1;
                            inventoryItem.PhysicalContent = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_OxygenContainerObject>(item.itemName.ToString());
                            (inventoryItem.PhysicalContent as MyObjectBuilder_GasContainerObject).GasLevel = (float)item.amount;
                        }
                        else if (item.itemType.ToString().Equals("GasBottle"))
                        {
                            inventoryItem.Amount = 1;
                            inventoryItem.PhysicalContent = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_GasContainerObject>(item.itemName.ToString());
                            (inventoryItem.PhysicalContent as MyObjectBuilder_GasContainerObject).GasLevel = (float)item.amount;
                        }
                        inventoryItem.ItemId = itemId++;
                        inventory.Items.Add(inventoryItem);
                    }
                    inventory.nextItemId = itemId;
                }

                if (MySession.Static.CreativeMode)
                    items = scenario.CreativeModeAmmoItems;
                else
                    items = scenario.SurvivalModeAmmoItems;

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var inventoryItem = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_InventoryItem>();
                        inventoryItem.Amount = item.amount;
                        inventoryItem.PhysicalContent = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_AmmoMagazine>(item.itemName.ToString());
                        inventoryItem.ItemId = itemId++;
                        inventory.Items.Add(inventoryItem);
                    }
                    inventory.nextItemId = itemId;
                }

                MyObjectBuilder_InventoryItem[] inventoryItems = MySession.Static.CreativeMode ? scenario.CreativeInventoryItems : scenario.SurvivalInventoryItems;
                if (inventoryItems != null)
                {
                    foreach (var ob in inventoryItems)
                    {
                        var item = ob.Clone() as MyObjectBuilder_InventoryItem;
                        item.ItemId = itemId++;
                        inventory.Items.Add(item);
                    }
                    inventory.nextItemId = itemId;
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


        public static MyVoxelMap AddAsteroidPrefab(string prefabName, MatrixD worldMatrix, string name)
        {
            var fileName = GetVoxelPrefabPath(prefabName);
            var storage = LoadRandomizedVoxelMapPrefab(fileName);
            return AddVoxelMap(name, storage, worldMatrix);
        }

        public static MyVoxelMap AddAsteroidPrefab(string prefabName, Vector3D position, string name)
        {
            var fileName = GetVoxelPrefabPath(prefabName);
            var storage = LoadRandomizedVoxelMapPrefab(fileName);
            return AddVoxelMap(name, storage, position);
        }

        public static MyVoxelMap AddAsteroidPrefabCentered(string prefabName, Vector3D position, MatrixD rotation, string name)
        {
            var fileName = GetVoxelPrefabPath(prefabName);
            var storage = LoadRandomizedVoxelMapPrefab(fileName);
            Vector3 offset = storage.Size * MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;

            rotation.Translation = position - offset;

            return AddVoxelMap(name, storage, rotation);
        }

        public static MyVoxelMap AddAsteroidPrefabCentered(string prefabName, Vector3D position, string name)
        {
            var fileName = GetVoxelPrefabPath(prefabName);
            var storage = LoadRandomizedVoxelMapPrefab(fileName);
            Vector3 offset = storage.Size * MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;

            return AddVoxelMap(name, storage, position - offset);
        }

        public static MyVoxelMap AddVoxelMap(string storageName, MyStorageBase storage, Vector3D positionMinCorner, long entityId =0)
        {
            var voxelMap = new MyVoxelMap();
            if (entityId != 0)
            {
                voxelMap.EntityId = entityId;
            }
            voxelMap.Init(storageName, storage, positionMinCorner);
            MyEntities.RaiseEntityCreated(voxelMap);
            MyEntities.Add(voxelMap);
            voxelMap.IsReadyForReplication = true;
            return voxelMap;
        }

        public static MyVoxelMap AddVoxelMap(string storageName, MyStorageBase storage, MatrixD worldMatrix, long entityId=0, bool lazyPhysics = false)
        {
            ProfilerShort.Begin("AddVoxelMap");

            var voxelMap = new MyVoxelMap();
            if (entityId != 0)
            {
                voxelMap.EntityId = entityId;
            }
            voxelMap.DelayRigidBodyCreation = lazyPhysics;
            voxelMap.Init(storageName, storage, worldMatrix);
            MyEntities.Add(voxelMap);
            MyEntities.RaiseEntityCreated(voxelMap);
            voxelMap.IsReadyForReplication = true;

            ProfilerShort.End();
            return voxelMap;
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

        private static void SetupBase(string basePrefabName, Vector3 offset, string voxelFilename, string beaconName = null, long factionId = 0)
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
            MyPrefabManager.Static.AddShipPrefab("SmallShip_SingleBlock", Matrix.CreateTranslation(new Vector3(-5.20818424f, -0.442984432f, -8.31522751f) + offset), factionId);

            if (voxelFilename != null)
            {
                var filePath = GetVoxelPrefabPath("VerticalIsland_128x128x128");
                var storage = LoadRandomizedVoxelMapPrefab(filePath);
                AddVoxelMap(voxelFilename, storage, new Vector3(-20, -110, -60) + offset);
            }
        }

        public static MyStorageBase LoadRandomizedVoxelMapPrefab(string prefabFilePath)
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
            switch ((MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum)asteroidAmount)
            {
                case MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralNone:
                    sessionSettings.ProceduralDensity = 0.00f;
                    break;
                case MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralLow:
                    sessionSettings.ProceduralDensity = 0.25f;
                    break;
                case MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralNormal:
                    sessionSettings.ProceduralDensity = 0.35f;
                    break;
                case MyGuiScreenWorldGeneratorSettings.AsteroidAmountEnum.ProceduralHigh:
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
