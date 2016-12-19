using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.GUI;
using VRageRender;
using Sandbox.Game.Entities.Cube;
using VRage.ObjectBuilders;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.ModAPI;
using VRage.Game.Entity;

namespace Sandbox.Game.Entities
{
    class MyFloatingObjectClipboard
    {
        private List<MyObjectBuilder_FloatingObject> m_copiedFloatingObjects = new List<MyObjectBuilder_FloatingObject>();
        private List<Vector3> m_copiedFloatingObjectOffsets = new List<Vector3>();
        private List<MyFloatingObject> m_previewFloatingObjects = new List<MyFloatingObject>();

        // Paste position
        private Vector3D m_pastePosition;
        private Vector3D m_pastePositionPrevious;

        // Paste velocity
        private bool m_calculateVelocity = true;
        private Vector3 m_objectVelocity = Vector3.Zero;

        // Paste orientation
        private float m_pasteOrientationAngle = 0.0f;
        private Vector3 m_pasteDirUp = new Vector3(1.0f, 0.0f, 0.0f);
        private Vector3 m_pasteDirForward = new Vector3(0.0f, 1.0f, 0.0f);

        // Copy position
        private float m_dragDistance;
        private Vector3 m_dragPointToPositionLocal;

        // Placement flags
        private bool m_canBePlaced;

        // Raycasting
        List<MyPhysics.HitInfo> m_raycastCollisionResults = new List<MyPhysics.HitInfo>();
        private float m_closestHitDistSq = float.MaxValue;
        private Vector3D m_hitPos = new Vector3(0.0f, 0.0f, 0.0f);
        private Vector3 m_hitNormal = new Vector3(1.0f, 0.0f, 0.0f);
        //private MyEntity m_hitEntity = null;

        private bool m_visible = true;

        public bool IsActive
        {
            get;
            private set;
        }

        public List<MyFloatingObject> PreviewFloatingObjects
        {
            get { return m_previewFloatingObjects; }
        }

        bool m_enableStationRotation = false;
        public bool EnableStationRotation
        {
            get
            {
                return m_enableStationRotation && MyFakes.ENABLE_STATION_ROTATION;
            }

            set
            {
                m_enableStationRotation = value;
            }
        }

        public MyFloatingObjectClipboard(bool calculateVelocity = true)
        {
            m_calculateVelocity = calculateVelocity;
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
            if (IsActive && m_previewFloatingObjects.Count == 0)
                ChangeClipboardPreview(true);
        }

        public void CutFloatingObject(MyFloatingObject floatingObject)
        {
            if (floatingObject == null)
                return;

            CopyfloatingObject(floatingObject);
            MyFloatingObjects.RemoveFloatingObject(floatingObject, true);
            Deactivate();
        }

        public void CopyfloatingObject(MyFloatingObject floatingObject)
        {
            if (floatingObject == null)
                return;
            m_copiedFloatingObjects.Clear();
            m_copiedFloatingObjectOffsets.Clear();
            CopyFloatingObjectInternal(floatingObject);
            Activate();
        }

        private void CopyFloatingObjectInternal(MyFloatingObject toCopy)
        {
            m_copiedFloatingObjects.Add((MyObjectBuilder_FloatingObject)toCopy.GetObjectBuilder(true));
            if (m_copiedFloatingObjects.Count == 1)
            {
                MatrixD pasteMatrix = GetPasteMatrix();
                Vector3 dragPointGlobal = toCopy.WorldMatrix.Translation;

                m_dragPointToPositionLocal = Vector3D.TransformNormal(toCopy.PositionComp.GetPosition() - dragPointGlobal, toCopy.PositionComp.WorldMatrixNormalizedInv);
                m_dragDistance = (float)(dragPointGlobal - pasteMatrix.Translation).Length();

                m_pasteDirUp = toCopy.WorldMatrix.Up;
                m_pasteDirForward = toCopy.WorldMatrix.Forward;
                m_pasteOrientationAngle = 0.0f;
            }
            m_copiedFloatingObjectOffsets.Add(toCopy.WorldMatrix.Translation - m_copiedFloatingObjects[0].PositionAndOrientation.Value.Position);
        }

