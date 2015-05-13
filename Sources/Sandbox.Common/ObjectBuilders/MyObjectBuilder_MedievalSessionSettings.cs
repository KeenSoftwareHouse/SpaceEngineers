using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Medieval.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MedievalSessionSettings : MyObjectBuilder_SessionSettings
    {
        [ProtoMember(1)]
        [Display(Name = "Enable structural simulation")]
        [GameRelationAttribute(Game.MedievalEngineers)]
        public bool EnableStructuralSimulation = true;

        [ProtoMember(2)]
        [Display(Name = "Enable barbarians")]
        [GameRelationAttribute(Game.MedievalEngineers)]
        public bool EnableBarbarians = false;

        [ProtoMember(3)]
        [Display(Name = "Max active fracture pieces")]
        [GameRelationAttribute(Game.MedievalEngineers)]
        [Range(0, int.MaxValue)]
        //Max of any fracture pieces
        public int MaxActiveFracturePieces = 400;

        [ProtoMember(4)]
        [Display(Name = "Game day in real minutes")]
        [GameRelationAttribute(Game.MedievalEngineers)]
        [Range(1, int.MaxValue)]
        public float GameDayInRealMinutes = 20;

        [ProtoMember(5)]
        [Display(Name = "Day night ratio")]
        [GameRelationAttribute(Game.MedievalEngineers)]
        [Range(0.01f, 1)]
        public float DayNightRatio = 2.0f/3.0f;

        [ProtoMember(6)]
        [Display(Name = "Enable animals")]
        [GameRelationAttribute(Game.MedievalEngineers)]
        public bool EnableAnimals = false;

        [ProtoMember(7)]
        [Display(Name = "Maximum bots")]
        [GameRelation(Game.MedievalEngineers)]
        [Range(0, int.MaxValue)]
        public short MaximumBots = 10;
    }
}
