using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_CameraBlockDefinition))]
    public class MyCameraBlockDefinition : MyCubeBlockDefinition
    {
	    public string ResourceSinkGroup;
        public float RequiredPowerInput;
        public float RequiredChargingInput;
        public string OverlayTexture;

        public float MinFov;
        public float MaxFov;
        public float RaycastConeLimit;
        public double RaycastDistanceLimit;
        public float RaycastTimeMultiplier;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obCamera = builder as MyObjectBuilder_CameraBlockDefinition;
            MyDebug.AssertDebug(obCamera != null, "Initializing camera definition using wrong object builder.!");
	        ResourceSinkGroup = obCamera.ResourceSinkGroup;
            RequiredPowerInput = obCamera.RequiredPowerInput;
            RequiredChargingInput = obCamera.RequiredChargingInput;
            OverlayTexture = obCamera.OverlayTexture;

            MinFov = obCamera.MinFov;
            MaxFov = obCamera.MaxFov;
            RaycastConeLimit = obCamera.RaycastConeLimit;
            RaycastDistanceLimit = obCamera.RaycastDistanceLimit;
            RaycastTimeMultiplier = obCamera.RaycastTimeMultiplier;
        }
    }
}
