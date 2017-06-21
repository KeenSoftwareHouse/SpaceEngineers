using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Campaign
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CampaignSMNode : MyObjectBuilder_Base
    {
        public string Name;
        public string SaveFilePath;
        public SerializableVector2 Location;
    }
}
