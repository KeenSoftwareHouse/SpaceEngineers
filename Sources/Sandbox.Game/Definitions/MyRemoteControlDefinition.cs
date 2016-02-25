using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_RemoteControlDefinition))]
    public class MyRemoteControlDefinition : MyShipControllerDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public float RequiredPowerInput;
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obRemote = builder as MyObjectBuilder_RemoteControlDefinition;
            MyDebug.AssertDebug(obRemote != null, "Initializing remote control using wrong definition");
	        ResourceSinkGroup = MyStringHash.GetOrCompute(obRemote.ResourceSinkGroup);
            RequiredPowerInput = obRemote.RequiredPowerInput;
        }
    }
}
