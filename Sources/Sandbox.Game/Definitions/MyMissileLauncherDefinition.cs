using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Utils;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_MissileLauncherDefinition))]
    class MyMissileLauncherDefinition : MyCubeBlockDefinition
    {
        public string ProjectileMissile;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_MissileLauncherDefinition)builder;
            ProjectileMissile = ob.ProjectileMissile;
        }
    }
}