        public bool PasteFloatingObject(MyInventory buildInventory = null)
        {
            if (m_copiedFloatingObjects.Count == 0)
                return false;

            if ((m_copiedFloatingObjects.Count > 0) && !IsActive)
            {
                if (CheckPastedFloatingObjects())
                {
                    Activate();
                }
                else
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.CopyPasteBlockNotAvailable);
                    MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                }
                return true;
            }

            if (!m_canBePlaced)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
                return false;
            }

            MyGuiAudio.PlaySound(MyGuiSounds.HudPlaceItem);

            MyEntities.RemapObjectBuilderCollection(m_copiedFloatingObjects);

            bool retVal = false;
            int i = 0;
            foreach (var floatingObjectBuilder in m_copiedFloatingObjects)
            {
                floatingObjectBuilder.PersistentFlags = MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.Enabled;
                //MyFloatingObject pastedFloatingObject = MyEntities.CreateFromObjectBuilderAndAdd(floatingObjectBuilder) as MyFloatingObject;
                //if (i == 0) firstPastedFloatingObject = pastedFloatingObject;

                //if (pastedFloatingObject == null)
                //{
                //    retVal = true;
                //    continue;
                //}

                floatingObjectBuilder.PositionAndOrientation = new MyPositionAndOrientation(m_previewFloatingObjects[i].WorldMatrix);
                i++;
                // No velocity saving :)
                //floatingObjectBuilder.LinearVelocity = m_objectVelocity;
                //if (MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity.Entity.Physics != null && m_calculateVelocity)
                //{
                //    pastedFloatingObject.Physics.AngularVelocity = MySession.Static.ControlledEntity.Entity.Physics.AngularVelocity;
                //}

                MyFloatingObjects.RequestSpawnCreative(floatingObjectBuilder);
                retVal = true;
            }

            Deactivate();
            return retVal;
        }

        /// <summary>
        /// Checks the pasted object builder for non-existent blocks (e.g. copying from world with a cube block mod to a world without it)
        /// </summary>
        /// <returns>True when the grid can be pasted</returns>
        private bool CheckPastedFloatingObjects()
        {
            MyPhysicalItemDefinition cbDef;
            foreach (var floatingObjectBuilder in m_copiedFloatingObjects)
            {
                MyDefinitionId id = floatingObjectBuilder.Item.PhysicalContent.GetId();
                if (MyDefinitionManager.Static.TryGetPhysicalItemDefinition(id, out cbDef) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public void SetFloatingObjectFromBuilder(MyObjectBuilder_FloatingObject floatingObject, Vector3 dragPointDelta, float dragVectorLength)
        {
            if (IsActive)
            {
                Deactivate();
            }

            m_copiedFloatingObjects.Clear();
            m_copiedFloatingObjectOffsets.Clear();

            Matrix pasteMatrix = GetPasteMatrix();
            m_dragPointToPositionLocal = dragPointDelta;
            m_dragDistance = dragVectorLength;
            var transform = floatingObject.PositionAndOrientation ?? MyPositionAndOrientation.Default;
            m_pasteDirUp = transform.Up;
            m_pasteDirForward = transform.Forward;

            SetFloatingObjectFromBuilderInternal(floatingObject, Vector3.Zero);

            Activate();
        }

        private void SetFloatingObjectFromBuilderInternal(MyObjectBuilder_FloatingObject floatingObject, Vector3 offset)
        {
            Debug.Assert(floatingObject.Item.Amount > 0, "The floating object should gave positive amount");
            m_copiedFloatingObjects.Add(floatingObject);
            m_copiedFloatingObjectOffsets.Add(offset);
        }

        private void ChangeClipboardPreview(bool visible)
        {
            if (m_copiedFloatingObjects.Count == 0 || !visible)
            {
                foreach (var grid in m_previewFloatingObjects)
                {
                    MyEntities.EnableEntityBoundingBoxDraw(grid, false);
                    grid.Close();
                }
                m_previewFloatingObjects.Clear();
                m_visible = false;
                return;
            }

            MyEntities.RemapObjectBuilderCollection(m_copiedFloatingObjects);

            foreach (var gridBuilder in m_copiedFloatingObjects)
            {
                var previewFloatingObject = MyEntities.CreateFromObjectBuilder(gridBuilder) as MyFloatingObject;
                if (previewFloatingObject == null)
                {
                    ChangeClipboardPreview(false);
                    return;// Not enough memory to create preview grid or there was some error.
                }

                MakeTransparent(previewFloatingObject);
                IsActive = visible;
                m_visible = visible;
                MyEntities.Add(previewFloatingObject);
                // MW: we want the floating object to be added to the scene, but we dont want to treat it as a real floating object
                MyFloatingObjects.UnregisterFloatingObject(previewFloatingObject);
                previewFloatingObject.Save = false;
                DisablePhysicsRecursively(previewFloatingObject);
                m_previewFloatingObjects.Add(previewFloatingObject);
            }
        }

        private void MakeTransparent(MyFloatingObject floatingObject)
        {
            floatingObject.Render.Transparency = MyGridConstants.BUILDER_TRANSPARENCY;
        }

        private void DisablePhysicsRecursively(MyEntity entity)
        {
            if (entity.Physics != null && entity.Physics.Enabled)
                entity.Physics.Enabled = false;

            var floatingObject = entity as MyFloatingObject;
            if (floatingObject != null)
                floatingObject.NeedsUpdate = MyEntityUpdateEnum.NONE;

            foreach (var child in entity.Hierarchy.Children)
                DisablePhysicsRecursively(child.Container.Entity as MyEntity);
        }

        public void Update()
        {
            if (!IsActive || !m_visible)
                return;

            UpdateHitEntity();
            UpdatePastePosition();
            UpdateFloatingObjectTransformations();

            if (m_calculateVelocity)
                m_objectVelocity = (m_pastePosition - m_pastePositionPrevious) / VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            m_canBePlaced = TestPlacement();

            UpdatePreviewBBox();

            if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), "FW: " + m_pasteDirForward.ToString(), Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 20.0f), "UP: " + m_pasteDirUp.ToString(), Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 40.0f), "AN: " + m_pasteOrientationAngle.ToString(), Color.Red, 1.0f);
            }
        }

        void UpdateHitEntity()
        {
            Debug.Assert(m_raycastCollisionResults.Count == 0);

            MatrixD pasteMatrix = GetPasteMatrix();
            MyPhysics.CastRay(pasteMatrix.Translation, pasteMatrix.Translation + pasteMatrix.Forward * m_dragDistance, m_raycastCollisionResults);

            m_closestHitDistSq = float.MaxValue;
            m_hitPos = new Vector3(0.0f, 0.0f, 0.0f);
            m_hitNormal = new Vector3(1.0f, 0.0f, 0.0f);
            //m_hitEntity = null;

            foreach (var hit in m_raycastCollisionResults)
            {
                MyPhysicsBody body = (MyPhysicsBody)hit.HkHitInfo.Body.UserObject;
                if (body == null)
                    continue;
                IMyEntity entity = body.Entity;
                if (entity is MyVoxelMap || (entity is MyCubeGrid && entity.EntityId != m_previewFloatingObjects[0].EntityId))
                {
                    float distSq = (float)(hit.Position - pasteMatrix.Translation).LengthSquared();
                    if (distSq < m_closestHitDistSq)
                    {
                        m_closestHitDistSq = distSq;
                        m_hitPos = hit.Position;
                        m_hitNormal = hit.HkHitInfo.Normal;
                        //m_hitEntity = entity;
                    }
                }
            }

            m_raycastCollisionResults.Clear();
        }

        private bool TestPlacement()
        {
            return true; /// yaay
            for (int i = 0; i < m_previewFloatingObjects.Count; ++i)
            {
                var floatingObject = m_previewFloatingObjects[i];

                var rotation = Quaternion.CreateFromRotationMatrix(floatingObject.WorldMatrix);
                var position = floatingObject.PositionComp.GetPosition() + Vector3D.Transform(floatingObject.PositionComp.LocalVolume.Center, rotation);
                var bodies = new List<HkBodyCollision>();

                MyPhysics.GetPenetrationsShape(floatingObject.Physics.RigidBody.GetShape(), ref position, ref rotation, bodies, MyPhysics.CollisionLayers.FloatingObjectCollisionLayer);
                foreach (var body in bodies)
                {
                    var ent = body.GetCollisionEntity();
                    if (ent != null && !ent.Closed)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void UpdateFloatingObjectTransformations()
        {
            Matrix originalOrientation = GetFirstGridOrientationMatrix();
            var invRotation = Matrix.Invert(m_copiedFloatingObjects[0].PositionAndOrientation.Value.GetMatrix()).GetOrientation();
            Matrix orientationDelta = invRotation * originalOrientation; // matrix from original orientation to new orientation

            for (int i = 0; i < m_previewFloatingObjects.Count; i++)
            {
                MatrixD worldMatrix2 = m_copiedFloatingObjects[i].PositionAndOrientation.Value.GetMatrix(); //get original rotation and position
                var offset = worldMatrix2.Translation - m_copiedFloatingObjects[0].PositionAndOrientation.Value.Position; //calculate offset to first pasted grid
                m_copiedFloatingObjectOffsets[i] = Vector3.TransformNormal(offset, orientationDelta); // Transform the offset to new orientation
                Vector3 translation = m_pastePosition + m_copiedFloatingObjectOffsets[i]; //correct position
                worldMatrix2 = worldMatrix2 * orientationDelta;

                worldMatrix2.Translation = Vector3.Zero;
                worldMatrix2 = Matrix.Orthogonalize(worldMatrix2);
                worldMatrix2.Translation = translation;

                m_previewFloatingObjects[i].PositionComp.SetWorldMatrix(worldMatrix2);// Set the corrected position
            }
        }

        private void UpdatePastePosition()
        {
            m_pastePositionPrevious = m_pastePosition;

            // Current position of the placed entity is either simple translation or
            // it can be calculated by raycast, if we want to snap to surfaces
            MatrixD pasteMatrix = GetPasteMatrix();
            Vector3 dragVectorGlobal = pasteMatrix.Forward * m_dragDistance;

            m_pastePosition = pasteMatrix.Translation + dragVectorGlobal;
            Matrix firstGridOrientation = GetFirstGridOrientationMatrix();
            m_pastePosition += Vector3.TransformNormal(m_dragPointToPositionLocal, firstGridOrientation);

            if (MyDebugDrawSettings.DEBUG_DRAW_COPY_PASTE)
            {
                MyRenderProxy.DebugDrawSphere(pasteMatrix.Translation + dragVectorGlobal, 0.15f, Color.Pink, 1.0f, false);
                MyRenderProxy.DebugDrawSphere(m_pastePosition, 0.15f, Color.Pink.ToVector3(), 1.0f, false);
            }
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

        private Matrix GetFirstGridOrientationMatrix()
        {
            return Matrix.CreateWorld(Vector3.Zero, m_pasteDirForward, m_pasteDirUp) * Matrix.CreateFromAxisAngle(m_pasteDirUp, m_pasteOrientationAngle);
        }

        private void UpdatePreviewBBox()
        {
            if (m_previewFloatingObjects == null)
                return;

            if (m_visible == false)
            {
                foreach (var floatingObject in m_previewFloatingObjects)
                    MyEntities.EnableEntityBoundingBoxDraw(floatingObject, false);
                return;
            }

            Vector4 color = new Vector4(Color.Red.ToVector3() * 0.8f, 1);
            if (m_canBePlaced)
            {
                color = Color.Gray.ToVector4();
            }

            // Draw a little inflated bounding box
            var inflation = new Vector3(0.1f);
            foreach (var floatingObject in m_previewFloatingObjects)
                MyEntities.EnableEntityBoundingBoxDraw(floatingObject, true, color, lineWidth: 0.04f, inflateAmount: inflation);
        }

        public void CalculateRotationHints(MyBlockBuilderRotationHints hints, bool isRotating)
        {
            MyEntity entity = PreviewFloatingObjects.Count > 0 ? PreviewFloatingObjects[0] : null;
            if (entity != null)
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                if (grid != null && (!grid.IsStatic || EnableStationRotation))
                {
                    Vector3I gridSize = grid.Max - grid.Min + new Vector3I(1, 1, 1);
                    BoundingBoxD worldBox = new BoundingBoxD(-gridSize * grid.GridSize * 0.5f, gridSize * grid.GridSize * 0.5f);

                    MatrixD mat = entity.WorldMatrix;
                    Vector3 positionToDragPointGlobal = Vector3.TransformNormal(-m_dragPointToPositionLocal, mat);
                    mat.Translation = mat.Translation + positionToDragPointGlobal;

                    hints.CalculateRotationHints(mat, worldBox, !MyHud.MinimalHud && !MyHud.CutsceneHud && MySandboxGame.Config.RotationHints && MyFakes.ENABLE_ROTATION_HINTS, isRotating);
                }
            }
        }

        public bool HasCopiedFloatingObjects()
        {
            return m_copiedFloatingObjects.Count > 0;
        }

        public string CopiedGridsName
        {
            get
            {
                if (HasCopiedFloatingObjects())
                {
                    return m_copiedFloatingObjects[0].Name;
                }

                return null;
            }
        }

        public void HideWhenColliding(List<Vector3D> collisionTestPoints)
        {
            if (m_previewFloatingObjects.Count == 0) return;
            bool visible = true;
            foreach (var point in collisionTestPoints)
            {
                foreach (var floatingObject in m_previewFloatingObjects)
                {
                    var localPoint = Vector3.Transform(point, floatingObject.PositionComp.WorldMatrixNormalizedInv);
                    if (floatingObject.PositionComp.LocalAABB.Contains(localPoint) == ContainmentType.Contains)
                    {
                        visible = false;
                        break;
                    }
                }
                if (!visible)
                    break;
            }
            foreach (var floatingObject in m_previewFloatingObjects)
            {
                floatingObject.Render.Visible = visible;
            }
        }

        public void ClearClipboard()
        {
            if (IsActive)
                Deactivate();
            m_copiedFloatingObjects.Clear();
            m_copiedFloatingObjectOffsets.Clear();
        }

        #region Pasting transform control
        public void RotateAroundAxis(int axisIndex, int sign, bool newlyPressed, float angleDelta)
        {
            switch (axisIndex)
            {
                case 0:
                    if (sign < 0)
                        UpMinus(angleDelta);
                    else
                        UpPlus(angleDelta);
                    break;

                case 1:
                    if (sign < 0)
                        AngleMinus(angleDelta);
                    else
                        AnglePlus(angleDelta);
                    break;

                case 2:
                    if (sign < 0)
                        RightPlus(angleDelta);
                    else
                        RightMinus(angleDelta);
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
        }

        private void AnglePlus(float angle)
        {
            m_pasteOrientationAngle += angle;
            if (m_pasteOrientationAngle >= (float)Math.PI * 2.0f)
            {
                m_pasteOrientationAngle -= (float)Math.PI * 2.0f;
            }
        }

        private void AngleMinus(float angle)
        {
            m_pasteOrientationAngle -= angle;
            if (m_pasteOrientationAngle < 0.0f)
            {
                m_pasteOrientationAngle += (float)Math.PI * 2.0f;
            }
        }

        private void UpPlus(float angle)
        {
            ApplyOrientationAngle();
            Vector3 right = Vector3.Cross(m_pasteDirForward, m_pasteDirUp);
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            Vector3 up = m_pasteDirUp * cos - m_pasteDirForward * sin;
            m_pasteDirForward = m_pasteDirUp * sin + m_pasteDirForward * cos;
            m_pasteDirUp = up;
        }

        private void UpMinus(float angle)
        {
            UpPlus(-angle);
        }

        private void RightPlus(float angle)
        {
            ApplyOrientationAngle();
            Vector3 right = Vector3.Cross(m_pasteDirForward, m_pasteDirUp);
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            m_pasteDirUp = m_pasteDirUp * cos + right * sin;
        }

        private void RightMinus(float angle)
        {
            RightPlus(-angle);
        }

        public void MoveEntityFurther()
        {
            m_dragDistance *= 1.1f;
        }

        public void MoveEntityCloser()
        {
            m_dragDistance /= 1.1f;
        }

        private void ApplyOrientationAngle()
        {
            m_pasteDirForward = Vector3.Normalize(m_pasteDirForward);
            m_pasteDirUp = Vector3.Normalize(m_pasteDirUp);

            Vector3 right = Vector3.Cross(m_pasteDirForward, m_pasteDirUp);
            float cos = (float)Math.Cos(m_pasteOrientationAngle);
            float sin = (float)Math.Sin(m_pasteOrientationAngle);
            m_pasteDirForward = m_pasteDirForward * cos - right * sin;
            m_pasteOrientationAngle = 0.0f;
        }

        #endregion
    }
}
