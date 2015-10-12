using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_MedicalRoomDefinition))]
    public class MyMedicalRoomDefinition : MyCubeBlockDefinition
    {
	    public string ResourceSinkGroup;
        public string IdleSound;
        public string ProgressSound;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var landingGearBuilder = builder as MyObjectBuilder_MedicalRoomDefinition;
            MyDebug.AssertDebug(landingGearBuilder != null);
	        ResourceSinkGroup = landingGearBuilder.ResourceSinkGroup;
            IdleSound = landingGearBuilder.IdleSound;
            ProgressSound = landingGearBuilder.ProgressSound;
        }
    }
}
