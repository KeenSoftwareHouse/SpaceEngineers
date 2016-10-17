using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Campaign
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CampaignSM : MyObjectBuilder_Base
    {
        public MyObjectBuilder_CampaignSMNode[] Nodes;
        public MyObjectBuilder_CampaignSMTransition[] Transitions;
    }
}
