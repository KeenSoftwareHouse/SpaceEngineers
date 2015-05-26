#region Using

using Sandbox.Common.Components;
using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Game.Entities
{
    public abstract class MyPlaceArea : MyComponentBase
    {
        public int PlaceAreaProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;

        public abstract BoundingBoxD WorldAABB { get; }
        public MyStringId AreaType { get; private set; }

        public static MyPlaceArea FromEntity(long entityId)
        {
            MyPlaceArea area = null;
            MyEntity entity = null;
            if (!MyEntities.TryGetEntityById(entityId, out entity))
                return area;

            if (entity.Components.TryGet<MyPlaceArea>(out area))
                return area;
            else
                return null;
        }

        public MyPlaceArea(MyStringId areaType)
        {
            AreaType = areaType;
        }

        public override void OnAddedToContainer(MyComponentContainer container)
        {
            base.OnAddedToContainer(container);
            MyPlaceAreas.AddPlaceArea(this);
        }

        public override void OnRemovedFromContainer(MyComponentContainer container)
        {
            MyPlaceAreas.RemovePlaceArea(this);
            base.OnRemovedFromContainer(container);
        }

		public abstract double DistanceSqToPoint(Vector3D point);

        public abstract bool TestPoint(Vector3D point);
    }
}
