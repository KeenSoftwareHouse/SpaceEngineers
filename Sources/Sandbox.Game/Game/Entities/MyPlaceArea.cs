#region Using

using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using VRageMath;

#endregion

namespace Sandbox.Game.Entities
{
    [MyEntityType(typeof(MyObjectBuilder_PlaceArea))]
    class MyPlaceArea : MyEntity
    {
        public int PlaceAreaProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;

        public MyPlaceArea() 
        {
            this.PositionComp = new MyPositionComponent();
            PositionComp.LocalMatrix = Matrix.Identity;

            AddDebugRenderComponent(new MyDebugRenderComponent(this));
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            MyPlaceAreas.AddPlaceArea(this);
        }

        public override void OnRemovedFromScene(object source)
        {
            MyPlaceAreas.RemovePlaceArea(this);
            base.OnRemovedFromScene(source);
        }
    }
}
