using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Input;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 900)]
    public class MyEntityTransformationSystem : MySessionComponentBase
    {
        private static float PICKING_RAY_LENGTH = 1000f;
        private static float PLANE_THICKNESS = 0.005f;
        private static bool DEBUG = false;

        public enum CoordinateMode
        {
            LocalCoords,
            WorldCoords
        }

        public enum OperationMode
        {
            Translation,
            Rotation,
            HierarchyAssignment
        }

        private MyEntity m_controlledEntity;
        // Collision layers of all entities in physical group of moved entity
        private readonly Dictionary<MyEntity,int> m_cachedBodyCollisionLayers 
            = new Dictionary<MyEntity, int>();

        // Object oriented bounding boxes used as dragg points for position
        private MyOrientedBoundingBoxD m_xBB;
        private MyOrientedBoundingBoxD m_yBB;
        private MyOrientedBoundingBoxD m_zBB;

        // Planes used as rotation planes of the controled object
        private MyOrientedBoundingBoxD m_xPlane;
        private MyOrientedBoundingBoxD m_yPlane;
        private MyOrientedBoundingBoxD m_zPlane;

        // "index" of selected obb for highlight purpose,
        // because MyOrientedBoundingBoxD is value type.
        private int m_selected;

        // Just a axis render to make it all look nice
        private MatrixD m_gizmoMatrix;

        #region Translation Fields
        // Helper plane for movement of translation controls
        private PlaneD m_dragPlane;
        // Dragg active flagg
        private bool m_dragActive;
        // Flag that tells whenever we should use only axis locked tranformation
        private bool m_dragOverAxis;
        // First intersection that determines the amount of transformation from start
        private Vector3D m_dragStartingPoint;
        // Axis used for axis locked transformation
        private Vector3D m_dragAxis;
        // Position in which the entity was when the drag started
        private Vector3D m_dragStartingPosition;
        #endregion


        #region Rotation Fields

        // Rotation active flag
        private bool m_rotationActive;
        // Plane used for picking of rotation vectors
        private PlaneD m_rotationPlane;
        // Axis of rotation
        private Vector3D m_rotationAxis;
        // First interception point
        private Vector3D m_rotationStartingPoint;
        // Stored transformation data
        private MatrixD m_storedOrientation;
        private MatrixD m_storedScale;
        private Vector3D m_storedTranslation;
        private MatrixD m_storedWorldMatrix;

        #endregion


        #region Cache Fields

        // Debug
        private LineD m_lastRay;

        // Previous mode cache
        private OperationMode m_previousOperation;

        // Results
        private readonly List<MyLineSegmentOverlapResult<MyEntity>> m_rayCastResultList
            = new List<MyLineSegmentOverlapResult<MyEntity>>();

        #endregion


        #region Public Properties

        private bool m_active;
        // Current state of session component
        public bool Active
        {
            get { return m_active; }
            set
            {
                if (Session == null || !Session.CreativeMode)
                    return;

                if (!value)
                {
                    SetControlledEntity(null);
                }

                m_active = value;
            }
        }

        /// <summary>
        /// This will disable the transformation changes of picked entity.
        /// </summary>
        public bool DisableTransformation { get; set; }

        /// <summary>
        /// Triggered when controlled entity has changed.
        /// First old, second new.
        /// </summary>
        public event Action<MyEntity, MyEntity> ControlledEntityChanged;

        /// <summary>
        /// Triggered when the component casts new ray.
        /// </summary>
        public event Action<LineD> RayCasted;

        /// <summary>
        /// Coordinate mode - Local or World space.
        /// </summary>
        public CoordinateMode Mode { get; set; }

        /// <summary>
        /// Transformation type.
        /// </summary>
        public OperationMode Operation { get; set; }

        /// <summary>
        /// Currently selected entity.
        /// </summary>
        public MyEntity ControlledEntity
        {
            get { return m_controlledEntity; }
        }


        /// <summary>
        /// Prevents picking of entities by left click.
        /// </summary>
        public bool DisablePicking { get; set; }

        /// <summary>
        /// Last picking raycast.
        /// </summary>
        public LineD LastRay
        {
            get { return m_lastRay; }
        }

        #endregion


        public MyEntityTransformationSystem()
        {
            Active = false;

            Mode = CoordinateMode.WorldCoords;
            m_selected = -1;
            // One little hack to get rid of mouse cursor from gameplay screen and to
            // activate/deactivate this component.
            MySession.Static.CameraAttachedToChanged += (old, @new) 
                =>
            {
                Active = false;
            };
        }

        // Draws the gizmo
        public override void Draw()
        {
            if(!Active) return;

            if(DEBUG)
            {
                MyRenderProxy.DebugDrawLine3D(m_lastRay.From, m_lastRay.To, Color.Green, Color.Green, true);
            }

            if(ControlledEntity == null)
                return;

            if (ControlledEntity.Parent != null)
            {
                var parent = ControlledEntity.Parent;
                while(parent != null)
                {
                    // Draw line to parent
                    MyRenderProxy.DebugDrawLine3D(  ControlledEntity.Parent.PositionComp.GetPosition(), 
                                                    ControlledEntity.PositionComp.GetPosition(), 
                                                    Color.Orange, 
                                                    Color.Blue, 
                                                    false);
                    
                    parent = parent.Parent;
                }
            }

            var textPosition = new Vector2(20, Session.Camera.ViewportSize.Y / 2);
            switch (Operation)
            {
                case OperationMode.Translation:
                    MyRenderProxy.DebugDrawText2D(textPosition, "Translation", Color.Yellow, 1);
                    break;
                case OperationMode.Rotation:
                    MyRenderProxy.DebugDrawText2D(textPosition, "Rotation", Color.Yellow, 1);
                    break;
                case OperationMode.HierarchyAssignment:
                    MyRenderProxy.DebugDrawText2D(textPosition, "Hierarchy", Color.Yellow, 1);
                    break;
            }

            if (Operation == OperationMode.Translation && !DisableTransformation)
            {
                // Change the size of the control elements
                var camPosition = Session.Camera.Position;
                var distance = Vector3D.Distance(m_xBB.Center, camPosition);
                var f = Session.Camera.ProjectionMatrix.Up.LengthSquared();

                m_xBB.HalfExtent = Vector3D.One*0.008*distance*f;
                m_yBB.HalfExtent = Vector3D.One*0.008*distance*f;
                m_zBB.HalfExtent = Vector3D.One*0.008*distance*f;

                DrawOBB(m_xBB, Color.Red, 0.5f, 0);
                DrawOBB(m_yBB, Color.Green, 0.5f, 1);
                DrawOBB(m_zBB, Color.Blue, 0.5f, 2);
            }

            if(Operation != OperationMode.HierarchyAssignment && !DisableTransformation)
            {
                DrawOBB(m_xPlane, Color.Red, 0.2f, 3);
                DrawOBB(m_yPlane, Color.Green, 0.2f, 4);
                DrawOBB(m_zPlane, Color.Blue, 0.2f, 5);
            }
            else
            {
                var volumeCenter = ControlledEntity.PositionComp.WorldVolume.Center;
                var volumeRadius = ControlledEntity.PositionComp.WorldVolume.Radius;

                MyRenderProxy.DebugDrawSphere(volumeCenter, (float)volumeRadius, Color.Yellow, 0.2f, true);
            }
        }

        // Draw OBB and highlight it white if selected
        private void DrawOBB(MyOrientedBoundingBoxD obb, Color color, float alpha, int identificationIndex)
        {
            if (identificationIndex == m_selected)
            {
                MyRenderProxy.DebugDrawOBB(obb, Color.White, 0.2f, false, false);
            }
            else
            {
                MyRenderProxy.DebugDrawOBB(obb, color, alpha, false, false);
            }
        }

        // Changes positions of gizmo elements
        public override void UpdateAfterSimulation()
        {
            if (!Active) return;

            if ((m_dragActive || m_rotationActive) && MyInput.Static.IsNewRightMousePressed())
            {
                // Cancel and restore tranformation
                SetWorldMatrix(ref m_storedWorldMatrix);
                m_dragActive = false;
                m_rotationActive = false;
                m_selected = -1;
            }

            // Stop when transformation is disabled 
            if (!DisableTransformation)
            {
                if (m_dragActive)
                {
                    PerformDragg(m_dragOverAxis);
                }

                if (m_rotationActive)
                {
                    PerformRotation();
                }
            }

            // Switch to rotation mode and back
            if (MyInput.Static.IsNewKeyPressed(MyKeys.R))
            {
                switch (Operation)
                {
                    case OperationMode.Translation:
                        Operation = OperationMode.Rotation;
                        break;
                    case OperationMode.Rotation:
                        Operation = OperationMode.Translation;
                        break;
                    case OperationMode.HierarchyAssignment:
                        Operation = m_previousOperation;
                        break;
                }
            }

            // Switch the world and local systems
            if (MyInput.Static.IsNewKeyPressed(MyKeys.T))
            {
                switch (Mode)
                {
                    case CoordinateMode.LocalCoords:
                        Mode = CoordinateMode.WorldCoords;
                        break;
                    case CoordinateMode.WorldCoords:
                        Mode = CoordinateMode.LocalCoords;
                        break;
                }

                if (ControlledEntity != null)
                {
                    UpdateGizmoPosition();
                }
            }

            // Switch to Hierarchy mode and back to previous
            if (MyInput.Static.IsNewKeyPressed(MyKeys.H))
            {
                if (Operation == OperationMode.HierarchyAssignment)
                {
                    Operation = m_previousOperation;
                }
                else
                {
                    // Cannot use this mode without controlled entity
                    if (ControlledEntity != null)
                    {
                        m_previousOperation = Operation;
                        Operation = OperationMode.HierarchyAssignment;
                    }
                }
            }

            // Try to pick an entity
            if (MyInput.Static.IsNewLeftMousePressed())
            {
                if (DisablePicking) return;

                // Create a ray that is used in both below method calls
                m_lastRay = CreateRayFromCursorPosition();

                if(RayCasted != null)
                    RayCasted(m_lastRay);

                if (Operation == OperationMode.HierarchyAssignment)
                {
                    PerformGrouping();
                }
                else
                {
                    // First try to pick a control
                    if (ControlledEntity != null && PickControl())
                    {
                        // Store tranformations
                        m_storedWorldMatrix = ControlledEntity.PositionComp.WorldMatrix;
                    }
                    else
                    {
                        m_selected = -1;
                        var entity = PickEntity();
                        // Select new controlled entity
                        SetControlledEntity(entity);
                    }
                }
            }

            if (MyInput.Static.IsNewLeftMouseReleased())
            {
                // Stop the drag or rotation
                m_dragActive = false;
                m_rotationActive = false;
                m_selected = -1;
            }

            // Remove speed from controlled entity no matter what. Causes a lot of issues.
            // Namely Rotors, Pistons, Tires.
            if (ControlledEntity != null && ControlledEntity.Physics != null)
            {
                ControlledEntity.Physics.ClearSpeed();
            }
        }

        // Try to perform the grouping operation
        private void PerformGrouping()
        {
            if(ControlledEntity == null)
                return;

            var groupWithEntity = PickEntity();
            if (groupWithEntity != null)
            {
                DisablePicking = true;
                // Create a confirmation dialog for grouping and group items together
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText:
                        new StringBuilder("Do you want to put " + ControlledEntity.DisplayName +
                                            " under hierarchy of " + groupWithEntity.DisplayName + "?"),
                    messageCaption: new StringBuilder("Hierarchy Group"), callback: @enum =>
                    {
                        if (@enum == MyGuiScreenMessageBox.ResultEnum.YES)
                        {
                            // This causes the entity to get removed from Pruning structure
                            MyEntities.Remove(ControlledEntity);
                            // Entity would get added to pruning structure but does not because Parent != null
                            groupWithEntity.Hierarchy.AddChild(ControlledEntity, true);
                        }

                        DisablePicking = false;
                    }));
            }
            else
            {
                DisablePicking = true;
                // Create a confirmation dialog for grouping and group items together
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText:
                        new StringBuilder("Do you want to Remove the " + ControlledEntity.DisplayName +
                                            " from the hierarchy?"),
                    messageCaption: new StringBuilder("Hierarchy Ungroup"), callback: @enum =>
                    {
                        if (@enum == MyGuiScreenMessageBox.ResultEnum.YES)
                        {
                            ControlledEntity.Parent.Hierarchy.RemoveChild(ControlledEntity, true);
                            // Needs to be added back
                            MyEntities.Add(ControlledEntity);
                            MyGamePruningStructure.Add(ControlledEntity);
                            Operation = m_previousOperation;
                        }

                        DisablePicking = false;
                    }));
            }
            
        }

        // Rotation operation performed per update
        private void PerformRotation()
        {
            var ray = CreateRayFromCursorPosition();
            // Calculate the intersection point
            var planeIntersectionPoint = m_rotationPlane.Intersection(ref ray.From, ref ray.Direction);
            // Prevent too small steps
            if (Vector3D.DistanceSquared(m_rotationStartingPoint, planeIntersectionPoint) < double.Epsilon*20)
                return;

            var fromRotationVector = m_rotationStartingPoint - m_gizmoMatrix.Translation;
            var toRotationVector = planeIntersectionPoint - m_gizmoMatrix.Translation;

            var rotationMat = MatrixD.CreateFromQuaternion(QuaternionD.CreateFromTwoVectors(fromRotationVector, toRotationVector));
            var resultMat = m_storedOrientation*rotationMat*m_storedScale;
            resultMat.Translation = m_storedTranslation;

            SetWorldMatrix(ref resultMat);
        }

        private LineD CreateRayFromCursorPosition()
        {
            // Construct a ray from current cursor position
            var cursorPosition = MyInput.Static.GetMousePosition();
            var longRay = Session.Camera.WorldLineFromScreen(cursorPosition);
            // Shorten the long ray
            return new LineD(longRay.From, longRay.From + longRay.Direction*PICKING_RAY_LENGTH);
        }

        // Drag operation performed per update
        private void PerformDragg(bool lockToAxis = true)
        {
            var ray = CreateRayFromCursorPosition();

            // Calculate the intersection point
            var planeIntersectionPoint = m_dragPlane.Intersection(ref ray.From, ref ray.Direction);
            // Calculate the offset
            var dragDelta = planeIntersectionPoint - m_dragStartingPoint;

            if (lockToAxis)
            {
                // axis must be unit vector in order to project right
                var deltaProjection = dragDelta.Dot(ref m_dragAxis);
                // Do nothing for small deltas
                if (Math.Abs(deltaProjection) < double.Epsilon)
                    return;

                // Create new world matrix
                var worldMat = ControlledEntity.PositionComp.WorldMatrix;
                worldMat.Translation = m_dragStartingPosition + m_dragAxis*deltaProjection;

                SetWorldMatrix(ref worldMat);
            }
            else
            {
                // Prevent small changes
                if (dragDelta.LengthSquared() < double.Epsilon)
                    return;

                // Create new world matrix
                var worldMat = ControlledEntity.PositionComp.WorldMatrix;
                worldMat.Translation = m_dragStartingPosition + dragDelta;

                SetWorldMatrix(ref worldMat);
            }

            UpdateGizmoPosition();
        }

        // Pick the control and determine which operation to do
        private bool PickControl()
        {
            // Used only for translation
            if (m_xBB.Intersects(ref m_lastRay) != null)
            {
                m_selected = 0;
                PrepareDrag(null, m_gizmoMatrix.Right);
                m_dragActive = true;
                return true;
            }
            // Used only for translation
            if (m_yBB.Intersects(ref m_lastRay) != null)
            {
                m_selected = 1;
                PrepareDrag(null, m_gizmoMatrix.Up);
                m_dragActive = true;
                return true;
            }
            // Used only for translation
            if (m_zBB.Intersects(ref m_lastRay) != null)
            {
                m_selected = 2;
                PrepareDrag(null, m_gizmoMatrix.Backward);
                m_dragActive = true;
                return true;
            }
            // Used for both. Translation and rotation
            if (m_xPlane.Intersects(ref m_lastRay) != null)
            {
                if (Operation == OperationMode.Rotation)
                {
                    PrepareRotation(m_gizmoMatrix.Right);
                    m_rotationActive = true;
                }
                else
                {
                    PrepareDrag(m_gizmoMatrix.Right, null);
                    m_dragActive = true;
                }
                m_selected = 3;
                return true;
            }
            // Used for both. Translation and rotation
            if (m_yPlane.Intersects(ref m_lastRay) != null)
            {
                if (Operation == OperationMode.Rotation)
                {
                    PrepareRotation(m_gizmoMatrix.Up);
                    m_rotationActive = true;
                }
                else
                {
                    PrepareDrag(m_gizmoMatrix.Up, null);
                    m_dragActive = true;
                }
                m_selected = 4;
                return true;
            }
            // Used for both. Translation and rotation
            if (m_zPlane.Intersects(ref m_lastRay) != null)
            {
                if (Operation == OperationMode.Rotation)
                {
                    PrepareRotation(m_gizmoMatrix.Backward);
                    m_rotationActive = true;
                }
                else
                {
                    PrepareDrag(m_gizmoMatrix.Backward, null);
                    m_dragActive = true;
                }
                m_selected = 5;
                return true;
            }

            return false;
        }

        // Changes the world matrix of entity in the generic way 
        private void SetWorldMatrix(ref MatrixD newWorldMatrix)
        {
            var cubeGrid = ControlledEntity as MyCubeGrid;
            // Cube grids and their physical groups are special case
            if(cubeGrid != null) {
                var groups = MyCubeGridGroups.Static.Physical.GetGroup(cubeGrid);

                var prevWorldInv = cubeGrid.PositionComp.WorldMatrixNormalizedInv;
                // Move the cubeGrid
                cubeGrid.PositionComp.WorldMatrix = newWorldMatrix;
                foreach (var member in groups.m_members)
                {
                    // Update only top most
                    if (member.NodeData.Parent == null && member.NodeData != cubeGrid)
                    {
                        // Back to world origin and then to new position
                        var newMemberWorldMatrix = member.NodeData.PositionComp.WorldMatrix * prevWorldInv * newWorldMatrix;
                        member.NodeData.PositionComp.WorldMatrix = newMemberWorldMatrix;
                    }
                }
            }
            else if (ControlledEntity.Parent != null)
            {
                // world matrix of entities in hierarchy can be set just from parent
                ControlledEntity.PositionComp.SetWorldMatrix(newWorldMatrix, ControlledEntity.Parent, true);
            }
            else
            {
                ControlledEntity.PositionComp.WorldMatrix = newWorldMatrix;
            }
        }

        // Control was picked, prepare for rotation operation
        private void PrepareRotation(Vector3D axis)
        {
            m_rotationAxis = axis;
            // Create plane for picking of rotation vectors
            m_rotationPlane = new PlaneD(m_gizmoMatrix.Translation, m_rotationAxis);
            // Set up the starting point of rotation
            m_rotationStartingPoint = m_rotationPlane.Intersection(ref m_lastRay.From, ref m_lastRay.Direction);

            // Store the transformations
            var worldMat = ControlledEntity.PositionComp.WorldMatrix;
            m_storedScale = MatrixD.CreateScale(worldMat.Scale);
            m_storedTranslation = worldMat.Translation;
            m_storedOrientation = worldMat.GetOrientation();
        }

        // Control was picked, prepare for drag operation
        private void PrepareDrag(Vector3D? planeNormal, Vector3D? axis)
        {
            if (axis.HasValue)
            {
                var toCameraVect = Session.Camera.Position - m_gizmoMatrix.Translation;
                var rightVect = Vector3D.Cross(axis.Value, toCameraVect);
                planeNormal = Vector3D.Cross(axis.Value, rightVect);
                // now get rid of the axis part to create perpendicular vector

                m_dragPlane = new PlaneD(m_gizmoMatrix.Translation, planeNormal.Value);
            }
            else if (planeNormal.HasValue)
            {
                m_dragPlane = new PlaneD(m_gizmoMatrix.Translation, planeNormal.Value);
            }

            m_dragStartingPoint = m_dragPlane.Intersection(ref m_lastRay.From, ref m_lastRay.Direction);
            // store axis
            if (axis != null)
            {
                m_dragAxis = axis.Value;
            }
            // Set the flag for drag in plane option.
            m_dragOverAxis = axis != null;
            // store position
            m_dragStartingPosition = ControlledEntity.PositionComp.GetPosition();
        }

        private MyEntity PickEntity()
        {
            var hitInfo = MyPhysics.CastRay(m_lastRay.From, m_lastRay.To);

            m_rayCastResultList.Clear();
            MyGamePruningStructure.GetAllEntitiesInRay(ref m_lastRay, m_rayCastResultList);

            // Reorder the entities, that the grids are in the back
            var frontCursor = 0;
            for (var index = 0; index < m_rayCastResultList.Count; index++)
            {
                var currentElement = m_rayCastResultList[index];
                if (!(currentElement.Element is MyCubeGrid))
                {
                    m_rayCastResultList.Swap(frontCursor, index);
                    frontCursor++;
                }
            }

            // No results nothing to do
            if (m_rayCastResultList.Count == 0 && hitInfo == null)
                return null;

            MyEntity foundEntity = null;
            double overlapDistance = Double.MaxValue;
            foreach (var overlapResult in m_rayCastResultList)
            {
                if (overlapResult.Element.PositionComp.WorldAABB.Intersects(ref m_lastRay, out overlapDistance) 
                    && (
                    overlapResult.Element is MyCubeGrid 
                    || overlapResult.Element is MyFloatingObject 
                    || overlapResult.Element.GetType() == typeof(MyEntity))
                    )
                {
                    foundEntity = overlapResult.Element;
                    break;
                }
            }

            if (hitInfo.HasValue && Vector3D.Distance(hitInfo.Value.Position, m_lastRay.From) < overlapDistance)
            {
                // hit info is better option
                var hitEntity = hitInfo.Value.HkHitInfo.GetHitEntity();
                if (hitEntity is MyCubeGrid
                    || hitEntity is MyFloatingObject)
                {
                    return (MyEntity)hitEntity;
                }
            }

            if (foundEntity == null)
            {
                // Just to try to traverse it children again
                foundEntity = ControlledEntity;
            }

            if (foundEntity == null)
            {
                return null;
            }

            return foundEntity;
        }

        // Sets values of the session component to default values
        public void SetControlledEntity(MyEntity entity)
        {
            if(ControlledEntity == entity)
                return;

            if (ControlledEntity != null)
            {
                ControlledEntity.PositionComp.OnPositionChanged -= ControlledEntityPositionChanged;
                // Restore cached collision layers
                Physics_RestorePreviousCollisionLayerState();
            }

            var old = ControlledEntity;
            m_controlledEntity = entity;

            // Fire up an event for others to see.
            if (ControlledEntityChanged != null)
            {
                ControlledEntityChanged(old, entity);
            }

            if (entity != null)
            {
                // Register position changed callback
                ControlledEntity.PositionComp.OnPositionChanged += ControlledEntityPositionChanged;
                // Register on closing event in case of entity deletion
                ControlledEntity.OnClosing += myEntity =>
                {
                    myEntity.PositionComp.OnPositionChanged -= ControlledEntityPositionChanged;
                    m_controlledEntity = null;
                };

                // Cache physics collision layers and move to NoCollisionLayer
                Physics_ClearCollisionLayerCache();
                Physics_MoveEntityToNoCollisionLayer(ControlledEntity);

                UpdateGizmoPosition();
            }
        }

        // Restores previous collision layers for cached entities.
        private void Physics_RestorePreviousCollisionLayerState()
        {
            foreach (var cacheData in m_cachedBodyCollisionLayers)
            {
                cacheData.Key.Physics.RigidBody.Layer = cacheData.Value;
            }
        }

        // Clears Collision layer cache
        private void Physics_ClearCollisionLayerCache()
        {
            m_cachedBodyCollisionLayers.Clear();
        }

        // Moves entity, her hierarchy and all physical linked grids to NoCollisionLayer
        // and caches the previous state.
        private void Physics_MoveEntityToNoCollisionLayer(MyEntity entity)
        {
            var cubeGrid = entity as MyCubeGrid;
            if (cubeGrid != null)
            {
                var group = MyCubeGridGroups.Static.Physical.GetGroup(cubeGrid);
                foreach (var member in @group.m_members)
                {
                    if (member.NodeData.Parent == null)
                    {
                        Physics_MoveEntityToNoCollisionLayerRecursive(member.NodeData);
                    }
                }
            }
            else
            {
                Physics_MoveEntityToNoCollisionLayerRecursive(entity);   
            }
        }

        // Moves entity with physics and its hierarchy to NoCollisionLayer and caches previous state.
        private void Physics_MoveEntityToNoCollisionLayerRecursive(MyEntity entity)
        {
            if (entity.Physics != null) 
            { 
                // Cache previous collision layer and move to NoCollisionLayer.
                // Assuming that adding the same entity twice should not be valid!
                m_cachedBodyCollisionLayers.Add(entity, entity.Physics.RigidBody.Layer);
                entity.Physics.RigidBody.Layer = MyPhysics.CollisionLayers.NoCollisionLayer;
            }

            foreach (var child in entity.Hierarchy.Children)
            {
                if (child.Entity.Physics != null 
                    && child.Entity.Physics.RigidBody != null)
                {
                    // Cache previous collision layer and move to NoCollisionLayer.
                    Physics_MoveEntityToNoCollisionLayerRecursive((MyEntity)child.Entity);
                }
            }
        }

        private void ControlledEntityPositionChanged(MyPositionComponentBase myPositionComponentBase)
        {
            UpdateGizmoPosition();
        }

        private void UpdateGizmoPosition()
        {
            var worldMat = ControlledEntity.PositionComp.WorldMatrix;
            // Size of gizmo is equal to the size of bounding volume radius
            var gizmoSize = ControlledEntity.PositionComp.WorldVolume.Radius;
            // Most of the time the gizmo center is offset 
            var gizmoCenterOffset = (ControlledEntity.PositionComp.WorldVolume.Center - worldMat.Translation).Length();
            gizmoSize += (float) gizmoCenterOffset;

            m_gizmoMatrix = MatrixD.Identity;

            // For local coordinates we also need to apply rotation
            if (Mode == CoordinateMode.LocalCoords)
            {
                m_gizmoMatrix = worldMat;
                // needs to be normalized in case of scale
                m_gizmoMatrix = MatrixD.Normalize(m_gizmoMatrix);
            }
            else
            {
                // there is no need for scale or rotation
                m_gizmoMatrix.Translation = worldMat.Translation;
            }

            // reset to default
            m_xBB.Center = new Vector3D(gizmoSize, 0, 0);
            m_yBB.Center = new Vector3D(0, gizmoSize, 0);
            m_zBB.Center = new Vector3D(0, 0, gizmoSize);
            m_xBB.Orientation = Quaternion.Identity;
            m_yBB.Orientation = Quaternion.Identity;
            m_zBB.Orientation = Quaternion.Identity;

            // Transform to gizmo space
            m_xBB.Transform(m_gizmoMatrix);
            m_yBB.Transform(m_gizmoMatrix);
            m_zBB.Transform(m_gizmoMatrix);

            // reset to default position around 0,0,0 with entity size
            m_xPlane.Center = new Vector3D(-PLANE_THICKNESS/2, gizmoSize/2, gizmoSize/2);
            m_yPlane.Center = new Vector3D(gizmoSize/2, PLANE_THICKNESS/2, gizmoSize/2);
            m_zPlane.Center = new Vector3D(gizmoSize/2, gizmoSize/2, PLANE_THICKNESS/2);
            m_xPlane.HalfExtent = new Vector3D(PLANE_THICKNESS/2, gizmoSize/2, gizmoSize/2);
            m_yPlane.HalfExtent = new Vector3D(gizmoSize/2, PLANE_THICKNESS/2, gizmoSize/2);
            m_zPlane.HalfExtent = new Vector3D(gizmoSize/2, gizmoSize/2, PLANE_THICKNESS/2);
            m_xPlane.Orientation = Quaternion.Identity;
            m_yPlane.Orientation = Quaternion.Identity;
            m_zPlane.Orientation = Quaternion.Identity;

            // Transform to gizmo space
            m_xPlane.Transform(m_gizmoMatrix);
            m_yPlane.Transform(m_gizmoMatrix);
            m_zPlane.Transform(m_gizmoMatrix);
        }
    }
}
