using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System.ComponentModel.DataAnnotations;
using VRage.ObjectBuilders;

namespace Medieval.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MedievalSessionSettings : MyObjectBuilder_SessionSettings
    {
        [ProtoMember]
        [Display(Name = "Enable structural simulation")]
        [GameRelationAttribute(Game.MedievalEngineers)]
        public bool EnableStructuralSimulation = true;

        [ProtoMember]
        [Display(Name = "Enable barbarians")]
        [GameRelationAttribute(Game.MedievalEngineers)]
        public bool EnableBarbarians = true;

        [ProtoMember]
        [Display(Name = "Max active fracture pieces")]
        [GameRelationAttribute(Game.MedievalEngineers)]
        [Range(0, int.MaxValue)]
        //Max of any fracture pieces
        public int MaxActiveFracturePieces = 400;

        [ProtoMember]
        [Display(Name = "Game day in real minutes")]
        [GameRelationAttribute(Game.MedievalEngineers)]
        [Range(20, int.MaxValue)]
        public float GameDayInRealMinutes = 20;

        [ProtoMember]
        [Display(Name = "Day night ratio")]
        [GameRelationAttribute(Game.MedievalEngineers)]
        [Range(0.01f, 1)]
        public float DayNightRatio = 2.0f/3.0f;

        [ProtoMember]
        [Display(Name = "Enable animals")]
        [GameRelationAttribute(Game.MedievalEngineers)]
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
