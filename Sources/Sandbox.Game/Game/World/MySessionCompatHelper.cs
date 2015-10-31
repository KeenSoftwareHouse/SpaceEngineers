using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using VRage.Components;

namespace Sandbox.Game.World
{
    public class MySessionCompatHelper
    {
        public virtual void FixSessionObjectBuilders(MyObjectBuilder_Checkpoint checkpoint, MyObjectBuilder_Sector sector)
        { }

        public virtual void AfterEntitiesLoad(int saveVersion)
        { }

        public virtual void CheckAndFixPrefab(MyObjectBuilder_Definitions prefab)
        { }
    }
}
