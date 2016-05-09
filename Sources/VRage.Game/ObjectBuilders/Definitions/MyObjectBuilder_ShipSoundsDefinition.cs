using ProtoBuf;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShipSoundSystemDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public float MaxUpdateRange = 2000;

        [ProtoMember]
        public float FullSpeed = 95;

        [ProtoMember]
        public float LargeShipDetectionRadius = 15;

        [ProtoMember]
        public float WheelStartUpdateRange = 500;

        [ProtoMember]
        public float WheelStopUpdateRange = 750;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShipSoundsDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public float MinWeight = 3000f;

        [ProtoMember]
        public bool AllowSmallGrid = true;

        [ProtoMember]
        public bool AllowLargeGrid = true;

        [ProtoMember]
        public float EnginePitchRangeInSemitones = 4f;

        [ProtoMember]
        public float EngineTimeToTurnOn = 4;

        [ProtoMember]
        public float EngineTimeToTurnOff = 3;

        [ProtoMember]
        public float SpeedUpSoundChangeVolumeTo = 1f;

        [ProtoMember]
        public float SpeedDownSoundChangeVolumeTo = 1f;

        [ProtoMember]
        public float SpeedUpDownChangeSpeed = 0.2f;

        [ProtoMember]
        public float WheelsPitchRangeInSemitones = 4f;

        [ProtoMember]
        public float WheelsLowerThrusterVolumeBy = 0.33f;

        [ProtoMember]
        public float WheelsFullSpeed = 32;

        [ProtoMember]
        public float WheelsGroundMinVolume = 0.5f;

        [ProtoMember]
        public float ThrusterPitchRangeInSemitones = 4f;

        [ProtoMember]
        public float ThrusterCompositionMinVolume = 0.4f;

        [ProtoMember]
        public float ThrusterCompositionChangeSpeed = 0.025f;

        [ProtoMember, XmlArrayItem("WheelsVolume")]
        public List<ShipSoundVolumePair> WheelsVolumes;

        [ProtoMember, XmlArrayItem("ThrusterVolume")]
        public List<ShipSoundVolumePair> ThrusterVolumes;

        [ProtoMember, XmlArrayItem("EngineVolume")]
        public List<ShipSoundVolumePair> EngineVolumes;

        [ProtoMember, XmlArrayItem("Sound")]
        public List<ShipSound> Sounds;
    }

    [ProtoContract]
    public class ShipSound
    {
        [ProtoMember, XmlAttribute("Type")]
        public ShipSystemSoundsEnum SoundType = ShipSystemSoundsEnum.MainLoopMedium;

        [ProtoMember, XmlAttribute("SoundName")]
        public string SoundName = "";
    }

    [ProtoContract]
    public class ShipSoundVolumePair
    {
        [ProtoMember, XmlAttribute("Speed")]
        public float Speed = 0;

        [ProtoMember, XmlAttribute("Volume")]
        public float Volume = 0f;
    }

    public enum ShipSystemSoundsEnum
    {
        MainLoopSlow = 0,
        MainLoopMedium = 1,
        MainLoopFast = 2,
        IonThrusters = 3,
        EnginesStart = 4,
        EnginesEnd = 5,
        HydrogenThrusters = 6,
        AtmoThrustersSlow = 7,
        AtmoThrustersMedium = 8,
        AtmoThrustersFast = 9,
        IonThrustersIdle = 10,
        HydrogenThrustersIdle = 11,
        AtmoThrustersIdle = 12,
        WheelsEngineRun = 13,
        ShipIdle = 14,
        ShipEngine = 15,
        EnginesSpeedUp = 16,
        EnginesSpeedDown = 17,
        WheelsSecondary = 18
    }
}