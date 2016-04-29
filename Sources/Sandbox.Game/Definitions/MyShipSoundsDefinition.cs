using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ShipSoundsDefinition))]
    public class MyShipSoundsDefinition : MyDefinitionBase
    {
        public float MinWeight = 3000f;
        public bool AllowSmallGrid = true;
        public bool AllowLargeGrid = true;
        public Dictionary<ShipSystemSoundsEnum, MySoundPair> Sounds = new Dictionary<ShipSystemSoundsEnum,MySoundPair>();
        public List<MyTuple<float, float>> ThrusterVolumes = new List<MyTuple<float, float>>();
        public List<MyTuple<float, float>> EngineVolumes = new List<MyTuple<float, float>>();
        public List<MyTuple<float, float>> WheelsVolumes = new List<MyTuple<float, float>>();

        public float EnginePitchRangeInSemitones = 4f;
        public float EnginePitchRangeInSemitones_h = -2f;
        public float WheelsPitchRangeInSemitones = 4f;
        public float WheelsPitchRangeInSemitones_h = -2f;
        public float ThrusterPitchRangeInSemitones = 4f;
        public float ThrusterPitchRangeInSemitones_h = -2f;
        public float EngineTimeToTurnOn = 4f;
        public float EngineTimeToTurnOff = 3f;
        public float WheelsLowerThrusterVolumeBy = 0.33f;
        public float WheelsFullSpeed = 32f;
        public float WheelsSpeedCompensation = 3f;
        public float ThrusterCompositionMinVolume = 0.4f;
        public float ThrusterCompositionMinVolume_c = 0.4f / (1f - 0.4f);
        public float ThrusterCompositionChangeSpeed = 0.025f;
        public float SpeedUpSoundChangeVolumeTo = 1f;
        public float SpeedDownSoundChangeVolumeTo = 1f;
        public float SpeedUpDownChangeSpeed = 0.2f;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_ShipSoundsDefinition;
            MyDebug.AssertDebug(ob != null);

            this.MinWeight = ob.MinWeight;
            this.AllowSmallGrid = ob.AllowSmallGrid;
            this.AllowLargeGrid = ob.AllowLargeGrid;
            this.EnginePitchRangeInSemitones = ob.EnginePitchRangeInSemitones;
            this.EnginePitchRangeInSemitones_h = ob.EnginePitchRangeInSemitones * -0.5f;
            this.EngineTimeToTurnOn = ob.EngineTimeToTurnOn;
            this.EngineTimeToTurnOff = ob.EngineTimeToTurnOff;
            this.WheelsLowerThrusterVolumeBy = ob.WheelsLowerThrusterVolumeBy;
            this.WheelsFullSpeed = ob.WheelsFullSpeed;
            this.ThrusterCompositionMinVolume = ob.ThrusterCompositionMinVolume;
            this.ThrusterCompositionMinVolume_c = ob.ThrusterCompositionMinVolume / (1f - ob.ThrusterCompositionMinVolume);
            this.ThrusterCompositionChangeSpeed = ob.ThrusterCompositionChangeSpeed;
            this.SpeedDownSoundChangeVolumeTo = ob.SpeedDownSoundChangeVolumeTo;
            this.SpeedUpSoundChangeVolumeTo = ob.SpeedUpSoundChangeVolumeTo;
            this.SpeedUpDownChangeSpeed = ob.SpeedUpDownChangeSpeed * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            foreach (var sound in ob.Sounds)
            {
                if(sound.SoundName.Length == 0)
                    continue;
                MySoundPair soundPair = new MySoundPair(sound.SoundName);
                if (soundPair != MySoundPair.Empty)
                    this.Sounds.Add(sound.SoundType, soundPair);
            }

            List<MyTuple<float, float>> thrusterVolumesTemp = new List<MyTuple<float,float>>();
            foreach (var thrusterVolume in ob.ThrusterVolumes)
            {
                thrusterVolumesTemp.Add(new MyTuple<float, float>(Math.Max(0f, thrusterVolume.Speed), Math.Max(0f, thrusterVolume.Volume)));
            }
            this.ThrusterVolumes = thrusterVolumesTemp.OrderBy(o => o.Item1).ToList();

            List<MyTuple<float, float>> engineVolumesTemp = new List<MyTuple<float, float>>();
            foreach (var engineVolume in ob.EngineVolumes)
            {
                engineVolumesTemp.Add(new MyTuple<float, float>(Math.Max(0f, engineVolume.Speed), Math.Max(0f, engineVolume.Volume)));
            }
            this.EngineVolumes = engineVolumesTemp.OrderBy(o => o.Item1).ToList();

            List<MyTuple<float, float>> wheelsVolumesTemp = new List<MyTuple<float, float>>();
            foreach (var wheelsVolume in ob.WheelsVolumes)
            {
                wheelsVolumesTemp.Add(new MyTuple<float, float>(Math.Max(0f, wheelsVolume.Speed), Math.Max(0f, wheelsVolume.Volume)));
            }
            this.WheelsVolumes = wheelsVolumesTemp.OrderBy(o => o.Item1).ToList();
        }
    }
}