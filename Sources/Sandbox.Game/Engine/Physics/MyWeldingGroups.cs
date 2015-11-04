using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Groups;

namespace Sandbox.Engine.Physics
{
    class MyWeldingGroups : MyGroups<MyEntity, MyWeldGroupData>, IMySceneComponent
    {
        public static MyWeldingGroups Static = null;
        public void Load()
        {
            Static = this;
            SupportsOphrans = true;
        }

        public void Unload()
        {
            Static = null;
        }
    }
}
