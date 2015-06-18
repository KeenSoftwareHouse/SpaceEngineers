using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    public class MyRangedValue
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "Min")]
        public float Min =0.0f;
        [ProtoMember]
        [XmlAttribute(AttributeName = "Max")]
        public float Max =0.0f;
    }

    [ProtoContract]
    public class MyStructureParams
    {
        [ProtoMember]
        public MyRangedValue Treshold = new MyRangedValue();

        [ProtoMember]
        public MyRangedValue BlendSize = new MyRangedValue();

        [ProtoMember]
        public MyRangedValue SizeRatio = new MyRangedValue();

        [ProtoMember]
        public MyRangedValue Frequency = new MyRangedValue();

        [ProtoMember]
        public MyRangedValue NumNoises = new MyRangedValue();
    }

    [ProtoContract]
    public class MyPoleParams
    {
        [ProtoMember]
        public float Probability = 0.0f;

        [ProtoMember]
        public MyRangedValue Angle = new MyRangedValue();

        [ProtoMember]
        public MyRangedValue AngleDeviation = new MyRangedValue();
    }

    
    [ProtoContract]
    public class MyOreProbabilityRange
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "Type")]
        public string OreName;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Min")]
        public float Min = 0.0f;
        [ProtoMember]
        [XmlAttribute(AttributeName = "Max")]
        public float Max = 0.0f;
    }


    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PlanetDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public MyRangedValue Diameter = new MyRangedValue();

        [ProtoMember]
        public MyRangedValue Deviation = new MyRangedValue();

        [ProtoMember]
        public MyRangedValue StructureRatio = new MyRangedValue();

        [ProtoMember]
        public MyRangedValue NormalNoiseValue = new MyRangedValue();

        [ProtoMember]
        public bool HasAtmosphere = false;

        [ProtoMember]
        public MyStructureParams HillParams = new MyStructureParams();

        [ProtoMember]
        public MyStructureParams CanyonParams = new MyStructureParams();

        [ProtoMember]
        public float HostilityProbability = 0.0f;

        [ProtoMember]
        public MyRangedValue NumLayers = new MyRangedValue();

        [ProtoMember]
        public MyPoleParams SouthPole;

        [ProtoMember]
        public MyPoleParams NorthPole;

        [ProtoMember]
        public MyRangedValue OrganicHeightEnd = new MyRangedValue();

        [ProtoMember]
        public MyRangedValue FloraMaterialSpawnProbability = new MyRangedValue();

        [ProtoMember]
        public MyRangedValue MetalsHeightEndHostile = new MyRangedValue();

        [ProtoMember]
        public MyRangedValue MetalsSpawnProbability = new MyRangedValue();

        [ProtoMember]
        [XmlArrayItem("OreProbability")]
        public MyOreProbabilityRange[] MetalsOreProbability;

    }

}
