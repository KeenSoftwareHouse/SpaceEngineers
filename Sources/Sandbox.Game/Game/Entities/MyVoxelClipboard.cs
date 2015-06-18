using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities
{
    class MyVoxelClipboard
    {
        private List<MyObjectBuilder_VoxelMap> m_copiedVoxelMaps = new List<MyObjectBuilder_VoxelMap>();
        private List<IMyStorage> m_copiedStorages = new List<IMyStorage>();
        private List<Vector3> m_copiedVoxelMapOffsets = new List<Vector3>();
        private List<MyVoxelMap> m_previewVoxelMaps = new List<MyVoxelMap>();

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
        }

        public void Hide()
        {
            ChangeClipboardPreview(false);
        }

        public void Show()
        {
            if (IsActive && m_previewVoxelMaps.Count == 0)
                ChangeClipboardPreview(true);
        }

        public void CutVoxelMap(MyVoxelMap voxelMap)
        {
            if (voxelMap == null)
                return;

            CopyVoxelMap(voxelMap);
            voxelMap.SyncObject.SendCloseRequest();
            Deactivate();
        }

        public void CopyVoxelMap(MyVoxelMap voxelMap)
        {
            if (voxelMap == null)
                return;
            m_copiedVoxelMaps.Clear();
            m_copiedVoxelMapOffsets.Clear();
            CopyVoxelMapInternal(voxelMap);
            Activate();
        }

        private void CopyVoxelMapInternal(MyVoxelMap toCopy)
        {
            m_copiedVoxelMaps.Add((MyObjectBuilder_VoxelMap)toCopy.GetObjectBuilder(true));
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

            MyEntities.RemapObjectBuilderCollection(m_copiedVoxelMaps);

            foreach (var voxelMap in m_previewVoxelMaps)
            {
                if (MySession.Static.Settings.OnlineMode == MyOnlineModeEnum.OFFLINE || (Sync.IsServer && Sync.Clients.Count == 1))
                {
                    EnablePhysicsRecursively(voxelMap);
                    voxelMap.Save = true;
                    MakeVisible(voxelMap);
                    m_shouldMarkForClose = false;
                }
                else
                {
                    MyGuiScreenDebugSpawnMenu.SendAsteroid(voxelMap.PositionComp.GetPosition());
                }
            }

            Deactivate();
            return true;
        }

        public void SetVoxelMapFromBuilder(MyObjectBuilder_VoxelMap voxelMap, IMyStorage storage, Vector3 dragPointDelta, float dragVectorLength)
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

            SetVoxelMapFromBuilderInternal(voxelMap, storage, Vector3.Zero);

            Activate();
        }

        private void SetVoxelMapFromBuilderInternal(MyObjectBuilder_VoxelMap voxelMap, IMyStorage storage, Vector3 offset)
        {
            m_copiedVoxelMaps.Add(voxelMap);
            m_copiedStorages.Add(storage);
            m_copiedVoxelMapOffsets.Add(offset);
        }

        private void ChangeClipboardPreview(bool visible)
        {
            if (m_copiedVoxelMaps.Count == 0 || !visible)
            {
                foreach(var voxelMap in m_previewVoxelMaps)
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
                var previewVoxelMap = new MyVoxelMap();
                var pos = voxelMapOb.PositionAndOrientation.Value.Position;
                previewVoxelMap.Init(voxelMapOb.StorageName, storage, new Vector3(pos.x, pos.y, pos.z));

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

        private void MakeTransparent(MyVoxelMap voxelMap)
        {
            voxelMap.Render.Transparency = MyGridConstants.BUILDER_TRANSPARENCY;
        }

        private void MakeVisible(MyVoxelMap voxelMap)
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
            for (int i = 0; i < m_previewVoxelMaps.Count; ++i)
            {
                m_previewVoxelMaps[i].PositionLeftBottomCorner = m_pastePosition + m_copiedVoxelMapOffsets[i] - m_previewVoxelMaps[i].Storage.Size * 0.5f;
                m_previewVoxelMaps[i].PositionComp.SetPosition(m_pastePosition + m_copiedVoxelMapOffsets[i]);
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
        private HashSet<MyEntity> m_tmpResultHashset = new HashSet<MyEntity>();

        private bool TestPlacement()
        {
            if (MySession.ControlledEntity != null &&
                (MySession.GetCameraControllerEnum() == MyCameraControllerEnum.Entity || MySession.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator))
                for (int i = 0; i < m_previewVoxelMaps.Count; ++i)
                {
                    var aabb = m_previewVoxelMaps[i].PositionComp.WorldAABB;

                    using (m_tmpResultList.GetClearToken())
                    {
                        MyGamePruningStructure.GetAllEntitiesInBox(ref aabb, m_tmpResultList);

                        foreach (var entity in m_tmpResultList)
                        {
                            m_tmpResultHashset.Add(entity.GetTopMostParent());
                        }

                        foreach (var entity in m_tmpResultHashset)
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

                            switch (m_previewVoxelMaps[i].GetVoxelRangeTypeInBoundingBox(entity.PositionComp.WorldAABB))
                            {
                                case MyVoxelRangeType.EMPTY:
                                    break;
                                case MyVoxelRangeType.MIXED:
                                    {
                                        m_tmpResultList.Clear();
                                        m_tmpResultHashset.Clear();
                                        return false;
                                    }
                                    break;
                                case MyVoxelRangeType.FULL:
                                    {
                                        m_tmpResultList.Clear();
                                        m_tmpResultHashset.Clear();
                                        return false;
                                    }
                                    break;
                                default:
                                    throw new InvalidBranchException();
                                    break;
                            }
                        }
                        m_tmpResultHashset.Clear();
                    }
                }
            return true;
        }

        private static MatrixD GetPasteMatrix()
        {
            if (MySession.ControlledEntity != null &&
                (MySession.GetCameraControllerEnum() == MyCameraControllerEnum.Entity || MySession.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator))
            {
                return MySession.ControlledEntity.GetHeadMatrix(true);
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
