using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Campaign
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Campaign : MyObjectBuilder_Base
    {
        #region Editable
        public MyObjectBuilder_CampaignSM StateMachine;
        // paths to custom localization files
        [XmlArrayItem("Path")]
        public List<string> LocalizationPaths = new List<string>();
        // cached languages because we dont want to read full localization file
        // just to get the language.
        [XmlArrayItem("Language")]
        public List<string> LocalizationLanguages = new List<string>();
        // Default localization language.
        public string DefaultLocalizationLanguage;

        public string Name;
        public string Description;
        public string ImagePath;
        public bool IsMultiplayer;
        public string Difficulty;
        #endregion

        #region Runtime

        [XmlIgnore]
        public bool IsVanilla = true;

        [XmlIgnore]
        public bool IsLocalMod = true;

        [XmlIgnore]
        public string ModFolderPath;

        [XmlIgnore]
        public ulong PublishedFileId = 0;

        [XmlIgnore]
        public bool IsDebug = false;

        #endregion

    }
}
