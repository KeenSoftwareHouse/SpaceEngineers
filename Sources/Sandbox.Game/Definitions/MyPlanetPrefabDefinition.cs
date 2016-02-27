using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;

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
