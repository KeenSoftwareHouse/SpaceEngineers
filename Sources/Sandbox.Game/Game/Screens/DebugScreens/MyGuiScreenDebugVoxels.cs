using ParallelTasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender;
using Path = System.IO.Path;
using SearchOption = VRage.FileSystem.MySearchOption;

namespace Sandbox.Game.Screens.DebugScreens
{
    [MyDebugScreen("Game", "Voxels")]
    public class MyGuiScreenDebugVoxels : MyGuiScreenDebugBase
    {
        MyGuiControlCombobox m_filesCombo;
        MyGuiControlCombobox m_materialsCombo;
        MyGuiControlCombobox m_shapesCombo;

        string m_selectedVoxelFile;
        string m_selectedVoxelMaterial;

        public MyGuiScreenDebugVoxels()
        {
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugVoxels";
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            BackgroundColor = new Vector4(1f, 1f, 1f, 0.5f);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.13f);

            AddCaption("Voxels", Color.Yellow.ToVector4());
            AddShareFocusHint();

            AddSlider("Max precalc time", 0f, 20f, null, MemberHelper.GetMember(() => MyFakes.MAX_PRECALC_TIME_IN_MILLIS));
            AddCheckBox("Enable yielding", null, MemberHelper.GetMember(() => MyFakes.ENABLE_YIELDING_IN_PRECALC_TASK));

            m_filesCombo = MakeComboFromFiles(Path.Combine(MyFileSystem.ContentPath, "VoxelMaps"));
            m_filesCombo.ItemSelected += filesCombo_OnSelect;

            m_materialsCombo = AddCombo();
            foreach (var material in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
            {
                m_materialsCombo.AddItem(material.Index, new StringBuilder(material.Id.SubtypeName));
            }
            m_materialsCombo.ItemSelected += materialsCombo_OnSelect;
            m_materialsCombo.SelectItemByIndex(0);
            AddCombo<MyVoxelDebugDrawMode>(null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_VOXELS_MODE));

            AddButton(new StringBuilder("Remove all"), onClick: RemoveAllAsteroids);
            AddButton(new StringBuilder("Generate render"), onClick: GenerateRender);
            AddButton(new StringBuilder("Generate physics"), onClick: GeneratePhysics);
            AddButton(new StringBuilder("Voxelize all"), onClick: ForceVoxelizeAllVoxelMaps);
            AddButton(new StringBuilder("Resave prefabs"), onClick: ResavePrefabs);
            AddButton(new StringBuilder("Reset all"), onClick: ResetAll);
            AddButton(new StringBuilder("Reset part"), onClick: ResetPart);
            m_currentPosition.Y += 0.01f;

