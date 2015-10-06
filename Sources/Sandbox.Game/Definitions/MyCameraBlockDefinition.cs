using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_CameraBlockDefinition))]
    public class MyCameraBlockDefinition : MyCubeBlockDefinition
    {
	    public string ResourceSinkGroup;
        public float RequiredPowerInput;
        public string OverlayTexture;

        public float MinFov;
        public float MaxFov;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obCamera = builder as MyObjectBuilder_CameraBlockDefinition;
            MyDebug.AssertDebug(obCamera != null, "Initializing camera definition using wrong object builder.!");
	        ResourceSinkGroup = obCamera.ResourceSinkGroup;
            RequiredPowerInput = obCamera.RequiredPowerInput;
            OverlayTexture = obCamera.OverlayTexture;

            MinFov = obCamera.MinFov;
            MaxFov = obCamera.MaxFov;
        }
    }
}
