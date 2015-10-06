using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Game.Entities;
using System.Diagnostics;
using Sandbox.Common.Components;
using VRage.ModAPI;
using VRageMath;

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
                    if (eventInfo.GetShapeKey(0, i) == uint.MaxValue)
                    {
                        break;
                    }
                }
                shapeKey = eventInfo.GetShapeKey(0, i - 1);
                body = HkRigidBody.FromShape(rb.GetShape().GetContainer().GetShape(shapeKey)).GetBody();
            }
            return body;
        }

        public static IMyEntity GetHitEntity(this HkWorld.HitInfo hitInfo)
        {
            return hitInfo.Body.GetEntity(hitInfo.GetShapeKey(0));
        }

        public static IMyEntity GetCollisionEntity(this HkBodyCollision collision)
        {
            return collision.Body != null ? collision.Body.GetEntity(0) : null;
        }

        public static bool IsInWorldWelded(this MyPhysicsBody body)
        {
            return body.IsInWorld || (body.WeldInfo.Parent != null && body.WeldInfo.Parent.IsInWorld);
        }

    }
}
