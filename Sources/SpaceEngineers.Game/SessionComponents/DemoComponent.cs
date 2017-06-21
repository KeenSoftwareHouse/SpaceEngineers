using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObjectBuilders.Definitions;
using SpaceEngineers.Game.Definitions;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Components.Session;

namespace SpaceEngineers.Game.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class DemoComponent : MySessionComponentBase
    {
        public override void InitFromDefinition(MySessionComponentDefinition definition)
        {
            base.InitFromDefinition(definition);

            var def = (MyDemoComponentDefinition)definition;

            Debug.Print("Values from definition:");
            Debug.Print("Int: {0}", def.Int);
            Debug.Print("Float: {0}", def.Float);
            Debug.Print("String: {0}", def.String);
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
        }

        public override bool IsRequiredByGame
        {
            get { return false; }
        }
    }
}
