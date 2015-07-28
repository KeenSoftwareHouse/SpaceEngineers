using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Game.Entities;
using System.Diagnostics;
using Sandbox.Common.Components;
using VRage.ModAPI;

namespace Sandbox.Engine.Physics
{
    public static class MyPhysicsExtensions
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
                if (body.WeldInfo.Children.Count == 0 || shapeKey > 100)
                    return body.Entity;
                var shape = body.RigidBody.GetShape().GetContainer().GetShape(shapeKey);
                if(shape.IsValid)
                    body = HkRigidBody.FromShape(shape).GetBody();
            }
            return body != null ? body.Entity : null;
        }

        static List<IMyEntity> m_entityList = new List<IMyEntity>();
        public static List<IMyEntity> GetAllEntities(this HkEntity hkEntity)
        {
            //Debug.Assert(m_entityList.Count == 0, "List was not cleared!");

            var body = hkEntity.GetBody();
            if (body != null)
            {
                m_entityList.Add(body.Entity);
                if (body.IsWelded)
                {
                    foreach (var child in body.WeldInfo.Children)
                        m_entityList.Add(child.Entity);
                }
            }

            return m_entityList;
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
            var body = eventInfo.Base.GetRigidBody(index).GetBody();
            if (body.IsWelded)
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
                body = HkRigidBody.FromShape(eventInfo.Base.BodyA.GetShape().GetContainer().GetShape(shapeKey)).GetBody();
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

    }
}
