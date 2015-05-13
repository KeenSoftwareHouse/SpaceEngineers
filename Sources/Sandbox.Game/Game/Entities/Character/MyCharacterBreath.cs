using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Data;
using VRage.Data.Audio;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Game.Entities.Character
{
    class MyCharacterBreath
    {
        public enum State
        {
            Calm,
            Heated,
            Dead,
        }
        static MySoundPair BREATH_CALM = new MySoundPair("PlayVocBreath1L");
        static MySoundPair BREATH_HEAVY = new MySoundPair("PlayVocBreath2L");
        IMySourceVoice m_sound;

        MyCharacter m_character;
        private MyTimeSpan m_lastChange;
        private State m_state;

        public MyCharacterBreath(MyCharacter character)
        {
            CurrentState = State.Calm;
            m_character = character;
        }

        public State CurrentState
        {
            private get
            {
                return m_state;
            }
            set
            {
                if (m_state != value)
                {
                    m_state = value;
                    m_lastChange = MySandboxGame.Static.UpdateTime + MyTimeSpan.FromSeconds(2);
                }
            }
        }

        public void Update()
        {
            if (!Sandbox.Game.World.MySession.Static.Settings.RealisticSound)
                return;
            if(CurrentState == State.Dead)
            {
                if (m_sound != null && m_sound.IsPlaying)
                    m_sound.Stop();
                return;
            }

            if (m_lastChange < MySandboxGame.Static.UpdateTime)
            {
                switch(CurrentState)
                {
                    case State.Calm:
                        if (m_sound == null || m_sound.CueEnum != BREATH_CALM.SoundId)
                        {
                            PlaySound(BREATH_CALM.SoundId);
                        }
                        break;
                    case State.Heated:
                        if (m_sound == null || m_sound.CueEnum != BREATH_HEAVY.SoundId)
                        {
                            PlaySound(BREATH_HEAVY.SoundId);
                        }
                        break;
                }
            }
        }

        private void PlaySound(MyStringId soundId)
        {
            if (m_sound != null && m_sound.IsPlaying)
            {
                var effect = MyAudio.Static.ApplyEffect(m_sound, MyStringId.GetOrCompute("CrossFade"), new MyStringId[] { soundId }, 2000);
                m_sound = effect.OutputSound;
            }
            else
                m_sound = MyAudio.Static.PlaySound(soundId, null, MySoundDimensions.D2);
        }

        public void Close()
        {
            if (m_sound != null)
                m_sound.Stop(true);
        }
    }
}
