using System;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Components
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LocalizationSessionComponent : MyObjectBuilder_SessionComponent
    {
        public List<string> AdditionalPaths = new List<string>();
        public List<string> CampaignPaths = new List<string>(); 
        public string CampaignModFolderName = String.Empty;
        public string Language = "English";
    }
}
