using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.Voxels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Definitions
{
   [MyDefinitionType(typeof(MyObjectBuilder_PlanetPrefabDefinition))]
    public class MyPlanetPrefabDefinition : MyDefinitionBase
    {
       public MyObjectBuilder_Planet PlanetBuilder;

       protected override void Init(MyObjectBuilder_DefinitionBase builder)
       {
           base.Init(builder);
           var ob = builder as MyObjectBuilder_PlanetPrefabDefinition;

           this.PlanetBuilder = ob.PlanetBuilder;

       }
    }
}
