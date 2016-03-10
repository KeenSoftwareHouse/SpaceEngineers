using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System.ComponentModel.DataAnnotations;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Medieval.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_MedievalSessionSettings : MyObjectBuilder_SessionSettings
    {
        public MyObjectBuilder_MedievalSessionSettings()
        {
            // Override default values of base class here
            this.InventorySizeMultiplier = 1.0f;
            this.EnableSunRotation = false;
        }

        [ProtoMember]
        [Display(Name = "Enable barbarians")]
        [GameRelation(Game.MedievalEngineers)]
        public bool EnableBarbarians = true;

        [ProtoMember]
        [Display(Name = "Game day in real minutes")]
        [GameRelation(Game.MedievalEngineers)]
        [Range(20, int.MaxValue)]
        public float GameDayInRealMinutes = 20;

        [ProtoMember]
        [Display(Name = "Day night ratio")]
        [GameRelation(Game.MedievalEngineers)]
        [Range(0.01f, 1)]
        public float DayNightRatio = 2.0f/3.0f;

        [ProtoMember]
        [Display(Name = "Enable animals")]
        [GameRelation(Game.MedievalEngineers)]
        public bool EnableAnimals = true;

        [ProtoMember]
        [Display(Name = "Max uncontrolled bots")]
        [GameRelation(Game.MedievalEngineers)]
        [Range(0, 20)]
        public short MaximumBots = 10;

        [ProtoMember]
        [Display(Name = "Servants per player")]
        [GameRelation(Game.MedievalEngineers)]
        [Range(0, 5)]
        public short ServantsPerPlayer = 2;
    }
}
