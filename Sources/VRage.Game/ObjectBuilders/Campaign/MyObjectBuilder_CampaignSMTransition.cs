using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Campaign
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CampaignSMTransition : MyObjectBuilder_Base
    {
        public string Name;
        public string From;
        public string To;
    }
}
