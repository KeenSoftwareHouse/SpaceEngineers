using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Data.Audio;
using VRage.Utils;

namespace VRage.Audio
{
    class MyEffectBank
    {
        private Dictionary<MyStringHash, MyAudioEffect> m_effects = new Dictionary<MyStringHash, MyAudioEffect>(MyStringHash.Comparer);
        private List<MyEffectInstance> m_activeEffects = new List<MyEffectInstance>();
        XAudio2 m_engine;
        public MyEffectBank(ListReader<MyAudioEffect> effects, XAudio2 engine)
        {
            foreach (var effect in effects)
                m_effects[effect.EffectId] = effect;
            m_engine = engine;
        }

        public MyEffectInstance CreateEffect(IMySourceVoice input, MyStringHash effect, MySourceVoice[] cues = null, float? duration = null)
        {
            if(!m_effects.ContainsKey(effect))
            {
                Debug.Fail(string.Format("Effect not found: {0}", effect.ToString()));
                return null;
            }
            var instance = new MyEffectInstance(m_effects[effect], input, cues, duration, m_engine);
            m_activeEffects.Add(instance);
            return instance;
        }


        public void Update(int ms)
        {
            for (int i = m_activeEffects.Count - 1; i >= 0; i--)
            {
                if (m_activeEffects[i].Finished)
                {
                    m_activeEffects.RemoveAt(i);
                    continue;
                }
                if(m_activeEffects[i].AutoUpdate)
                    m_activeEffects[i].Update(ms);
            }
        }
    }
}
