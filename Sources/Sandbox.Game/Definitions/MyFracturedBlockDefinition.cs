using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_FracturedBlockDefinition))]
    class MyFracturedBlockDefinition : MyCubeBlockDefinition
    {
    }
}
