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

        public static IMyEntity GetEntity(this HkEntity hkEntity)
        {
            var body = hkEntity.GetBody();
            return body != null ? body.Entity : null;
        }

        public static IMyEntity GetOtherEntity(this HkContactPointEvent eventInfo, IMyEntity sourceEntity)
        {
            var entityA = eventInfo.Base.BodyA.GetEntity();
            var entityB = eventInfo.Base.BodyB.GetEntity();

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
    }
}
