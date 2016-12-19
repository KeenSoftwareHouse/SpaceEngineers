using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Game.Entities;
using System.Diagnostics;

using VRage.ModAPI;
using VRageMath;
using VRage.Game.Components;

namespace Sandbox.Engine.Physics
{
    public static partial class MyPhysicsExtensions
    {
        public static MyPhysicsBody GetBody(this HkEntity hkEntity)
        {
            return (hkEntity != null) ? hkEntity.UserObject as MyPhysicsBody : null;
        }

        public static IMyEntity GetEntity(this HkEntity hkEntity, uint shapeKey)
        {
            var body = hkEntity.GetBody();
            if(body != null)
            {
                if (shapeKey == 0)
                    return body.Entity;
                if (shapeKey > body.WeldInfo.Children.Count)
                    return body.Entity;
                var shape = body.RigidBody.GetShape().GetContainer().GetShape(shapeKey);
                if(shape.IsValid)
                    body = HkRigidBody.FromShape(shape).GetBody();
            }
            return body != null ? body.Entity : null;
        }

        [ThreadStatic]
        static List<IMyEntity> m_entityList;

        static List<IMyEntity> EntityList
        {
            get
            {
                if (m_entityList == null)
                    m_entityList = new List<IMyEntity>();
                return m_entityList;
            }
        }
        public static List<IMyEntity> GetAllEntities(this HkEntity hkEntity)
        {
            Debug.Assert(EntityList.Count == 0, "List was not cleared!");

            var body = hkEntity.GetBody();
            if (body != null)
            {
                EntityList.Add(body.Entity);
                foreach (var child in body.WeldInfo.Children)
                    EntityList.Add(child.Entity);
            }

            return EntityList;
        }

        public static IMyEntity GetSingleEntity(this HkEntity hkEntity)
        {
            var body = hkEntity.GetBody();
            return body != null ? body.Entity : null;
        }

        public static IMyEntity GetOtherEntity(this HkContactPointEvent eventInfo, IMyEntity sourceEntity)
        {
            var bodyA = eventInfo.GetPhysicsBody(0);
            var bodyB = eventInfo.GetPhysicsBody(1);

            var entityA = bodyA == null ? null : bodyA.Entity;
            var entityB = bodyB == null ? null : bodyB.Entity;

            if (sourceEntity == entityA)
            {
                return entityB;
            }
            else
            {
                //Debug.Assert(sourceEntity == entityB);
                return entityA;
            }
        }

        public static MyPhysicsBody GetPhysicsBody(this HkContactPointEvent eventInfo, int index)
        {
            var rb = eventInfo.Base.GetRigidBody(index);
            if (rb == null)
                return null;

            var body = rb.GetBody();
            if (body != null && body.IsWelded)
            {
                uint shapeKey = 0;
                int i = 0;
                for (; i < 4; i++)
                {
                    if (eventInfo.GetShapeKey(index, i) == uint.MaxValue)
                    {
                        break;
                    }
                }
                shapeKey = eventInfo.GetShapeKey(index, i - 1);
                body = HkRigidBody.FromShape(rb.GetShape().GetContainer().GetShape(shapeKey)).GetBody();
            }
            return body;
        }

        public static IMyEntity GetHitEntity(this HkWorld.HitInfo hitInfo)
        {
            return hitInfo.Body.GetEntity(hitInfo.GetShapeKey(0));
        }

        public static float GetConvexRadius(this HkWorld.HitInfo hitInfo)
        {
            if (hitInfo.Body == null)
                return 0;

            HkShape shape = hitInfo.Body.GetShape();
            for (int i = 0; i < HkWorld.HitInfo.ShapeKeyCount; i++)
			{
                var shapeKey = hitInfo.GetShapeKey(i);
                if (HkShape.InvalidShapeKey != shapeKey) 
                {
                    if (!shape.IsContainer())
                    {
                        break;
                    }
                    //shape = shape.GetContainer().GetShape(shapeKey);
                }
                else
                {
                    break;
                }
			}
            if (shape.ShapeType == HkShapeType.ConvexTransform || shape.ShapeType == HkShapeType.ConvexTranslate || shape.ShapeType == HkShapeType.Transform)
                shape = shape.GetContainer().GetShape(0);
            if (shape.ShapeType == HkShapeType.Sphere|| shape.ShapeType==HkShapeType.Capsule)
                return 0;
            if (!shape.IsConvex)
                return HkConvexShape.DefaultConvexRadius;

            return shape.ConvexRadius;
        }

        public static Vector3 GetFixedPosition(this MyPhysics.HitInfo hitInfo)
        {
            Vector3 position = hitInfo.Position;
            float convexRadiusAdjustment = hitInfo.HkHitInfo.GetConvexRadius();
            if (convexRadiusAdjustment != 0)
                position += -hitInfo.HkHitInfo.Normal * convexRadiusAdjustment;

            return position;
        }

        public static IEnumerable<HkShape> GetAllShapes(this HkShape shape)
        {
            if (shape.IsContainer())
            {
                var iterator = shape.GetContainer();
                while (iterator.CurrentShapeKey != HkShape.InvalidShapeKey)
                {
                    foreach (var child in iterator.CurrentValue.GetAllShapes())
                        yield return child;

                    iterator.Next();
                }

                yield break;
            }

            yield return shape;
        }

        public static IMyEntity GetCollisionEntity(this HkBodyCollision collision)
        {
            return collision.Body != null ? collision.Body.GetEntity(0) : null;
        }

        public static bool IsInWorldWelded(this MyPhysicsBody body)
        {
            return body.IsInWorld || (body.WeldInfo.Parent != null && body.WeldInfo.Parent.IsInWorld);
        }

        public static bool IsInWorldWelded(this MyPhysicsComponentBase body)
        {
            return body != null && (body is MyPhysicsBody) && IsInWorldWelded((MyPhysicsBody)body);
        }
    }
}
