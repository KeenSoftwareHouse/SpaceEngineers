using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Audio;
using VRage.Data.Audio;
using VRage.Game;
using VRage.Game.Definitions;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AudioEffectDefinition))]
    public class MyAudioEffectDefinition : MyDefinitionBase
    {
        public MyAudioEffect Effect = new MyAudioEffect();
       
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_AudioEffectDefinition;
            Effect.EffectId = Id.SubtypeId;
            foreach( var soundsEffects in ob.Sounds)
            {
                var soundsEffects2 = new List<MyAudioEffect.SoundEffect>();
                foreach(var effect in soundsEffects.SoundEffects)
                {
                    var seff = new MyAudioEffect.SoundEffect();
                    MyCurveDefinition def;
                    if(!MyDefinitionManager.Static.TryGetDefinition<MyCurveDefinition>(new MyDefinitionId(typeof (MyObjectBuilder_CurveDefinition), effect.VolumeCurve), out def))
                    {
                        seff.VolumeCurve = null;
                    }
                    else
                        seff.VolumeCurve = def.Curve;
                    seff.Duration = effect.Duration;
                    seff.Filter = effect.Filter;
                    seff.Frequency = (float)(2 * Math.Sin(3.14 * effect.Frequency / 44100));
                    
                    seff.OneOverQ = 1 / effect.Q;
                    seff.StopAfter = effect.StopAfter;
                    soundsEffects2.Add(seff);
                }
                Effect.SoundsEffects.Add(soundsEffects2);
            }
            if (ob.OutputSound == 0)
                Effect.ResultEmitterIdx = Effect.SoundsEffects.Count - 1;
            else
                Effect.ResultEmitterIdx = ob.OutputSound - 1;
        }
    }
}
