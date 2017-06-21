using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;
using VRageRender;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.Game.WorldEnvironment;
using Sandbox.Game.World;

namespace Sandbox.Game.Weapons.Guns
{
    public class MyDrillSensorRayCast : MyDrillSensorBase
    {
        private static List<MyLineSegmentOverlapResult<MyEntity>> m_raycastResults = new List<MyLineSegmentOverlapResult<MyEntity>>();

        private float m_rayLength;
        private float m_originOffset;
        private Vector3D m_origin;
        private List<MyPhysics.HitInfo> m_hits;

        public MyDrillSensorRayCast(float originOffset, float rayLength)
        {
            m_rayLength = rayLength;
            m_originOffset = originOffset;
            m_hits = new List<MyPhysics.HitInfo>();
        }

        public override void OnWorldPositionChanged(ref MatrixD worldMatrix)
        {
            var forward = worldMatrix.Forward;
            m_origin = worldMatrix.Translation + forward * m_originOffset;
            FrontPoint = m_origin + m_rayLength * forward;
            Center = (m_origin + FrontPoint) * 0.5f;
        }

		public static bool GetShapeCenter(HkShape shape, uint shapeKey, MyCubeGrid grid, ref Vector3D shapeCenter)
		{
            return false; //Disabled because of bad computing shapeCenter in relation with grid (alway around grid center).
                          //Called on grid part which has havok shape (only door?).
			bool shapeSet = true;

			switch (shape.ShapeType)
			{
				case HkShapeType.List:
					var listShape = (HkListShape)shape;
					shape = listShape.GetChildByIndex((int)shapeKey);
					break;
				case HkShapeType.Mopp:
					var moppShape = (HkMoppBvTreeShape)shape;
					shape = moppShape.ShapeCollection.GetShape(shapeKey, null);
					break;
				case HkShapeType.Box:
					var boxShape = (HkBoxShape)shape;
					shape = boxShape;
					break;
				case HkShapeType.ConvexTranslate:
					var convexTranslateShape = (HkConvexShape)shape;
					shape = convexTranslateShape;
					break;
				case HkShapeType.ConvexTransform:
					var convexTransformShape = (HkConvexTransformShape)shape;
					shape = convexTransformShape;
					break;
			/*	case HkShapeType.BvTree:
					var bvTreeShape = (HkBvTreeShape)shape;
					var iterator = bvTreeShape.Base.GetContainer();
					while (iterator.CurrentValue.IsContainer() && iterator.CurrentValue.ShapeType != HkShapeType.ConvexTranslate && iterator.CurrentValue.ShapeType != HkShapeType.ConvexTransform)
						iterator.Next();
					if (iterator.IsValid)
						shape = iterator.CurrentValue;
					else
						shapeSet = false;
					break;*/

				default:
					shapeSet = false;
					break;
			}

			if (shapeSet)
			{
				Vector4 min4, max4;
				shape.GetLocalAABB(0.05f, out min4, out max4);
				Vector3 worldMin = Vector3.Transform(new Vector3(min4), grid.PositionComp.WorldMatrix);
				Vector3 worldMax = Vector3.Transform(new Vector3(max4), grid.PositionComp.WorldMatrix);
				var worldAABB = new BoundingBoxD(worldMin, worldMax);

				shapeCenter = worldAABB.Center;
			}
			return shapeSet;
		}

