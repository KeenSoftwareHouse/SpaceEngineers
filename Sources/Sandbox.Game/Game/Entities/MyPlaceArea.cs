#region Using

using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using VRage.Library.Utils;
using VRageMath;

#endregion

namespace Sandbox.Game.Entities
{
    public abstract class MyPlaceArea : MyComponentBase
    {
        public int PlaceAreaProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;

        public abstract BoundingBoxD WorldAABB { get; }
        public MyStringId AreaType { get; private set; }

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
