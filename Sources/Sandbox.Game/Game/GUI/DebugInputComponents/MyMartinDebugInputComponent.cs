using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Diagnostics;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.AI;
using Sandbox.Game.AI.Pathfinding;
using Sandbox.Game.Entities.Character;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Voxels;

namespace Sandbox.Game.Gui
{
    public class MyMartinInputComponent : MyDebugComponent
    {
        //public static long OuterSum;
        //public static long InnerSum;

        public class MyMarker
        {
            public MyMarker(Vector3D position, Color color) { this.position = position; this.color = color; }
            public Vector3D position;
            public Color color;
        }

        List<MyMarker> m_markers = new List<MyMarker>();

        public MyMartinInputComponent()
        {
            AddShortcut(MyKeys.NumPad7, true, false, false, false, () => "Add bots", AddBots);
            AddShortcut(MyKeys.Z, true, false, false, false, () => "One AI step", OneAIStep);
            AddShortcut(MyKeys.NumPad8, true, false, false, false, () => "One Voxel step", OneVoxelStep);
            AddShortcut(MyKeys.Insert, true, false, false, false, () => "Add one bot", AddOneBot);
            AddShortcut(MyKeys.Home, true, false, false, false, () => "Add one barb", AddOneBarb);
            AddShortcut(MyKeys.T, true, false, false, false, () => "Do some action", DoSomeAction);
            AddShortcut(MyKeys.Y, true, false, false, false, () => "Clear some action", ClearSomeAction);
            AddShortcut(MyKeys.B, true, false, false, false, () => "Add berries", AddBerries);
            AddShortcut(MyKeys.L, true, false, false, false, () => "return to Last bot memory", ReturnToLastMemory);
            AddShortcut(MyKeys.N, true, false, false, false, () => "select Next bot", SelectNextBot);
            AddShortcut(MyKeys.K, true, false, false, false, () => "Kill not selected bots", KillNotSelectedBots);
            AddShortcut(MyKeys.M, true, false, false, false, () => "Toggle marker", ToggleMarker);

            AddSwitch(MyKeys.NumPad0, SwitchSwitch, new MyRef<bool>(() => MyFakes.DEBUG_BEHAVIOR_TREE, val => { MyFakes.DEBUG_BEHAVIOR_TREE = val; }), "allowed debug beh tree");
            AddSwitch(MyKeys.NumPad1, SwitchSwitch, new MyRef<bool>(() => MyFakes.DEBUG_BEHAVIOR_TREE_ONE_STEP, val => { MyFakes.DEBUG_BEHAVIOR_TREE_ONE_STEP = val; }), "one beh tree step");
            AddSwitch(MyKeys.H, SwitchSwitch, new MyRef<bool>(() => MyFakes.ENABLE_AUTO_HEAL, val => { MyFakes.ENABLE_AUTO_HEAL = val; }), "enable auto Heal");
        }

        private bool AddBerries()
        {
            AddSomething("Berries", 10);
            return true;
        }