            AddCheckBox("Geometry cell debug draw", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_VOXEL_GEOMETRY_CELL));
            AddCheckBox("Freeze terrain queries", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FreezeTerrainQueries));
            AddCheckBox("Debug render clipmap cells", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DebugRenderClipmapCells));
            AddCheckBox("Debug clipmap lod colors", () => MyRenderSettings.DebugClipmapLodColor, (value) => MyRenderSettings.DebugClipmapLodColor = value);
            AddCheckBox("Enable physics shape discard", null, MemberHelper.GetMember(() => MyFakes.ENABLE_VOXEL_PHYSICS_SHAPE_DISCARDING));
            AddCheckBox("Wireframe", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.Wireframe));
            AddCheckBox("Green background", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ShowGreenBackground));
            m_currentPosition.Y += 0.01f;

            AddSlider("Clipmap highest lod", MyClipmap.DebugClipmapMostDetailedLod, 0f, 15.9f, (slider) => MyClipmap.DebugClipmapMostDetailedLod = slider.Value);
            m_currentPosition.Y += 0.01f;
        }

        private MyGuiControlCombobox MakeComboFromFiles(string path, string filter = "*", SearchOption search = SearchOption.AllDirectories)
        {
            var combo = AddCombo();
            long key = 0;
            combo.AddItem(key++, "");
            foreach (var file in MyFileSystem.GetFiles(path, filter, search))
            {
                combo.AddItem(key++, Path.GetFileNameWithoutExtension(file));
            }
            combo.SelectItemByIndex(0);
            return combo;
        }

        private void filesCombo_OnSelect()
        {
            if (m_filesCombo.GetSelectedKey() == 0)
                return;
            m_selectedVoxelFile = Path.Combine(MyFileSystem.ContentPath, m_filesCombo.GetSelectedValue().ToString() + MyVoxelConstants.FILE_EXTENSION);
        }

        private void materialsCombo_OnSelect()
        {
            m_selectedVoxelMaterial = m_materialsCombo.GetSelectedValue().ToString();
        }

        private void RemoveAllAsteroids(MyGuiControlButton sender)
        {
            MySession.Static.VoxelMaps.Clear();
        }

        private void GenerateRender(MyGuiControlButton sender)
        {
            foreach (var voxelMap in MySession.Static.VoxelMaps.Instances)
            {
                voxelMap.Render.InvalidateRenderObjects();
            }
        }

        private void GeneratePhysics(MyGuiControlButton sender)
        {
            foreach (var voxelMap in MySession.Static.VoxelMaps.Instances)
            {
                if (voxelMap.Physics != null)
                {
                    (voxelMap.Physics as MyVoxelPhysicsBody).GenerateAllShapes();
                }
            }
        }

        private void ResavePrefabs(MyGuiControlButton sender)
        {
            var fileList = MyFileSystem.GetFiles(
                MyFileSystem.ContentPath,
                //Path.Combine(MyFileSystem.ContentPath, "VoxelMaps"),
                "*" + MyVoxelConstants.FILE_EXTENSION,
                SearchOption.AllDirectories).ToArray();

            for (int i = 0; i < fileList.Length; ++i)
            {
                var file = fileList[i];
                Debug.WriteLine(string.Format("Resaving [{0}/{1}] '{2}'", i+1, fileList.Length, file));
                var storage = MyStorageBase.LoadFromFile(file);
                byte[] savedData;
                storage.Save(out savedData);
                using (var stream = MyFileSystem.OpenWrite(file, System.IO.FileMode.Open))
                {
                    stream.Write(savedData, 0, savedData.Length);
                }
            }
            Debug.WriteLine("Saving prefabs finished.");
        }

        private void ForceVoxelizeAllVoxelMaps(MyGuiControlBase sender)
        {
            var instances = MySession.Static.VoxelMaps.Instances;
            int i = 0;
            foreach (var voxelMap in instances)
            {
                i++;
                Debug.WriteLine("Voxel map {0}/{1}", i, instances.Count);
                var octree = voxelMap.Storage as MyOctreeStorage;
                if (octree != null)
                    octree.Voxelize(MyStorageDataTypeFlags.Content);
            }
        }

        private void ResetAll(MyGuiControlBase sender)
        {
            var instances = MySession.Static.VoxelMaps.Instances;
            int i = 0;
            foreach (var voxelMap in instances)
            {
                i++;
                Debug.WriteLine("Voxel map {0}/{1}", i, instances.Count);
                if (!(voxelMap is MyVoxelPhysics))
                {
                    var octree = voxelMap.Storage as MyOctreeStorage;
                    if (octree != null)
                        octree.Reset(MyStorageDataTypeFlags.All);
                }
            }
        }

        private void ResetPart(MyGuiControlBase sender)
        {
            var instances = MySession.Static.VoxelMaps.Instances;
            foreach (var voxelMap in instances)
            {
                if (!(voxelMap is MyVoxelPhysics))
                {
                    var octree = voxelMap.Storage as MyOctreeStorage;
                    var worldAabb = voxelMap.PositionComp.WorldAABB;
                    BoundingBoxD resetAabb;
                    resetAabb.Min = worldAabb.Min + worldAabb.Size * 0.25f;
                    resetAabb.Max = worldAabb.Max - worldAabb.Size * 0.25f;
                    if (octree != null)
                        octree.ResetOutsideBorders(voxelMap, resetAabb);
                }
            }
        }
    }
}