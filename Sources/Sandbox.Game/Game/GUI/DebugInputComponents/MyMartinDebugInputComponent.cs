using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using VRage;
using VRage.FileSystem;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
    public class MyMartinInputComponent : MyDebugComponent
    {
        //public static long OuterSum;
        //public static long InnerSum;

        public override string GetName()
        {
            return "Martin";
        }

        public override bool HandleInput()
        {
            bool handled = false;

            //VoxelReading();
            //VoxelPlacement();
            //VoxelCellDrawing();

            //Stats.Timing.Write("Sum of outer loops", OuterSum / (float)Stopwatch.Frequency, VRage.Stats.MyStatTypeEnum.CurrentValue, 100, 4);
            //Stats.Timing.Write("Sum of inner loops", InnerSum / (float)Stopwatch.Frequency, VRage.Stats.MyStatTypeEnum.CurrentValue, 100, 4);

            // All of my keypresses require M to be pressed as well.
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

            return handled;
        }

        private static void VoxelCellDrawing()
        {
            var controlledObject = MySession.ControlledEntity;
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
                var cache = new MyStorageDataCache();
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
                    icon: definition.Icon,
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
                var character = MySession.LocalCharacter;
                if (character != null)
                    MyFakes.FakeTarget = character;
            }
            else
            {
                MyFakes.FakeTarget = null;
            }
        }
    }
}
