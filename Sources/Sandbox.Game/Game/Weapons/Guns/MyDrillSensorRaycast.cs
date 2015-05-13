using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Engine.Physics;
using VRageMath;
using VRageRender;
using Sandbox.Game.Entities;

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

        protected override void ReadEntitiesInRange()
        {
            m_entitiesInRange.Clear();
            m_hits.Clear();
            MyPhysics.CastRay(m_origin, FrontPoint, m_hits, MyPhysics.ObjectDetectionCollisionLayer);

            DetectionInfo value = new DetectionInfo();
            foreach (var hit in m_hits)
            {
                if (hit.HkHitInfo.Body == null) continue;
                var entity = hit.HkHitInfo.Body.GetEntity();
                if (entity == null) continue;
                var rootEntity = entity.GetTopMostParent();
                if (!IgnoredEntities.Contains(rootEntity))
                {
                    Vector3D fixedDetectionPoint = hit.Position;
                    if (rootEntity is MyCubeGrid)
                    {
                        MyCubeGrid grid = rootEntity as MyCubeGrid;
                        if (grid.GridSizeEnum == Common.ObjectBuilders.MyCubeSize.Large)
                            fixedDetectionPoint += hit.HkHitInfo.Normal * -0.08f;
                        else
                            fixedDetectionPoint += hit.HkHitInfo.Normal * -0.02f;
                    }
                    
                    if (m_entitiesInRange.TryGetValue(rootEntity.EntityId, out value))
                    {
                        if (Vector3.DistanceSquared(value.DetectionPoint, m_origin) > Vector3.DistanceSquared(fixedDetectionPoint, m_origin))
                            m_entitiesInRange[rootEntity.EntityId] = new DetectionInfo(rootEntity as MyEntity, fixedDetectionPoint);
                    }
                    else
                    {
                        m_entitiesInRange[rootEntity.EntityId] = new DetectionInfo(rootEntity as MyEntity, fixedDetectionPoint);
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
                        if (!(segment.Element is MyCubeBlock)) continue;

                        Vector3D point = new Vector3D();

                        MyCubeBlock block = segment.Element as MyCubeBlock;
                        if (block.SlimBlock.HasPhysics == false)
                        {
                            Vector3D localOrigin = Vector3D.Transform(m_origin, block.PositionComp.WorldMatrixNormalizedInv);
                            Vector3D localFront = Vector3D.Transform(FrontPoint, block.PositionComp.WorldMatrixNormalizedInv);
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