        private void AddSomething(string something, int amount)
        {
            foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var physicalItemDef = definition as MyPhysicalItemDefinition;
                if (physicalItemDef == null || physicalItemDef.CanSpawnFromScreen == false)
                    continue;

                if (definition.DisplayNameText == something)
                {
                    MyEntity invObject = MySession.Static.ControlledEntity as MyEntity;
                    MyInventory inventory = invObject.GetInventory(0) as MyInventory;
                    if (inventory != null)
                    {
                        var builder = (MyObjectBuilder_PhysicalObject)VRage.ObjectBuilders.MyObjectBuilderSerializer.CreateNewObject(definition.Id);
                        inventory.DebugAddItems(amount, builder);
                    }
                    break;
                }
            }
        }
        private void ConsumeSomething(string something, int amount)
        {
            foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var physicalItemDef = definition as MyPhysicalItemDefinition;
                if (physicalItemDef == null || physicalItemDef.CanSpawnFromScreen == false)
                    continue;

                if (definition.DisplayNameText == something)
                {
                    MyEntity invObject = MySession.Static.ControlledEntity as MyEntity;
                    MyInventory inventory = invObject.GetInventory(0) as MyInventory;
                    if (inventory != null)
                    {
                        var builder = (MyObjectBuilder_PhysicalObject)VRage.ObjectBuilders.MyObjectBuilderSerializer.CreateNewObject(definition.Id);
                        inventory.ConsumeItem(physicalItemDef.Id, amount, MySession.Static.LocalCharacterEntityId);
                    }
                    break;
                }
            }
        }

        private bool ReturnToLastMemory()
        {
            if (Sandbox.Engine.Utils.MyDebugDrawSettings.DEBUG_DRAW_BOTS)
            {
                // get active bot
                MyBotCollection bots = MyAIComponent.Static.Bots;
                foreach (var entry in MyAIComponent.Static.Bots.GetAllBots())
                {
                    var localBot = entry.Value;
                    var agent = localBot as MyAgentBot;

                    // return to previous bot memory
                    if ( agent != null && bots.IsBotSelectedForDegugging(agent) )
                    {
                        agent.ReturnToLastMemory();
                    }
                }
            }
            return true;
        }

        private bool ToggleMarker()
        {
            Vector3D pos = new Vector3D();
            if ( GetDirectedPositionOnGround(MySector.MainCamera.Position, MySector.MainCamera.ForwardVector, 1000, out pos) )
            {
                MyMarker marker = FindClosestMarkerInArea(pos, 1);
                if (marker != null)
                    m_markers.Remove(marker);
                else
                    m_markers.Add(new MyMarker(pos, Color.Blue));
                return true;
            }

            return false;
        }

        public bool GetDirectedPositionOnGround(Vector3D initPosition, Vector3D direction, float amount, out Vector3D outPosition, float raycastHeight = 100.0f)
        {
            //List<MyPhysics.HitInfo> m_tmpRaycastOutput = new List<MyPhysics.HitInfo>();
            //MyPhysics.CastRay(initPosition, end, m_tmpRaycastOutput);

            outPosition = default(Vector3D);
            var voxelMap = MySession.Static.VoxelMaps.TryGetVoxelMapByNameStart("Ground");
            if (voxelMap == null) return false;
            Vector3D toPosition = initPosition + direction * amount;
            LineD raycastLine = new LineD(initPosition, toPosition);
            Vector3D? groundIntersect = null;
            voxelMap.GetIntersectionWithLine(ref raycastLine, out groundIntersect);

            if (groundIntersect == null) return false;

            outPosition = (Vector3D)groundIntersect;
            return true;
        }

        MyMarker FindClosestMarkerInArea(Vector3D pos, double maxDistance)
        {
            double minDist = double.MaxValue;
            MyMarker closest = null;
            foreach(MyMarker marker in m_markers)
            {
                double dist = (marker.position-pos).Length();
                if ( dist < minDist )
                {
                    closest = marker;
                    minDist = dist;
                }
            }
            if (minDist < maxDistance)
                return closest;
            return null;
        }

        void AddMarker(MyMarker marker)
        {
            m_markers.Add(marker);
        }




        public bool SelectNextBot()
        {
            MyAIComponent.Static.Bots.DebugSelectNextBot();
            return true;
        }

        public bool KillNotSelectedBots()
        {
            if (Sandbox.Engine.Utils.MyDebugDrawSettings.DEBUG_DRAW_BOTS)
            {
                MyBotCollection bots = MyAIComponent.Static.Bots;
                foreach (var entry in MyAIComponent.Static.Bots.GetAllBots())
                {
                    var localBot = entry.Value;
                    var agent = localBot as MyAgentBot;

                    // return to previous bot memory
                    if (agent != null && !bots.IsBotSelectedForDegugging(agent))
                    {
                        if (agent.Player.Controller.ControlledEntity is MyCharacter)
                        {
                            MyDamageInformation damageInfo = new MyDamageInformation(false, 1000, MyDamageType.Weapon, MySession.Static.LocalPlayerId);
                            (agent.Player.Controller.ControlledEntity as MyCharacter).Kill(true, damageInfo);
                        }
                    }
                }
            }
            return true;
        }

        public bool SwitchSwitch(MyKeys key)
        {
            bool value = !GetSwitchValue(key);
            SetSwitch(key, value);
            return true;
        }

        public bool SwitchSwitchDebugBeh(MyKeys key)
        {
            MyFakes.DEBUG_BEHAVIOR_TREE = !MyFakes.DEBUG_BEHAVIOR_TREE;
            SetSwitch(key, MyFakes.DEBUG_BEHAVIOR_TREE);
            return true;
        }

        public bool SwitchSwitchOneStep(MyKeys key)
        {
            MyFakes.DEBUG_BEHAVIOR_TREE_ONE_STEP = true;
            SetSwitch(key, MyFakes.DEBUG_BEHAVIOR_TREE_ONE_STEP);
            return true;
        }


        private bool DoSomeAction()
        {
            MyFakes.DO_SOME_ACTION = true;
            return true;
        }

        private bool ClearSomeAction()
        {
            MyFakes.DO_SOME_ACTION = false;
            return true;
        }

        private bool AddBots()
        {
            for (int i = 0; i < 10; i++)
            {
                var barbarianBehavior = MyDefinitionManager.Static.GetBotDefinition(new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.AI.Bot.MyObjectBuilder_HumanoidBot), "TestingBarbarian")) as MyAgentDefinition;
                MyAIComponent.Static.SpawnNewBot(barbarianBehavior);
            }
            MyPathfindingStopwatch.StartMeasuring();
            return true;
        }

        private bool AddOneBot()
        {
            //var barbarianBehavior = MyDefinitionManager.Static.GetBotDefinition(new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.AI.Bot.MyObjectBuilder_HumanoidBot), "TestingBarbarian")) as MyAgentDefinition;
            //var barbarianBehavior = MyDefinitionManager.Static.GetBotDefinition(new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.AI.Bot.MyObjectBuilder_HumanoidBot), "SwordBarbarian")) as MyAgentDefinition;
            //var barbarianBehavior = MyDefinitionManager.Static.GetBotDefinition(new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.AI.Bot.MyObjectBuilder_HumanoidBot), "NormalWoodcutter")) as MyAgentDefinition;
            var barbarianBehavior = MyDefinitionManager.Static.GetBotDefinition(new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.AI.Bot.MyObjectBuilder_HumanoidBot), "NormalPeasant")) as MyAgentDefinition;
            MyAIComponent.Static.SpawnNewBot(barbarianBehavior);
            MyPathfindingStopwatch.StartMeasuring();
            return true;
        }
        private bool AddOneBarb()
        {
            //var barbarianBehavior = MyDefinitionManager.Static.GetBotDefinition(new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.AI.Bot.MyObjectBuilder_HumanoidBot), "TestingBarbarian")) as MyAgentDefinition;
            var barbarianBehavior = MyDefinitionManager.Static.GetBotDefinition(new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.AI.Bot.MyObjectBuilder_HumanoidBot), "SwordBarbarian")) as MyAgentDefinition;
            //var barbarianBehavior = MyDefinitionManager.Static.GetBotDefinition(new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.AI.Bot.MyObjectBuilder_HumanoidBot), "NormalWoodcutter")) as MyAgentDefinition;
            //var barbarianBehavior = MyDefinitionManager.Static.GetBotDefinition(new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.AI.Bot.MyObjectBuilder_HumanoidBot), "NormalPeasant")) as MyAgentDefinition;
            MyAIComponent.Static.SpawnNewBot(barbarianBehavior);
            MyPathfindingStopwatch.StartMeasuring();
            return true;
        }

        private bool OneAIStep()
        {
            MyFakes.DEBUG_ONE_AI_STEP = true;
            return true;
        }
        private bool OneVoxelStep()
        {
            MyFakes.DEBUG_ONE_VOXEL_PATHFINDING_STEP = true;
            return true;
        }


        public override string GetName()
        {
            return "Martin";
        }

        public override bool HandleInput()
        {
            bool handled = false;

            if (MySession.Static == null)
                return false;// game isn't loaded yet

            // check of autoheal
            CheckAutoHeal();
            //VoxelReading();
            //VoxelPlacement();
            //VoxelCellDrawing();

            //Stats.Timing.Write("Sum of outer loops", OuterSum / (float)Stopwatch.Frequency, VRage.Stats.MyStatTypeEnum.CurrentValue, 100, 4);
            //Stats.Timing.Write("Sum of inner loops", InnerSum / (float)Stopwatch.Frequency, VRage.Stats.MyStatTypeEnum.CurrentValue, 100, 4);

            // All of my keypresses require M to be pressed as well.
#if false
            if (!MyInput.Static.IsKeyPress(MyKeys.M))
                return handled;

            //foreach (var file in MyFileSystem.GetFiles(Path.Combine(MyFileSystem.ContentPath, "Data", "Screens")))
            //{
            //    MyObjectBuilder_GuiScreen screen;
            //    MyObjectBuilderSerializer.DeserializeXML(file, out screen);
            //}

            if (MySession.Static == null)
                return handled;

            // Toggle MyCharacter as enemy
            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad3))
            {
                MakeCharacterFakeTarget();
                handled = true;
            }

            // Reload definitions.
            if (MyInput.Static.IsKeyPress(MyKeys.NumPad4))
            {
                var inst = MyDefinitionManager.Static;
                inst.UnloadData();
                inst.LoadData(MySession.Static.Mods);
                handled = true;
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.R))
            {
                MyScreenManager.RecreateControls();
                handled = true;
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad5))
            {
                MakeScreenWithIconGrid();
                handled = true;
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad6))
            {
                foreach (var entity in MyEntities.GetEntities())
                    if (entity is MyFloatingObject)
                        entity.Close();
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad7))
            {

                XDocument document = XDocument.Parse(File.ReadAllText(Path.Combine(MyFileSystem.ContentPath, "Data", "Localization", "MyTexts.resx")));

                var data = document.Element("root").Elements("data").OrderBy(x => x.Attribute("name").Value);
                foreach (var item in data)
                {
                    Console.WriteLine(string.Format("{0}: {1}",
                        item.Attribute("name").Value,
                        item.Element("value").Value));
                }

            }
