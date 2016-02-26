using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_CryoChamberDefinition))]
    class MyCryoChamberDefinition : MyCockpitDefinition
    {
        public string OverlayTexture;

	    public string ResourceSinkGroup;
        public float IdlePowerConsumption;

        public MySoundPair OutsideSound;
        public MySoundPair InsideSound;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var chamberDef = builder as MyObjectBuilder_CryoChamberDefinition;
            OverlayTexture = chamberDef.OverlayTexture;
	        ResourceSinkGroup = chamberDef.ResourceSinkGroup;
            IdlePowerConsumption = chamberDef.IdlePowerConsumption;

            OutsideSound = new MySoundPair(chamberDef.OutsideSound);
            InsideSound = new MySoundPair(chamberDef.InsideSound);
        }
    }
}
