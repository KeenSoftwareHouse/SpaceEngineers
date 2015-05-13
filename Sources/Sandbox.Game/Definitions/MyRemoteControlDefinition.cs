using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_RemoteControlDefinition))]
    public class MyRemoteControlDefinition : MyShipControllerDefinition
    {
        public float RequiredPowerInput;
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obRemote = builder as MyObjectBuilder_RemoteControlDefinition;
            MyDebug.AssertDebug(obRemote != null, "Initializing remote control using wrong definition");
            RequiredPowerInput = obRemote.RequiredPowerInput;
        }
    }
}