        protected override void ReadEntitiesInRange()
        {
            m_entitiesInRange.Clear();
            m_hits.Clear();
            MyPhysics.CastRay(m_origin, FrontPoint, m_hits, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);

            DetectionInfo value = new DetectionInfo();
            bool encounteredModel = false;
            foreach (var hit in m_hits)
            {
				var hitInfo = hit.HkHitInfo;
				if (hitInfo.Body == null) continue;
				var entity = hitInfo.GetHitEntity();

                if (entity == null) continue;
                var rootEntity = entity.GetTopMostParent();
                if (!IgnoredEntities.Contains(rootEntity))
                {
                    Vector3D detectionPoint = hit.Position;
					
					MyCubeGrid grid = rootEntity as MyCubeGrid;
                    if (grid != null)
                    {
                        var shape = hitInfo.Body.GetShape();
                        int shapeIdx = 0;
                        if(grid.Physics.IsWelded || grid.GetPhysicsBody().WeldInfo.Children.Count != 0)
                        {
                            if (shape.IsContainer())
                            {
                                shape = shape.GetContainer().GetShape(hitInfo.GetShapeKey(0));
                                shapeIdx = 1;
                            }
                        }
                        if (!GetShapeCenter(shape, hitInfo.GetShapeKey(shapeIdx), grid, ref detectionPoint))
                        {
                            if (grid.GridSizeEnum == MyCubeSize.Large)
                                detectionPoint += hit.HkHitInfo.Normal * -0.08f;
                            else
                                detectionPoint += hit.HkHitInfo.Normal * -0.02f;
                        }
                    }
                    
                    if (m_entitiesInRange.TryGetValue(rootEntity.EntityId, out value))
                    {
						var oldDistance = Vector3.DistanceSquared(value.DetectionPoint, m_origin);
						var newDistance = Vector3.DistanceSquared(detectionPoint, m_origin);
						if (oldDistance > newDistance)
							m_entitiesInRange[rootEntity.EntityId] = new DetectionInfo(rootEntity as MyEntity, detectionPoint);
                    }
                    else
                    {
                        m_entitiesInRange[rootEntity.EntityId] = new DetectionInfo(rootEntity as MyEntity, detectionPoint);
                    }

                    if (entity is MyEnvironmentSector && !encounteredModel)
                    {
                        var sector = entity as MyEnvironmentSector;
                        var shapekey = hitInfo.GetShapeKey(0);
                        var itemId = sector.GetItemFromShapeKey(shapekey);
                        if (sector.DataView.Items[itemId].ModelIndex < 0) continue;
                        encounteredModel = true;
                        m_entitiesInRange[entity.EntityId] = new DetectionInfo(sector, detectionPoint, itemId);
                    }
                }
            }

            LineD line = new LineD(m_origin, FrontPoint);
            using (m_raycastResults.GetClearToken())
            {
                MyGamePruningStructure.GetAllEntitiesInRay(ref line, m_raycastResults);
                foreach (var segment in m_raycastResults)
                {
                    if (segment.Element == null) continue;
                    var rootEntity = segment.Element.GetTopMostParent();
                    if (!IgnoredEntities.Contains(rootEntity))
                    {
                        MyCubeBlock block = segment.Element as MyCubeBlock;
                        if (block == null) continue;

                        Vector3D point = new Vector3D();

                        if (block.SlimBlock.HasPhysics == false)
                        {
                            MatrixD blockWorldMatrixNormalizedInv = block.PositionComp.WorldMatrixNormalizedInv;
                            Vector3D localOrigin = Vector3D.Transform(m_origin, ref blockWorldMatrixNormalizedInv);
                            Vector3D localFront = Vector3D.Transform(FrontPoint, ref blockWorldMatrixNormalizedInv);
                            Ray ray = new Ray(localOrigin, Vector3.Normalize(localFront - localOrigin));
                            //MyRenderProxy.DebugDrawAABB(block.WorldAABB, Color.Red.ToVector3(), 1.0f, 1.0f, false);
                            float? dist = ray.Intersects(block.PositionComp.LocalAABB);
                            dist += 0.01f;
                            if (dist.HasValue && dist <= m_rayLength)
                                point = m_origin + Vector3D.Normalize(FrontPoint - m_origin) * dist.Value;
                            else
                                continue;
                        }
                        else
                        {
                            // This entity was caught by Havok raycast
                            continue;
                        }

                        if (m_entitiesInRange.TryGetValue(rootEntity.EntityId, out value))
                        {
                            if (Vector3.DistanceSquared(value.DetectionPoint, m_origin) > Vector3.DistanceSquared(point, m_origin))
                                m_entitiesInRange[rootEntity.EntityId] = new DetectionInfo(rootEntity, point);
                        }
                        else
                        {
                            m_entitiesInRange[rootEntity.EntityId] = new DetectionInfo(rootEntity, point);
                        }
                    }
                }
            }
        }

        public override void DebugDraw()
        {
            MyRenderProxy.DebugDrawLine3D(m_origin, FrontPoint, Color.Red, Color.Blue, false);
        }
    }
}
