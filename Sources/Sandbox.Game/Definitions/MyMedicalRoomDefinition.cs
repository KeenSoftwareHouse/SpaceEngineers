using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

using VRageMath;
using Sandbox.Engine.Utils;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_MedicalRoomDefinition))]
    public class MyMedicalRoomDefinition : MyCubeBlockDefinition
    {
        public string IdleSound;
        public string ProgressSound;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var landingGearBuilder = builder as MyObjectBuilder_MedicalRoomDefinition;
            MyDebug.AssertDebug(landingGearBuilder != null);
            IdleSound = landingGearBuilder.IdleSound;
            ProgressSound = landingGearBuilder.ProgressSound;
        }
    }
}