#endif

            return base.HandleInput();

            //return handled;
        }

        private void CheckAutoHeal()
        {
            if (MyFakes.ENABLE_AUTO_HEAL)
            {
                MyCharacter invObject = MySession.Static.ControlledEntity as MyCharacter;
                if (invObject != null && invObject.StatComp!=null)
                {
                    if (invObject.StatComp.HealthRatio < 1)
                    {
                        // eat some berries
                        AddSomething("Berries", 1);
                        ConsumeSomething("Berries", 1);
                    }
                }
            }
        }



        private static void VoxelCellDrawing()
        {
            var controlledObject = MySession.Static.ControlledEntity;
            if (controlledObject == null)
                return;
            var camera = MySector.MainCamera;
            if (camera == null)
                return;

            var entity = controlledObject.Entity;

            //var targetPosition = camera.Position + camera.ForwardVector * MyVoxelConstants.RENDER_CELL_SIZE_IN_METRES;
            var targetPosition = entity.WorldMatrix.Translation;// +entity.WorldMatrix.Forward * MyVoxelConstants.RENDER_CELL_SIZE_IN_METRES;
            MyVoxelBase targetVoxelMap = null;
            foreach (var voxelMap in MySession.Static.VoxelMaps.Instances)
            {
                if (voxelMap.PositionComp.WorldAABB.Contains(targetPosition) == ContainmentType.Contains)
                {
                    targetVoxelMap = voxelMap;
                    break;
                }
            }
            if (targetVoxelMap == null)
                return;

            BoundingBoxD renderCellAABB, geometryCellAABB, voxelAABB;
            MyCellCoord tmp = new MyCellCoord();
            MyVoxelCoordSystems.WorldPositionToRenderCellCoord(0, targetVoxelMap.PositionLeftBottomCorner, ref targetPosition, out tmp.CoordInLod);
            MyVoxelCoordSystems.RenderCellCoordToWorldAABB(targetVoxelMap.PositionLeftBottomCorner, ref tmp, out renderCellAABB);

            MyVoxelCoordSystems.WorldPositionToGeometryCellCoord(targetVoxelMap.PositionLeftBottomCorner, ref targetPosition, out tmp.CoordInLod);
            MyVoxelCoordSystems.GeometryCellCoordToWorldAABB(targetVoxelMap.PositionLeftBottomCorner, ref tmp.CoordInLod, out geometryCellAABB);

            MyVoxelCoordSystems.WorldPositionToVoxelCoord(targetVoxelMap.PositionLeftBottomCorner, ref targetPosition, out tmp.CoordInLod);
            MyVoxelCoordSystems.VoxelCoordToWorldAABB(targetVoxelMap.PositionLeftBottomCorner, ref tmp.CoordInLod, out voxelAABB);

            VRageRender.MyRenderProxy.DebugDrawAABB((BoundingBoxD)voxelAABB, Vector3.UnitX, 1f, 1f, true);
            VRageRender.MyRenderProxy.DebugDrawAABB((BoundingBoxD)geometryCellAABB, Vector3.UnitY, 1f, 1f, true);
            VRageRender.MyRenderProxy.DebugDrawAABB((BoundingBoxD)renderCellAABB, Vector3.UnitZ, 1f, 1f, true);
        }

        private static void VoxelPlacement()
        {
            var camera = MySector.MainCamera;
            if (camera == null)
                return;

            var offset = 0; // MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;
            var targetPosition = camera.Position + (Vector3D)camera.ForwardVector * 4.5f - offset;
            MyVoxelBase targetVoxelMap = null;
            foreach (var voxelMap in MySession.Static.VoxelMaps.Instances)
            {
                if (voxelMap.PositionComp.WorldAABB.Contains(targetPosition) == ContainmentType.Contains)
                {
                    targetVoxelMap = voxelMap;
                    break;
                }
            }
            if (targetVoxelMap == null)
                return;

            Vector3I targetVoxel;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(targetVoxelMap.PositionLeftBottomCorner, ref targetPosition, out targetVoxel);
            MyVoxelCoordSystems.VoxelCoordToWorldPosition(targetVoxelMap.PositionLeftBottomCorner, ref targetVoxel, out targetPosition);
            targetPosition += offset;
            var size = 3.0f;
            const int shapeType = 0;

            {
                BoundingBoxD aabb;
                MyVoxelCoordSystems.VoxelCoordToWorldAABB(targetVoxelMap.PositionLeftBottomCorner, ref targetVoxel, out aabb);
                VRageRender.MyRenderProxy.DebugDrawAABB(aabb, Color.Blue, 1f, 1f, true);
            }

            BoundingSphereD sphere;
            BoundingBoxD box;
            if (shapeType == 0)
            {
                sphere = new BoundingSphereD(targetPosition, size * 0.5f);
                VRageRender.MyRenderProxy.DebugDrawSphere(targetPosition, size * 0.5f, Color.White, 1f, true);
            }
            else if (shapeType == 1)
            {
                box = new BoundingBoxD(
                    targetPosition - size * 0.5f,
                    targetPosition + size * 0.5f);
                VRageRender.MyRenderProxy.DebugDrawAABB(box, Color.White, 1f, 1f, true);
            }
            else if (shapeType == 2)
            {
                MyVoxelCoordSystems.WorldPositionToVoxelCoord(targetVoxelMap.PositionLeftBottomCorner, ref targetPosition, out targetVoxel);
                //targetVoxel = Vector3I.Zero;
                MyVoxelCoordSystems.VoxelCoordToWorldAABB(targetVoxelMap.PositionLeftBottomCorner, ref targetVoxel, out box);
                VRageRender.MyRenderProxy.DebugDrawAABB(box, Vector3.One, 1f, 1f, true);
            }

            bool leftPressed = MyInput.Static.IsLeftMousePressed();
            if (leftPressed)
            {
                MyShape shape;
                if (shapeType == 0)
                {
                    shape = new MyShapeSphere()
                    {
                        Center = sphere.Center,
                        Radius = (float)sphere.Radius,
                    };
                }
                else if (shapeType == 1 || shapeType == 2)
                {
                    shape = new MyShapeBox()
                    {
                        Boundaries = box,
                    };
                }
                if (shape != null)
                {
                    float dummy;
                    MyVoxelMaterialDefinition dummy2;
                    MyVoxelGenerator.CutOutShapeWithProperties(targetVoxelMap, shape, out dummy, out dummy2);
                }
            }
        }

        private static void VoxelReading()
        {
            var camera = MySector.MainCamera;
            if (camera == null)
                return;

            var offset = 0; // MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF;
            var targetPosition = camera.Position + (Vector3D)camera.ForwardVector * 4.5f - offset;
            MyVoxelBase targetVoxelMap = null;
            foreach (var voxelMap in MySession.Static.VoxelMaps.Instances)
            {
                if (voxelMap.PositionComp.WorldAABB.Contains(targetPosition) == ContainmentType.Contains)
                {
                    targetVoxelMap = voxelMap;
                    break;
                }
            }
            if (targetVoxelMap == null)
                return;

            var targetMin = targetPosition - Vector3.One * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
            var targetMax = targetPosition + Vector3.One * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
            Vector3I minVoxel, maxVoxel;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(targetVoxelMap.PositionLeftBottomCorner, ref targetMin, out minVoxel);
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(targetVoxelMap.PositionLeftBottomCorner, ref targetMax, out maxVoxel);
            MyVoxelCoordSystems.VoxelCoordToWorldPosition(targetVoxelMap.PositionLeftBottomCorner, ref minVoxel, out targetMin);
            MyVoxelCoordSystems.VoxelCoordToWorldPosition(targetVoxelMap.PositionLeftBottomCorner, ref maxVoxel, out targetMax);

            {
                BoundingBoxD bbox = BoundingBoxD.CreateInvalid();
                bbox.Include(targetMin);
                bbox.Include(targetMax);
                VRageRender.MyRenderProxy.DebugDrawAABB(bbox, Vector3.One, 1f, 1f, true);
            }

            if (MyInput.Static.IsNewLeftMousePressed())
            {
                var cache = new MyStorageData();
                cache.Resize(minVoxel, maxVoxel);
                targetVoxelMap.Storage.ReadRange(cache, MyStorageDataTypeFlags.Content, 0, ref minVoxel, ref maxVoxel);
                targetVoxelMap.Storage.WriteRange(cache, MyStorageDataTypeFlags.Content, ref minVoxel, ref maxVoxel);
                Debug.Assert(true);

            }
        }

        private static void MakeScreenWithIconGrid()
        {
            var screen = new TmpScreen();
            var grid = new MyGuiControlGrid();
            screen.Controls.Add(grid);
            grid.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;
            grid.VisualStyle = MyGuiControlGridStyleEnum.Inventory;
            grid.RowsCount = 12;
            grid.ColumnsCount = 18;
            foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
            {
                grid.Add(new MyGuiControlGrid.Item(
                    icons: definition.Icons,
                    toolTip: definition.DisplayNameText));
            }

            MyGuiSandbox.AddScreen(screen);
        }

        class TmpScreen : MyGuiScreenBase
        {
            public TmpScreen()
                : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, null)
            {
                EnabledBackgroundFade = true;
                m_size = new Vector2(0.99f, 0.88544f);

                AddCaption("<new screen>", Vector4.One, new Vector2(0, 0.03f));
                CloseButtonEnabled = true;
                RecreateControls(true);
            }

            public override string GetFriendlyName()
            {
                return "TmpScreen";
            }

            public override void RecreateControls(bool contructor)
            {
                base.RecreateControls(contructor);
            }

        }

        private static void MakeCharacterFakeTarget()
        {
            if (MyFakes.FakeTarget == null)
            {
                var character = MySession.Static.LocalCharacter;
                if (character != null)
                    MyFakes.FakeTarget = character;
            }
            else
            {
                MyFakes.FakeTarget = null;
            }
        }

        public override void Draw()
        {
            base.Draw();

            foreach ( MyMarker marker in m_markers )
            {
                VRageRender.MyRenderProxy.DebugDrawSphere(marker.position, 0.5f, marker.color, 0.8f, true, cull: true);
                VRageRender.MyRenderProxy.DebugDrawSphere(marker.position, 0.1f, marker.color, 1.0f, false, cull: true);
                Vector3D textpos = marker.position;
                //textpos.Z += 1;
                textpos.Y += 0.6f;
                string str = String.Format("{0:0.0},{1:0.0},{2:0.0}", marker.position.X, marker.position.Y, marker.position.Z);
                VRageRender.MyRenderProxy.DebugDrawText3D(textpos, str, marker.color, 1, false);

            }
        }

    }

}
