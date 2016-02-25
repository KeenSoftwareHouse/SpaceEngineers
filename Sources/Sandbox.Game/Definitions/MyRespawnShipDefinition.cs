using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_RespawnShipDefinition))]
    public class MyRespawnShipDefinition : MyDefinitionBase
    {
        public int Cooldown;
        public MyPrefabDefinition Prefab;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_RespawnShipDefinition;
            Cooldown = ob.CooldownSeconds;
            Prefab = MyDefinitionManager.Static.GetPrefabDefinition(ob.Prefab);
        }
    }
}
