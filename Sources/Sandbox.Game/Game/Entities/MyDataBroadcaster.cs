using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public partial class MyDataBroadcaster
    {
        public MyEntity Parent { get; protected set; }

        public long EntityId
        {
            get { return Parent.EntityId; }
        }
        public Vector3D BroadcastPosition { get { return Parent.PositionComp.GetPosition(); } }
    }
}
