using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public class MyDataBroadcaster : MyEntityComponentBase
    {
        public Vector3D BroadcastPosition { get { return Entity.PositionComp.GetPosition(); } }

        public override string ComponentTypeDebugString
        {
            get { return "MyDataBroadcaster"; }
        }
    }
}
