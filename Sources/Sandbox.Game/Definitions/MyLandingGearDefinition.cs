using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

using VRageMath;
using Sandbox.Engine.Utils;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_LandingGearDefinition))]
    public class MyLandingGearDefinition : MyCubeBlockDefinition
    {
        public string LockSound;
        public string UnlockSound;
        public string FailedAttachSound;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var landingGearBuilder = builder as MyObjectBuilder_LandingGearDefinition;
            MyDebug.AssertDebug(landingGearBuilder != null);
            LockSound = landingGearBuilder.LockSound;
            UnlockSound = landingGearBuilder.UnlockSound;
            FailedAttachSound = landingGearBuilder.FailedAttachSound;
        }
    }
}