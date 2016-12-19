using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents.Clipboard;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Audio;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ObjectBuilders;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities
{
    class MyVoxelClipboard
    {
        private List<MyObjectBuilder_EntityBase> m_copiedVoxelMaps = new List<MyObjectBuilder_EntityBase>();
        private List<IMyStorage> m_copiedStorages = new List<IMyStorage>();
        private List<Vector3> m_copiedVoxelMapOffsets = new List<Vector3>();
        private List<MyVoxelBase> m_previewVoxelMaps = new List<MyVoxelBase>();

        // Paste position
        private Vector3D m_pastePosition;

        // Copy position
        private float m_dragDistance;
        private Vector3 m_dragPointToPositionLocal;

        // Placement flags
        private bool m_canBePlaced;

        private MyEntity m_blockingEntity = null;

        private bool m_visible = true;

        private bool m_shouldMarkForClose = true;

        private bool m_planetMode = false;

        public bool IsActive
        {
            get;
            private set;
        }

        private void Activate()
        {
            ChangeClipboardPreview(true);
            IsActive = true;
        }

        public void Deactivate()
        {
            ChangeClipboardPreview(false);
            IsActive = false;
            m_planetMode = false;
        }

        public void Hide()
        {
            ChangeClipboardPreview(false);
            m_planetMode = false;
        }

        public void Show()
        {
            if (IsActive && m_previewVoxelMaps.Count == 0)
                ChangeClipboardPreview(true);
        }

        public void ClearClipboard()
        {
            if (IsActive)
                Deactivate();
            m_copiedVoxelMapOffsets.Clear();
            m_copiedVoxelMaps.Clear();
        }

        public void CutVoxelMap(MyVoxelBase voxelMap)
        {
            if (voxelMap == null)
                return;

            CopyVoxelMap(voxelMap);
            voxelMap.SyncObject.SendCloseRequest();
            Deactivate();
        }

        public void CopyVoxelMap(MyVoxelBase voxelMap)
        {
            if (voxelMap == null)
                return;
            m_copiedVoxelMaps.Clear();
            m_copiedVoxelMapOffsets.Clear();
            CopyVoxelMapInternal(voxelMap);
            Activate();
        }

        private void CopyVoxelMapInternal(MyVoxelBase toCopy)
        {
            m_copiedVoxelMaps.Add((MyObjectBuilder_EntityBase)toCopy.GetObjectBuilder(true));
            if (m_copiedVoxelMaps.Count == 1)
            {
                MatrixD pasteMatrix = GetPasteMatrix();
                Vector3D dragPointGlobal = toCopy.WorldMatrix.Translation;

                m_dragPointToPositionLocal = Vector3D.TransformNormal(toCopy.PositionComp.GetPosition() - dragPointGlobal, toCopy.PositionComp.WorldMatrixNormalizedInv);
                m_dragDistance = (float)(dragPointGlobal - pasteMatrix.Translation).Length();
            }
            m_copiedVoxelMapOffsets.Add(toCopy.WorldMatrix.Translation - m_copiedVoxelMaps[0].PositionAndOrientation.Value.Position);
        }

        public bool PasteVoxelMap(MyInventory buildInventory = null)
        {
            if (m_planetMode)
            {
                if (!m_canBePlaced)
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.CopyPasteAsteoridObstructed);
                    MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                    return false;
                }

                MyEntities.RemapObjectBuilderCollection(m_copiedVoxelMaps);

                for (int i = 0; i < m_copiedVoxelMaps.Count; ++i)
                {
                    Vector3D pos = m_pastePosition - m_copiedVoxelMapOffsets[i];
                    MyGuiScreenDebugSpawnMenu.SpawnPlanet(pos);
                }

                Deactivate();
                return true;
            }

            if (m_copiedVoxelMaps.Count == 0)
                return false;

            if ((m_copiedVoxelMaps.Count > 0) && !IsActive)
            {
                Activate();
                return true;
            }

            if (!m_canBePlaced)
            {
                MyHud.Notifications.Add(MyNotificationSingletons.CopyPasteAsteoridObstructed);
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                return false;
            }

            MyGuiAudio.PlaySound(MyGuiSounds.HudPlaceBlock);

            Debug.Assert(m_previewVoxelMaps.Count == 1, "More than one voxel in clipboard");

            MyGuiScreenDebugSpawnMenu.RecreateAsteroidBeforePaste((float)m_previewVoxelMaps[0].PositionComp.GetPosition().Length());

            MyEntities.RemapObjectBuilderCollection(m_copiedVoxelMaps);

            foreach (var voxelMap in m_previewVoxelMaps)
            {
                if (Sync.IsServer)
                {
                    voxelMap.CreatedByUser = true;
                    voxelMap.AsteroidName = MyGuiScreenDebugSpawnMenu.GetAsteroidName();
                    EnablePhysicsRecursively(voxelMap);
                    voxelMap.Save = true;
                    MakeVisible(voxelMap);
                    m_shouldMarkForClose = false;
                    MyEntities.RaiseEntityCreated(voxelMap);
                    voxelMap.IsReadyForReplication = true;
                }
                else
                {
                    m_shouldMarkForClose = true;

                    MyGuiScreenDebugSpawnMenu.SpawnAsteroid(voxelMap.PositionComp.GetPosition());
                }

                voxelMap.AfterPaste();
            }
            Deactivate();
            return true;
        }

        public void SetVoxelMapFromBuilder(MyObjectBuilder_EntityBase voxelMap, IMyStorage storage, Vector3 dragPointDelta, float dragVectorLength)
        {
            if (IsActive)
            {
                Deactivate();
            }

            m_copiedVoxelMaps.Clear();
            m_copiedVoxelMapOffsets.Clear();
            m_copiedStorages.Clear();

            MatrixD pasteMatrix = GetPasteMatrix();
            m_dragPointToPositionLocal = dragPointDelta;
            m_dragDistance = dragVectorLength;
            Vector3 offset = Vector3.Zero;
            if (voxelMap is MyObjectBuilder_Planet)
            {
                offset = storage.Size / 2.0f;
            }

            SetVoxelMapFromBuilderInternal(voxelMap, storage, offset);

            Activate();
        }

        private void SetVoxelMapFromBuilderInternal(MyObjectBuilder_EntityBase voxelMap, IMyStorage storage, Vector3 offset)
        {
            m_copiedVoxelMaps.Add(voxelMap);
            m_copiedStorages.Add(storage);
            m_copiedVoxelMapOffsets.Add(offset);
        }

        private void ChangeClipboardPreview(bool visible)
        {
            if (m_copiedVoxelMaps.Count == 0 || !visible)
            {
                foreach (var voxelMap in m_previewVoxelMaps)
                {
                    MyEntities.EnableEntityBoundingBoxDraw(voxelMap, false);
                    if (m_shouldMarkForClose)
                        voxelMap.Close();
                }
                m_previewVoxelMaps.Clear();
                m_visible = false;
                return;
            }

            MyEntities.RemapObjectBuilderCollection(m_copiedVoxelMaps);

            for (int i = 0; i < m_copiedVoxelMaps.Count; ++i)
            {
                var voxelMapOb = m_copiedVoxelMaps[i];
                var storage = m_copiedStorages[i];

                MyVoxelBase previewVoxelMap = null;

                if (voxelMapOb is MyObjectBuilder_VoxelMap)
                {
                    previewVoxelMap = new MyVoxelMap();
                }
                if (voxelMapOb is MyObjectBuilder_Planet)
                {
                    m_planetMode = true;
                    IsActive = visible;
                    m_visible = visible;
                    continue;
                }

                var pos = voxelMapOb.PositionAndOrientation.Value.Position;
                previewVoxelMap.Init(voxelMapOb, storage);
                previewVoxelMap.BeforePaste();

                DisablePhysicsRecursively(previewVoxelMap);
                MakeTransparent(previewVoxelMap);
                MyEntities.Add(previewVoxelMap);
                previewVoxelMap.PositionLeftBottomCorner = m_pastePosition - previewVoxelMap.Storage.Size * 0.5f;
                previewVoxelMap.PositionComp.SetPosition(m_pastePosition);
                previewVoxelMap.Save = false;

                m_previewVoxelMaps.Add(previewVoxelMap);

                IsActive = visible;
                m_visible = visible;
                m_shouldMarkForClose = true;
            }


        }

        private void MakeTransparent(MyVoxelBase voxelMap)
        {
            voxelMap.Render.Transparency = MyGridConstants.BUILDER_TRANSPARENCY;
        }

        private void MakeVisible(MyVoxelBase voxelMap)
        {
            voxelMap.Render.Transparency = 0f;
        }

        private void DisablePhysicsRecursively(MyEntity entity)
        {
            if (entity.Physics != null && entity.Physics.Enabled)
                entity.Physics.Enabled = false;

            //var voxelMap = entity as MyVoxelMap;
            //if (voxelMap != null)
            //    voxelMap.NeedsUpdate = MyEntityUpdateEnum.NONE;

            foreach (var child in entity.Hierarchy.Children)
                DisablePhysicsRecursively(child.Container.Entity as MyEntity);
        }

        private void EnablePhysicsRecursively(MyEntity entity)
        {
            if (entity.Physics != null && !entity.Physics.Enabled)
                entity.Physics.Enabled = true;

            //var voxelMap = entity as MyVoxelMap;
            //if (voxelMap != null)
            //    voxelMap.NeedsUpdate = MyEntityUpdateEnum.NONE;

            foreach (var child in entity.Hierarchy.Children)
                EnablePhysicsRecursively(child.Container.Entity as MyEntity);
        }

        public void Update()
        {
            if (!IsActive || !m_visible)
                return;

            UpdatePastePosition();
            UpdateVoxelMapTransformations();

            m_canBePlaced = TestPlacement();
        }

        private void UpdateVoxelMapTransformations()
        {
            if (m_planetMode)
            {
                for (int i = 0; i < m_copiedVoxelMaps.Count; ++i)
                {
                    MyObjectBuilder_Planet builder = m_copiedVoxelMaps[i] as MyObjectBuilder_Planet;
                    if (builder != null)
                    {
                        VRageRender.MyRenderProxy.DebugDrawSphere(m_pastePosition, builder.Radius * 1.1f, Color.Green, 1.0f, true, true);
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_previewVoxelMaps.Count; ++i)
                {
                    m_previewVoxelMaps[i].PositionLeftBottomCorner = m_pastePosition + m_copiedVoxelMapOffsets[i] - m_previewVoxelMaps[i].Storage.Size * 0.5f;
                    m_previewVoxelMaps[i].PositionComp.SetPosition(m_pastePosition + m_copiedVoxelMapOffsets[i]);
                }
            }
        }

        private void UpdatePastePosition()
        {
            // Current position of the placed entity is either simple translation or
            // it can be calculated by raycast, if we want to snap to surfaces
            MatrixD pasteMatrix = GetPasteMatrix();
            Vector3D dragVectorGlobal = pasteMatrix.Forward * m_dragDistance;

            m_pastePosition = pasteMatrix.Translation + dragVectorGlobal;

            if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
            {
                MyRenderProxy.DebugDrawSphere(m_pastePosition, 0.15f, Color.Pink.ToVector3(), 1.0f, false);
            }
        }

        private List<MyEntity> m_tmpResultList = new List<MyEntity>();
        private bool TestPlacement()
        {
            if (MySession.Static.ControlledEntity != null &&
                (MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Entity || MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator || MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Spectator))
            {
                for (int i = 0; i < m_previewVoxelMaps.Count; ++i)
                {
                    var aabb = m_previewVoxelMaps[i].PositionComp.WorldAABB;

                    using (m_tmpResultList.GetClearToken())
                    {
                        MyGamePruningStructure.GetTopMostEntitiesInBox(ref aabb, m_tmpResultList);
                        if (TestPlacement(m_tmpResultList) == false)
                        {
                            return false;
                        }
                    }
                }

                if (m_planetMode)
                {
                    for (int i = 0; i < m_copiedVoxelMaps.Count; ++i)
                    {
                        MyObjectBuilder_Planet builder = m_copiedVoxelMaps[i] as MyObjectBuilder_Planet;
                        if (builder != null)
                        {
                            using (m_tmpResultList.GetClearToken())
                            {
                                BoundingSphereD sphere = new BoundingSphereD(m_pastePosition, builder.Radius * 1.1f);
                                MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, m_tmpResultList);

                                if (TestPlacement(m_tmpResultList) == false)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private bool TestPlacement(List<MyEntity> entities)
        {
            foreach (var entity in entities)
            {
                //ignore asteroids
                if (entity is MyVoxelBase)
                {
                    continue;
                }

                //ignore stations
                if (entity is MyCubeGrid)
                {
                    var grid = entity as MyCubeGrid;
                    if (grid.IsStatic)
                    {
                        continue;
                    }
                }

                entities.Clear();
                return false;
            }
            return true;
        }

        private static MatrixD GetPasteMatrix()
        {
            if (MySession.Static.ControlledEntity != null &&
                (MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Entity || MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator))
            {
                return MySession.Static.ControlledEntity.GetHeadMatrix(true);
            }
            else
            {
                return MySector.MainCamera.WorldMatrix;
            }
        }

        #region Pasting transform control

        public void MoveEntityFurther()
        {
            m_dragDistance *= 1.1f;
        }

        public void MoveEntityCloser()
        {
            m_dragDistance /= 1.1f;
        }

        #endregion
    }
}
