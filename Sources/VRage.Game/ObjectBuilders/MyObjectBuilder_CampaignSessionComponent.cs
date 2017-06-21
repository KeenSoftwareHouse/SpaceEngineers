using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CampaignSessionComponent : MyObjectBuilder_SessionComponent
    {
        public string CampaignName;
        public string ActiveState;
        public bool IsVanilla;
        public MyObjectBuilder_Checkpoint.ModItem Mod;

        public string CurrentOutcome;
    }
}
