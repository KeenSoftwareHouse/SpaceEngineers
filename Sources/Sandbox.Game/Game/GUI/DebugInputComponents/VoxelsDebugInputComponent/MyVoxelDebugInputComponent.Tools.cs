using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.FileSystem;
using VRage.Game.Entity;
using VRage.Input;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.GUI.DebugInputComponents
{
    public partial class MyVoxelDebugInputComponent
    {
        private class ToolsComponent : MyDebugComponent
        {
            private MyVoxelDebugInputComponent m_comp;

            private MyVoxelBase m_selectedVoxel;

            public ToolsComponent(MyVoxelDebugInputComponent comp)
            {
                m_comp = comp;

                AddShortcut(MyKeys.NumPad8, true, false, false, false, () => "Shrink selected storage to fit.", () => StorageShrinkToFit());
            }

            private static void ShowAlert(string message, params object[] args)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.OK,
                    messageText: new StringBuilder(string.Format(message, args)),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionWarning)
                    ));
            }

            private static void Confirm(string message, Action successCallback)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: new StringBuilder(message),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionWarning),
                    callback: x => { if (x == MyGuiScreenMessageBox.ResultEnum.YES) successCallback(); }
                    ));
            }

            private bool StorageShrinkToFit()
            {
                if (m_selectedVoxel == null)
                {
                    ShowAlert("Please select a voxel map with the voxel probe box.");
                    return true;
                }

                if (m_selectedVoxel is MyPlanet)
                {
                    ShowAlert("Planets cannot be shrunk to fit.");
                    return true;
                }

                long totalSize = m_selectedVoxel.Size.Size;

                Confirm(string.Format("Are you sure you want to shrink \"{0}\" ({1} voxels total)? This will overwrite the original storage.", m_selectedVoxel.StorageName, totalSize), ShrinkVMap);

                return true;
            }

            void ShrinkVMap()
            {
                Vector3I min, max;

                m_selectedVoxel.GetFilledStorageBounds(out min, out max);

                MyVoxelMapStorageDefinition def = null;
                if (m_selectedVoxel.AsteroidName != null)
                    MyDefinitionManager.Static.TryGetVoxelMapStorageDefinition(m_selectedVoxel.AsteroidName, out def);

                var origSize = m_selectedVoxel.Size;

                var tightSize = max - min + 1;

                var storage = new MyOctreeStorage(null, tightSize);

                var offset = (storage.Size - tightSize)/2 + 1;

                MyStorageData data = new MyStorageData();
                data.Resize(tightSize);

                m_selectedVoxel.Storage.ReadRange(data, MyStorageDataTypeFlags.ContentAndMaterial, 0, min, max);

                min = offset;
                max = offset + tightSize - 1;
                storage.WriteRange(data, MyStorageDataTypeFlags.ContentAndMaterial, ref min, ref max);

                var newMap = MyWorldGenerator.AddVoxelMap(m_selectedVoxel.StorageName, storage, m_selectedVoxel.WorldMatrix);

                m_selectedVoxel.Close();

                newMap.Save = true;

                if (def == null)
                {
                    ShowAlert("Voxel map {0} does not have a definition, the shrunk voxel map will be saved with the world instead.", m_selectedVoxel.StorageName);
                }
                else
                {
                    byte[] cVmapData;
                    newMap.Storage.Save(out cVmapData);

                    using (var ostream = MyFileSystem.OpenWrite(Path.Combine(MyFileSystem.ContentPath, def.StorageFile), FileMode.Open))
                    {
                        ostream.Write(cVmapData, 0, cVmapData.Length);
                    }
                    var notification = new MyHudNotification(MyStringId.GetOrCompute("Voxel prefab {0} updated succesfuly (size changed from {1} to {2})."), 4000);
                    notification.SetTextFormatArguments(def.Id.SubtypeName, origSize, storage.Size);
                    MyHud.Notifications.Add(notification);
                }
            }

            public override void Draw()
            {
                base.Draw();

                if (MySession.Static == null) return;

                const float raycastDist = 200;

                var ray = new LineD(MySector.MainCamera.Position, MySector.MainCamera.Position + raycastDist * MySector.MainCamera.ForwardVector);

                var entities = new List<MyLineSegmentOverlapResult<MyEntity>>();

                MyGamePruningStructure.GetTopmostEntitiesOverlappingRay(ref ray, entities, MyEntityQueryType.Static);

                double closest = double.PositiveInfinity;

                foreach (var e in entities)
                {
                    var voxel = e.Element as MyVoxelBase;
                    if (voxel != null && e.Distance < closest)
                    {
                        m_selectedVoxel = voxel;
                    }
                }

                if (m_selectedVoxel != null) Text(Color.DarkOrange, 1.5f, "Selected Voxel: {0}:{1}", m_selectedVoxel.StorageName, m_selectedVoxel.EntityId);
            }

            public override string GetName()
            {
                return "Tools";
            }
        }
    }
}
