using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

using VRageMath;
using Sandbox.Engine.Utils;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_DoorDefinition))]
    public class MyDoorDefinition : MyCubeBlockDefinition
    {
        public float MaxOpen;
        public string OpenSound;
        public string CloseSound;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var doorBuilder = builder as MyObjectBuilder_DoorDefinition;
            MyDebug.AssertDebug(doorBuilder != null);
            MaxOpen = doorBuilder.MaxOpen;
            OpenSound = doorBuilder.OpenSound;
            CloseSound = doorBuilder.CloseSound;
        }
    }
}
