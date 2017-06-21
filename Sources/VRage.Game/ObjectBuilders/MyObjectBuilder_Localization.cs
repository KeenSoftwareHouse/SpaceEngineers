using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game.ObjectBuilders
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Localization : MyObjectBuilder_Base
    {
        // WARNING: Do not ever change these until you check the MyLocalization class.
        public ulong Id;
        public string Language = "English";
        public string Context = "VRage";
        public string ResourceName = "Default Name";
        public bool Default = false;
        // Tag to Text
        public SerializableDictionary<string, string> Entries = new SerializableDictionary<string, string>();
        // Runtime flag
        [XmlIgnore]
        public bool Modified = false;

        public override string ToString()
        {
            return ResourceName + " " + Id;
        }
    }
}
